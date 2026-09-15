// Content packages (0.4.80): a Workshop package with [content] in its manifest and no DLL -
// the Soviet Mod Loader convention (resources, deposits, needs, buildings as INI fragments plus
// assets under assets\media_soviet\...). RMM provides such a package by copying the fragments to
// user_config\.autoload\content\<id>\ and the assets into the loader's vfs; the keyed editors of
// the target plugins (Resources, Deposits Plus, Needs, Buildings Plus) merge every provided
// fragment into their effective INI as original entries. Removing the package takes the
// fragments and the copied assets away again, and the editors write their INIs without them.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TesmioAutoload
{
    public static class ContentTargets
    {
        public static readonly string[] Keys={"resources","deposits","needs","buildings"};
        // The plugins that read a fragment kind, in order of preference; the first one whose
        // effective INI exists in plugins\ takes the fragment.
        public static string[] PluginsFor(string key)
        {
            switch((key??"").ToLowerInvariant())
            {
                case "resources":return new[]{"resources"};
                case "deposits":return new[]{"deposits_plus","deposits"};
                case "needs":return new[]{"needs"};
                case "buildings":return new[]{"buildings_plus","buildings"};
            }
            return new string[0];
        }
        public static string KeyForPlugin(string plugin)
        {foreach(string key in Keys)if(PluginsFor(key).Any(x=>x.Equals(plugin,StringComparison.OrdinalIgnoreCase)))return key;return "";}
        public static string StateRoot(string build){return SafeFiles.Child(Path.GetFullPath(build),"user_config\\.autoload\\content");}
        public static string FolderName(string packageId)
        {
            if(Regex.IsMatch(packageId??"","^[A-Za-z0-9._-]{1,80}$"))return packageId;
            return "content_"+SafeFiles.Hash(SafeFiles.Utf8.GetBytes(packageId??"")).Substring(0,16);
        }
        public static string StateDir(string build,string packageId){return Path.Combine(StateRoot(build),FolderName(packageId));}
        // The ids a fragment declares: section names for deposits and buildings, [list] keys for
        // resources and needs. Reserved switch sections of the plugins are not entries.
        public static List<string> IdsOf(string key,string text)
        {
            var result=new List<string>();LooseIni doc;try{doc=new LooseIni(text??"");}catch(Exception){return result;}
            if(key=="deposits"||key=="buildings")
            {
                foreach(string section in doc.SectionNames()){string s=section.Trim();if(s.Length==0||s.Equals("deposits",StringComparison.OrdinalIgnoreCase)||s.Equals("deposits_plus",StringComparison.OrdinalIgnoreCase)||s.Equals("buildings",StringComparison.OrdinalIgnoreCase)||s.Equals("buildings_plus",StringComparison.OrdinalIgnoreCase)||s.Equals("list",StringComparison.OrdinalIgnoreCase))continue;result.Add(s);}
                return result;
            }
            foreach(var pair in doc.Entries("list"))result.Add(pair.Key);
            return result;
        }
    }

    // Added: the ids this fragment actually contributed the last time its editor wrote (from the
    // receipt); only those are ever stripped from an effective INI that becomes a new base.
    public sealed class ContentFragment{public string PackageId="",PackageName="",Key="",Text="";public readonly List<string> Added=new List<string>();}
    // What one package contributed to one editor: the ids taken over and the ids that were
    // already there (an existing entry always wins, whether original or personal).
    public sealed class ContentContribution{public string PackageId="",PackageName="";public readonly List<string> Added=new List<string>(),Skipped=new List<string>();}

    public static class ContentLayer
    {
        // The provided fragments for one target plugin, read from the receipts.
        public static List<ContentFragment> Fragments(string build,string plugin)
        {
            var result=new List<ContentFragment>();string key=ContentTargets.KeyForPlugin(plugin);if(key.Length==0)return result;
            string root=ContentTargets.StateRoot(build);if(!Directory.Exists(root))return result;
            foreach(string dir in Directory.GetDirectories(root).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase))
            {
                string receipt=Path.Combine(dir,"receipt.ini"),fragment=Path.Combine(dir,key+".ini");
                if(!File.Exists(receipt)||!File.Exists(fragment))continue;
                LooseIni r;try{r=new LooseIni(SafeFiles.Text(receipt));}catch(Exception){continue;}
                if((r.Get("content","provided")??"")!="1")continue;
                string target=r.Get("content","target."+key)??"";
                if(target.Length>0&&!target.Equals(plugin,StringComparison.OrdinalIgnoreCase))continue;
                var f=new ContentFragment{PackageId=r.Get("content","id")??Path.GetFileName(dir),PackageName=r.Get("content","name")??Path.GetFileName(dir),Key=key,Text=SafeFiles.Text(fragment)};
                f.Added.AddRange((r.Get("added",key)??"").Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0));
                result.Add(f);
            }
            return result;
        }
        static void CopySection(LooseIni from,LooseIni to,string section)
        {
            to.EnsureSection(section);
            foreach(string key in from.Entries(section).Select(x=>x.Key).Distinct(StringComparer.OrdinalIgnoreCase))to.SetAll(section,key,from.GetAll(section,key));
        }
        // baseText plus every fragment: sections or list entries that the base does not have yet
        // are appended; ids the base already knows are skipped and reported.
        public static string Merge(LocalEditorSpec spec,string baseText,List<ContentFragment> fragments,List<ContentContribution> contributions)
        {
            if(fragments==null||fragments.Count==0)return baseText;
            var doc=new LooseIni(baseText);
            foreach(ContentFragment fragment in fragments)
            {
                var c=new ContentContribution{PackageId=fragment.PackageId,PackageName=fragment.PackageName};
                LooseIni part;try{part=new LooseIni(fragment.Text);}catch(Exception){c.Skipped.Add("*");contributions.Add(c);continue;}
                if(spec.IsSections)
                {
                    foreach(string section in part.SectionNames())
                    {
                        string name=section.Trim();if(name.Length==0||spec.IsReserved(name))continue;
                        if(doc.SectionNames().Any(x=>x.Trim().Equals(name,StringComparison.OrdinalIgnoreCase))){c.Skipped.Add(name);continue;}
                        CopySection(part,doc,name);c.Added.Add(name);
                    }
                }
                else
                {
                    string list=spec.ListSection;
                    foreach(var pair in part.Entries(list))
                    {
                        if(doc.Get(list,pair.Key)!=null){c.Skipped.Add(pair.Key);continue;}
                        doc.Set(list,pair.Key,pair.Value);c.Added.Add(pair.Key);
                        if(spec.IsList)continue;
                        // Resources: the [custom:<id>] section and the per-resource keys of the
                        // other sections (prices) travel with the list entry.
                        foreach(string section in part.SectionNames())
                        {
                            string name=section.Trim();
                            if(name.Equals(list,StringComparison.OrdinalIgnoreCase))continue;
                            if(name.Equals(spec.ItemSectionPrefix+pair.Key,StringComparison.OrdinalIgnoreCase)){if(!doc.HasSection(name))CopySection(part,doc,name);continue;}
                            if(name.StartsWith(spec.ItemSectionPrefix,StringComparison.OrdinalIgnoreCase))continue;
                            string value=part.Get(name,pair.Key);
                            if(value!=null&&doc.Get(name,pair.Key)==null)doc.Set(name,pair.Key,value);
                        }
                    }
                }
                contributions.Add(c);
            }
            return doc.Render();
        }
        // The opposite of Merge, for an effective INI that is taken as a new base while fragments
        // are provided: the entries a fragment contributed came from RMM and must not become part
        // of the base. Only the ids recorded as added are taken out - an entry that was there before
        // the package (and made the package's own copy skip) is the user's and stays.
        public static string Strip(LocalEditorSpec spec,string text,List<ContentFragment> fragments)
        {
            if(fragments==null||fragments.Count==0)return text;
            var doc=new LooseIni(text);
            foreach(ContentFragment fragment in fragments)
            {
                if(spec.IsSections)
                {
                    foreach(string id in fragment.Added){if(spec.IsReserved(id))continue;foreach(string present in doc.SectionNames().Where(x=>x.Trim().Equals(id,StringComparison.OrdinalIgnoreCase)).ToList())doc.RemoveSection(present);}
                    continue;
                }
                string list=spec.ListSection;
                foreach(string id in fragment.Added)
                {
                    if(doc.Get(list,id)==null)continue;
                    doc.Remove(list,id);
                    if(spec.IsList)continue;
                    foreach(string section in doc.SectionNames().ToList())
                    {
                        string name=section.Trim();
                        if(name.Equals(spec.ItemSectionPrefix+id,StringComparison.OrdinalIgnoreCase))doc.RemoveSection(section);
                        else if(!name.Equals(list,StringComparison.OrdinalIgnoreCase)&&!name.StartsWith(spec.ItemSectionPrefix,StringComparison.OrdinalIgnoreCase)&&doc.Get(name,id)!=null)doc.Remove(name,id);
                    }
                }
            }
            return doc.Render();
        }
    }

    // One content package against one loader folder: its receipt, what it would provide, and the
    // switch. Commit writes fragments, assets and receipt in one transaction and then lets every
    // target editor write its effective INI again.
    public sealed class ContentSession
    {
        public readonly Package Package;public readonly string Build,StateDir,ReceiptPath;
        public bool Provided,PendingOn;public string ProvidedVersion="",ProvidedHash="",CurrentHash="";
        // fragment key -> plugin that takes it ("" = no such plugin has an INI in plugins\)
        public readonly Dictionary<string,string> Targets=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        public readonly List<string> CopiedAssets=new List<string>();
        public readonly Dictionary<string,List<string>> Skipped=new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase),Added=new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase);
        public readonly List<string> Notes=new List<string>();
        readonly Dictionary<string,string> Before=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        public ContentSession(Package package,string build)
        {
            if(package==null||package.Kind!="content")throw new InvalidOperationException(Msg.Key("err_kein_inhaltspaket"));
            Package=package;Build=Path.GetFullPath(build);StateDir=ContentTargets.StateDir(Build,package.Id);ReceiptPath=Path.Combine(StateDir,"receipt.ini");
            CurrentHash=Hash(package);
            ReadReceipt();PendingOn=Provided;
            foreach(string key in ContentTargets.Keys)
            {
                if(!package.ContentFragments.ContainsKey(key))continue;
                // 0.5.3: under SML its own embedded component is the target - it writes
                // plugins\buildings.ini, so naming buildings_plus here would be wrong.
                string found=Sml.Hosts(Build,key)?key:"";
                if(found.Length==0)foreach(string plugin in ContentTargets.PluginsFor(key))if(File.Exists(SafeFiles.Child(Build,"plugins\\"+plugin+".ini"))){found=plugin;break;}
                Targets[key]=found;
                if(found.Length==0)Notes.Add(Msg.Key("content_missing_target",key,String.Join(", ",ContentTargets.PluginsFor(key))));
            }
            Watch();
        }
        void Watch()
        {
            Before.Clear();Before[ReceiptPath]=SafeFiles.HashFile(ReceiptPath);
            foreach(string key in ContentTargets.Keys){string path=Path.Combine(StateDir,key+".ini");Before[path]=SafeFiles.HashFile(path);}
            string vfs=LocalEditorSpec.VfsRoot(Build);
            foreach(string relative in CopiedAssets.Concat(Package.AssetFiles).Distinct(StringComparer.OrdinalIgnoreCase)){string path=SafeFiles.Child(vfs,relative);Before[path]=SafeFiles.HashFile(path);}
        }
        void ReadReceipt()
        {
            Provided=false;ProvidedVersion="";ProvidedHash="";CopiedAssets.Clear();Skipped.Clear();Added.Clear();
            if(!File.Exists(ReceiptPath))return;
            LooseIni r;try{r=new LooseIni(SafeFiles.Text(ReceiptPath));}catch(Exception){return;}
            Provided=(r.Get("content","provided")??"")=="1";ProvidedVersion=r.Get("content","version")??"";ProvidedHash=r.Get("content","hash")??"";
            foreach(var pair in r.Entries("assets"))CopiedAssets.Add(pair.Value);
            foreach(var pair in r.Entries("skipped"))Skipped[pair.Key]=pair.Value.Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0).ToList();
            foreach(var pair in r.Entries("added"))Added[pair.Key]=pair.Value.Split('|').Select(x=>x.Trim()).Where(x=>x.Length>0).ToList();
        }
        // 0.5.3: Soviet Mod Loader merges a package with [content] itself and keeps no receipt of
        // ours. The honest question is not who wrote the entries but whether they stand in the
        // effective INIs of the target plugins - so that is what is asked, id by id. Under SML the
        // target is its own embedded component (buildings, not buildings_plus), because that is
        // the file it writes.
        public static bool MergedByLoader(string build,Package package)
        {
            if(package==null||package.Kind!="content"||!Sml.Active(build))return false;
            try
            {
                bool any=false;
                foreach(var pair in package.ContentFragments)
                {
                    var wanted=ContentTargets.IdsOf(pair.Key,pair.Value);if(wanted.Count==0)continue;
                    string plugin=Sml.Hosts(build,pair.Key)?pair.Key:"";
                    if(plugin.Length==0)foreach(string candidate in ContentTargets.PluginsFor(pair.Key))if(File.Exists(SafeFiles.Child(build,"plugins\\"+candidate+".ini"))){plugin=candidate;break;}
                    if(plugin.Length==0)return false;
                    string target=SafeFiles.Child(build,"plugins\\"+plugin+".ini");
                    if(!File.Exists(target))return false;
                    var present=new HashSet<string>(ContentTargets.IdsOf(pair.Key,SafeFiles.Text(target)),StringComparer.OrdinalIgnoreCase);
                    foreach(string id in wanted)if(!present.Contains(id))return false;
                    any=true;
                }
                return any;
            }
            catch(Exception){return false;}
        }
        public static bool IsProvided(string build,string packageId)
        {
            try{string receipt=Path.Combine(ContentTargets.StateDir(build,packageId),"receipt.ini");return File.Exists(receipt)&&(new LooseIni(SafeFiles.Text(receipt)).Get("content","provided")??"")=="1";}
            catch(Exception){return false;}
        }
        // Fragment texts plus the assets' names, sizes and write times: enough to notice a
        // Workshop update without hashing every mesh at each start.
        public static string Hash(Package package)
        {
            var text=new StringBuilder();
            foreach(string key in ContentTargets.Keys){string fragment;if(package.ContentFragments.TryGetValue(key,out fragment))text.Append(key).Append('\n').Append(fragment).Append('\n');}
            foreach(string relative in package.AssetFiles){var info=new FileInfo(Path.Combine(package.AssetsDir,relative));text.Append(relative.ToLowerInvariant()).Append('|').Append(info.Length).Append('|').Append(info.LastWriteTimeUtc.Ticks).Append('\n');}
            return SafeFiles.Hash(SafeFiles.Utf8.GetBytes(text.ToString()));
        }
        public bool UpdatePending{get{return Provided&&ProvidedHash!=CurrentHash;}}
        public bool NeedsWrite{get{return PendingOn!=Provided||(PendingOn&&UpdatePending);}}
        public void AssertUnchanged(){foreach(var pair in Before)if(SafeFiles.HashFile(pair.Key)!=pair.Value)throw new IOException(Msg.Key("err_datei_inzwischen_geaendert_neu",pair.Key));}
        // Deposit types must be unique among everything the deposits plugin reads: the fragment
        // keeps its own numbers when they are free, a taken or missing one becomes the next free
        // number (10..127), and a number once assigned stays with its section across updates.
        string AssignDepositTypes(string text,string plugin)
        {
            LooseIni doc;try{doc=new LooseIni(text);}catch(Exception){return text;}
            var taken=new HashSet<int>();var own=new HashSet<string>(doc.SectionNames().Select(x=>x.Trim()),StringComparer.OrdinalIgnoreCase);
            string effective=SafeFiles.Child(Build,"plugins\\"+plugin+".ini");
            // The effective INI may already carry this package's sections from the last time; their
            // numbers are not "taken by somebody else".
            if(File.Exists(effective))try{var e=new LooseIni(SafeFiles.Text(effective));foreach(string s in e.SectionNames()){if(own.Contains(s.Trim()))continue;int t;if(Int32.TryParse((e.Get(s,"type")??"").Trim(),out t))taken.Add(t);}}catch(Exception){}
            foreach(ContentFragment other in ContentLayer.Fragments(Build,plugin))
            {
                if(other.PackageId.Equals(Package.Id,StringComparison.OrdinalIgnoreCase))continue;
                try{var o=new LooseIni(other.Text);foreach(string s in o.SectionNames()){int t;if(Int32.TryParse((o.Get(s,"type")??"").Trim(),out t))taken.Add(t);}}catch(Exception){}
            }
            var previous=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
            string stored=Path.Combine(StateDir,"deposits.ini");
            if(File.Exists(stored))try{var p=new LooseIni(SafeFiles.Text(stored));foreach(string s in p.SectionNames()){int t;if(Int32.TryParse((p.Get(s,"type")??"").Trim(),out t))previous[s.Trim()]=t;}}catch(Exception){}
            foreach(string section in doc.SectionNames())
            {
                string name=section.Trim();if(name.Length==0||name.Equals("deposits",StringComparison.OrdinalIgnoreCase)||name.Equals("deposits_plus",StringComparison.OrdinalIgnoreCase))continue;
                int want;if(!previous.TryGetValue(name,out want)&&!Int32.TryParse((doc.Get(section,"type")??"").Trim(),out want))want=0;
                if(want<10||want>127||taken.Contains(want)){want=10;while(want<=127&&taken.Contains(want))want++;if(want>127)throw new FormatException(Msg.Key("err_keine_freie_vorkommensnummer",name));}
                taken.Add(want);doc.Set(section,"type",want.ToString());
                // The map channel is the plugin's business, never the package's.
                doc.Remove(section,"component");doc.Set(section,"map","auto");
            }
            return doc.Render();
        }
        // 0.4.87: ids of this package that a target plugin already knows from somewhere else -
        // the player's own entry, the plugin's shipped INI, another package. Those never lose:
        // an id that is in the way stops the whole package from being provided, so nothing is
        // written and no file lands in the vfs. The player removes his own entry and switches
        // the package on again. Ids this package provided itself (the receipt knows them) are
        // not conflicts, otherwise a package could never be saved twice.
        public Dictionary<string,List<string>> Conflicts(Func<string,LocalResourceSession> sessionFor)
        {
            var result=new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase);
            if(sessionFor==null)return result;
            foreach(var pair in Package.ContentFragments)
            {
                string plugin;if(!Targets.TryGetValue(pair.Key,out plugin)||plugin.Length==0)continue;
                LocalResourceSession session;try{session=sessionFor(plugin);}catch(Exception){continue;}
                if(session==null)continue;
                List<string> mine;Added.TryGetValue(pair.Key,out mine);
                var known=new HashSet<string>(session.Items().Select(x=>x.Id),StringComparer.OrdinalIgnoreCase);
                var clash=ContentTargets.IdsOf(pair.Key,pair.Value)
                    .Where(id=>known.Contains(id)&&(mine==null||!mine.Contains(id,StringComparer.OrdinalIgnoreCase)))
                    .ToList();
                if(clash.Count>0)result[pair.Key]=clash;
            }
            return result;
        }
        // Writes the switch state. sessionFor(plugin) opens the keyed editor of a target plugin,
        // or returns null when it is not available; every editor that gets a session writes its
        // effective INI once more, with or without this package's entries.
        public string Commit(Action guard,Func<string,LocalResourceSession> sessionFor)
        {
            guard();AssertUnchanged();
            if(PendingOn)
            {
                foreach(var clash in Conflicts(sessionFor))
                {
                    string plugin;Targets.TryGetValue(clash.Key,out plugin);
                    throw new RuleException("content_conflict",Package.Name,String.Join(", ",clash.Value),plugin??"",clash.Key);
                }
            }
            bool on=PendingOn;var writes=new Dictionary<string,byte[]>(StringComparer.OrdinalIgnoreCase);
            string vfs=LocalEditorSpec.VfsRoot(Build);var assets=new List<string>();
            foreach(string old in CopiedAssets)writes[SafeFiles.Child(vfs,old)]=null;
            foreach(string key in ContentTargets.Keys)writes[Path.Combine(StateDir,key+".ini")]=null;
            var skippedAssets=new List<string>();
            if(on)
            {
                foreach(var pair in Package.ContentFragments)
                {
                    string text=pair.Value;string plugin;
                    if(pair.Key=="deposits"&&Targets.TryGetValue("deposits",out plugin)&&plugin.Length>0)text=AssignDepositTypes(text,plugin);
                    writes[Path.Combine(StateDir,pair.Key+".ini")]=SafeFiles.Utf8.GetBytes(text);
                }
                // A file that was already in the vfs before this package is somebody else's - it is
                // neither overwritten nor, later, removed. Only files this package created are ours.
                foreach(string relative in Package.AssetFiles)
                {
                    string target=SafeFiles.Child(vfs,relative);
                    bool ours=CopiedAssets.Contains(relative,StringComparer.OrdinalIgnoreCase);
                    if(!ours&&File.Exists(target)){skippedAssets.Add(relative);writes.Remove(target);continue;}
                    writes[target]=SafeFiles.Read(Path.Combine(Package.AssetsDir,relative),64*1024*1024);assets.Add(relative);
                }
            }
            // The receipt keeps the ids added last time until the editors have written again, so an
            // editor that has to take its effective INI as a new base strips exactly those.
            writes[ReceiptPath]=SafeFiles.Utf8.GetBytes(RenderReceipt(on,assets,Added,Skipped));
            foreach(string path in writes.Keys.ToList())if(!Before.ContainsKey(path))Before[path]=SafeFiles.HashFile(path);
            string backup=Transaction.Apply(Build,"content_"+ContentTargets.FolderName(Package.Id),writes,Before,guard);
            // The editors read the fragments from disk, so they are opened only now.
            Dictionary<string,List<string>> added=new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase),skipped=new Dictionary<string,List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach(string key in ContentTargets.Keys)
            {
                string plugin;if(!Targets.TryGetValue(key,out plugin)||plugin.Length==0)continue;
                LocalResourceSession session=sessionFor(plugin);if(session==null)continue;
                session.Commit(guard);
                if(on)foreach(ContentContribution c in session.Content)if(c.PackageId.Equals(Package.Id,StringComparison.OrdinalIgnoreCase)){if(c.Added.Count>0)added[key]=c.Added;if(c.Skipped.Count>0)skipped[key]=c.Skipped;}
            }
            if(on&&skippedAssets.Count>0)skipped["assets"]=skippedAssets;
            Directory.CreateDirectory(StateDir);File.WriteAllText(ReceiptPath,RenderReceipt(on,assets,added,skipped),SafeFiles.Utf8);
            ReadReceipt();PendingOn=Provided;Watch();
            return backup;
        }
        string RenderReceipt(bool on,List<string> assets,Dictionary<string,List<string>> added,Dictionary<string,List<string>> skipped)
        {
            var text=new StringBuilder();
            text.Append("; Written by Republic Mod Manager: what this content package currently provides.\r\n[content]\r\nid = ").Append(Package.Id).Append("\r\nname = ").Append(Package.Name).Append("\r\nprovided = ").Append(on?"1":"0").Append("\r\nversion = ").Append(Package.Version).Append("\r\nhash = ").Append(CurrentHash).Append("\r\n");
            foreach(var pair in Targets)if(pair.Value.Length>0)text.Append("target.").Append(pair.Key).Append(" = ").Append(pair.Value).Append("\r\n");
            text.Append("\r\n[assets]\r\n");int n=0;if(on)foreach(string relative in assets)text.Append(n++).Append(" = ").Append(relative).Append("\r\n");
            text.Append("\r\n[added]\r\n");if(on)foreach(var pair in added)if(pair.Value.Count>0)text.Append(pair.Key).Append(" = ").Append(String.Join(" | ",pair.Value)).Append("\r\n");
            text.Append("\r\n[skipped]\r\n");if(on)foreach(var pair in skipped)if(pair.Value.Count>0)text.Append(pair.Key).Append(" = ").Append(String.Join(" | ",pair.Value)).Append("\r\n");
            return text.ToString();
        }
    }
}
