using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TesmioAutoload
{
    // 0.4.88: what the next game start will do. One step per plugin the game will load, in the
    // order it will happen, plus the dependencies that come too late in that order - the trap
    // behind "my translation key is not resolved": the Workshop Bridge hands the packages to the
    // loader in folder-name order, and a service only exists once its provider's init has run.
    public sealed class LoadStep
    {
        public string Name="",Target="",Key="";
        public bool Bridge;                 // loaded by the Workshop Bridge, not from plugins\
        public int Index;
    }

    public static class Startup
    {
        // The loader walks [plugins] of tesmioloader.ini in file order and loads what is not 0;
        // the bridge then adds the Workshop packages of its allowlist, in folder-name order.
        public static List<LoadStep> Order(string build,IEnumerable<CatalogEntry> entries)
        {
            var list=new List<LoadStep>();
            var known=(entries??new CatalogEntry[0]).ToList();
            string loaderIni=SafeFiles.Child(build,"tesmioloader.ini");
            if(File.Exists(loaderIni))
            {
                try
                {
                    foreach(var pair in new LooseIni(SafeFiles.Text(loaderIni)).Entries("plugins"))
                    {
                        if((pair.Value??"").Trim()=="0")continue;
                        string target=(pair.Key??"").Trim();
                        if(target.Length==0)continue;
                        // 0.5.4: a key whose DLL is gone loads nothing. tesmioloader.ini keeps such
                        // leftovers for years (accumulator, easystart), and counting them as loaded
                        // made the overview claim plugins that are not even installed.
                        if(!File.Exists(SafeFiles.Child(build,"plugins\\"+target+".dll")))continue;
                        CatalogEntry entry=known.FirstOrDefault(e=>e.Target.Equals(target,StringComparison.OrdinalIgnoreCase)&&!e.Installed)
                            ??known.FirstOrDefault(e=>e.Target.Equals(target,StringComparison.OrdinalIgnoreCase));
                        list.Add(new LoadStep{Target=target,Key=target,Name=entry!=null?entry.Name:target});
                    }
                }
                catch(Exception){}
            }
            // 0.5.4: with SML the bridge idles and SML hands the Workshop packages to the loader
            // itself - every subscribed package, not an allowlist. Its order is dependencies first,
            // then folder name; that is what this reproduces. Without it the window listed the
            // plugins\ DLLs only and claimed the ten packages would not load at all.
            if(Sml.Active(build))
            {
                // 0.5.9: SML sorts by [mod] priority first and only then by the folder - a package
                // that declares one would otherwise stand in the wrong place here.
                var packages=known.Where(e=>!e.Installed&&!e.LocalEditor&&e.Supported&&e.Problem.Length==0&&e.Kind!="content"&&e.Target.Length>0)
                    .OrderBy(e=>Priority(e)).ThenBy(e=>Path.GetFileName((e.Root??"").TrimEnd('\\')),StringComparer.OrdinalIgnoreCase).ToList();
                var placed=new List<CatalogEntry>();
                foreach(CatalogEntry entry in packages)Place(placed,packages,entry,0);
                foreach(CatalogEntry entry in placed)
                    list.Add(new LoadStep{Key=Path.GetFileName((entry.Root??"").TrimEnd('\\')),Bridge=true,Target=entry.Target,Name=entry.Name});
                for(int i=0;i<list.Count;i++)list[i].Index=i;
                return list;
            }
            bool bridge=list.Any(x=>x.Target.Equals("workshop_bridge",StringComparison.OrdinalIgnoreCase));
            if(bridge)
            {
                foreach(string key in Bridge.PackageKeys(build).Where(k=>Bridge.Listed(build,k)).OrderBy(k=>k,StringComparer.OrdinalIgnoreCase))
                {
                    CatalogEntry entry=known.FirstOrDefault(e=>Path.GetFileName((e.Root??"").TrimEnd('\\')).Equals(key,StringComparison.OrdinalIgnoreCase));
                    if(entry!=null&&entry.Kind=="content")continue;      // content packages carry no DLL
                    list.Add(new LoadStep{Key=key,Bridge=true,Target=entry!=null?entry.Target:key,Name=entry!=null?entry.Name:key});
                }
            }
            for(int i=0;i<list.Count;i++)list[i].Index=i;
            return list;
        }

        // [mod] priority of a package, read straight from its manifest; anything missing or
        // unreadable counts as 0, which is what Soviet Mod Loader assumes too.
        static int Priority(CatalogEntry entry)
        {
            try
            {
                string manifest=Path.Combine(entry.Root??"","soviet.mod.ini");
                if(!File.Exists(manifest))return 0;
                int value;
                return Int32.TryParse(new LooseIni(SafeFiles.Text(manifest)).Get("mod","priority").Trim(),out value)?value:0;
            }
            catch(Exception){return 0;}
        }

        // Puts an entry after everything it declares a dependency on, the way SML resolves its
        // order. The depth guard keeps a circular declaration from looping forever.
        static void Place(List<CatalogEntry> placed,List<CatalogEntry> all,CatalogEntry entry,int depth)
        {
            if(entry==null||placed.Contains(entry))return;
            if(depth<16)
                foreach(Dependency dependency in entry.Dependencies)
                {
                    CatalogEntry provider=all.FirstOrDefault(e=>e!=entry&&(e.Id.Equals(dependency.Id,StringComparison.OrdinalIgnoreCase)
                        ||e.Target.Equals(LastPart(dependency.Id),StringComparison.OrdinalIgnoreCase)));
                    if(provider!=null)Place(placed,all,provider,depth+1);
                }
            if(!placed.Contains(entry))placed.Add(entry);
        }
        static string LastPart(string id)
        { int dot=(id??"").LastIndexOf('.'); return dot<0?(id??""):id.Substring(dot+1); }

        // Dependencies that load after the plugin that needs them. Returns pairs (needs, provider)
        // as ids, so the window can name both without knowing how they were found.
        public static List<KeyValuePair<string,string>> LateDependencies(List<LoadStep> steps,IEnumerable<CatalogEntry> entries)
        {
            var late=new List<KeyValuePair<string,string>>();
            if(steps==null||steps.Count==0)return late;
            var known=(entries??new CatalogEntry[0]).ToList();
            foreach(CatalogEntry entry in known)
            {
                LoadStep mine=StepOf(steps,entry);
                if(mine==null)continue;
                foreach(Dependency dependency in entry.Dependencies)
                {
                    string tail=Tail(dependency.Id);
                    LoadStep other=steps.FirstOrDefault(s=>s.Target.Equals(tail,StringComparison.OrdinalIgnoreCase)||Tail(s.Key).Equals(tail,StringComparison.OrdinalIgnoreCase))
                        ??StepOf(steps,known.FirstOrDefault(e=>e.Id.Equals(dependency.Id,StringComparison.OrdinalIgnoreCase)));
                    if(other==null||other.Index<mine.Index)continue;
                    late.Add(new KeyValuePair<string,string>(entry.Name,other.Name));
                }
            }
            return late;
        }
        static LoadStep StepOf(List<LoadStep> steps,CatalogEntry entry)
        {
            if(entry==null)return null;
            string folder=Path.GetFileName((entry.Root??"").TrimEnd('\\'));
            return steps.FirstOrDefault(s=>s.Bridge&&s.Key.Equals(folder,StringComparison.OrdinalIgnoreCase))
                ??steps.FirstOrDefault(s=>!s.Bridge&&entry.Target.Length>0&&s.Target.Equals(entry.Target,StringComparison.OrdinalIgnoreCase));
        }
        static string Tail(string id)
        {
            if(String.IsNullOrEmpty(id))return "";
            int dot=id.LastIndexOf('.');
            return dot<0?id:id.Substring(dot+1);
        }

        // 0.4.88: the history line of a version out of a package README - what a yellow "Update"
        // mark does not say today. The READMEs list their versions as "- **0.1.4:** ...".
        public static string Changes(string packageRoot,string version,string languageCode)
        {
            if(String.IsNullOrEmpty(packageRoot)||String.IsNullOrEmpty(version))return "";
            string[] names=languageCode=="de"?new[]{"README_DE.md","README_EN.md"}:new[]{"README_EN.md","README_DE.md"};
            foreach(string name in names)
            {
                string path;
                try{path=SafeFiles.Child(packageRoot,name);}catch(Exception){continue;}
                if(!File.Exists(path))continue;
                try
                {
                    foreach(string raw in SafeFiles.Text(path).Replace("\r\n","\n").Split('\n'))
                    {
                        string line=raw.Trim();
                        if(!line.StartsWith("- **"))continue;
                        int end=line.IndexOf(":**");
                        if(end<0)continue;
                        if(line.Substring(4,end-4).Trim()!=version)continue;
                        string text=line.Substring(end+3).Trim();
                        return text.Length>400?text.Substring(0,400)+"…":text;
                    }
                }
                catch(Exception){}
            }
            return "";
        }
    }
}
