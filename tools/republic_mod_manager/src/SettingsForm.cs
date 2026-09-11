using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace TesmioAutoload
{
    public sealed partial class MainForm : Form
    {
        readonly UiState state; readonly UiStateStore stateStore; readonly bool persistUi;
        readonly ModList mods = new ModList(); readonly TextBox search = new TextBox();
        Button saveButton;   // 0.4.51: primary only while something is unsaved
        readonly Label searchLabel = Theme.Label("",10,false), pluginLabel = Theme.Label("",15,true);
        readonly Label heading = Theme.Label("",22,true), description = Theme.Label("",11,false), switchLabel = Theme.Label("",11,false), switchNote = Theme.Label("",8,false);
        readonly ToggleSwitch activation = new ToggleSwitch();
        readonly Label status = Theme.Label("",12,true), statusDetail = Theme.Label("",9,false);
        readonly TabStrip tabStrip = new TabStrip(); readonly FlowLayoutPanel content = new FlowLayoutPanel();
        // Status bar below the tabs (0.4.12): load path of the DLL, where the files live, last game start.
        readonly Panel statusBar = new Panel(); readonly FlowLayoutPanel statusFlow = new FlowLayoutPanel(); readonly Label statusRight = new Label();
        readonly Dictionary<Button,string> translatedButtons = new Dictionary<Button,string>();
        readonly Dictionary<SidebarButton,string> translatedSidebar = new Dictionary<SidebarButton,string>();
        readonly List<Button> actions = new List<Button>(); readonly List<CatalogEntry> entries = new List<CatalogEntry>();
        readonly Dictionary<string,string> draft = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase), baseline = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> resets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string,Action<string>> setters = new Dictionary<string,Action<string>>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string,Label> origins = new Dictionary<string,Label>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string,VectorButton> resetButtons = new Dictionary<string,VectorButton>(StringComparer.OrdinalIgnoreCase);   // 0.4.38: shown only while the value differs from the default
        readonly IconCache icons = new IconCache(); readonly ToolTip tips = new ToolTip(); readonly StringBuilder journal = new StringBuilder();
        readonly Panel header; readonly SidebarButton languageButton; Button restoreButton; TableLayoutPanel page;
        Session session; Presentation presentation; LocalResourceSession resourceSession; LocalEditorSpec localSpec; string selectedLocalResource=""; readonly Dictionary<string,ResourceRegistry> registries=new Dictionary<string,ResourceRegistry>(StringComparer.OrdinalIgnoreCase); CatalogEntry current; Language language;
        bool selecting, initialized, refreshing, viewWarning, resizing;
        string lastAction = "ready",lastConsistencyAlert="";
        internal Func<DialogResult> PendingPrompt = null;
        internal Func<string,DialogResult> ResourceRemovePrompt = null;

        public MainForm(UiState initial, UiStateStore store, bool saveUi)
        {
            state = initial.Copy(); stateStore = store; persistUi = saveUi; language = new Language(state.Language); Theme.CopyMenuLabel=()=>language.T("copy_text");
            mods.Cache=icons; icons.Warning=message=>Report(language.T("icon_warning")+" "+message); icons.LoaderExe=()=>Path.Combine(state.Build,"tesmiolauncher.exe");
            Text = "Republic Mod Manager 0.4.65-beta  ·  "+language.T("app_dependency"); Font = new Font("Segoe UI",10); ForeColor = Theme.Ink; BackColor = Color.White;Theme.ApplyWindowChrome(this);
            AutoScaleDimensions = new SizeF(96,96); AutoScaleMode = AutoScaleMode.Dpi; Size = new Size(1600,1000); MinimumSize = new Size(1560,760); StartPosition = FormStartPosition.CenterScreen;   // 0.4.36: minimum 1560 - the section editors' detail panel needs it; RestoreWindowSize caps it to the screen
            Load += (s,e) => RestoreWindowSize();
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Tesmio.icon")) using (var icon = new Icon(stream)) Icon = (Icon)icon.Clone();
            var shell = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Margin = Padding.Empty, Padding = Padding.Empty };
            shell.RowStyles.Add(new RowStyle(SizeType.Percent,100)); shell.RowStyles.Add(new RowStyle(SizeType.Absolute,113)); Controls.Add(shell);
            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,330)); body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); shell.Controls.Add(body,0,0);
            var sidebar = new TableLayoutPanel { BackColor = Theme.Navy, Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(12,20,12,0), Margin = Padding.Empty };
            foreach (float height in new float[] {38,24,37,8}) sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
            sidebar.RowStyles.Add(new RowStyle(SizeType.Percent,100)); sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute,58));
            pluginLabel.ForeColor = Color.White; sidebar.Controls.Add(pluginLabel,0,0); searchLabel.ForeColor = Color.FromArgb(185,204,226); sidebar.Controls.Add(searchLabel,0,1);
            search.Dock = DockStyle.Fill; search.AccessibleName = "Plugin search"; search.TextChanged += (s,e) => FilterList(); sidebar.Controls.Add(search,0,2);
            mods.Dock = DockStyle.Fill; mods.Margin = new Padding(-8,0,-8,0); mods.SelectedIndexChanged += (s,e) => { if (!selecting) Run(SelectionChanged); }; sidebar.Controls.Add(mods,0,4);
            var navigation=new SidebarBar();
            navigation.Controls.Add(Sidebar("folders","folder",ChooseFolders));
            navigation.Controls.Add(Sidebar("log","document",ShowLog));
            navigation.Controls.Add(Sidebar("profiles","archive",ShowProfiles));
            languageButton=Sidebar("language","text:DE",ChooseLanguage); navigation.Controls.Add(languageButton);
            var reload=Sidebar("refresh","reload",()=>Scan(true));reload.Accent=true;navigation.Controls.Add(reload); sidebar.Controls.Add(navigation,0,5);
            body.Controls.Add(sidebar,0,0);
            page = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(26,20,24,0), Margin = Padding.Empty };
            page.RowStyles.Add(new RowStyle(SizeType.Absolute,105)); page.RowStyles.Add(new RowStyle(SizeType.Absolute,48)); page.RowStyles.Add(new RowStyle(SizeType.Absolute,36)); page.RowStyles.Add(new RowStyle(SizeType.Percent,100)); body.Controls.Add(page,1,0);
            header = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            heading.Location = new Point(0,0); description.Location = new Point(0,45); description.ForeColor=Theme.Muted;
            var enableBox = new Panel { Dock = DockStyle.Right, Width = 252 }; switchLabel.Location=new Point(0,12); activation.Location=new Point(183,4); switchNote.Location=new Point(0,42); switchNote.MaximumSize=new Size(244,0); switchNote.ForeColor=Theme.Muted;
            enableBox.Controls.AddRange(new Control[] {switchLabel,activation,switchNote});
            // The caption sits right beside the switch, whatever its translated width.
            switchLabel.SizeChanged+=(s,e)=>switchLabel.Left=Math.Max(0,activation.Left-switchLabel.Width-8);
            header.Controls.AddRange(new Control[] {heading,description,enableBox}); page.Controls.Add(header,0,0);
            activation.CheckedChanged += (s,e) => Run(ActivationChanged);
            // The header text wraps beside the switch boxes and the header row grows
            // with it, so the description never runs under the switches or into the
            // tab strip - on the first paint as well as after every resize.
            header.SizeChanged += (s,e) => LayoutHeader(); description.TextChanged += (s,e) => LayoutHeader(); heading.TextChanged += (s,e) => LayoutHeader();
            ResizeEnd += (s,e) => LayoutHeader();
            tabStrip.Dock=DockStyle.Fill; tabStrip.AutoScroll=true; page.Controls.Add(tabStrip,0,1);
            statusBar.Dock=DockStyle.Fill; statusBar.Margin=Padding.Empty; statusBar.BackColor=Theme.Pale; statusBar.Padding=new Padding(12,0,12,0); statusBar.Visible=false; statusBar.AccessibleName="status-bar";
            statusRight.AutoSize=true; statusRight.Dock=DockStyle.Right; statusRight.Font=new Font("Segoe UI",9.5f); statusRight.ForeColor=Theme.Muted; statusRight.Padding=new Padding(8,9,0,0); statusRight.UseMnemonic=false; statusRight.ContextMenuStrip=Theme.CopyMenu();
            statusFlow.Dock=DockStyle.Fill; statusFlow.WrapContents=false; statusFlow.Margin=Padding.Empty; statusFlow.Padding=new Padding(0,8,0,0);
            statusBar.Controls.Add(statusFlow); statusBar.Controls.Add(statusRight); page.Controls.Add(statusBar,0,2);
            content.Dock=DockStyle.Fill; content.FlowDirection=FlowDirection.TopDown; content.WrapContents=false; content.AutoScroll=true; content.Margin=Padding.Empty; content.Padding=new Padding(0,12,0,0); content.SizeChanged+=(s,e)=>ResizeCards(); content.ClientSizeChanged+=(s,e)=>ResizeCards(); page.Controls.Add(content,0,3);
            var footer = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=2, RowCount=1, BackColor=Theme.Chrome, Padding=new Padding(22,17,18,10), Margin=Padding.Empty };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); shell.Controls.Add(footer,0,1);
            var statuses=new FlowLayoutPanel { Dock=DockStyle.Fill, FlowDirection=FlowDirection.TopDown, WrapContents=false }; statuses.Controls.Add(status); statuses.Controls.Add(statusDetail); statusDetail.MaximumSize=new Size(440,0); footer.Controls.Add(statuses,0,0);
            var buttons = new FlowLayoutPanel { AutoSize=true, FlowDirection=FlowDirection.LeftToRight, WrapContents=false, Anchor=AnchorStyles.Right|AnchorStyles.Top };
            restoreButton=Button("restore",RestoreOriginal,false,false); restoreButton.Visible=false; restoreButton.Enabled=false; buttons.Controls.Add(restoreButton);
            var resetAll=Button("reset",ResetAll,false,true);resetAll.BackColor=Color.White;buttons.Controls.Add(resetAll); saveButton=Button("save",SaveAction,false,true);saveButton.BackColor=Color.White;buttons.Controls.Add(saveButton); buttons.Controls.Add(Button("save_start",Launch,true,true)); footer.Controls.Add(buttons,1,0);
            TranslateShell(); SetActions(false); Report(language.T("startup"));
            Shown+=(s,e)=>{ if(!initialized) Run(InitializeCatalog); };
            FormClosing+=(s,e)=>{ try { if(!ResolvePending()) e.Cancel=true; else SaveView(); } catch(Exception failure) { e.Cancel=true; ShowError(failure); } };
        }
        Button Button(string key,Action action,bool primary,bool packageAction)
        { var b=Theme.Button(language.T(key),()=>Run(action),primary); translatedButtons[b]=key; if(packageAction) actions.Add(b); return b; }
        SidebarButton Sidebar(string key,string glyph,Action action)
        {
            var button=new SidebarButton {Glyph=glyph}; button.Click+=(s,e)=>Run(action); translatedSidebar[button]=key; return button;
        }
        void LayoutHeader()
        {
            if(header==null||page==null||heading==null||description==null) return;
            int reserved=268;
            int w=Math.Max(200,header.ClientSize.Width-reserved);
            // Both labels are measured explicitly: a narrow window wraps the heading
            // too, and the description has to start below the wrapped heading.
            TextFormatFlags wrap=TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix;
            int headingHeight=Math.Max(heading.Font.Height,TextRenderer.MeasureText(heading.Text.Length==0?" ":heading.Text,heading.Font,new Size(w,Int32.MaxValue),wrap).Height);
            heading.AutoSize=false; heading.MaximumSize=Size.Empty; heading.Size=new Size(w,headingHeight+2);
            // At most five lines of description; the tooltip on the heading has the rest.
            int wrapped=description.Text.Length==0?0:TextRenderer.MeasureText(description.Text,description.Font,new Size(w,Int32.MaxValue),wrap).Height;
            int lines=Math.Max(1,(int)Math.Ceiling(wrapped/(double)Math.Max(1,description.Font.Height)));
            int shown=Math.Min(lines,5)*description.Font.Height+4;
            description.AutoSize=false; description.MaximumSize=Size.Empty; description.Location=new Point(0,heading.Bottom+6); description.Size=new Size(w,shown); description.AutoEllipsis=lines>5;
            int need=Math.Max(105,description.Bottom+18);
            if(page.RowStyles.Count>0 && (int)page.RowStyles[0].Height!=need)
            {
                page.RowStyles[0].Height=need;
                // A row style changed from inside a layout pass (a live resize) is
                // not applied until the next pass, so ask for one right after this.
                if(IsHandleCreated) BeginInvoke(new Action(()=>{ page.PerformLayout(); header.PerformLayout(); }));
                else page.PerformLayout();
            }
        }
        void TranslateShell()
        {
            pluginLabel.Text=language.T("plugins"); searchLabel.Text=language.T("search"); switchLabel.Text=language.T("enabled"); activation.AccessibleName=switchLabel.Text;
            foreach(var pair in translatedButtons) pair.Key.Text=language.T(pair.Value); UpdateStatus();
            foreach(var pair in translatedSidebar) { pair.Key.AccessibleName=language.T(pair.Value); tips.SetToolTip(pair.Key,language.T(pair.Value)); }
            languageButton.Glyph="text:"+LanguageInitials(); languageButton.Invalidate();
            tips.SetToolTip(mods,language.T("activity_help"));
        }
        string LanguageInitials()
        { string code=language.Code.Split('-')[0].ToUpperInvariant(); return code.Length>2?code.Substring(0,2):code; }
        void SetActions(bool enabled) { foreach(var button in actions) button.Enabled=enabled; restoreButton.Enabled=enabled&&session!=null&&session.Package.Installed&&session.Package.HasConfig; activation.Enabled=enabled && (resourceSession!=null || session!=null && presentation!=null && session.SwitchMode!="none" && session.Package.Visible); }
        void Run(Action action) { try { action(); } catch(Exception e) { ShowError(e); } }
        // The one header switch, "plugin active": the loader entry (or the bridge
        // list) plus the INI's own enabled key. Switching on sets both; switching off
        // only stops the loader, the INI keeps its values.
        void ActivationChanged()
        {
            if(refreshing) return; bool on=activation.Checked;
            if(resourceSession!=null) { if(session!=null&&session.LoaderSwitchApplies) session.SetLoaderEnabled(on); resourceSession.SetLoaderEnabled(on); lastAction="ready"; if(on && localSpec.ActivityField!=null) BuildLocalResourceEditor(); else { SyncActivation(); UpdateStatus(); } return; }
            if(session==null || presentation==null) return;
            if(session.LoaderSwitchApplies) { session.SetLoaderEnabled(on); lastAction="ready"; }
            if(presentation.EnabledField.Length>0 && (on || !session.LoaderSwitchApplies)) Edit(presentation.EnabledField,on?"1":"0"); else UpdateStatus();
            SyncActivation();
        }
        void SyncActivation()
        {
            refreshing=true;
            try
            {
                if(resourceSession!=null) { bool loaded=session!=null?(!session.LoaderSwitchApplies||session.LoaderEnabled):resourceSession.LoaderEnabled; activation.Checked=loaded&&resourceSession.ActivityOn; return; }
                if(session==null || presentation==null) { activation.Checked=false; return; }
                bool loader=!session.LoaderSwitchApplies || session.LoaderEnabled, ini=presentation.EnabledField.Length==0 || draft[presentation.EnabledField]=="1";
                activation.Checked=session.SwitchMode=="ini" ? ini : loader && ini;
            }
            finally { refreshing=false; }
        }
        void ShowSwitch(bool visible,string note)
        // The switch explains itself; the note only survives as a tooltip on it.
        { switchLabel.Visible=visible; activation.Visible=visible; switchNote.Visible=false; switchNote.Text=""; tips.SetToolTip(activation,note); tips.SetToolTip(switchLabel,note); }
        internal bool SwitchVisible { get { return activation.Visible; } }
        internal bool SwitchChecked { get { return activation.Checked; } }
        internal string SwitchNote { get { return tips.GetToolTip(activation) ?? ""; } }
        internal void TestSwitch(bool on) { activation.Checked=on; }
        string ErrorText(Exception e)
        {
            var rule=e as RuleException;
            if(rule==null)return language.Localize(e.Message);
            if(rule.TranslationKey=="resource_custom_needs_transport")return language.Format(rule.TranslationKey,rule.TranslationArguments);
            if(rule.TranslationKey=="error_positive_collection_required"&&rule.TranslationArguments.Length>0&&presentation!=null)
            {
                string collectionId=Convert.ToString(rule.TranslationArguments[0]);
                CollectionSpec collection=presentation.Collections.FirstOrDefault(x=>x.Id.Equals(collectionId,StringComparison.OrdinalIgnoreCase));
                GroupSpec group=collection==null?null:presentation.Groups.FirstOrDefault(x=>x.Id.Equals(collection.ResourceGroup,StringComparison.OrdinalIgnoreCase));
                TabSpec tab=group==null?null:presentation.Tabs.FirstOrDefault(x=>x.Id.Equals(group.Tab,StringComparison.OrdinalIgnoreCase));
                if(group!=null&&tab!=null)return language.Format(rule.TranslationKey,tab.Label,group.Label);
                return language.T("error_positive_collection_required_fallback");
            }
            return language.Format(rule.TranslationKey,rule.TranslationArguments);
        }
        void ShowError(Exception e) { string detail=ErrorText(e);Report(detail); MessageWindow.Show(this,language,Text,language.T("failed")+"\n\n"+language.T("technical")+":\n"+detail,MessageWindow.Kind.Error,DialogResult.OK); }
        public void Report(string text) { journal.AppendLine(DateTime.Now.ToString("HH:mm:ss")+"  "+language.Localize(text)); }
        public void InitializeCatalog() { initialized=true; Scan(false); }
        void Scan(bool ask)
        {
            if(ask && !ResolvePending()) return;
            state.WorkshopRoot=Catalog.NormalizeRoot(state.WorkshopRoot); Catalog.SchemaRoot=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings_schemas"); var notes=new List<string>(); entries.Clear(); entries.AddRange(Catalog.Scan(state.WorkshopRoot,notes));entries.AddRange(Catalog.ScanLocalEditors(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings_schemas"),notes));entries.AddRange(Catalog.ScanInstalled(state.Build,entries,notes));Catalog.ResolveAll(entries);entries.Sort((a,b)=>String.Compare(a.Name,b.Name,StringComparison.CurrentCultureIgnoreCase));
            foreach(string note in notes) Report(note);
            RefreshActivity();
            var chosen=Catalog.RestoreSelection(entries,state.SelectedId,state.SelectedSource); FilterList(); ShowEntry(chosen); FilterList();InspectStartupConsistency();
        }
        void RefreshActivity()
        {
            mods.ActiveStates.Clear(); foreach(var entry in entries) mods.ActiveStates[entry.Root]=RuntimeStatus.ConfiguredActive(entry,state.Build);
            mods.UpdateBadge=language.T("update_badge"); pendingUpdates=Catalog.MarkUpdates(entries,state.Build); mods.Invalidate();
        }
        List<string> pendingUpdates=new List<string>();
        void FilterList()
        {
            selecting=true; mods.BeginUpdate();
            try { mods.Items.Clear(); foreach(var e in entries.Where(e=> (e.Name+" "+e.Id).IndexOf(search.Text,StringComparison.CurrentCultureIgnoreCase)>=0)) mods.Items.Add(e); mods.SelectedItem=current; }
            finally { mods.EndUpdate(); selecting=false; }
        }
        void SelectionChanged()
        { var candidate=mods.SelectedItem as CatalogEntry; if(candidate==null || candidate==current) return; if(!ResolvePending()) { FilterList(); return; } ShowEntry(candidate); }
        void ShowEntry(CatalogEntry entry)
        {
            bool same=current==null ? state.SelectedSource==(entry==null?"":entry.Root) : current.Root==(entry==null?"":entry.Root);
            if(!same) state.SelectedTab="";
            current=entry; session=null; presentation=null;resourceSession=null;localSpec=null;selectedLocalResource=""; registries.Clear(); restoreButton.Visible=entry!=null&&entry.Installed&&entry.Supported&&entry.Problem.Length==0; ShowSwitch(false,""); draft.Clear(); baseline.Clear(); resets.Clear(); lastAction="ready"; SetActions(false);
            Theme.DisposeChildren(tabStrip); Theme.DisposeChildren(content); setters.Clear(); origins.Clear(); resetButtons.Clear(); UpdateLoadPath();
            refreshing=true; activation.Checked=false; refreshing=false;
            if(entry==null) { heading.Text=language.T("no_mods"); description.Text=""; tips.SetToolTip(heading,""); ShowInfo(language.T("no_mods_help")); SaveView(); UpdateStatus(); return; }
            state.SelectedId=entry.Id; state.SelectedSource=entry.Root; heading.Text=entry.Name; description.Text=""; tips.SetToolTip(heading,language.T("source")+": "+entry.Root+"\n"+entry.Version);
            if(entry.Problem.Length>0 || !entry.Supported) { ShowProblem(language.T("unsupported")+"\n\n"+language.Localize(entry.Problem)); SaveView(); UpdateStatus(); return; }
            try
            {
                if(entry.LocalEditor)
                {
                    localSpec=LocalEditorSpec.Load(entry.Root);resourceSession=new LocalResourceSession(localSpec,state.Build);resourceSession.References=new ReferenceSets(state.Build,state.WorkshopRoot,language.Code);heading.Text=localSpec.LocalizedName(language);description.Text=localSpec.LocalizedDescription(language);BuildLocalResourceEditor();SetActions(true);UpdateLoadPath();foreach(string note in resourceSession.Notes)Report(note);if(resourceSession.DisappearedExternal.Count>0)ShowDisappeared(resourceSession.DisappearedExternal);SaveView();UpdateStatus();return;
                }
                var package=entry.Installed?InstalledPlugins.Load(state.Build,entry.Target,Catalog.SchemaRoot):Package.Load(entry.Root);
                Catalog.ResolveDependencies(package.Dependencies,entries);
                foreach(string hint in package.Hints) Report(entry.Name+": "+language.Localize(hint));
                foreach(Dependency d in package.Dependencies) Report(entry.Name+": "+language.Localize(d.Note));
                if(package.Kind=="content")
                {
                    description.Text=language.T("content_package"); ShowInfo(language.T("content_package")+"\n\n"+String.Join("\n",package.Hints.Select(language.Localize))); SaveView(); UpdateStatus(); return;
                }
                session=new Session(package,state.Build); LoadDraft();
                if(package.EditorManaged)
                {
                    // The package ships a keyed editor schema: the master-detail editor owns
                    // the INI (its baseline is the package INI), Session handles DLL, loader
                    // entry, bridge list and local copy.
                    localSpec=LocalEditorSpec.Load(package.EditorSchema);resourceSession=new LocalResourceSession(localSpec,state.Build,package);resourceSession.References=new ReferenceSets(state.Build,state.WorkshopRoot,language.Code);
                    heading.Text=localSpec.LocalizedName(language);description.Text=localSpec.LocalizedDescription(language);
                    BuildLocalResourceEditor();SetActions(true);UpdateLoadPath();foreach(string note in session.Notes.Concat(resourceSession.Notes))Report(note);SaveView();UpdateStatus();return;
                }
                BuildEditor(); SetActions(true); UpdateLoadPath(); foreach(string note in session.Notes.Concat(presentation.Notes)) Report(note);
            }
            catch(Exception e) { session=null; presentation=null;resourceSession=null;localSpec=null; string detail=ErrorText(e); ShowProblem(language.T("failed")+"\n\n"+detail); Report(detail); }
            SaveView(); UpdateStatus();
        }
        // The status bar: which loader brings the DLL into the game (bridge, SML or the classic
        // plugins folder), where the files live, and what the loader saw on its last run.
        void UpdateLoadPath()
        {
            statusBar.SuspendLayout(); Theme.DisposeChildren(statusFlow); statusRight.Text="";
            if(current==null||(session==null&&resourceSession==null)){statusBar.Visible=false;statusBar.ResumeLayout();return;}
            bool installed=session!=null?session.Package.Installed:current.Installed;
            bool bridge=session!=null&&session.BridgeActive, sml=session!=null&&session.SmlActive&&!installed, loader=!bridge&&!sml, local=session!=null&&session.PreferLocal;
            LoadPathText(language.T("status_loadpath")+":",Theme.Muted,null);
            LoadPathDot(bridge,language.T("status_bridge"),language.T(bridge?"status_bridge_tip":"status_inactive_tip"));
            LoadPathDot(sml,language.T("status_sml"),language.T(sml?"status_sml_tip":"status_inactive_tip"));
            LoadPathDot(loader,language.T("status_loader"),language.T(loader?(installed?"status_loader_installed_tip":"status_loader_tip"):"status_inactive_tip"));
            string filesKey=local?"status_files_local":(bridge||sml)?"status_files_package":"status_files_plugins"; string filesTip=language.T(filesKey+"_tip");
            var gap=LoadPathText("",Theme.Muted,null); gap.Margin=new Padding(10,0,0,0);
            LoadPathText(language.T("status_files")+":",Theme.Muted,filesTip); var files=LoadPathText(language.T(filesKey),Theme.Blue,filesTip); files.Font=new Font("Segoe UI",9.5f,FontStyle.Bold);
            string target=session!=null?session.Package.Target:current.Target; DateTime? when=InstalledPlugins.LogTime(state.Build); string seen;
            if(when.HasValue&&!String.IsNullOrEmpty(target)&&InstalledPlugins.LoaderVersions(state.Build).TryGetValue(target,out seen)) statusRight.Text=language.Format("status_last_seen",when.Value.ToString("g"),seen);
            else if(when.HasValue) statusRight.Text=language.Format("status_last_seen_not",when.Value.ToString("g"));
            tips.SetToolTip(statusRight,language.T("status_last_seen_tip"));
            statusBar.Visible=true; statusBar.ResumeLayout();
        }
        Label LoadPathText(string text,Color color,string tip)
        {var l=Theme.Label(text,9.5f,false);l.ForeColor=color;l.Margin=new Padding(0,0,5,0);if(tip!=null)tips.SetToolTip(l,tip);statusFlow.Controls.Add(l);return l;}
        void LoadPathDot(bool on,string text,string tip)
        {
            // A drawn 13 px dot, vertically centred on the text line, close to its caption (0.4.13).
            Color fill=on?Color.FromArgb(45,202,83):Color.FromArgb(196,203,214);
            var dot=new Panel{Size=new Size(13,13),Margin=new Padding(14,4,4,0),BackColor=Color.Transparent,AccessibleName="status-dot:"+(on?"on":"off")};
            dot.Paint+=(s,e)=>{e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var b=new SolidBrush(fill))e.Graphics.FillEllipse(b,0,0,12,12);};
            tips.SetToolTip(dot,tip);statusFlow.Controls.Add(dot);
            LoadPathText(text,on?Theme.Ink:Theme.Muted,tip);
        }
        Field PresentedField(string id)
        {return presentation==null?null:presentation.Fields.Select(x=>x.Field).FirstOrDefault(x=>x.Id==id);}
        string ComparableValue(string id,string value)
        {
            Field field=PresentedField(id);if(field==null)return value;
            if(field.Type=="decimal"||field.Type=="integer")value=NumberInput.Canonical(value);
            return field.Normalize(value);
        }
        bool SameDraftValue(string id,string left,string right)
        {try{return ComparableValue(id,left)==ComparableValue(id,right);}catch(FormatException){return left==right;}}
        bool IsPackageDefault(string id,string value)
        {try{return session.Package.IsDefault(id,ComparableValue(id,value));}catch(FormatException){return false;}}
        Dictionary<string,string> DraftDifferences()
        {
            var values=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            foreach(var pair in draft)
            {
                string value=ComparableValue(pair.Key,pair.Value);
                if(!session.Package.IsDefault(pair.Key,value))values[pair.Key]=value;
            }
            return values;
        }
        Ini DraftConfig() {return new Ini(session.Package.Defaults.Render(DraftDifferences()));}
        void EnsurePresentationValues(bool baselineToo)
        {
            foreach(var spec in presentation.Fields)
            {
                string id=spec.Field.Id;
                if(!draft.ContainsKey(id))draft[id]=spec.Field.DefaultValue;
                if(baselineToo&&!baseline.ContainsKey(id))baseline[id]=spec.Field.DefaultValue;
            }
        }
        void RebuildPresentation()
        {presentation=new Presentation(session.Package,language,DraftConfig(),registries);EnsurePresentationValues(false);}
        void LoadRegistries()
        {registries.Clear();foreach(CollectionSpec collection in session.Package.Collections)registries[collection.Id]=ResourceRegistry.Load(session.Build,collection);}
        void LoadDraft()
        {
            draft.Clear();baseline.Clear();resets.Clear();var config=new Ini(session.Effective());
            foreach(var pair in config.Values)
            {
                // A generic (INI-derived) schema shows the bare value; an inline comment
                // such as "; (stock 121)" stays in the file but not in the input box.
                string value=pair.Value;Field field=session.Package.Fields.FirstOrDefault(x=>x.Id==pair.Key);
                if(field!=null&&field.Lenient)value=GenericSchema.StripComment(value);
                draft[pair.Key]=value;baseline[pair.Key]=value;
            }
            LoadRegistries();presentation=new Presentation(session.Package,language,config,registries);EnsurePresentationValues(true);
        }
        // The window opens at 1600x1000 design units, or at the size it had when the view
        // was last saved (physical pixels, so it is applied after DPI scaling, in Load).
        // Both are clamped to the working area of the screen the window sits on.
        void RestoreWindowSize()
        {
            var area=Screen.FromControl(this).WorkingArea;
            // A minimum wider than the screen (1080p at 125 %, or 150 %) would leave a window nobody can
            // shrink; below the full minimum the detail panels fall back to a single column (0.4.36).
            MinimumSize=new Size(Math.Min(MinimumSize.Width,area.Width),Math.Min(MinimumSize.Height,area.Height));
            int w=state.WindowWidth>0?state.WindowWidth:Width, h=state.WindowHeight>0?state.WindowHeight:Height;
            Size=new Size(Math.Max(MinimumSize.Width,Math.Min(w,area.Width-24)),Math.Max(MinimumSize.Height,Math.Min(h,area.Height-24)));
            if(StartPosition==FormStartPosition.CenterScreen) Location=new Point(area.Left+(area.Width-Width)/2,area.Top+(area.Height-Height)/2);
            if(state.WindowMaximized) WindowState=FormWindowState.Maximized;
        }
        void RememberWindowSize()
        {
            if(!IsHandleCreated || WindowState==FormWindowState.Minimized) return;
            var size=WindowState==FormWindowState.Normal?Size:RestoreBounds.Size;
            state.WindowWidth=size.Width; state.WindowHeight=size.Height; state.WindowMaximized=WindowState==FormWindowState.Maximized;
        }
        void SaveView()
        { if(!persistUi || stateStore==null || !initialized) return; try { RememberWindowSize(); stateStore.Save(state); } catch(Exception e) { if(!viewWarning) { viewWarning=true; Report(language.T("view_warning")+" "+e.Message); } } }
        void ChangeLanguage(string code)
        {
            state.Language=code; language=new Language(code);
            TranslateShell();
            // A package-backed editor has both sessions; the editor decides the layout.
            if(resourceSession!=null){if(session!=null)RebuildPresentation();heading.Text=localSpec.LocalizedName(language);description.Text=localSpec.LocalizedDescription(language);BuildLocalResourceEditor();}
            else if(session!=null) {LoadRegistries();RebuildPresentation();BuildEditor();} else ShowEntry(current);
            SaveView();
        }
        void ChooseLanguage()
        {
            using(var dialog=new Form {Text=language.T("language"),Font=Font,Size=new Size(420,390),FormBorderStyle=FormBorderStyle.FixedDialog,StartPosition=FormStartPosition.CenterParent,MaximizeBox=false,MinimizeBox=false,ShowInTaskbar=false,Icon=Icon})
            {
                Theme.ApplyWindowChrome(dialog);
                var list=new ListBox {Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,Font=new Font("Segoe UI",12),ItemHeight=36,IntegralHeight=false};
                foreach(var item in Language.Available()) list.Items.Add(item); list.DisplayMember="Value"; list.ValueMember="Key";
                for(int i=0;i<list.Items.Count;i++) if(((KeyValuePair<string,string>)list.Items[i]).Key==state.Language) list.SelectedIndex=i;
                if(list.SelectedIndex<0) list.SelectedIndex=0;
                var row=new FlowLayoutPanel {Dock=DockStyle.Bottom,Height=62,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(8)};
                var cancel=Theme.Button(language.T("cancel"),()=>{dialog.DialogResult=DialogResult.Cancel;dialog.Close();},false);
                var apply=Theme.Button(language.T("apply"),()=>{dialog.DialogResult=DialogResult.OK;dialog.Close();},true);
                row.Controls.Add(apply); row.Controls.Add(cancel); dialog.AcceptButton=apply; dialog.CancelButton=cancel;
                list.DoubleClick+=(s,e)=>{if(list.SelectedItem!=null){dialog.DialogResult=DialogResult.OK;dialog.Close();}};
                dialog.Controls.Add(list); dialog.Controls.Add(row);
                if(dialog.ShowDialog(this)==DialogResult.OK && list.SelectedItem!=null) ChangeLanguage(((KeyValuePair<string,string>)list.SelectedItem).Key);
            }
        }
        bool HasPending { get { return resourceSession!=null?(resourceSession.Dirty||session!=null&&(session.LoaderChanged||session.LocalCopyChanged)):session!=null && (session.LoaderChanged || session.LocalCopyChanged || resets.Count>0 || draft.Count!=baseline.Count || draft.Any(p=>!baseline.ContainsKey(p.Key)||!SameDraftValue(p.Key,p.Value,baseline[p.Key]))); } }
        DialogResult Question(string text,bool allowDiscard)
        {
            // The RMM message window (0.4.38); Yes/No keep their meaning "save" / "discard".
            var buttons=allowDiscard
                ?new[]{new KeyValuePair<DialogResult,string>(DialogResult.Yes,language.T("yes")),new KeyValuePair<DialogResult,string>(DialogResult.No,language.T("no")),new KeyValuePair<DialogResult,string>(DialogResult.Cancel,language.T("cancel"))}
                :new[]{new KeyValuePair<DialogResult,string>(DialogResult.OK,language.T("confirm")),new KeyValuePair<DialogResult,string>(DialogResult.Cancel,language.T("cancel"))};
            return MessageWindow.Show(this,language,language.T("confirm"),text,MessageWindow.Kind.Question,buttons);
        }
        bool ResolvePending()
        { if(!HasPending) return true; DialogResult choice=PendingPrompt!=null?PendingPrompt():Question(language.T("pending"),true); if(choice==DialogResult.Cancel) return false; if(choice==DialogResult.Yes) return SaveCurrent(); return true; }
        Dictionary<string,string> Prospective()
        {
            if(session==null) throw new InvalidOperationException(language.T("unavailable"));
            var desired=new Dictionary<string,string>(draft,StringComparer.OrdinalIgnoreCase);
            foreach(var field in presentation.Fields.Select(x=>x.Field).Where(f=>f.Type!="readonly"))
            {
                if(resets.Contains(field.Id))desired[field.Id]=field.DefaultValue;
                else desired[field.Id]=field.Normalize(NumberInput.Canonical(desired[field.Id]));
            }
            var values=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            foreach(var pair in desired)if(!session.Package.IsDefault(pair.Key,pair.Value))values[pair.Key]=pair.Value;
            ConfigRules.Validate(session.Package,new Ini(session.Package.Defaults.Render(values))); return values;
        }
        void Collect()
        { var values=Prospective(); session.Overrides.Clear(); foreach(var p in values) session.Overrides[p.Key]=p.Value; }
        // Asked only when saving would actually write a DLL into plugins\: never
        // for installed plugins, never under SML or the bridge (they load the
        // package copy), and never when the copy in plugins\ is already this one.
        bool NativeDllChanges()
        { return session!=null && !session.Package.Installed && !session.SmlActive && !session.BridgeActive && SafeFiles.HashFile(session.LocalDll)!=SafeFiles.Hash(session.Package.Dll); }
        bool ConfirmNativeTrust()
        {
            string text=language.T("trust")+"\n\n"+session.Package.Name+" "+session.Package.Version+"\n"+language.T("source")+": "+session.Package.Root+"\nSHA-256: "+SafeFiles.Hash(session.Package.Dll)+"\n\n"+language.T("target")+": "+session.Build+"\\plugins\n\n"+language.T("trust_question");
            return MessageWindow.Show(this,language,language.T("confirm"),text,MessageWindow.Kind.Warning,new[]{new KeyValuePair<DialogResult,string>(DialogResult.OK,language.T("confirm")),new KeyValuePair<DialogResult,string>(DialogResult.Cancel,language.T("cancel"))})==DialogResult.OK;
        }
        bool SaveAndDeploy(Action guard,Func<bool> approveNative)
        {
            Collect(); guard(); session.AssertUnchanged();
            bool approved=false;
            if(session.LocalCopyChanged)
            {
                string text=LocalCopyText();
                if((LocalCopyPrompt!=null?LocalCopyPrompt(text):Question(text,false))!=DialogResult.OK) return false;
                approved=session.PreferLocal;    // the file list named the DLL and its hash
            }
            if(NativeDllChanges() && !approved && !approveNative()) return false;
            string result=session.Commit(true,guard); lastAction="saved"; Report(language.T("saved")+" "+language.T("backup")+": "+result);
            LoadDraft(); BuildEditor(); SaveView(); RefreshActivity(); return true;
        }
        bool SaveCurrent()
        {
            if(resourceSession!=null)
            {
                LocalResourceGuard.Check(resourceSession);
                if(session!=null)
                {
                    // Package-backed editor: DLL, loader entry, bridge list and local copy go
                    // first (Session never touches this INI), the editor's INI afterwards.
                    RuntimeGuard.Check(session);session.AssertUnchanged();bool approved=false;
                    if(session.LocalCopyChanged){string text=LocalCopyText();if((LocalCopyPrompt!=null?LocalCopyPrompt(text):Question(text,false))!=DialogResult.OK)return false;approved=session.PreferLocal;}
                    if(NativeDllChanges()&&!approved&&!ConfirmNativeTrust())return false;
                    session.Commit(true,()=>RuntimeGuard.Check(session));
                }
                string result=resourceSession.Commit(()=>LocalResourceGuard.Check(resourceSession));
                if(session!=null)session=new Session(session.Package,state.Build);
                lastAction="saved";Report(language.T("saved")+" "+language.T("backup")+": "+result);BuildLocalResourceEditor();SaveView();RefreshActivity();return true;
            }
            return SaveAndDeploy(()=>RuntimeGuard.Check(session),ConfirmNativeTrust);
        }
        void SaveAction() { SaveCurrent(); }
        void Launch()
        {
            if(resourceSession!=null){if(!SaveCurrent())return;LocalResourceGuard.Check(resourceSession);EnsureGlobalResourceConsistency();if(!ConfirmGameVersion())return;Process.Start(new ProcessStartInfo(SafeFiles.Child(resourceSession.Build,"tesmiolauncher.exe")){Arguments=LauncherOptions.Arguments,WorkingDirectory=resourceSession.Build,UseShellExecute=true});Report(language.T("launched"));Close();return;}
            SaveAndLaunch(()=>RuntimeGuard.Check(session),ConfirmNativeTrust,()=>{EnsureGlobalResourceConsistency();Process.Start(new ProcessStartInfo(SafeFiles.Child(session.Build,"tesmiolauncher.exe")) { Arguments=LauncherOptions.Arguments, WorkingDirectory=session.Build, UseShellExecute=true });});
        }
        void InspectStartupConsistency()
        {
            var messages=new List<string>(); var startup=new List<string>();
            try
            {
                foreach(string duplicate in Catalog.DuplicateDlls(entries,state.Build)) startup.Add(language.Format("duplicate_dll",duplicate));
                // Updates are news, not problems: reported in the log and the status line, never as a blocking dialog.
                if(pendingUpdates.Count>0) Report(language.Format("updates_pending",String.Join(", ",pendingUpdates)));
                string version; bool unsupported=GameVersion.Warn(state.Build,out version); version=language.Localize(version); Report(language.Format("game_version",version));
                if(GameVersion.CheckEnabled&&unsupported) startup.Add(language.Format("game_version_unknown",version));
                foreach(var local in entries.Where(x=>x.LocalEditor))
                { try { var check=new LocalResourceSession(LocalEditorSpec.Load(local.Root),state.Build);if(check.DisappearedExternal.Count>0)messages.Add(local.Name+": "+language.Format("resource_external_disappeared",String.Join(", ",check.DisappearedExternal))); } catch(IOException) { } }
                messages.AddRange(ResourceConsistency.ValidateReferences(state.Build,entries).Select(language.Localize));
            }
            catch(Exception e){messages.Add(e.Message);}
            if(messages.Count==0&&startup.Count==0){lastConsistencyAlert="";return;}
            var parts=new List<string>(); if(startup.Count>0) parts.Add(language.T("startup_warnings")+"\n\n"+String.Join("\n",startup.Distinct())); if(messages.Count>0) parts.Add(language.T("resource_consistency_failed")+"\n\n"+String.Join("\n",messages.Distinct()));
            string text=String.Join("\n\n",parts);foreach(string message in startup.Concat(messages))Report(message);
            if(persistUi&&text!=lastConsistencyAlert){lastConsistencyAlert=text;MessageWindow.Show(this,language,Text,text,MessageWindow.Kind.Warning,DialogResult.OK);}
        }
        void ShowDisappeared(IEnumerable<string> ids)
        {string text=language.Format("resource_external_disappeared",String.Join(", ",ids));Report(text);if(persistUi&&text!=lastConsistencyAlert){lastConsistencyAlert=text;MessageWindow.Show(this,language,Text,text,MessageWindow.Kind.Warning,DialogResult.OK);}}
        void EnsureGlobalResourceConsistency()
        {
            ISet<string> ids=null;if(resourceSession!=null)ids=new HashSet<string>(new LooseIni(resourceSession.Effective()).Entries(localSpec.ListSection).Select(x=>x.Key),StringComparer.OrdinalIgnoreCase);
            var issues=ResourceConsistency.ValidateReferences(state.Build,entries,ids);if(issues.Count>0)throw new IOException(language.T("resource_start_blocked")+"\n"+String.Join("\n",issues.Select(language.Localize)));
        }
        bool SaveAndLaunch(Action guard,Func<bool> approveNative,Action start)
        {
            if(!SaveAndDeploy(guard,approveNative))return false;
            if(!ConfirmGameVersion())return false;
            guard();start();Report(language.T("launched"));Close();return true;
        }
        void RestoreOriginal()
        {
            if(session==null||!session.Package.Installed||!session.Package.HasConfig)return;
            if(Question(language.T("restore_question"),false)!=DialogResult.OK)return;
            RuntimeGuard.Check(session);session.AssertUnchanged();
            string result=session.RestoreOriginal(()=>RuntimeGuard.Check(session));lastAction="saved";Report(language.T("restored")+" "+language.T("backup")+": "+result);
            LoadDraft();BuildEditor();SaveView();RefreshActivity();
        }
        internal Func<DialogResult> VersionPrompt = null;
        bool ConfirmGameVersion()
        {
            string version; bool unsupported=GameVersion.Warn(state.Build,out version);
            if(!GameVersion.CheckEnabled||!unsupported) return true;
            Report(language.Format("game_version_unknown",version));
            // Non-interactive runs (tests, snapshots) never block on a dialog.
            if(VersionPrompt!=null) return VersionPrompt()==DialogResult.OK;
            if(!persistUi) return true;
            return Question(language.Format("game_version_confirm",version),false)==DialogResult.OK;
        }
        void ResetAll()
        {
            if(resourceSession!=null){if(Question(language.T(localSpec.IsList?"list_reset_question":"resource_reset_question"),false)!=DialogResult.OK)return;resourceSession.Reset();selectedLocalResource="";BuildLocalResourceEditor();return;}
            if(session==null||Question(language.T("reset_question"),false)!=DialogResult.OK)return;
            draft.Clear();foreach(var pair in session.Package.Defaults.Values)draft[pair.Key]=pair.Value;
            resets.Clear();foreach(var f in session.Package.Fields.Where(f=>f.Type!="readonly"))resets.Add(f.Id);
            RebuildPresentation();BuildEditor();
        }
        void ChooseFolders()
        {
            using(var form=new Form { Text=language.T("folders"), Font=Font, Size=new Size(820,260), StartPosition=FormStartPosition.CenterParent, MinimizeBox=false, MaximizeBox=false, FormBorderStyle=FormBorderStyle.FixedDialog, ShowInTaskbar=false, Icon=Icon })
            {
                var table=new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=3, RowCount=3, Padding=new Padding(18) }; table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,160)); table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,105)); form.Controls.Add(table);
                var paths=new[] {new TextBox {Text=state.Build,Dock=DockStyle.Fill},new TextBox {Text=state.WorkshopRoot,Dock=DockStyle.Fill}};
                for(int i=0;i<2;i++) { int index=i; table.RowStyles.Add(new RowStyle(SizeType.Absolute,48)); table.Controls.Add(Theme.Label(language.T(i==0?"build":"workshop"),10,false),0,i); table.Controls.Add(paths[i],1,i); table.Controls.Add(Theme.Button(language.T("browse"),()=>{using(var browse=new FolderBrowserDialog {SelectedPath=paths[index].Text,ShowNewFolderButton=false}) if(browse.ShowDialog(form)==DialogResult.OK) paths[index].Text=browse.SelectedPath;},false),2,i); }
                table.Controls.Add(Theme.Button(language.T("apply"),()=>Run(()=>{ string b=Path.GetFullPath(paths[0].Text), w=Catalog.NormalizeRoot(paths[1].Text); if(!ResolvePending()) return; state.Build=b; state.WorkshopRoot=w; form.DialogResult=DialogResult.OK; }),true),2,2);
                if(form.ShowDialog(this)==DialogResult.OK) Scan(false);
            }
        }
        void ShowLog()
        { using(var form=new LogWindow(language,state.Build,()=>journal.ToString(),Font,Icon)) form.ShowDialog(this); }
        // Profiles and restore points write through the same transaction as a save;
        // pending edits are settled first and the list is rescanned afterwards.
        void ShowProfiles()
        {
            if(!ResolvePending()) return;
            using(var form=new ProfilesWindow(language,state.Build,Font,Icon)) { form.ShowDialog(this); foreach(string note in form.Journal) Report(note); if(form.Changed) Scan(false); }
        }
        protected override void Dispose(bool disposing) { if(disposing) { tips.Dispose(); icons.Dispose(); if(Icon!=null) Icon.Dispose(); } base.Dispose(disposing); }
        internal int ModCount {get{return mods.Items.Count;}}
        internal string SelectedSource {get{return current==null?"":current.Root;}}
        internal string DisplayedValue(string id) {string value;return draft.TryGetValue(id,out value)?value:null;}
        internal void SelectIndex(int index) {mods.SelectedIndex=index;}
        internal bool HasEditor {get{return resourceSession!=null||session!=null && session.Package.Visible;}}
        internal int TabCount {get{return presentation!=null?presentation.Tabs.Count:localSpec!=null?localSpec.Tabs.Count:0;}}
        internal string SelectedTabId {get{return state.SelectedTab;}}
        internal int ActionCount {get{return actions.Count;}}
        internal string LanguageGlyph {get{return languageButton.Glyph;}}
        internal bool Activity(string root) {bool value;return mods.ActiveStates.TryGetValue(root,out value)&&value;}
        internal string SelectedTab {get{return state.SelectedTab;}}
        internal bool IsDirty {get{return HasPending;}}
        internal int ResourceChoiceCount {get{return registries.Values.Sum(x=>x.Options.Count);}}
        internal int ResourceUsedCount
        {get{if(session==null)return 0;Ini config=DraftConfig();int total=0;foreach(CollectionSpec c in session.Package.Collections){ResourceRegistry registry;if(registries.TryGetValue(c.Id,out registry))total+=registry.Options.Count(x=>CollectionRules.Names(c,config).Any(n=>n.Equals(x.Id,StringComparison.OrdinalIgnoreCase)));}return total;}}
        internal void TestEdit(string id,string value) {Edit(id,value);}
        internal void TestAddResource(string id,string first,string second,string third,string fourth)
        {var c=session.Package.Collections.First();string[] values={first,second,third,fourth};AddResource(c,id,c.TargetSections.Select((x,i)=>new {x,i}).ToDictionary(x=>x.x,x=>values[x.i],StringComparer.OrdinalIgnoreCase));}
        internal bool TestRemoveResource(string id) {return RequestRemoveResource(session.Package.Collections.First().Id,id);}
        internal Form TestResourceDialog() {return BuildResourceDialog(session.Package.Collections.First());}
        internal void TestResetField(string id) {ResetField(id);}
        internal void TestLanguage(string code) {ChangeLanguage(code);}
        internal void TestSearch(string query) {search.Text=query;}
        internal string StatusText {get{return status.Text;}}
        internal bool StatusValid {get{return status.Text==language.T("valid")||status.Text==language.T("unsaved");}}
        internal bool SaveEnabled {get{return saveButton!=null&&saveButton.Enabled;}}
        internal string HeadingText {get{return heading.Text;}}
        internal string StatusDetailText {get{return statusDetail.Text;}}
        internal void TestCommit(Action guard) {Collect();session.Commit(false,guard);LoadDraft();BuildEditor();}
        internal bool TestApply(Action guard) {return SaveAndDeploy(guard,()=>true);}
        internal bool TestSaveAndLaunch(Action guard,Action start) {return SaveAndLaunch(guard,()=>true,start);}
        internal bool HasLocalResourceEditor {get{return resourceSession!=null;}}
        internal int LocalResourceCount {get{return resourceSession==null?0:resourceSession.Items().Count;}}
        internal void TestAddLocalResource(string id,string template,string display,string transport){resourceSession.Add(id,template,display,transport);selectedLocalResource=id;BuildLocalResourceEditor();}
        internal void TestAddListItem(string id,string raw){resourceSession.AddRaw(id,raw);selectedLocalResource=id;BuildLocalResourceEditor();}
        internal void TestAddSection(string name,Dictionary<string,string> values){resourceSession.AddSection(name,values);selectedLocalResource=name;BuildLocalResourceEditor();}
        internal string LocalNextValue(string field){return resourceSession.NextValue(localSpec.Fields.First(x=>x.Id==field));}
        internal void TestSetLocalField(string id,string field,string value){resourceSession.SetField(id,localSpec.Fields.First(x=>x.Id==field),value);UpdateStatus();}
        internal bool TestLocalOwned(string id){return resourceSession.Items().First(x=>x.Id==id).Owned;}
        internal bool TestSaveLocal(Action guard){string result=resourceSession.Commit(guard);BuildLocalResourceEditor();RefreshActivity();return result.Length>0;}
        internal string LocalEffective(){return resourceSession.Effective();}
        internal void TestRemoveLocalResource(string id){if(localSpec.HidesOriginals)RemoveLocalListItem(id,resourceSession.Items().First(x=>x.Id==id).Owned);else RemoveLocalResource(id);}
        internal void TestSetGlobal(string field,string value){resourceSession.SetGlobal(localSpec.Fields.First(x=>x.Id==field),value);UpdateStatus();}
        internal void TestSetListColumn(string id,int column,string value){string[] parts=localSpec.TupleColumns(resourceSession.Items().First(x=>x.Id==id).ListValue);parts[column]=value;resourceSession.SetListRaw(id,ListTuple.Render(parts));BuildLocalResourceEditor();}
        internal List<string> LocalHidden(){return resourceSession.SuppressedIds();}
    }
}
