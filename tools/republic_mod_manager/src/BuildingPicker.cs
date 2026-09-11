using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TesmioAutoload
{
    // A "lines" field: a multi-line box with a live line counter, a drag grip and
    // an expand toggle below it, and an optional picker button beside it.
    sealed class LinesBox : Panel
    {
        // Footer (0.4.50): 8 px grip, 5 px air, the 34 px button row, 6 px below - the button's top
        // border used to sit right under the grip and looked cut off.
        public const int MinimumBoxHeight = 132, Footer = 53, RowTop = 13;
        public readonly TextBox Box = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true, WordWrap = false, Font = new Font("Consolas", 9.5f) };
        readonly Label count = Theme.Label("", 9, false); readonly Panel grip = new Panel(); readonly VectorButton expand; readonly Button pick; readonly ToolTip tips = new ToolTip();
        public Func<int, string> CountText = n => n.ToString();
        public Action Pick;
        public int MaximumLines;
        bool expanded; int dragStart, dragHeight;
        public LinesBox(bool withPicker, string pickLabel, string expandLabel)
        {
            Height = MinimumBoxHeight + Footer; Margin = Padding.Empty;
            Box.Location = new Point(0, 0); Box.Height = MinimumBoxHeight; Controls.Add(Box);
            Box.TextChanged += (s, e) => UpdateCount();
            count.ForeColor = Theme.Muted; count.AutoSize = true; Controls.Add(count);
            grip.Cursor = Cursors.SizeNS; grip.Height = 8; grip.BackColor = Color.Transparent; Controls.Add(grip);
            grip.Paint += (s, e) => { using (var pen = new Pen(Theme.Line)) for (int i = 0; i < 3; i++) e.Graphics.DrawLine(pen, grip.Width / 2 - 14, 2 + i * 2, grip.Width / 2 + 14, 2 + i * 2); };
            grip.MouseDown += (s, e) => { dragStart = Cursor.Position.Y; dragHeight = Box.Height; grip.Capture = true; };
            grip.MouseMove += (s, e) => { if (grip.Capture) SetBoxHeight(dragHeight + Cursor.Position.Y - dragStart); };
            grip.MouseUp += (s, e) => { grip.Capture = false; };
            // Footer: the counter on the left, the picker button in the middle, the
            // expand/shrink toggle (plus/minus) on the right.
            expand = new VectorButton { Symbol = "plus", Size = new Size(22, 22), GlyphScale = 1.1f, TabStop = false, AccessibleName = expandLabel, Cursor = Cursors.Hand };
            expand.Click += (s, e) => { expanded = !expanded; SetBoxHeight(expanded ? ContentHeight() : MinimumBoxHeight); };
            tips.SetToolTip(expand, expandLabel); Controls.Add(expand);
            if (withPicker)
            {
                pick = Theme.Button(pickLabel, () => { if (Pick != null) Pick(); }, false); pick.AutoSize = false; pick.Height = 34; pick.MinimumSize = Size.Empty; pick.Padding = new Padding(4, 0, 4, 0); pick.AccessibleName = pickLabel; pick.TextAlign = ContentAlignment.MiddleCenter;
                pick.Width = TextRenderer.MeasureText(pickLabel, pick.Font).Width + 36;
                Controls.Add(pick);
            }
            Layout += (s, e) => Arrange(); UpdateCount();
        }
        public override string Text { get { return Box.Text; } set { Box.Text = value ?? ""; UpdateCount(); } }
        public int BoxHeight { get { return Box.Height; } set { SetBoxHeight(value); } }
        public int LineCount { get { return Box.Text.Replace("\r\n", "\n").Split('\n').Count(l => l.Trim().Length > 0); } }
        int ContentHeight() { int lines = Math.Max(6, Box.Text.Replace("\r\n", "\n").Split('\n').Length); return Math.Max(MinimumBoxHeight, lines * Box.Font.Height + 14); }
        void SetBoxHeight(int height)
        {
            height = Math.Max(MinimumBoxHeight, Math.Min(height, Math.Max(MinimumBoxHeight, ContentHeight())));
            if (Box.Height == height) return;
            Box.Height = height; expanded = height >= ContentHeight(); expand.Symbol = expanded ? "minus" : "plus"; expand.Invalidate(); Height = height + Footer; Arrange();
        }
        void Arrange()
        {
            Box.Width = Math.Max(100, Width);
            grip.Location = new Point(0, Box.Bottom); grip.Width = Box.Width;
            // Footer: counter left, picker button right, the expand toggle only while there is more to show.
            bool expandable = ContentHeight() > MinimumBoxHeight; expand.Visible = expandable;
            count.Location = new Point(0, Box.Bottom + RowTop + (34 - count.Height) / 2);
            expand.Location = new Point(Box.Width - 22, Box.Bottom + RowTop + 6);
            if (pick != null)
            {
                // Measured here, not in the constructor: the real font arrives only once the box is parented.
                pick.Width = TextRenderer.MeasureText(pick.Text, pick.Font).Width + 36;
                pick.Location = new Point(Math.Max(count.Right + 8, Box.Width - (expandable ? 30 : 0) - pick.Width), Box.Bottom + RowTop);
            }
        }
        void UpdateCount()
        {
            int n = LineCount; count.Text = CountText(n); count.ForeColor = MaximumLines > 0 && n > MaximumLines ? Theme.Danger : Theme.Muted;
            if (expanded) SetBoxHeight(ContentHeight()); Arrange();
        }
    }

    // Groups of check boxes with collapsible headers, laid out in columns.
    sealed class GroupedCheckList : Panel
    {
        sealed class Group { public string Key, Title; public Panel Header; public Label HeaderText; public FlowLayoutPanel Items; public int Total; }
        readonly List<Group> groups = new List<Group>(); readonly Dictionary<string, CheckBox> boxes = new Dictionary<string, CheckBox>(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> Collapsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public Action<string, bool> Toggled;
        public int Columns = 3;
        public GroupedCheckList() { AutoScroll = true; BackColor = Color.White; BorderStyle = BorderStyle.FixedSingle; DoubleBuffered = true; }
        public void Clear() { Theme.DisposeChildren(this); groups.Clear(); boxes.Clear(); }
        public void AddGroup(string key, string title)
        {
            var header = new Panel { Height = 30, BackColor = Theme.Chrome, Cursor = Cursors.Hand, Margin = Padding.Empty };
            var text = Theme.Label("", 10, true); text.Location = new Point(8, 6); text.AutoSize = true; header.Controls.Add(text);
            var items = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, Padding = new Padding(8, 4, 0, 6), Margin = Padding.Empty, BackColor = Color.White };
            var group = new Group { Key = key, Title = title, Header = header, HeaderText = text, Items = items };
            EventHandler toggle = (s, e) => { if (Collapsed.Contains(key)) Collapsed.Remove(key); else Collapsed.Add(key); Relayout(); };
            header.Click += toggle; text.Click += toggle;
            groups.Add(group); Controls.Add(header); Controls.Add(items);
        }
        public void AddItem(string groupKey, string id, string text, bool isChecked, bool enabled, string tooltip, ToolTip tips)
        {
            Group group = groups.First(g => g.Key == groupKey);
            var box = new CheckBox { Text = text, Checked = isChecked, Enabled = enabled, AutoSize = false, Height = 24, Margin = new Padding(0, 1, 6, 1), AccessibleName = "building:" + id, Tag = id, UseMnemonic = false, AutoEllipsis = true };
            if (!enabled) box.ForeColor = Theme.Muted;
            if (tooltip.Length > 0 && tips != null) tips.SetToolTip(box, tooltip);
            box.CheckedChanged += (s, e) => { if (Toggled != null && box.Focused) Toggled(id, box.Checked); };
            group.Items.Controls.Add(box); boxes[id] = box; group.Total++;
        }
        // Visibility is tracked here, not read back from the controls: Control.Visible
        // is false for every child while the window itself is not shown yet.
        readonly HashSet<string> hidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public bool Contains(string id) { return boxes.ContainsKey(id); }
        public void SetVisible(string id, bool visible) { CheckBox box; if (!boxes.TryGetValue(id, out box)) return; box.Visible = visible; if (visible) hidden.Remove(id); else hidden.Add(id); }
        public bool IsVisible(string id) { return boxes.ContainsKey(id) && !hidden.Contains(id); }
        public void Remove(string id) { CheckBox box; if (!boxes.TryGetValue(id, out box)) return; foreach (Group g in groups) if (g.Items.Controls.Contains(box)) { g.Items.Controls.Remove(box); g.Total--; } boxes.Remove(id); hidden.Remove(id); box.Dispose(); }
        public int VisibleCount { get { return boxes.Keys.Count(id => !hidden.Contains(id)); } }
        public IEnumerable<string> Ids { get { return boxes.Keys; } }
        public void ExpandAll() { Collapsed.Clear(); Relayout(); }
        public void CollapseAll() { foreach (Group g in groups) Collapsed.Add(g.Key); Relayout(); }
        public void Relayout()
        {
            SuspendLayout();
            int width = Math.Max(300, ClientSize.Width - 4), y = -VerticalScroll.Value, column = Math.Max(140, (width - 16) / Math.Max(1, Columns) - 8);
            foreach (Group group in groups)
            {
                int shown = group.Items.Controls.Cast<Control>().Count(c => !hidden.Contains((string)c.Tag));
                bool collapsed = Collapsed.Contains(group.Key);
                group.Header.Visible = shown > 0; group.Items.Visible = shown > 0 && !collapsed;
                if (shown == 0) continue;
                group.HeaderText.Text = (collapsed ? "▸ " : "▾ ") + group.Title + "  (" + shown + ")";
                group.Header.Location = new Point(0, y); group.Header.Width = width; y += group.Header.Height;
                if (!collapsed)
                {
                    foreach (Control c in group.Items.Controls) c.Width = column;
                    group.Items.Location = new Point(0, y); group.Items.MaximumSize = new Size(width, 0); group.Items.MinimumSize = new Size(width, 0); group.Items.PerformLayout(); y += group.Items.Height;
                }
                y += 4;
            }
            ResumeLayout(true);
        }
        protected override void OnResize(EventArgs e) { base.OnResize(e); if (groups.Count > 0) Relayout(); }
    }

    // The building picker: filters and a counter on top, the selected buildings in
    // the middle, every other building below, grouped by kind.
    sealed class BuildingPickerWindow : Form
    {
        readonly Language language; readonly List<BuildingEntry> all; readonly List<string> initial;
        readonly Dictionary<string, string> usedElsewhere; readonly Dictionary<string, BuildingEntry> byTarget = new Dictionary<string, BuildingEntry>(StringComparer.OrdinalIgnoreCase);
        readonly TextBox search = new TextBox(); readonly ComboBox origin = new ComboBox(), kind = new ComboBox(); readonly CheckBox obsolete = new CheckBox();
        readonly Label counter = Theme.Label("", 10, true); readonly GroupedCheckList selected = new GroupedCheckList(), available = new GroupedCheckList();
        readonly List<string> chosen = new List<string>(); readonly ToolTip tips = new ToolTip();
        readonly List<string> originKeys = new List<string>(), kindKeys = new List<string>();
        static readonly Dictionary<string, List<BuildingEntry>> cache = new Dictionary<string, List<BuildingEntry>>(StringComparer.OrdinalIgnoreCase);
        public List<string> Result;
        bool loading;

        public BuildingPickerWindow(Language language, string build, string workshopRoot, IEnumerable<string> current, IDictionary<string, string> used, Font font, Icon icon)
        {
            this.language = language; initial = current.Select(x => x.Trim()).Where(x => x.Length > 0).ToList(); usedElsewhere = new Dictionary<string, string>(used ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
            string cacheKey = (build ?? "") + "|" + (workshopRoot ?? "");
            lock (cache) { if (!cache.TryGetValue(cacheKey, out all)) { all = GameBuildings.Scan(build, workshopRoot); cache[cacheKey] = all; } }
            foreach (BuildingEntry entry in all) byTarget[entry.Target] = entry;
            foreach (string target in initial) if (!byTarget.ContainsKey(target)) { var missing = new BuildingEntry { Target = target, Name = target, Type = "", Origin = "missing", OriginLabel = "" }; all = all.Concat(new[] { missing }).ToList(); byTarget[target] = missing; }
            chosen.AddRange(initial.Where(t => byTarget.ContainsKey(t)).Distinct(StringComparer.OrdinalIgnoreCase));
            Text = language.T("picker_title"); Font = font; Icon = icon; Size = new Size(1180, 820); MinimumSize = new Size(900, 600); StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false; BackColor = Color.White;
            var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(12) };
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 200)); shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 26)); shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            Controls.Add(shell);
            // Filters.
            var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
            var searchLabel = Theme.Label(language.T("picker_search"), 10, false); searchLabel.Margin = new Padding(0, 8, 6, 0); bar.Controls.Add(searchLabel);
            search.Width = 200; search.Margin = new Padding(0, 4, 14, 0); search.TextChanged += delegate { ApplyFilter(); }; bar.Controls.Add(search);
            origin.DropDownStyle = ComboBoxStyle.DropDownList; origin.Width = 190; origin.Margin = new Padding(0, 4, 14, 0); bar.Controls.Add(origin);
            kind.DropDownStyle = ComboBoxStyle.DropDownList; kind.Width = 220; kind.Margin = new Padding(0, 4, 14, 0); bar.Controls.Add(kind);
            obsolete.Text = language.T("picker_obsolete"); obsolete.AutoSize = true; obsolete.Margin = new Padding(0, 8, 14, 0); obsolete.CheckedChanged += delegate { ApplyFilter(); }; bar.Controls.Add(obsolete);
            shell.Controls.Add(bar, 0, 0);
            // Counter row with the expand/collapse buttons at its right.
            var counterRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty };
            counterRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); counterRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            counter.Dock = DockStyle.Fill; counter.Margin = new Padding(0, 6, 0, 0); counterRow.Controls.Add(counter, 0, 0);
            var fold = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
            var expandAll = Theme.Button(language.T("picker_expand"), () => { selected.ExpandAll(); available.ExpandAll(); }, false); expandAll.Height = 28; expandAll.MinimumSize = new Size(60, 28); expandAll.Margin = new Padding(0, 0, 8, 0); fold.Controls.Add(expandAll);
            var collapseAll = Theme.Button(language.T("picker_collapse"), () => { selected.CollapseAll(); available.CollapseAll(); }, false); collapseAll.Height = 28; collapseAll.MinimumSize = new Size(60, 28); collapseAll.Margin = Padding.Empty; fold.Controls.Add(collapseAll);
            counterRow.Controls.Add(fold, 1, 0); shell.Controls.Add(counterRow, 0, 1);
            selected.Dock = DockStyle.Fill; selected.Margin = new Padding(0, 0, 0, 6); shell.Controls.Add(selected, 0, 2);
            var availableTitle = Theme.Label(language.T("picker_available"), 10, true); availableTitle.Margin = new Padding(0, 4, 0, 0); shell.Controls.Add(availableTitle, 0, 3);
            available.Dock = DockStyle.Fill; available.Margin = Padding.Empty; shell.Controls.Add(available, 0, 4);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Margin = new Padding(0, 10, 0, 0) };
            var cancel = Theme.Button(language.T("cancel"), () => { DialogResult = DialogResult.Cancel; Close(); }, false); cancel.Margin = new Padding(8, 0, 0, 0);
            var apply = Theme.Button(language.T("apply"), () => { Result = new List<string>(chosen); DialogResult = DialogResult.OK; Close(); }, true);
            buttons.Controls.Add(apply); buttons.Controls.Add(cancel); shell.Controls.Add(buttons, 0, 5); AcceptButton = apply; CancelButton = cancel;
            selected.Toggled = (id, on) => { if (!on) Unselect(id); };
            available.Toggled = (id, on) => { if (on) Select(id); };
            FillFilters(); Populate(); ApplyFilter();
        }
        string OriginLabel(BuildingEntry e)
        {
            if (e.Origin == "game") return language.T("picker_game");
            if (e.Origin.StartsWith("dlc", StringComparison.Ordinal)) return language.Format("picker_dlc", e.OriginLabel);
            if (e.Origin == "missing") return language.T("picker_not_found");
            return language.T("picker_workshop") + ": " + e.OriginLabel;
        }
        string KindLabel(string type) { if (type.Length == 0) return language.T("picker_kind_unknown"); string key = "btype." + type.ToLowerInvariant(); string text = language.T(key); return text == key ? type : text; }
        void FillFilters()
        {
            loading = true;
            origin.Items.Add(language.T("picker_all_origins")); originKeys.Add("");
            foreach (var group in all.Where(e => e.Origin != "missing").GroupBy(e => e.Origin).OrderBy(g => g.First().OriginRank).ThenBy(g => g.First().OriginLabel, StringComparer.CurrentCultureIgnoreCase))
            { origin.Items.Add(OriginLabel(group.First())); originKeys.Add(group.Key); }
            origin.SelectedIndex = 0; origin.SelectedIndexChanged += delegate { if (!loading) ApplyFilter(); };
            kind.Items.Add(language.T("picker_all_kinds")); kindKeys.Add("");
            foreach (string type in all.Select(e => e.Type).Distinct().OrderBy(t => KindLabel(t), StringComparer.CurrentCultureIgnoreCase)) { kind.Items.Add(KindLabel(type)); kindKeys.Add(type); }
            kind.SelectedIndex = 0; kind.SelectedIndexChanged += delegate { if (!loading) ApplyFilter(); };
            loading = false;
        }
        IEnumerable<IGrouping<string, BuildingEntry>> Grouped(IEnumerable<BuildingEntry> entries)
        { return entries.GroupBy(e => e.Origin == "missing" ? "missing" : e.Type).OrderBy(g => g.Key == "missing" ? 1 : 0).ThenBy(g => KindLabel(g.Key == "missing" ? "" : g.Key), StringComparer.CurrentCultureIgnoreCase); }
        string ItemText(BuildingEntry e) { return e.Name + "  · " + OriginLabel(e) + (e.Obsolete ? "  · " + language.T("picker_obsolete_tag") : ""); }
        void Populate()
        {
            selected.Clear(); available.Clear();
            var chosenSet = new HashSet<string>(chosen, StringComparer.OrdinalIgnoreCase);
            foreach (var group in Grouped(all))
            {
                string key = group.Key; string title = key == "missing" ? language.T("picker_not_found") : KindLabel(key);
                selected.AddGroup(key, title); available.AddGroup(key, title);
                foreach (BuildingEntry e in group.OrderBy(x => x.OriginRank).ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase))
                {
                    string other; bool used = usedElsewhere.TryGetValue(e.Target, out other) && !chosenSet.Contains(e.Target);
                    string text = ItemText(e) + (used ? "  · " + language.Format("picker_used", other) : "");
                    if (chosenSet.Contains(e.Target)) selected.AddItem(key, e.Target, ItemText(e), true, true, e.Target, tips);
                    available.AddItem(key, e.Target, text, false, !used, used ? language.Format("picker_used", other) : e.Target, tips);
                    if (chosenSet.Contains(e.Target)) available.SetVisible(e.Target, false);
                }
            }
        }
        bool Passes(BuildingEntry e)
        {
            if (e.Origin == "missing") return false;
            string needle = search.Text.Trim();
            if (needle.Length > 0 && e.Name.IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) < 0 && e.Target.IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) < 0 && KindLabel(e.Type).IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) < 0) return false;
            string o = origin.SelectedIndex >= 0 ? originKeys[origin.SelectedIndex] : ""; if (o.Length > 0 && e.Origin != o) return false;
            string k = kind.SelectedIndex >= 0 ? kindKeys[kind.SelectedIndex] : ""; if (k.Length > 0 && e.Type != k) return false;
            if (e.Obsolete && !obsolete.Checked) return false;
            return true;
        }
        void ApplyFilter()
        {
            var chosenSet = new HashSet<string>(chosen, StringComparer.OrdinalIgnoreCase);
            foreach (BuildingEntry e in all) available.SetVisible(e.Target, !chosenSet.Contains(e.Target) && Passes(e));
            available.Relayout(); selected.Relayout(); UpdateCounter();
        }
        void UpdateCounter()
        {
            counter.Text = language.Format("picker_count", chosen.Count, available.VisibleCount, all.Count(e => e.Origin != "missing"));
            counter.ForeColor = Theme.Ink;
        }
        void Select(string target)
        {
            if (chosen.Contains(target, StringComparer.OrdinalIgnoreCase)) return;
            chosen.Add(target); BuildingEntry e = byTarget[target];
            available.SetVisible(target, false); selected.AddItem(e.Origin == "missing" ? "missing" : e.Type, target, ItemText(e), true, true, target, tips);
            available.Relayout(); selected.Relayout(); UpdateCounter();
        }
        void Unselect(string target)
        {
            chosen.RemoveAll(t => t.Equals(target, StringComparison.OrdinalIgnoreCase)); selected.Remove(target);
            BuildingEntry e; if (byTarget.TryGetValue(target, out e) && e.Origin != "missing") available.SetVisible(target, Passes(e));
            available.Relayout(); selected.Relayout(); UpdateCounter();
        }
        internal int Total { get { return all.Count(e => e.Origin != "missing"); } }
        internal int SelectedCount { get { return chosen.Count; } }
        internal int VisibleCount { get { return available.VisibleCount; } }
        internal string CounterText { get { return counter.Text; } }
        internal void TestSearch(string text) { search.Text = text; }
        internal void TestObsolete(bool show) { obsolete.Checked = show; }
        internal void TestSelect(string target) { Select(target); }
        internal void TestUnselect(string target) { Unselect(target); }
        internal List<string> TestResult() { return new List<string>(chosen); }
    }
}
