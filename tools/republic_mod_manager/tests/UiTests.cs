using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TesmioAutoload;

static class UiTests
{
    static int passed;
    static void Check(bool value,string name){if(!value)throw new Exception("FAIL "+name);passed++;Console.WriteLine("PASS "+name);}
    static void HiddenShow(MainForm form){form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show();Application.DoEvents();}
    static IEnumerable<Control> Children(Control root){foreach(Control child in root.Controls){yield return child;foreach(Control nested in Children(child))yield return nested;}}
    static void CopyTree(string source,string target){foreach(string file in Directory.GetFiles(source,"*",SearchOption.AllDirectories)){string destination=Path.Combine(target,file.Substring(source.Length+1));Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(file,destination,true);}}
    static void Write(string path,string text){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,text,SafeFiles.Utf8);}
    static string MakeSimplePackage(string collection,string dllSource)
    {
        string package=Path.Combine(collection,"5550000000");Directory.CreateDirectory(Path.Combine(package,"hooks"));Directory.CreateDirectory(Path.Combine(package,"config"));File.Copy(dllSource,Path.Combine(package,"hooks","sample_plugin.dll"));
        Write(Path.Combine(package,"hooks","sample_plugin.ini"),"[general]\nenabled=1\nlimit=5\nmode=normal\n");
        Write(Path.Combine(package,"config","sample.launcher.ini"),"[launcher]\nlayout_version=1\nvisible=1\nid=example.sample\nconfig=sample_plugin.ini\nenabled_field=general/enabled\nicon=builtin:gear\n"+
            "[field:enabled]\nsection=general\nkey=enabled\nlabel=Enabled\ntype=boolean\ngroup=main\n"+
            "[field:limit]\nsection=general\nkey=limit\nlabel=Limit\ntype=integer\nminimum=0\nmaximum=20\ngroup=main\n"+
            "[field:mode]\nsection=general\nkey=mode\nlabel=Mode\ntype=choice\nchoices=normal|strict\ngroup=main\n"+
            "[tab:settings]\nlabel=Settings\n[group:main]\ntab=settings\nlabel=Main\n");
        Write(Path.Combine(package,"soviet.mod.ini"),"[mod]\nid=example.sample\nname=Sample Plugin\nversion=2.0-beta\nenabled=1\ntesmio_api_min=4\ntesmio_api_max=4\n[hooks]\ndll=hooks\\sample_plugin.dll\n"+
            "[configuration]\ndefaults=hooks\\sample_plugin.ini\nlauncher_schema=config\\sample.launcher.ini\nuser_config=sample_plugin.ini\n[autoload]\nformat=1\nkind=plugin\ntarget=sample_plugin\n");return package;
    }

    [STAThread] static int Main(string[] args)
    {
        try
        {
            if(args.Length!=2)return 2;string source=Path.GetFullPath(args[0]),root=Path.GetFullPath(args[1]);if(Directory.Exists(root))throw new IOException("Use a fresh UI test directory.");
            string collection=Path.Combine(root,"workshop"),package=Path.Combine(collection,"fixture_vehicle_materials"),loader=Path.Combine(root,"build");CopyTree(source,package);
            string simple=MakeSimplePackage(collection,Path.Combine(package,"hooks","vehicle_materials.dll"));string unsupported=Path.Combine(collection,"9876543210");Write(Path.Combine(unsupported,"soviet.mod.ini"),"[mod]\nid=example.other\nname=ZZ Other Mod\nversion=1.0\n[hooks]\ndll=one.dll\ndll=two.dll\n");
            string plugins=Path.Combine(loader,"plugins"),resourceIni=Path.Combine(plugins,"resources.ini"),resourceDll=Path.Combine(plugins,"resources.dll");Directory.CreateDirectory(plugins);File.WriteAllBytes(resourceDll,new byte[]{1,2,3});string needsDll=Path.Combine(plugins,"needs.dll");File.WriteAllBytes(needsDll,new byte[]{9,8,7});Write(Path.Combine(plugins,"needs.ini"),"[list]\nfurniture = eletronics, 1.0, advanced, 0.35, 0.010\nmedicine = eletronics, 0.5, none, 0.30, 0.008\n[needs]\nenabled = 1\ndemand = 1\nstorage = 1\nmax_demands = 7\nwhen_full = skip\nprobe = 1\nlog_seconds = 60\n");string needsDllHash=SafeFiles.HashFile(needsDll);string depositsDll=Path.Combine(plugins,"deposits.dll");File.WriteAllBytes(depositsDll,new byte[]{5,5,5});Write(Path.Combine(plugins,"deposits.ini"),"[deposits]\ncode_patch = 1\nminimap = 1\neditor = 1\n[copper]\ntoken = $TYPE_MINE_COPPER\ntype = 10\nmap = resourcemap2\ncomponent = 3\nradius = ore\nicon = copper_ore\nminimap = 1\neditor = copper\n[sand]\ntoken = $TYPE_MINE_SAND\ntype = 11\nmap = terrain\ncomponent = 1\nradius = gravel\nicon = sand\nminimap = 1\neditor = sand\n");string depositsDllHash=SafeFiles.HashFile(depositsDll);
            Write(resourceIni,"[list]\nglass=aluminium, Glass\ncable=steel, Cable\nsand=bauxite, Sand\n[resources]\nhook=2\n");Write(Path.Combine(loader,"tesmioloader.ini"),"[tesmioloader]\nplugins=1\n[plugins]\nresources=1\nneeds=1\ndeposits=1\nvehicle_materials=1\nsample_plugin=1\n");
            string resourcesHash=SafeFiles.HashFile(resourceIni),resourceDllHash=SafeFiles.HashFile(resourceDll),profile=Path.Combine(root,"ui-state.ini");var defaults=new UiState{Build=loader,WorkshopRoot=collection,SelectedSource=package,Language="de"};var notes=new List<string>();var store=new UiStateStore(profile);
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);

            using(var form=new MainForm(store.Load(defaults,notes),store,true))
            {
                HiddenShow(form);
                Check(form.ModCount==6,"startup lists Workshop packages and the schema-driven local Resources, Needs and Deposits editors");
                Check(form.SelectedSource==package&&form.HasEditor,"remembered package opens directly without package-specific code");
                Check(form.TabCount==4&&form.ActionCount==3,"tabs come from schema and footer keeps exactly three actions");
                Check(form.DisplayedValue("resources/count")=="0"&&form.DisplayedValue("general/enabled")=="0","Vehicle Materials starts disabled with an empty user-owned collection");Check(form.SwitchVisible&&!form.SwitchChecked&&form.SwitchNote.Contains("tesmioloader.ini"),"single plugin switch shows off while the INI switch is 0");
                form.TestEdit("general/enabled","1");Check(form.StatusDetailText.Contains("Reiter „Ressourcen“")&&form.StatusDetailText.Contains("unter „Materialien“")&&form.StatusDetailText.Contains("Mit +")&&!form.StatusDetailText.Contains("Sammlung"),"German validation names the schema-derived tab and group");form.TestEdit("general/enabled","0");
                Check(form.ResourceChoiceCount==3&&form.ResourceUsedCount==0,"provider catalogue is available while no material is preselected");
                Check(!File.Exists(Path.Combine(plugins,"vehicle_materials.dll"))&&!File.Exists(Path.Combine(plugins,"vehicle_materials.ini")),"viewing never deploys native files");
                Check(SafeFiles.HashFile(resourceIni)==resourcesHash,"viewing never edits provider INI");
                using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(0,0,form.Width,form.Height));image.Save(Path.Combine(root,"ui-empty-vehicle-materials.png"));}

                form.SelectTab("vehicles");
                Check(Children(form).OfType<Label>().Any(x=>x.Text.Contains("noch keine Materialien"))&&Children(form).OfType<NoticeIcon>().Count(x=>x.Warning)==1,"schema warning appears while the collection is empty");
                Check(Children(form).OfType<Card>().Single().HeaderText=="Materialverbrauch nach Fahrzeugklasse","group title is rendered in the full-width card header");
                var activeTab=Children(form).OfType<Button>().Single(x=>Convert.ToString(x.Tag)=="vehicles");Check(activeTab.BackColor==Theme.SelectionBlue&&activeTab.ForeColor==Color.White,"active tab uses the selected-plugin blue and white text");
                using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(0,0,form.Width,form.Height));image.Save(Path.Combine(root,"ui-empty-warning.png"));}

                form.SelectTab("resources");Check(Children(form).OfType<Card>().Single().HeaderText=="Materialien","Vehicle Materials uses the requested resource group title");var resourceAdd=Children(form).OfType<VectorButton>().Single(x=>x.Symbol=="plus"&&x.Height==38);Check(resourceAdd.Primary&&resourceAdd.BackColor==Theme.Blue,"collection add button remains a primary action");
                Check(resourceAdd.GlyphScale>=2.5f&&!Children(form).OfType<TextBox>().Any(x=>x.Text=="0"),"large add glyph and non-focusable readonly display are used");
                using(var dialog=form.TestResourceDialog())
                {
                    dialog.ShowInTaskbar=false;dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-20000,-20000);dialog.Show();Application.DoEvents();var picker=Children(dialog).OfType<ResourcePickerList>().Single();
                    Check(picker.UsedCount==0&&picker.AvailableCount==3,"picker shows every provider entry as initially available");
                    Check(Children(dialog).OfType<NumberInput>().All(x=>x.Width<=165)&&Children(dialog).OfType<VectorButton>().Where(x=>x.Symbol=="plus"||x.Symbol=="minus").All(x=>x.Primary&&x.ForeColor==Color.White),"resource dialog uses compact inputs with blue spin buttons");
                    using(var image=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(image,new Rectangle(0,0,dialog.Width,dialog.Height));image.Save(Path.Combine(root,"ui-resource-dialog.png"));}
                    picker.SelectedIndex=2;Application.DoEvents();Check(picker.Choice!=null&&picker.Choice.Option.Id=="sand","unused entry is selectable");dialog.Close();
                }
                form.TestAddResource("sand","0.020","0.010","0.005","0.003");
                Check(form.IsDirty&&form.DisplayedValue("resources/count")=="1"&&form.DisplayedValue("resources/resource0")=="sand"&&form.DisplayedValue("road/sand")=="0.02","plus creates a fully schema-driven collection item");
                Check(form.ResourceUsedCount==1,"new item is immediately marked used");
                form.SelectTab("vehicles");Check(!Children(form).OfType<Label>().Any(x=>x.Text.Contains("noch keine Materialien")),"empty warning disappears immediately after the first item is added");
                form.TestEdit("general/enabled","1");Check(form.StatusValid,"plugin can be enabled after a positive item exists");
                Check(form.IsDirty&&form.StatusText=="Ungespeicherte Änderungen"&&form.SaveEnabled&&form.HeadingText.EndsWith(" *"),"unsaved state shows the amber heading, the primary save button and the heading mark (0.4.51)");Check(form.SwitchChecked,"single plugin switch follows the INI switch");
                form.TestCommit(()=>{});string user=Path.Combine(loader,"user_config","vehicle_materials.ini");Ini saved=new Ini(SafeFiles.Text(user));Check(saved.Get("resources","count")=="1"&&saved.Get("resources","resource0")=="sand"&&saved.Get("airplane","sand")=="0.003","save persists empty-package collection entirely as personal data");
                form.TestEdit("road/sand","0.0200");Check(!form.IsDirty,"numeric formatting-only edit is not reported as a change");
                form.TestEdit("road/sand","0.021");Check(form.TestApply(()=>{}),"generic Save deploys DLL and effective INI");
                Check(new Ini(SafeFiles.Text(Path.Combine(plugins,"vehicle_materials.ini"))).Get("road","sand")=="0.021"&&form.Activity(package),"activity light follows manifest target and enabled field");
                form.SelectTab("resources");Check(Children(form).OfType<VectorButton>().Count(x=>x.Symbol=="trash")==1,"user-owned item receives a delete button");
                string question="";form.ResourceRemovePrompt=text=>{question=text;return DialogResult.Cancel;};Check(!form.TestRemoveResource("sand")&&question.Contains("sand"),"delete confirmation names item and honours Cancel");
                form.ResourceRemovePrompt=text=>DialogResult.OK;Check(form.TestRemoveResource("sand")&&form.DisplayedValue("resources/count")=="0"&&form.DisplayedValue("road/sand")==null,"confirmed delete removes list item and every target value");
                form.SelectTab("vehicles");Check(Children(form).OfType<Label>().Any(x=>x.Text.Contains("noch keine Materialien")),"empty warning returns after the final item is removed");
                Check(form.DisplayedValue("general/enabled")=="0"&&form.StatusValid,"removing the final positive item safely disables the plugin");form.TestCommit(()=>{});
                Check(!form.IsDirty&&!form.SaveEnabled&&form.StatusText=="Konfiguration gültig"&&!form.HeadingText.EndsWith(" *"),"after the commit the heading mark and the save emphasis are gone (0.4.51)");

                form.TestSearch("Sample");form.SelectIndex(0);Application.DoEvents();Check(form.SelectedSource==simple&&form.HasEditor&&form.TabCount==1&&form.DisplayedValue("general/limit")=="5","unrelated second plugin renders from its own schema");
                form.TestEdit("general/limit","7");Check(form.TestApply(()=>{})&&new Ini(SafeFiles.Text(Path.Combine(plugins,"sample_plugin.ini"))).Get("general","limit")=="7","unrelated plugin saves to its manifest-derived filename");
                Check(form.SwitchVisible&&form.SwitchChecked&&form.SwitchNote.Contains("tesmioloader.ini"),"installed plugin shows the single plugin switch on");
                form.TestSwitch(false);Check(!form.SwitchChecked&&form.DisplayedValue("general/enabled")=="1"&&form.TestApply(()=>{})&&new Ini(SafeFiles.Text(Path.Combine(loader,"tesmioloader.ini"))).Get("plugins","sample_plugin")=="0","switching off writes only the loader entry and leaves the INI switch alone");
                form.TestSwitch(true);Check(form.SwitchChecked&&form.TestApply(()=>{})&&new Ini(SafeFiles.Text(Path.Combine(loader,"tesmioloader.ini"))).Get("plugins","sample_plugin")=="1","switching on restores the loader entry");
                form.TestEdit("general/enabled","0");Check(!form.SwitchChecked,"an INI switch set to 0 shows the header switch off although the loader entry is on");form.TestSwitch(true);Check(form.SwitchChecked&&form.DisplayedValue("general/enabled")=="1","switching on sets the INI switch back to 1");form.TestApply(()=>{});
                Check(SafeFiles.HashFile(resourceIni)==resourcesHash,"ordinary plugin operations leave the Resources provider INI byte-for-byte unchanged");
                form.TestSearch("Resources");form.SelectIndex(0);Application.DoEvents();Check(form.HasLocalResourceEditor&&form.LocalResourceCount==3&&form.Activity(form.SelectedSource),"local Resources editor is discovered from its launcher schema and shows the provider list");
                Check(Children(form).OfType<Card>().Single().HeaderText=="Ressourcendefinitionen"&&!Children(form).OfType<VectorButton>().Any(x=>x.Symbol=="trash")&&Children(form).OfType<ResourceDefinitionList>().Count()==1,"selected original resource has a lock-aware master list and no delete action");
                {int[] before=form.TestCardWidths();int wide=form.TestContentWidth;form.TestDeferredRebuild();Application.DoEvents();Application.DoEvents();int[] after=form.TestCardWidths();Check(after.Length==before.Length&&after.All(w=>w>=wide-28)&&before.All(w=>w>=wide-28),"a deferred rebuild (dropdown path) keeps every card at full width: "+String.Join(",",after)+" of "+wide+" (before "+String.Join(",",before)+"; content "+form.TestContentSize+")");}
                form.TestAddLocalResource("hydrogen","custom","Hydrogen","oil");form.TestSetLocalField("hydrogen","kind","1");form.TestSetLocalField("hydrogen","pinned_base_price","180, 150");form.TestSetLocalField("hydrogen","forced_price","200, 175");var localEffective=new LooseIni(form.LocalEffective());Check(form.LocalResourceCount==4&&form.TestLocalOwned("hydrogen")&&localEffective.Get("custom:hydrogen","transport")=="oil"&&localEffective.Get("base_price","hydrogen")=="180, 150"&&localEffective.Get("price","hydrogen")=="200, 175","master-detail editor generates custom and keyed price entries for a personal resource");
                Check(Children(form).OfType<VectorButton>().Any(x=>x.Symbol=="trash"&&x.Danger),"personal resource receives the red delete action");Check(form.TestSaveLocal(()=>{})&&SafeFiles.HashFile(resourceDll)==resourceDllHash,"Resources settings save without replacing the provider DLL");
                string warning="";form.ResourceRemovePrompt=text=>{warning=text;return DialogResult.Cancel;};form.TestRemoveLocalResource("hydrogen");Check(form.LocalResourceCount==4&&warning.Contains("Spielstand"),"resource deletion warns about saved-game compatibility and honours Cancel");form.ResourceRemovePrompt=text=>DialogResult.OK;form.TestRemoveLocalResource("hydrogen");Check(form.LocalResourceCount==3&&form.TestSaveLocal(()=>{}),"confirmed deletion removes only the personal resource and saves the clean overlay");
                using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(0,0,form.Width,form.Height));image.Save(Path.Combine(root,"ui-resource-master-detail.png"));}
                // 0.4.36: below 600 px of detail width the captions move above the inputs; wide again afterwards.
                form.TestSearch("Deposits");form.SelectIndex(0);Application.DoEvents();form.SelectTab("deposits");Application.DoEvents();
                form.MinimumSize=new Size(900,700);form.Width=1150;Application.DoEvents();Application.DoEvents();
                var narrow=Children(form).OfType<TableLayoutPanel>().Where(t=>Equals(t.Tag,"narrow")).ToList();
                Check(narrow.Count>0&&narrow.All(t=>t.Controls.Cast<Control>().All(c=>t.GetColumn(c)==0)),"narrow details stack captions above inputs (0.4.36)");
                form.Width=1600;Application.DoEvents();Application.DoEvents();
                Check(!Children(form).OfType<TableLayoutPanel>().Any(t=>Equals(t.Tag,"narrow")),"wide details return to two columns (0.4.36)");
                form.TestSearch("Needs");form.SelectIndex(0);Application.DoEvents();Check(form.HasLocalResourceEditor&&form.LocalResourceCount==2&&form.Activity(form.SelectedSource),"local Needs editor is discovered from its keyed_list schema and lists the original needs");
                Check(form.TabCount==2&&form.SelectedTabId=="needs"&&Children(form).OfType<Card>().Count()==1&&Children(form).OfType<Card>().First().HeaderText=="Bedürfnisse der Bürger"&&Children(form).OfType<ComboBox>().Any(x=>x.AccessibleName=="column:donor"),"Needs editor opens on its needs tab with the list card and column inputs");
                form.SelectTab("general");Check(form.SelectedTabId=="general"&&Children(form).OfType<Card>().Count()==1&&Children(form).OfType<Card>().First().HeaderText=="Plugin-Einstellungen"&&Children(form).OfType<ToggleSwitch>().Count(x=>(x.AccessibleName??"").StartsWith("global:"))==3&&!Children(form).OfType<ToggleSwitch>().Any(x=>x.AccessibleName=="global:enabled"),"general tab shows the plugin settings card without the enabled switch, which lives in the header");
                Check(form.SwitchVisible&&form.SwitchChecked&&form.SwitchNote.Contains("tesmioloader.ini"),"Needs editor shows the single plugin switch in the header");
                form.TestSwitch(false);Check(!form.SwitchChecked&&form.IsDirty,"switching Needs off marks the session dirty");form.TestSwitch(true);Check(form.SwitchChecked&&!form.IsDirty,"switching Needs back on clears the pending change");
                form.SelectTab("needs");
                Check(Children(form).OfType<VectorButton>().Count(x=>x.Symbol=="trash"&&x.Danger)==1,"original need offers the hide action");
                form.TestAddListItem("glass","clothes, 2, medium, 0.5, 0.01");form.TestSetListColumn("furniture",3,"0.5");form.TestSetGlobal("max_demands","5");var needsEffective=new LooseIni(form.LocalEffective());Check(form.LocalResourceCount==3&&form.TestLocalOwned("glass")&&needsEffective.Get("list","glass")=="clothes, 2, medium, 0.5, 0.01"&&needsEffective.Get("list","furniture")=="eletronics, 1, advanced, 0.5, 0.01"&&needsEffective.Get("needs","max_demands")=="5","Needs editor writes personal lines, column overrides and plugin switches into the effective needs.ini");
                string needsWarning="";form.ResourceRemovePrompt=text=>{needsWarning=text;return DialogResult.Cancel;};form.TestRemoveLocalResource("medicine");Check(form.LocalResourceCount==3&&needsWarning.Contains("Spielstand"),"hiding an original need warns about saved games and honours Cancel");form.ResourceRemovePrompt=text=>DialogResult.OK;form.TestRemoveLocalResource("medicine");Check(form.LocalResourceCount==2&&form.LocalHidden().Contains("medicine")&&Children(form).OfType<Button>().Any(x=>(x.AccessibleName??"")=="unhide:medicine"),"hidden original is listed with a show-again action");
                Check(form.TestSaveLocal(()=>{})&&new LooseIni(SafeFiles.Text(Path.Combine(plugins,"needs.ini"))).Get("list","medicine")==null&&SafeFiles.HashFile(Path.Combine(plugins,"needs.dll"))==needsDllHash,"Needs settings save the effective INI without replacing the DLL");
                form.TestSwitch(false);Check(form.TestSaveLocal(()=>{})&&new Ini(SafeFiles.Text(Path.Combine(loader,"tesmioloader.ini"))).Get("plugins","needs")=="0"&&!form.Activity(form.SelectedSource),"switching Needs off writes [plugins] needs = 0 and turns the activity light off");
                form.TestSwitch(true);Check(form.TestSaveLocal(()=>{})&&new Ini(SafeFiles.Text(Path.Combine(loader,"tesmioloader.ini"))).Get("plugins","needs")=="1"&&form.Activity(form.SelectedSource),"switching Needs back on restores the loader entry and the activity light");
                using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(0,0,form.Width,form.Height));image.Save(Path.Combine(root,"ui-needs-master-detail.png"));}
                form.TestSearch("Deposits");form.SelectIndex(0);Application.DoEvents();Check(form.HasLocalResourceEditor&&form.LocalResourceCount==2&&form.TabCount==2&&form.SelectedTabId=="deposits"&&form.SwitchVisible&&form.SwitchChecked&&form.Activity(form.SelectedSource),"local Deposits editor is discovered from its keyed_sections schema and opens on the deposits tab");
                Check(Children(form).OfType<Card>().Single().HeaderText=="Vorkommen"&&Children(form).OfType<ComboBox>().Any(x=>x.AccessibleName=="item:map")&&Children(form).OfType<ToggleSwitch>().Any(x=>x.AccessibleName=="item:minimap")&&Children(form).OfType<TextBox>().Any(x=>x.AccessibleName=="item:token")&&Children(form).OfType<VectorButton>().Count(x=>x.Symbol=="trash"&&x.Danger)==1,"deposit details show every key with the matching input and the hide action");
                Check(form.LocalNextValue("type")=="12","the add dialog proposes the next free type number");
                form.TestAddSection("glass",new Dictionary<string,string>{{"token","$TYPE_MINE_GLASS"},{"type","12"},{"map","auto"},{"radius","ore"},{"icon","glass"},{"minimap","1"},{"editor","glass"}});var depositsEffective=new LooseIni(form.LocalEffective());Check(form.LocalResourceCount==3&&form.TestLocalOwned("glass")&&depositsEffective.Get("glass","token")=="$TYPE_MINE_GLASS"&&depositsEffective.Get("glass","type")=="12"&&form.IsDirty,"a personal deposit becomes a new section in the effective deposits.ini");
                bool duplicateRejected=false;try{form.TestAddSection("dup",new Dictionary<string,string>{{"token","$TYPE_MINE_DUP"},{"type","12"}});}catch(Exception){duplicateRejected=true;}Check(duplicateRejected&&form.LocalResourceCount==3,"a taken type number is refused with an error");
                form.SelectTab("general");Check(Children(form).OfType<Card>().Single().HeaderText=="Plugin-Einstellungen"&&Children(form).OfType<ToggleSwitch>().Count(x=>(x.AccessibleName??"").StartsWith("global:"))==3&&Children(form).OfType<Label>().Any(x=>x.Text.Contains("Spielstand")),"general tab shows the three plugin switches and the saved-game notice");
                form.SelectTab("deposits");form.ResourceRemovePrompt=text=>DialogResult.OK;form.TestRemoveLocalResource("copper");Check(form.LocalResourceCount==2&&form.LocalHidden().Contains("copper"),"an original deposit can be hidden");
                Check(form.TestSaveLocal(()=>{})&&!new LooseIni(SafeFiles.Text(Path.Combine(plugins,"deposits.ini"))).HasSection("copper")&&new LooseIni(SafeFiles.Text(Path.Combine(plugins,"deposits.ini"))).Get("glass","type")=="12"&&SafeFiles.HashFile(Path.Combine(plugins,"deposits.dll"))==depositsDllHash,"Deposits settings save the effective INI without replacing the DLL");
                using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(0,0,form.Width,form.Height));image.Save(Path.Combine(root,"ui-deposits-master-detail.png"));}
                form.TestSearch("Vehicle");form.SelectIndex(0);Application.DoEvents();form.SelectTab("general");Check(Children(form).OfType<Button>().Count(x=>(x.AccessibleName??"").StartsWith("link:"))==1&&Children(form).OfType<Button>().Any(x=>(x.AccessibleName??"")=="link:readme"&&x.Enabled),"general tab shows exactly the guide of the active language");form.SelectTab("resources");Check(!Children(form).OfType<Button>().Any(x=>(x.AccessibleName??"").StartsWith("link:"))&&!Children(form).OfType<Card>().Any(x=>x.HeaderText=="Hinweise"),"other tabs carry neither guides nor the notices card");
                // 0.4.15: number inputs keep the reset button's column free at every window size (user report: cut off after resizing).
                {
                    Func<bool> inside=()=>{var boxes=Children(form).OfType<NumberInput>().Where(x=>x.Parent!=null).ToList();return boxes.Count>0&&boxes.All(x=>x.Right<=x.Parent.ClientSize.Width-30);};
                    form.SelectTab("general");Application.DoEvents();
                    Check(inside(),"number inputs leave room for the reset button after the tab is built");
                    form.Width+=300;Application.DoEvents();Check(inside(),"number inputs still fit after widening the window");
                    form.Width-=500;Application.DoEvents();Check(inside(),"number inputs still fit after narrowing the window");
                    form.WindowState=FormWindowState.Maximized;Application.DoEvents();Check(inside(),"number inputs still fit maximized");
                    form.WindowState=FormWindowState.Normal;Application.DoEvents();Check(inside(),"number inputs still fit after restoring");
                }
                form.TestSearch("ZZ Other");form.SelectIndex(0);Application.DoEvents();Check(form.SelectedSource==unsupported&&!form.HasEditor,"rejected Workshop package remains visible but cannot deploy");
            }

            var againStore=new UiStateStore(profile);using(var form=new MainForm(againStore.Load(defaults,notes),againStore,true))
            {HiddenShow(form);Check(form.SelectedSource==unsupported,"second launch remembers selected package tab");form.TestLanguage("en");Check(form.LanguageGlyph=="text:EN","language selection remains general application state");}

            using(var form=new MainForm(new UiState{Build=loader,WorkshopRoot=collection,SelectedSource=package,Language="en"},null,false))
            {HiddenShow(form);form.PendingPrompt=()=>DialogResult.No;Check(form.DisplayedValue("resources/count")=="0","deleted collection remains empty after reopening");form.TestEdit("general/enabled","1");Check(form.StatusDetailText.Contains("“Resources” tab")&&form.StatusDetailText.Contains("under “Materials”")&&form.StatusDetailText.Contains("Use +")&&!form.StatusDetailText.Contains("Reiter"),"English validation names the translated schema-derived tab and group");form.TestEdit("general/enabled","0");bool started=false;Check(form.TestSaveAndLaunch(()=>{},()=>started=true)&&started&&!form.Visible,"Save + Start closes Settings after successful launch");}
            Check(SafeFiles.HashFile(resourceIni)==resourcesHash,"all collection operations leave provider INI byte-for-byte unchanged");
            using(var icon=Icon.ExtractAssociatedIcon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"rmm.exe"))){Check(icon!=null,"EXE contains native application icon");}
            // 0.23.0: profiles and restore points window, driven through its test hooks.
            using(var window=new ProfilesWindow(new Language("de"),loader,new Font("Segoe UI",10),null))
            {
                window.NamePrompt=()=>new KeyValuePair<string,string>("Testprofil","vor dem Umbau");window.Confirm=q=>DialogResult.Yes;window.Guard=()=>{};
                Check(window.ProfileCount==0&&window.PointCount>0&&window.ProfileInfoText.Contains("Noch kein Profil"),"window starts without profiles and lists the restore points of earlier saves");
                window.TestSave();Check(window.ProfileCount==1&&window.SummaryText.Contains("Testprofil")&&window.ProfileFileRows>0,"saving creates a profile from the loader configuration");
                string loaderIni=Path.Combine(loader,"tesmioloader.ini");string original=File.ReadAllText(loaderIni,SafeFiles.Utf8);Write(loaderIni,original+"zeta=1\n");
                Check(window.ProfileInfoText.Contains("abweichend: 0"),"details are refreshed only on demand");window.Reload();Check(window.ProfileInfoText.Contains("abweichend: 1"),"a changed file shows as differing");
                window.TestApply();Check(File.ReadAllText(loaderIni,SafeFiles.Utf8)==original&&window.Changed,"applying the profile writes the captured file back");
                window.TestShowPoints();window.TestSelectPoint("profile");Check(window.PointsShown&&window.PointCount>0,"the profile application left its own restore point");
                window.TestRestore();Check(File.ReadAllText(loaderIni,SafeFiles.Utf8)==original+"zeta=1\n","restoring the point brings the edited file back");
                Write(loaderIni,original);window.TestDelete();Check(window.ProfileCount==0,"deleting removes the profile");
            }
            using(var form=new MainForm(new UiState{Build=loader,WorkshopRoot=collection,SelectedSource=package,Language="de"},null,false))
            {HiddenShow(form);Check(Children(form).OfType<SidebarButton>().Count()==5,"sidebar offers folders, log, profiles, language and refresh");}
            // 0.32.0: the building picker, driven through its hooks against a fake game folder.
            {
                string game=Path.Combine(root,"fake-game"),ms=Path.Combine(game,"media_soviet");
                Write(Path.Combine(ms,"buildings_types","cement_plant.ini"),"$OBSOLETE\r\n$TYPE_FACTORY\r\n");Write(Path.Combine(ms,"buildings_types","cement_plant_v3.ini"),"$TYPE_FACTORY\r\n");Write(Path.Combine(ms,"buildings_types","flat.ini"),"$TYPE_LIVING\r\n");Write(Path.Combine(ms,"dlc3","buildings","ts_small","building.ini"),"$TYPE_GARBAGE_OFFICE\r\n");
                string fakeBuild=Path.Combine(game,"tesmioloader","build");Directory.CreateDirectory(fakeBuild);
                var used=new Dictionary<string,string>{{"buildings_types\\flat.ini","other_rules"}};
                using(var picker=new BuildingPickerWindow(new Language("de"),fakeBuild,"",new[]{"buildings_types\\cement_plant_v3.ini","2496571917\\gone\\building.ini"},used,new Font("Segoe UI",10),null))
                {
                    Check(picker.Total==4&&picker.SelectedCount==2&&picker.VisibleCount==2&&picker.CounterText.Contains("2 ausgewählt"),"picker lists the game buildings, keeps unknown targets and hides obsolete ones by default");
                    picker.TestObsolete(true);Check(picker.VisibleCount==3,"the obsolete filter reveals superseded versions");
                    picker.TestSearch("ts_");Check(picker.VisibleCount==1,"search narrows the available list");picker.TestSearch("");
                    picker.TestSelect("dlc3\\buildings\\ts_small\\building.ini");picker.TestUnselect("2496571917\\gone\\building.ini");
                    var result=picker.TestResult();Check(result.Count==2&&result[0]=="buildings_types\\cement_plant_v3.ini"&&result[1]=="dlc3\\buildings\\ts_small\\building.ini","selection keeps the existing order and appends new targets");
                }
            }
            // 0.4.21: the game-text picker on a synthetic language file.
            {
                string textGame=Path.Combine(root,"text-game");Directory.CreateDirectory(Path.Combine(textGame,"media_soviet"));
                var texts=new List<KeyValuePair<int,string>>{new KeyValuePair<int,string>(5,"kurz"),new KeyValuePair<int,string>(1970,"Zeigt den Bereich an, in dem ein moegliches Problem auf der Route besteht!\n(Das ist der naechstgelegene Punkt zum Ziel)"),new KeyValuePair<int,string>(2412,"Ein anderer sehr langer Hinweis, der ebenfalls aus dem Fenster laeuft und umgebrochen werden koennte")};
                File.WriteAllBytes(Path.Combine(textGame,"media_soviet","sovietGerman.btf"),GameTexts.Build(texts));File.WriteAllBytes(Path.Combine(textGame,"media_soviet","sovietEnglish.btf"),GameTexts.Build(texts.Take(1).ToList()));Write(Path.Combine(textGame,"media_soviet","config.ini"),"$TEXT LANGUAGE2 auto\n");
                using(var picker=new GameTextPickerWindow(new Language("de"),Path.Combine(textGame,"tesmioloader","build"),new[]{5},new Font("Segoe UI",10),null))
                {
                    Check(picker.SelectedLanguage=="German"&&picker.LoadedCount==3,"text picker loads the German file for the German UI");
                    Check(picker.VisibleCount==2,"text picker hides used ids and short lines at the default minimum of 60");
                    picker.TestSetFilter("",0);Check(picker.VisibleCount==2,"text picker shows every unused text at minimum 0");
                    picker.TestSetFilter("Route",0);Check(picker.VisibleCount==1,"text picker filters by text");
                    picker.TestSetFilter("2412",0);Check(picker.VisibleCount==1,"text picker finds an id typed into the search");
                    picker.TestSetFilter("",80);Check(picker.VisibleCount==1,"text picker filters by the longest line");
                    picker.TestSetFilter("",0);picker.TestSelect(1970);Check(picker.Result==1970,"text picker returns the chosen id");
                }
                using(var picker=new GameTextPickerWindow(new Language("en"),Path.Combine(textGame,"tesmioloader","build"),null,new Font("Segoe UI",10),null))
                    Check(picker.SelectedLanguage=="English"&&picker.LoadedCount==1,"text picker follows the English UI when the game says auto");
                using(var picker=new GameTextPickerWindow(new Language("de"),Path.Combine(root,"nowhere","tesmioloader","build"),null,new Font("Segoe UI",10),null))
                    Check(picker.LoadedCount==0&&picker.VisibleCount==0,"text picker without a game folder stays empty instead of failing");
            }
            // 0.4.42: the line picker composes edit commands from the lines of a Vanilla research block.
            {
                var entry=GameResearch.Parse(new[]{"$RESEARCH faculty_geology","$UNLOCK_RESEARCH uranium_study","$UNLOCK_RESEARCH bauxite_study","$COST 1500","$RESEARCH_ADD"})[0];
                string noGame=Path.Combine(root,"nowhere","tesmioloader","build");
                using(var lines=new ResearchLinesWindow(new Language("de"),noGame,"faculty_geology",entry,"line_anchor",null,new Font("Segoe UI",10),null))
                {Check(lines.LineCount==3,"line picker lists the editable lines of the block");lines.TestChoose(0,1,null);Check(lines.Result=="$UNLOCK_RESEARCH uranium_study | $UNLOCK_RESEARCH bauxite_study","line picker builds a move command from line and anchor");}
                using(var lines=new ResearchLinesWindow(new Language("de"),noGame,"faculty_geology",entry,"line_edit",null,new Font("Segoe UI",10),null))
                {lines.TestChoose(2,-1,"$COST 1800");Check(lines.Result=="$COST 1500 | $COST 1800","line picker builds a replace command from line and edited copy");}
                using(var lines=new ResearchLinesWindow(new Language("de"),noGame,"faculty_geology",entry,"edit",null,new Font("Segoe UI",10),null))
                {lines.TestChoose(-1,-1,"$UNLOCK_RESEARCH clay_study");Check(lines.Result=="$UNLOCK_RESEARCH clay_study","line picker returns a new line for add");}
                using(var lines=new ResearchLinesWindow(new Language("de"),noGame,"faculty_geology",entry,"line",null,new Font("Segoe UI",10),null))
                {lines.TestChoose(1,-1,null);Check(lines.Result=="$UNLOCK_RESEARCH bauxite_study","line picker returns the chosen line for remove");}
            }
            // 0.4.44: a number field reports the typed number as its Text, so the add dialogs keep it.
            using(var number=new NumberInput(1,100000,100)){number.Input.Text="1300";Check(number.Text=="1300","number input exposes the typed value as Text");number.Text="1800";Check(number.Input.Text=="1800","number input takes a value through Text");}
            // 0.4.45: +/- clicks raise Stepped and start an empty field at the minimum.
            using(var number=new NumberInput(1,100000,100)){int stepped=0;number.Stepped+=(s,e)=>stepped++;number.Input.Text="1300";number.TestStep(1);Check(number.Text=="1400"&&stepped==1,"plus click steps the value and raises Stepped");number.Input.Text="";number.TestStep(1);Check(number.Text=="1"&&stepped==2,"plus click on an empty field lands on the minimum");number.TestStep(-1);Check(number.Text=="1"&&stepped==2,"minus click below the minimum changes nothing");}
            // 0.4.71: edits in several entries survive switching and are written by one save.
            using(var form=new MainForm(new UiState{Build=loader,WorkshopRoot=collection,SelectedSource=package,Language="de"},null,false))
            {
                HiddenShow(form);form.PendingPrompt=()=>{throw new Exception("FAIL switching entries must not ask about unsaved changes (0.4.71)");};
                form.TestSearch("Sample");form.SelectIndex(0);Application.DoEvents();form.TestEdit("general/limit","9");Check(form.IsDirty&&form.DirtyCount==1&&form.DirtyMarks==1,"an edit in the first entry counts as unsaved");
                form.TestSearch("Needs");form.SelectIndex(0);Application.DoEvents();
                Check(form.HasLocalResourceEditor&&!form.IsDirty&&form.DirtyCount==1&&form.DirtyMarks==1&&form.SaveEnabled&&form.StatusText=="Ungespeicherte Änderungen"&&form.StatusDetailText.Contains("Sample Plugin"),"switching parks the first entry: footer names it, save stays enabled, its dot stays (0.4.71)");
                form.TestSetGlobal("max_demands","4");Check(form.IsDirty&&form.DirtyCount==2&&form.DirtyMarks==2&&form.StatusDetailText.Contains("Needs")&&form.StatusDetailText.Contains("Sample Plugin"),"the second entry adds to the unsaved entries");
                form.TestSearch("Sample");form.SelectIndex(0);Application.DoEvents();Check(form.DisplayedValue("general/limit")=="9"&&form.IsDirty&&form.DirtyCount==2&&form.HeadingText.EndsWith(" *"),"the parked edit comes back when the entry is shown again");
                Check(form.TestSaveAll(()=>{})&&!form.IsDirty&&form.DirtyCount==0&&form.DirtyMarks==0&&new Ini(SafeFiles.Text(Path.Combine(plugins,"sample_plugin.ini"))).Get("general","limit")=="9"&&new LooseIni(SafeFiles.Text(Path.Combine(plugins,"needs.ini"))).Get("needs","max_demands")=="4","one save writes every unsaved entry (0.4.71)");
                form.TestEdit("general/limit","11");form.TestSearch("Needs");form.SelectIndex(0);Application.DoEvents();form.TestSetGlobal("max_demands","6");Check(form.DirtyCount==2,"two entries are unsaved again");
                form.PendingPrompt=()=>DialogResult.Cancel;Check(!form.TestResolvePending()&&form.DirtyCount==2,"cancelling the unsaved-changes question keeps every parked edit");
                form.PendingPrompt=()=>DialogResult.No;Check(form.TestResolvePending()&&!form.IsDirty&&form.DirtyCount==0&&form.DirtyMarks==0,"discarding drops the parked edits and reloads the shown entry");
                form.TestSearch("Sample");form.SelectIndex(0);Application.DoEvents();Check(form.DisplayedValue("general/limit")=="9","a discarded parked edit is gone when the entry is shown again");
            }
            Console.WriteLine("RESULT "+passed+" UI assertions passed. "+root);return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
