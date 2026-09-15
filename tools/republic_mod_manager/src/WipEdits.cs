using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TesmioAutoload
{
    // 0.5.5 step 3: personal changes to the building.ini of a generated Workshop folder.
    //
    // The file itself is output - the generator rewrites it whenever the declaration or the
    // donor changes, and a copy kept here would throw the package's own improvements away on
    // the next update. So what is stored is the CHANGES (replace / remove / add), the way
    // Vanilla Buildings stores its rule sets, plus a verbatim copy of the generator's output as
    // the baseline they are applied to. After a regeneration the new output becomes the
    // baseline and the same changes are applied to it; an anchor that no longer matches is
    // reported instead of quietly doing nothing.
    public sealed class WipOperation
    {
        public string Kind = "replace";   // replace | remove | add
        public string Anchor = "", Value = "";
        // 0.5.6: for the eye only. The game's building.ini aligns its values with runs of spaces,
        // and a replacement shows the line twice - squeezed into one row that is unreadable. The
        // stored Anchor and Value keep every space: the anchor has to match the generated line
        // exactly, or the change would no longer find its place after a regeneration.
        public static string Squeeze(string line)
        {
            return String.IsNullOrEmpty(line) ? "" : Regex.Replace(line.Trim(), "[ \t]{2,}", " ");
        }
        public override string ToString()
        {
            return Kind == "add" ? "+ " + Squeeze(Value)
                 : Kind == "remove" ? "- " + Squeeze(Anchor)
                 : "~ " + Squeeze(Anchor) + "  ->  " + Squeeze(Value);
        }
    }

    public sealed class WipEdit
    {
        public string Id = "", Object = "", Section = "", StampHash = "", ResultHash = "";
        public string Baseline = "";
        public readonly List<WipOperation> Operations = new List<WipOperation>();
        public bool Exists;
    }

    public static class WipEdits
    {
        // Temporary name first, then into place: a crash never leaves half a building.ini behind.
        static void WriteAtomic(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".rmm.tmp";
            File.WriteAllText(temp, text, SafeFiles.Utf8);
            if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
        }
        public static string Folder(string build) { return Path.Combine(Path.GetFullPath(build), "user_config", ".autoload", "wip"); }
        static string Receipt(string build, string id) { return Path.Combine(Folder(build), id + ".receipt.ini"); }
        static string BaselineFile(string build, string id) { return Path.Combine(Folder(build), id + ".baseline.ini"); }

        // The generated file of one building: <folder>\<object>\building.ini.
        public static string BuildingFile(WipBuilding entry)
        {
            if (entry == null || entry.Object.Length == 0) return null;
            string path = Path.Combine(entry.Folder, entry.Object, "building.ini");
            return File.Exists(path) ? path : null;
        }
        // The hash out of the generator's stamp. It covers the declaration, the generator version
        // and the donor's files - when it changes, the folder was written anew.
        public static string StampHash(WipBuilding entry)
        {
            try
            {
                string stamp = Path.Combine(entry.Folder, "tesmioloader.stamp");
                if (!File.Exists(stamp)) return "";
                foreach (string line in SafeFiles.Text(stamp).Replace("\r\n", "\n").Split('\n'))
                    if (line.StartsWith("hash=", StringComparison.OrdinalIgnoreCase)) return line.Substring(5).Trim();
            }
            catch (Exception) { }
            return "";
        }

        public static WipEdit Load(string build, string id)
        {
            var edit = new WipEdit { Id = id };
            string receipt = Receipt(build, id);
            if (!File.Exists(receipt)) return edit;
            try
            {
                var ini = new LooseIni(SafeFiles.Text(receipt));
                edit.Object = ini.Get("wip", "object") ?? "";
                edit.Section = ini.Get("wip", "section") ?? "";
                edit.StampHash = ini.Get("wip", "stamp_hash") ?? "";
                edit.ResultHash = ini.Get("wip", "result_hash") ?? "";
                foreach (string value in ini.GetAll("operations", "replace"))
                {
                    int bar = value.IndexOf('|');
                    if (bar > 0) edit.Operations.Add(new WipOperation { Kind = "replace", Anchor = value.Substring(0, bar).Trim(), Value = value.Substring(bar + 1).Trim() });
                }
                foreach (string value in ini.GetAll("operations", "remove"))
                    if (value.Trim().Length > 0) edit.Operations.Add(new WipOperation { Kind = "remove", Anchor = value.Trim() });
                foreach (string value in ini.GetAll("operations", "add"))
                    if (value.Trim().Length > 0) edit.Operations.Add(new WipOperation { Kind = "add", Value = value.Trim() });
                string baseline = BaselineFile(build, id);
                if (File.Exists(baseline)) edit.Baseline = SafeFiles.Text(baseline);
                edit.Exists = true;
            }
            catch (Exception) { return new WipEdit { Id = id }; }
            return edit;
        }

        public static void Save(string build, WipEdit edit)
        {
            Directory.CreateDirectory(Folder(build));
            var text = new StringBuilder();
            text.Append("; written by Republic Mod Manager - personal changes to a generated building.ini.\r\n");
            text.Append("; The building.ini itself belongs to the generator; these lines are applied to it again\r\n");
            text.Append("; after every regeneration. Delete this file and the building goes back to what the\r\n");
            text.Append("; generator writes (the copy beside it, <id>.baseline.ini, is that state).\r\n\r\n");
            text.Append("[wip]\r\n");
            text.Append("id = " + edit.Id + "\r\n");
            text.Append("object = " + edit.Object + "\r\n");
            if (edit.Section.Length > 0) text.Append("section = " + edit.Section + "\r\n");
            text.Append("stamp_hash = " + edit.StampHash + "\r\n");
            text.Append("result_hash = " + edit.ResultHash + "\r\n\r\n");
            text.Append("[operations]\r\n");
            foreach (WipOperation op in edit.Operations)
            {
                if (op.Kind == "replace") text.Append("replace = " + op.Anchor + " | " + op.Value + "\r\n");
                else if (op.Kind == "remove") text.Append("remove = " + op.Anchor + "\r\n");
                else text.Append("add = " + op.Value + "\r\n");
            }
            WriteAtomic(Receipt(build, edit.Id), text.ToString());
            WriteAtomic(BaselineFile(build, edit.Id), edit.Baseline);
        }

        public static void Forget(string build, string id)
        {
            foreach (string path in new[] { Receipt(build, id), BaselineFile(build, id) })
                try { if (File.Exists(path)) File.Delete(path); } catch (Exception) { }
        }

        // The changes applied to a baseline. Every anchor has to match exactly one line, the same
        // rule Vanilla Buildings uses; anything else lands in "problems" and the change is skipped,
        // so a package update that moved a line is reported and never silently ignored.
        public static List<string> Apply(IEnumerable<string> baseline, IEnumerable<WipOperation> operations, out List<string> problems)
        {
            var lines = baseline.ToList();
            problems = new List<string>();
            foreach (WipOperation op in operations)
            {
                if (op.Kind == "add") { lines.Add(op.Value); continue; }
                var hits = new List<int>();
                for (int i = 0; i < lines.Count; i++) if (lines[i].Trim() == op.Anchor.Trim()) hits.Add(i);
                if (hits.Count != 1) { problems.Add(op.Anchor); continue; }
                if (op.Kind == "remove") lines.RemoveAt(hits[0]);
                else lines[hits[0]] = op.Value;
            }
            return lines;
        }

        public static List<string> Split(string text)
        {
            return (text ?? "").Replace("\r\n", "\n").TrimEnd('\n').Split('\n').ToList();
        }
        public static string Join(IEnumerable<string> lines) { return String.Join("\r\n", lines) + "\r\n"; }

        // Writes the result of the changes into the building.ini and records what was written, so
        // a later hand edit outside RMM can be told apart from our own file.
        public static List<string> Write(string build, WipBuilding entry, WipEdit edit)
        {
            string file = BuildingFile(entry);
            if (file == null) throw new IOException(Msg.Key("err_wip_keine_building_ini", entry.Id));
            List<string> problems;
            string result = Join(Apply(Split(edit.Baseline), edit.Operations, out problems));
            WriteAtomic(file, result);
            edit.Object = entry.Object; edit.Section = entry.Section;
            edit.StampHash = StampHash(entry);
            edit.ResultHash = SafeFiles.Hash(Encoding.UTF8.GetBytes(result));
            Save(build, edit);
            return problems;
        }

        public enum Sync { None, InSync, Regenerated, Foreign }
        // What happened to the file since RMM last wrote it:
        //   None         no personal changes recorded
        //   InSync       the file is exactly what we wrote
        //   Regenerated  the generator rewrote the folder - the changes have to go in again
        //   Foreign      same stamp, different file: somebody edited it outside RMM
        public static Sync State(string build, WipBuilding entry, WipEdit edit)
        {
            if (edit == null || !edit.Exists) return Sync.None;
            if (StampHash(entry) != edit.StampHash) return Sync.Regenerated;
            string file = BuildingFile(entry);
            if (file == null) return Sync.Regenerated;
            try { return SafeFiles.Hash(Encoding.UTF8.GetBytes(SafeFiles.Text(file))) == edit.ResultHash ? Sync.InSync : Sync.Foreign; }
            catch (Exception) { return Sync.Foreign; }
        }

        // After a regeneration: the generator's fresh output becomes the new baseline and the
        // recorded changes are applied to it. That is the whole point of storing changes instead
        // of a copy - what the package improved in the meantime stays.
        public static List<string> Reapply(string build, WipBuilding entry, WipEdit edit)
        {
            string file = BuildingFile(entry);
            if (file == null) throw new IOException(Msg.Key("err_wip_keine_building_ini", entry.Id));
            edit.Baseline = SafeFiles.Text(file);
            return Write(build, entry, edit);
        }

        // The ids RMM has recorded changes for. Used by the reset, which has to put those
        // building.ini files back before it throws the receipts away.
        public static List<string> Recorded(string build)
        {
            var ids = new List<string>();
            try
            {
                string folder = Folder(build);
                if (!Directory.Exists(folder)) return ids;
                foreach (string file in Directory.GetFiles(folder, "*.receipt.ini"))
                {
                    string name = Path.GetFileName(file);
                    ids.Add(name.Substring(0, name.Length - ".receipt.ini".Length));
                }
            }
            catch (Exception) { }
            return ids;
        }
        // Revert without a scan in hand: the folder is looked up by id.
        public static bool RevertById(string build, string id)
        {
            WipEdit edit = Load(build, id);
            if (!edit.Exists) return false;
            string root = WipBuildings.Root(build);
            string dir = root == null ? null : Path.Combine(root, id);
            if (dir != null && Directory.Exists(dir)) Revert(build, WipBuildings.Read(dir), edit);
            else Forget(build, id);
            return true;
        }
        // Back to what the generator wrote, and the receipt goes with it.
        public static void Revert(string build, WipBuilding entry, WipEdit edit)
        {
            string file = BuildingFile(entry);
            if (file != null && edit.Baseline.Length > 0) WriteAtomic(file, edit.Baseline);
            Forget(build, entry.Id);
        }
    }
}
