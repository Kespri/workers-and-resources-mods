using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TesmioAutoload
{
    static class Theme
    {
        public static readonly Color Blue = Color.FromArgb(0, 85, 215), SelectionBlue = Color.FromArgb(34,84,151), TabHover = Color.FromArgb(231,240,255), Navy = Color.FromArgb(12, 32, 54), Ink = Color.FromArgb(22, 33, 49), Muted = Color.FromArgb(91, 104, 121), Line = Color.FromArgb(217, 224, 234), Pale = Color.FromArgb(246, 248, 252), Chrome = Color.FromArgb(239,243,249), Danger = Color.FromArgb(190,42,47);
        [DllImport("dwmapi.dll", PreserveSig=true)] static extern int DwmSetWindowAttribute(IntPtr handle,int attribute,ref int value,int size);
        public static Label Label(string text, float size, bool bold)
        { return new Label { Text = text, AutoSize = true, Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), ForeColor = Ink, Margin = new Padding(0, 0, 0, 7), UseMnemonic = false, ContextMenuStrip = CopyMenu() }; }
        // Labels cannot be selected, so every text label offers "copy text" on a
        // right click: hints, error details, descriptions and field labels can be
        // pasted into a report. The caption follows the current UI language.
        public static Func<string> CopyMenuLabel = () => "Copy text";
        static ContextMenuStrip copyMenu;
        public static ContextMenuStrip CopyMenu()
        {
            if (copyMenu != null) return copyMenu;
            copyMenu = new ContextMenuStrip();
            var item = copyMenu.Items.Add(CopyMenuLabel());
            copyMenu.Opening += (s, e) => { item.Text = CopyMenuLabel(); var source = copyMenu.SourceControl as Label; e.Cancel = source == null || source.Text.Length == 0; };
            item.Click += (s, e) => { var source = copyMenu.SourceControl as Label; if (source == null || source.Text.Length == 0) return; try { Clipboard.SetText(source.Text); } catch (ExternalException) { } };
            return copyMenu;
        }
        // 0.4.51: the "unsaved" tone of the footer heading and the list mark.
        public static readonly Color Amber = Color.FromArgb(176, 108, 0);
        public static Button Button(string text, Action click, bool primary)
        {
            var button = new Button { Text = text, AutoSize = true, Height = 38, MinimumSize = new Size(80, 38), Padding = new Padding(10, 4, 10, 4), FlatStyle = FlatStyle.Flat, BackColor = primary ? Blue : Chrome, ForeColor = primary ? Color.White : Ink, Cursor = Cursors.Hand, UseVisualStyleBackColor = false, Margin = new Padding(4) };
            button.FlatAppearance.BorderColor = Line; button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(0,70,180) : Color.FromArgb(222,232,248);
            button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(0,61,158) : Color.FromArgb(217,231,252);
            button.Click += (s,e) => click(); return button;
        }
        public static void ApplyWindowChrome(Form form)
        {
            form.HandleCreated+=(s,e)=>
            {
                try
                {
                    int caption=ColorTranslator.ToWin32(Chrome),text=ColorTranslator.ToWin32(Ink);
                    DwmSetWindowAttribute(form.Handle,35,ref caption,sizeof(int));
                    DwmSetWindowAttribute(form.Handle,36,ref text,sizeof(int));
                }
                catch(DllNotFoundException) { }
                catch(EntryPointNotFoundException) { }
            };
        }
        public static GraphicsPath Rounded(Rectangle r, int radius)
        {
            var p = new GraphicsPath(); int d = radius * 2;
            p.AddArc(r.X,r.Y,d,d,180,90); p.AddArc(r.Right-d,r.Y,d,d,270,90); p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90); p.AddArc(r.X,r.Bottom-d,d,d,90,90); p.CloseFigure(); return p;
        }
        // 0.4.50: painting off while a pane is torn down and rebuilt - every save showed the
        // half-built cards for as long as the rebuild took. Switching redraw back on repaints
        // the pane and all its children once.
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);
        public static void SetRedraw(Control control, bool enabled)
        { if (control == null || !control.IsHandleCreated) return; SendMessage(control.Handle, 0x000B, (IntPtr)(enabled ? 1 : 0), IntPtr.Zero); if (enabled) control.Refresh(); }
        public static void DisposeChildren(Control control)
        { while (control.Controls.Count > 0) { var c = control.Controls[0]; control.Controls.Remove(c); c.Dispose(); } }
        // A soft drop shadow towards the bottom right: translucent rounded layers,
        // each shifted one pixel further, so the band next to the frame is darkest.
        public const int Shadow = 6;
        public static void DrawShadow(Graphics g, Rectangle frame, int radius, int size, int alpha)
        {
            for (int k = size; k >= 1; --k)
                using (var brush = new SolidBrush(Color.FromArgb(alpha, 30, 40, 60)))
                using (var path = Rounded(new Rectangle(frame.X + k, frame.Y + k, frame.Width, frame.Height), radius))
                    g.FillPath(brush, path);
        }
        // A halo on every side, for a control that should stand out from a flat row.
        public static void DrawHalo(Graphics g, Rectangle frame, int size, int alpha)
        {
            for (int k = size; k >= 1; --k)
                using (var brush = new SolidBrush(Color.FromArgb(alpha, 30, 40, 60)))
                using (var path = Rounded(new Rectangle(frame.X - k, frame.Y - k, frame.Width + 2 * k, frame.Height + 2 * k), k + 2))
                    g.FillPath(brush, path);
        }
    }
    // A white rounded card with an optional blue header and a light shadow. The
    // shadow lives inside the control's own bounds (right and bottom band), so the
    // padding reserves that band and layout code needs no special handling.
    sealed class Card : Panel
    {
        public string HeaderText = "";
        const int HeaderHeight = 56;
        public Card(string header = "") { DoubleBuffered = true; ResizeRedraw = true; BackColor = Color.White; HeaderText=header??""; int s=Theme.Shadow; Padding = HeaderText.Length==0?new Padding(20,20,20+s,20+s):new Padding(20,HeaderHeight+17,20+s,20+s); Margin = new Padding(0,0,0,18); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var frame=new Rectangle(0,0,Width-1-Theme.Shadow,Height-1-Theme.Shadow);
            Theme.DrawShadow(e.Graphics,frame,8,Theme.Shadow,7);
            using (var p = Theme.Rounded(frame,8))
            {
                using (var white = new SolidBrush(Color.White)) e.Graphics.FillPath(white,p);
                if(HeaderText.Length>0)
                {
                    var state=e.Graphics.Save();e.Graphics.SetClip(p);
                    using(var fill=new SolidBrush(Theme.SelectionBlue))e.Graphics.FillRectangle(fill,0,0,frame.Width+1,HeaderHeight);
                    e.Graphics.Restore(state);
                    using(var font=new Font("Segoe UI",15,FontStyle.Bold))TextRenderer.DrawText(e.Graphics,HeaderText,font,new Rectangle(23,0,Math.Max(0,frame.Width-46),HeaderHeight),Color.White,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
                }
                using (var pen = new Pen(Theme.Line)) e.Graphics.DrawPath(pen,p);
            }
        }
    }
    // A tinted notice or warning box with a slightly stronger shadow than a card.
    // Children get the tint as their background, so no transparency is needed.
    sealed class NoticeBox : TableLayoutPanel
    {
        public readonly Color Tint; public readonly string Tone;
        public NoticeBox(bool warning) : this(warning ? "warning" : "info") { }
        // 0.4.55: tones info (blue), warning (amber) and error (red); the error box replaces the red text lines.
        public NoticeBox(string tone)
        {
            Tone = tone; Tint = tone == "error" ? Color.FromArgb(253,235,235) : tone == "warning" ? Color.FromArgb(255,247,225) : Color.FromArgb(237,245,255);
            DoubleBuffered = true; ResizeRedraw = true; BackColor = Color.White; int s = Theme.Shadow;
            ColumnCount = 2; AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink; Dock = DockStyle.Top; Padding = new Padding(12,9,12+s,9+s); Margin = new Padding(0,2,0,16);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var back = new SolidBrush(Parent != null ? Parent.BackColor : Color.White)) e.Graphics.FillRectangle(back, ClientRectangle);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var frame = new Rectangle(0,0,Width-1-Theme.Shadow,Height-1-Theme.Shadow);
            Theme.DrawShadow(e.Graphics,frame,6,Theme.Shadow,10);
            using (var path = Theme.Rounded(frame,6)) using (var fill = new SolidBrush(Tint)) e.Graphics.FillPath(fill,path);
        }
    }
    // The row of tab buttons: a line in the selection blue underneath, on which the
    // active tab sits, and a soft halo around the active tab.
    sealed class TabStrip : FlowLayoutPanel
    {
        public const int TabHeight = 42, Inset = 4;
        public TabStrip()
        {
            DoubleBuffered = true; WrapContents = false; Margin = Padding.Empty; Padding = new Padding(0,Inset,0,0); BackColor = Color.White;
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            foreach (Control child in Controls)
                if (child.Visible && child.BackColor == Theme.SelectionBlue) Theme.DrawHalo(e.Graphics,child.Bounds,5,22);
            int y = Inset + TabHeight;
            using (var pen = new Pen(Theme.SelectionBlue,2)) e.Graphics.DrawLine(pen,0,y+1,Math.Max(Width,DisplayRectangle.Right),y+1);
        }
        protected override void OnControlAdded(ControlEventArgs e) { base.OnControlAdded(e); Invalidate(); }
        protected override void OnControlRemoved(ControlEventArgs e) { base.OnControlRemoved(e); Invalidate(); }
    }
    sealed class NoticeIcon : Control
    {
        public bool Warning, Error;   // Error (0.4.55): filled red circle with a white cross
        public Color Tone=Color.Empty; public string Symbol="";   // 0.4.38: message window colours and glyphs
        public NoticeIcon(){Size=new Size(27,27);Margin=new Padding(0,1,9,0);TabStop=false;}
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;Color color=Tone.IsEmpty?(Error?Theme.Danger:Warning?Color.FromArgb(171,102,0):Theme.Blue):Tone;
            if(Error&&Symbol.Length==0){using(var fill=new SolidBrush(color))e.Graphics.FillEllipse(fill,3,3,20,20);using(var pen=new Pen(Color.White,2.4f)){e.Graphics.DrawLine(pen,9,9,17,17);e.Graphics.DrawLine(pen,17,9,9,17);}return;}
            using(var pen=new Pen(color,2.2f))e.Graphics.DrawEllipse(pen,3,3,20,20);
            using(var font=new Font("Segoe UI",13,FontStyle.Bold))TextRenderer.DrawText(e.Graphics,Symbol.Length>0?Symbol:(Warning?"!":"i"),font,new Rectangle(2,0,23,27),color,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);
        }
    }
    sealed class ToggleSwitch : CheckBox
    {
        public ToggleSwitch() { AutoSize = false; Size = new Size(60,32); Cursor = Cursors.Hand; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; e.Graphics.Clear(BackColor);
            using (var path = Theme.Rounded(new Rectangle(2,3,54,27),13)) using (var fill = new SolidBrush(!Enabled ? Color.LightGray : Checked ? Theme.Blue : Color.FromArgb(152,163,179))) e.Graphics.FillPath(fill,path);
            using (var fill = new SolidBrush(Color.White)) e.Graphics.FillEllipse(fill,Checked ? 31 : 5,6,21,21);
            if (Focused) ControlPaint.DrawFocusRectangle(e.Graphics,ClientRectangle);
        }
        protected override void OnCheckedChanged(EventArgs e) { base.OnCheckedChanged(e); Invalidate(); }
    }
    // Raw text is retained until validation. Unlike NumericUpDown, displaying a
    // value never rounds it to three decimals or silently discards an invalid edit.
    // Uniform input rows (0.4.16): every field is 37 px high with an 11 pt font, the reset
    // button beside it is 30 x 30 and vertically centred. Text boxes are wrapped in a bordered
    // panel (a single-line TextBox cannot be taller than its font), combo boxes are owner-drawn
    // so their item height sets the control height.
    static class Fields
    {
        public const int Height = 37, Reset = 30, Reserve = 38;
        public static readonly Font Font = new Font("Segoe UI", 11f);
        public static FieldBox Wrap(TextBox box) { return new FieldBox(box); }
        public static void Tall(ComboBox combo)
        {
            combo.Font = Font; combo.Height = Height;
            if (combo is GroupedPicker) { combo.ItemHeight = Height - 6; return; }
            combo.DrawMode = DrawMode.OwnerDrawFixed; combo.ItemHeight = Height - 6;
            combo.DrawItem += (s, e) =>
            {
                e.DrawBackground();
                if (e.Index >= 0) TextRenderer.DrawText(e.Graphics, combo.GetItemText(combo.Items[e.Index]), combo.Font, new Rectangle(e.Bounds.X + 3, e.Bounds.Y, e.Bounds.Width - 3, e.Bounds.Height), e.ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                e.DrawFocusRectangle();
            };
        }
        public static TextBox Inner(Control input) { var box = input as FieldBox; if (box != null) return box.Input; var number = input as NumberInput; return number != null ? number.Input : null; }
        // Editable combo boxes (typed value plus suggestions) keep their font-high edit area at the
        // top when the control is made taller, so they are hosted in a bordered panel that centres
        // them instead (0.4.17). Fixed lists are drawn tall directly.
        public static Control Host(ComboBox combo)
        {
            if (combo.DropDownStyle != ComboBoxStyle.DropDown) { Tall(combo); return combo; }
            return new ComboHost(combo);
        }
        public static ComboBox ComboOf(Control input) { var host = input as ComboHost; return host != null ? host.Combo : input as ComboBox; }
    }
    sealed class FieldBox : Panel
    {
        public readonly TextBox Input;
        public FieldBox(TextBox box)
        {
            Input = box; Height = Fields.Height; BackColor = Color.White; BorderStyle = BorderStyle.FixedSingle; AccessibleName = box.AccessibleName;
            box.BorderStyle = BorderStyle.None; box.Dock = DockStyle.Fill; box.Margin = Padding.Empty; box.Font = Fields.Font;
            var inner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(9,7,3,2), BackColor = Color.White }; inner.Controls.Add(box); Controls.Add(inner);
            Click += (s, e) => box.Focus(); inner.Click += (s, e) => box.Focus();
        }
        public override string Text { get { return Input.Text; } set { Input.Text = value; } }
    }
    sealed class ComboHost : Panel
    {
        public readonly ComboBox Combo;
        public ComboHost(ComboBox combo)
        {
            Combo = combo; Height = Fields.Height; BackColor = Color.White; BorderStyle = BorderStyle.FixedSingle; AccessibleName = combo.AccessibleName;
            combo.Font = Fields.Font; combo.FlatStyle = FlatStyle.Flat; combo.BackColor = Color.White;
            var grouped = combo as GroupedPicker; if (grouped != null) grouped.ItemHeight = 22;
            combo.Location = new Point(0, 0); Controls.Add(combo);
            EventHandler fit = (s, e) => { combo.Width = ClientSize.Width; combo.Top = Math.Max(0, (ClientSize.Height - combo.Height) / 2); };
            SizeChanged += fit; combo.SizeChanged += fit; fit(this, EventArgs.Empty);
        }
        public override string Text { get { return Combo.Text; } set { Combo.Text = value; } }
    }
    sealed class NumberInput : UserControl
    {
        public readonly TextBox Input = new TextBox();
        readonly decimal minimum, maximum, step;
        public event EventHandler ValueEdited;
        // 0.4.45: raised after a +/- click, so editors that apply a value on Leave can apply it at once.
        public event EventHandler Stepped;
        // 0.4.44: the control's Text is the typed number, so dialogs that collect
        // their inputs by Text (add dialogs of the list and section editors) see it.
        public override string Text { get { return Input.Text; } set { Input.Text = value ?? ""; } }
        public NumberInput(Field f, decimal increment) : this(f.Minimum, f.Maximum, increment) { }
        // 0.4.39: range and step only, for the list/section editors whose fields are not Field objects.
        public NumberInput(decimal low, decimal high, decimal increment)
        {
            minimum = low; maximum = high; step = increment; Height = Fields.Height; MinimumSize = new Size(110,Fields.Height); BackColor = Color.White; Input.Font = Fields.Font;
            BorderStyle = BorderStyle.FixedSingle;
            var spin = new Panel { Dock = DockStyle.Right, Width = 28, Margin = Padding.Empty };
            foreach (int direction in new[] { 1,-1 })
            {
                int change = direction; var button = new VectorButton { Symbol = direction > 0 ? "plus" : "minus", Dock = direction > 0 ? DockStyle.Top : DockStyle.Bottom, Height = 17, Margin = Padding.Empty, TabStop = false, AccessibleName = direction > 0 ? "+" : "−", DrawBorder = false, Primary=true, BackColor=Theme.Blue, ForeColor=Color.White, GlyphScale=1.15f };
                button.Click += (s,e) =>
                {
                    decimal n = 0; bool empty = Input.Text.Trim().Length == 0;
                    if (!empty && !Decimal.TryParse(Canonical(Input.Text),NumberStyles.Float,CultureInfo.InvariantCulture,out n)) return;
                    if (empty) n = change > 0 ? minimum - step : minimum + step;   // the first click lands on the minimum
                    try { decimal next = n + change * step; if (next >= minimum && next <= maximum) { Input.Text = next.ToString("0.############################",CultureInfo.InvariantCulture); if (Stepped != null) Stepped(this,EventArgs.Empty); } } catch (OverflowException) { }
                };
                spin.Controls.Add(button);
            }
            Input.BorderStyle = BorderStyle.None; Input.Dock = DockStyle.Fill; Input.Margin = Padding.Empty;
            var inner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(9,7,1,2) }; inner.Controls.Add(Input);
            Controls.Add(inner); Controls.Add(spin);
            Input.TextChanged += (s,e) => { if (ValueEdited != null) ValueEdited(this,EventArgs.Empty); };
        }
        // Test hook: the same path a +/- click takes.
        internal void TestStep(int direction) { foreach (Control c in Controls) foreach (Control b in c.Controls) { var v = b as VectorButton; if (v != null && v.Symbol == (direction > 0 ? "plus" : "minus")) v.PerformTestClick(); } }
        public static string Canonical(string value)
        {
            value = value.Trim();
            // A single comma is a decimal separator, never a thousands separator.
            if (value.IndexOf('.') < 0 && value.IndexOf(',') == value.LastIndexOf(',')) value = value.Replace(',','.');
            return value;
        }
    }
    sealed class IconCache : IDisposable
    {
        readonly Dictionary<string, Bitmap> images = new Dictionary<string, Bitmap>();
        public readonly List<string> Notes = new List<string>();
        public Action<string> Warning = null;
        // The list icon says where an entry comes from: Steam's own icon for a Workshop
        // folder, the TesmioLauncher icon for the loader's plugins folder, a gear otherwise.
        public Func<string> LoaderExe = null;
        static string SteamExe()
        {
            try { string steam = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string; return String.IsNullOrEmpty(steam) ? null : Path.Combine(steam.Replace('/', '\\'), "steam.exe"); }
            catch (Exception) { return null; }
        }
        public bool DrawOrigin(Graphics g, string origin, Rectangle r)
        {
            string path = origin == "steam" ? SteamExe() : origin == "loader" && LoaderExe != null ? LoaderExe() : null;
            if (String.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            string id = "origin|" + path; Bitmap bitmap;
            if (!images.TryGetValue(id, out bitmap))
            {
                try { using (var icon = Icon.ExtractAssociatedIcon(path)) bitmap = icon == null ? null : icon.ToBitmap(); }
                catch (Exception) { bitmap = null; }
                images[id] = bitmap;
            }
            if (bitmap == null) return false;
            // Both icons are dark; a light rounded plate keeps them readable on the navy list.
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var plate = Theme.Rounded(new Rectangle(r.X - 3, r.Y - 3, r.Width + 6, r.Height + 6), 7)) using (var fill = new SolidBrush(Color.FromArgb(236, 241, 247))) g.FillPath(fill, plate);
            var previous = g.InterpolationMode; g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(bitmap, r); g.InterpolationMode = previous; return true;
        }
        public void Draw(Graphics g, string root, string key, Rectangle r, Color color)
        {
            if (String.IsNullOrEmpty(key)) return;
            if (key.StartsWith("builtin:", StringComparison.Ordinal)) { DrawBuiltin(g,key.Substring(8),r,color); return; }
            string id = root + "|" + key; Bitmap bitmap;
            if (!images.TryGetValue(id,out bitmap))
            {
                if (images.Count > 128) { DrawBuiltin(g,"gear",r,color); return; }
                try
                {
                    string path = SafeFiles.Child(root,key), ext = Path.GetExtension(path).ToLowerInvariant();
                    if (ext != ".png" && ext != ".ico") throw new IOException(Msg.Key("err_only_png_ico_symbols", key));
                    using (var ms = new MemoryStream(SafeFiles.Read(path,2*1024*1024))) using (var img = Image.FromStream(ms,true,true))
                    { if (img.Width > 2048 || img.Height > 2048) throw new IOException(Msg.Key("err_icon_exceeds_2048_pixels", key)); bitmap = new Bitmap(img); }
                }
                catch (Exception e) { bitmap = null; Notes.Add(e.Message); if (Warning != null) Warning(e.Message); }
                images[id] = bitmap;
            }
            if (bitmap == null) { DrawBuiltin(g,"gear",r,color); return; }
            float scale = Math.Min((float)r.Width/bitmap.Width,(float)r.Height/bitmap.Height);
            int w = (int)(bitmap.Width*scale), h = (int)(bitmap.Height*scale);
            g.DrawImage(bitmap,new Rectangle(r.X+(r.Width-w)/2,r.Y+(r.Height-h)/2,w,h));
        }
        public static void DrawBuiltin(Graphics g, string kind, Rectangle bounds, Color color)
        {
            var state = g.Save(); g.TranslateTransform(bounds.X,bounds.Y); g.ScaleTransform(bounds.Width/32f,bounds.Height/32f); g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(color)) using (var p = new Pen(color,2))
            {
                if (kind == "truck")
                {
                    g.FillRectangle(b,2,7,17,14); g.FillPolygon(b,new[] {new Point(20,12),new Point(26,12),new Point(30,18),new Point(30,24),new Point(20,24)});
                    g.FillRectangle(b,3,21,24,3); g.FillEllipse(b,5,23,7,7); g.FillEllipse(b,22,23,7,7);
                }
                else if (kind == "rail")
                { g.DrawRectangle(p,7,3,18,22); g.DrawRectangle(p,10,6,12,8); g.FillEllipse(b,9,18,4,4); g.FillEllipse(b,19,18,4,4); g.DrawLine(p,10,25,5,31); g.DrawLine(p,22,25,27,31); g.DrawLine(p,8,28,24,28); }
                else if (kind == "ship")
                { g.FillPolygon(b,new[] {new Point(2,18),new Point(16,12),new Point(30,18),new Point(25,26),new Point(7,26)}); g.FillRectangle(b,11,6,10,9); g.DrawLine(p,16,1,16,10); g.DrawBezier(p,1,29,6,25,9,33,14,29); g.DrawBezier(p,14,29,21,25,23,33,31,29); }
                else if (kind == "plane")
                { g.FillPolygon(b,new[] {new Point(14,1),new Point(18,1),new Point(19,13),new Point(31,21),new Point(31,24),new Point(18,20),new Point(18,27),new Point(22,30),new Point(10,30),new Point(14,27),new Point(14,20),new Point(1,24),new Point(1,21),new Point(13,13)}); }
                else if (kind == "search") { g.DrawEllipse(p,4,3,17,17); g.DrawLine(p,20,20,29,29); }
                else if (kind == "folder")
                { g.FillPolygon(b,new[] {new Point(2,9),new Point(12,9),new Point(15,13),new Point(30,13),new Point(27,27),new Point(2,27)}); g.FillRectangle(b,3,6,11,6); }
                else if (kind == "document")
                { g.DrawRectangle(p,6,2,19,27); g.DrawLine(p,10,9,21,9); g.DrawLine(p,10,14,21,14); g.DrawLine(p,10,19,21,19); g.DrawLine(p,10,24,18,24); }
                else if (kind == "reload")
                { g.DrawArc(p,5,5,22,22,35,285); g.FillPolygon(b,new[] {new Point(28,12),new Point(21,8),new Point(26,4)}); }
                else if (kind == "loader")
                { g.DrawRectangle(p,5,8,22,16); g.FillRectangle(b,9,12,14,8); for (int i=0;i<4;i++) { g.DrawLine(p,8+i*5,8,8+i*5,4); g.DrawLine(p,8+i*5,24,8+i*5,28); } }
                else if (kind == "lock")
                {
                    // 0.4.60: a padlock whose shackle is visible at 22 px - thick rounded stroke, legs down to the body.
                    using (var shackle = new Pen(color,3.4f)) { shackle.StartCap = LineCap.Round; shackle.EndCap = LineCap.Round; g.DrawArc(shackle,10,2,12,12,180,180); g.DrawLine(shackle,10,8,10,15); g.DrawLine(shackle,22,8,22,15); }
                    g.FillRectangle(b,6,15,20,14); using(var hole=new SolidBrush(Color.White)){g.FillEllipse(hole,14,18,4,4);g.FillRectangle(hole,15,21,2,4);}
                }
                else if (kind == "archive")
                { g.DrawRectangle(p,4,10,24,18); g.FillRectangle(b,2,4,28,6); g.FillRectangle(b,12,15,8,3); }
                else
                { g.DrawEllipse(p,7,7,18,18); g.DrawEllipse(p,12,12,8,8); for (int i=0;i<8;i++) { double a=i*Math.PI/4; g.DrawLine(p,16+(float)Math.Cos(a)*10,16+(float)Math.Sin(a)*10,16+(float)Math.Cos(a)*15,16+(float)Math.Sin(a)*15); } }
            }
            g.Restore(state);
        }
        public void Dispose() { foreach (var bitmap in images.Values) if (bitmap != null) bitmap.Dispose(); }
    }
    sealed class Glyph : Control
    {
        public string Key, Root; public IconCache Cache;
        public Glyph() { Size = new Size(32,32); Margin = new Padding(0,3,10,0); }
        protected override void OnPaint(PaintEventArgs e) { Cache.Draw(e.Graphics,Root,Key,ClientRectangle,ForeColor); }
    }
    sealed class ModList : ListBox
    {
        public IconCache Cache;
        public readonly Dictionary<string,string> IconKeys = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string,bool> ActiveStates = new Dictionary<string,bool>(StringComparer.OrdinalIgnoreCase);
        public string UpdateBadge = "Update";
        // 0.4.51: roots of the entries with unsaved changes, drawn as an amber dot before the name;
        // since 0.4.71 several at once (parked entries).
        public readonly HashSet<string> DirtyRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public ModList() { DrawMode = DrawMode.OwnerDrawFixed; ItemHeight = 80; BorderStyle = BorderStyle.None; BackColor = Theme.Navy; ForeColor = Color.White; IntegralHeight = false; }
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count) return;
            var mod = (CatalogEntry)Items[e.Index]; bool selected = (e.State & DrawItemState.Selected) != 0;
            using (var fill = new SolidBrush(selected ? Color.FromArgb(34,84,151) : BackColor)) e.Graphics.FillRectangle(fill,e.Bounds);
            var iconRect=new Rectangle(e.Bounds.X+14,e.Bounds.Y+22,30,30);
            if(Cache==null||!Cache.DrawOrigin(e.Graphics,mod.Origin,iconRect)) IconCache.DrawBuiltin(e.Graphics,mod.Origin=="loader"?"loader":"gear",iconRect,Color.White);
            bool dirty=DirtyRoots.Contains(mod.Root);
            if(dirty){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var amber=new SolidBrush(Color.FromArgb(232,166,36)))e.Graphics.FillEllipse(amber,e.Bounds.X+55,e.Bounds.Y+24,9,9);}
            using (var title = new Font(Font,FontStyle.Bold)) TextRenderer.DrawText(e.Graphics,mod.Name,title,new Rectangle(e.Bounds.X+(dirty?69:55),e.Bounds.Y+15,e.Bounds.Width-94-(dirty?14:0),28),Color.White,TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(e.Graphics,mod.Version,Font,new Rectangle(e.Bounds.X+55,e.Bounds.Y+47,e.Bounds.Width-91,22),Color.FromArgb(184,207,233),TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
            bool active; ActiveStates.TryGetValue(mod.Root,out active);
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            if (mod.Updated)
            {
                // A small amber "update" pill under the activity light.
                string badge=UpdateBadge; Size size=TextRenderer.MeasureText(badge,Font);
                var pill=new Rectangle(e.Bounds.Right-30-size.Width+6,e.Bounds.Y+49,size.Width+8,size.Height+2);
                using(var amber=new SolidBrush(Color.FromArgb(232,166,36))) e.Graphics.FillRectangle(amber,pill);
                TextRenderer.DrawText(e.Graphics,badge,Font,pill,Color.FromArgb(28,38,52),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);
            }
            using(var light=new SolidBrush(active?Color.FromArgb(45,202,83):Color.FromArgb(104,122,143)))
                e.Graphics.FillEllipse(light,e.Bounds.Right-30,e.Bounds.Y+31,14,14);
            if (Focused && selected) e.DrawFocusRectangle();
        }
    }

    sealed class ResourceChoice
    {
        public ResourceOption Option; public bool Used;
        public override string ToString() { return Option.ToString(); }
    }
    sealed class ResourcePickerList : ListBox
    {
        bool correcting;
        public ResourcePickerList()
        {
            DrawMode=DrawMode.OwnerDrawFixed; ItemHeight=48; IntegralHeight=false; BorderStyle=BorderStyle.FixedSingle;
        }
        public ResourceChoice Choice { get { return SelectedItem as ResourceChoice; } }
        public int UsedCount { get { return Items.Cast<ResourceChoice>().Count(x=>x.Used); } }
        public int AvailableCount { get { return Items.Count-UsedCount; } }
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if(e.Index<0||e.Index>=Items.Count)return;
            var item=(ResourceChoice)Items[e.Index]; bool selected=!item.Used&&(e.State&DrawItemState.Selected)!=0;
            using(var background=new SolidBrush(selected?Color.FromArgb(231,240,255):BackColor))e.Graphics.FillRectangle(background,e.Bounds);
            Color main=item.Used?Color.FromArgb(142,151,163):Theme.Ink,secondary=item.Used?Color.FromArgb(166,174,184):Theme.Muted;
            TextRenderer.DrawText(e.Graphics,item.Option.Display,Font,new Rectangle(e.Bounds.X+14,e.Bounds.Y+6,e.Bounds.Width-60,20),main,TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
            using(var small=new Font(Font.FontFamily,8.5f))TextRenderer.DrawText(e.Graphics,item.Option.Id,small,new Rectangle(e.Bounds.X+14,e.Bounds.Y+27,e.Bounds.Width-60,17),secondary,TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
            if(item.Used)
            {
                e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
                using(var dot=new SolidBrush(Color.FromArgb(45,202,83)))e.Graphics.FillEllipse(dot,e.Bounds.Right-27,e.Bounds.Y+(e.Bounds.Height-12)/2,12,12);
            }
            if(Focused&&selected)e.DrawFocusRectangle();
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            int index=IndexFromPoint(e.Location); if(index>=0&&((ResourceChoice)Items[index]).Used){Focus();return;} base.OnMouseDown(e);
        }
        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            if(!correcting&&Choice!=null&&Choice.Used){correcting=true;SelectedIndex=-1;correcting=false;} base.OnSelectedIndexChanged(e);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if(e.KeyCode==Keys.Up||e.KeyCode==Keys.Down||e.KeyCode==Keys.Home||e.KeyCode==Keys.End)
            {
                int direction=e.KeyCode==Keys.Up||e.KeyCode==Keys.End?-1:1;
                int index=e.KeyCode==Keys.Home?-1:e.KeyCode==Keys.End?Items.Count:SelectedIndex;
                for(index+=direction;index>=0&&index<Items.Count;index+=direction)if(!((ResourceChoice)Items[index]).Used){SelectedIndex=index;break;}
                e.Handled=true;return;
            }
            base.OnKeyDown(e);
        }
    }

    // A drop-down whose items sit under bold, unselectable group headers.
    // Add(header) / Add(value, text): the value is what the picker reports,
    // the text what it shows; a value not in the list can be appended so an
    // existing INI entry is never hidden.
    sealed class GroupedPicker : ComboBox
    {
        public sealed class Entry { public string Value, Text; public bool Header; public override string ToString() { return Text; } }
        int lastGood = -1;
        public GroupedPicker() { DropDownStyle = ComboBoxStyle.DropDownList; DrawMode = DrawMode.OwnerDrawFixed; ItemHeight = 22; MaxDropDownItems = 24; IntegralHeight = false; DropDownHeight = 440; }
        // Editable (0.4.8): typed entry plus the grouped suggestions; choices should then use their value as text.
        public bool Editable { get { return DropDownStyle == ComboBoxStyle.DropDown; } set { DropDownStyle = value ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList; } }
        public void AddHeader(string text) { Items.Add(new Entry { Header = true, Text = text, Value = "" }); }
        public void AddChoice(string value, string text) { Items.Add(new Entry { Value = value, Text = text }); }
        public string Value { get { var e = SelectedItem as Entry; return e == null || e.Header ? "" : e.Value; } }
        public bool Select(string value)
        {
            for (int i = 0; i < Items.Count; i++) { var e = (Entry)Items[i]; if (!e.Header && e.Value.Equals(value ?? "", StringComparison.OrdinalIgnoreCase)) { SelectedIndex = i; lastGood = i; return true; } }
            return false;
        }
        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            var entry = SelectedItem as Entry;
            if (entry != null && entry.Header) { SelectedIndex = lastGood; return; }   // headers cannot be chosen
            lastGood = SelectedIndex; base.OnSelectedIndexChanged(e);
        }
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count) return;
            var entry = (Entry)Items[e.Index]; bool selected = (e.State & DrawItemState.Selected) != 0 && !entry.Header;
            using (var fill = new SolidBrush(selected ? Theme.SelectionBlue : entry.Header ? Theme.Chrome : Color.White)) e.Graphics.FillRectangle(fill, e.Bounds);
            using (var font = new Font(Font, entry.Header ? FontStyle.Bold : FontStyle.Regular))
                TextRenderer.DrawText(e.Graphics, entry.Text, font, new Rectangle(e.Bounds.X + (entry.Header ? 4 : 12), e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height), selected ? Color.White : entry.Header ? Theme.Muted : Theme.Ink, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            if (selected) e.DrawFocusRectangle();
        }
    }

    sealed class ResourceDefinitionList : ListBox
    {
        public ResourceDefinitionList(){DrawMode=DrawMode.OwnerDrawFixed;ItemHeight=48;IntegralHeight=false;BorderStyle=BorderStyle.FixedSingle;}
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if(e.Index<0||e.Index>=Items.Count)return;var item=(LocalResourceItem)Items[e.Index];bool selected=(e.State&DrawItemState.Selected)!=0;
            using(var fill=new SolidBrush(selected?Color.FromArgb(231,240,255):BackColor))e.Graphics.FillRectangle(fill,e.Bounds);
            Color main=Theme.Ink,secondary=item.Owned?Color.FromArgb(26,132,61):Theme.Muted;
            if(item.Owned)using(var dot=new SolidBrush(Color.FromArgb(45,202,83)))e.Graphics.FillEllipse(dot,e.Bounds.X+11,e.Bounds.Y+17,12,12);
            else IconCache.DrawBuiltin(e.Graphics,"lock",new Rectangle(e.Bounds.X+7,e.Bounds.Y+11,22,22),Color.FromArgb(92,108,128));
            TextRenderer.DrawText(e.Graphics,item.Display.Length==0?item.Id:item.Display,Font,new Rectangle(e.Bounds.X+36,e.Bounds.Y+5,e.Bounds.Width-42,22),main,TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
            using(var small=new Font(Font.FontFamily,8))TextRenderer.DrawText(e.Graphics,item.Subtitle.Length==0?item.Id:item.Subtitle,small,new Rectangle(e.Bounds.X+36,e.Bounds.Y+27,e.Bounds.Width-42,18),secondary,TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
            if(Focused&&selected)e.DrawFocusRectangle();
        }
    }

    sealed class SidebarButton : Control
    {
        public string Glyph = "gear";
        public bool Accent;
        bool hot;
        public SidebarButton()
        {
            Size=new Size(45,42); Margin=new Padding(1,0,2,0); Cursor=Cursors.Hand; TabStop=true;
            ForeColor=Color.White; BackColor=Theme.Navy;
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Color fill=Accent?(hot?Color.FromArgb(0,70,180):Theme.Blue):(hot?Color.FromArgb(28,61,94):Theme.Navy);
            e.Graphics.Clear(fill);
            if(Glyph.StartsWith("text:",StringComparison.Ordinal))
            {
                using(var font=new Font("Segoe UI",15,FontStyle.Regular))
                    TextRenderer.DrawText(e.Graphics,Glyph.Substring(5),font,ClientRectangle,ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);
            }
            else IconCache.DrawBuiltin(e.Graphics,Glyph,new Rectangle((Width-27)/2,(Height-27)/2,27,27),ForeColor);
            if(Focused) ControlPaint.DrawFocusRectangle(e.Graphics,ClientRectangle);
        }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hot=true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hot=false; Invalidate(); }
        protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if(e.KeyCode==Keys.Enter || e.KeyCode==Keys.Space) { OnClick(EventArgs.Empty); e.Handled=true; } }
    }

    sealed class VectorButton : Control
    {
        public string Symbol = "minus";
        public bool DrawBorder = true;
        public bool Primary;
        public bool Danger;
        public float GlyphScale=1f;
        bool hot;
        public VectorButton()
        {
            BackColor=Color.White; ForeColor=Theme.Ink; Cursor=Cursors.Hand;
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Color fill=!Enabled?SystemColors.Control:Danger?(hot?Color.FromArgb(154,29,35):Theme.Danger):Primary?(hot?Color.FromArgb(0,70,180):Theme.Blue):(hot?Theme.TabHover:BackColor);
            e.Graphics.Clear(fill);
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            if(DrawBorder) using(var border=new Pen(Theme.Line)) e.Graphics.DrawRectangle(border,0,0,Width-1,Height-1);
            float cx=Width/2f,cy=(Height-1)/2f;
            float scale=Math.Max(.75f,GlyphScale);
            using(var pen=new Pen(Enabled?ForeColor:SystemColors.GrayText,1.35f*scale))
            {
                pen.StartCap=LineCap.Round; pen.EndCap=LineCap.Round;
                if(Symbol=="plus") {e.Graphics.DrawLine(pen,cx-3.5f*scale,cy,cx+3.5f*scale,cy);e.Graphics.DrawLine(pen,cx,cy-3.5f*scale,cx,cy+3.5f*scale);}
                else if(Symbol=="minus") e.Graphics.DrawLine(pen,cx-3.5f*scale,cy,cx+3.5f*scale,cy);
                else if(Symbol=="trash")
                {
                    e.Graphics.DrawRectangle(pen,cx-4.5f*scale,cy-4f*scale,9f*scale,10f*scale);
                    e.Graphics.DrawLine(pen,cx-6f*scale,cy-6f*scale,cx+6f*scale,cy-6f*scale);
                    e.Graphics.DrawLine(pen,cx-2.5f*scale,cy-7.5f*scale,cx+2.5f*scale,cy-7.5f*scale);
                    e.Graphics.DrawLine(pen,cx-1.8f*scale,cy-2f*scale,cx-1.8f*scale,cy+4f*scale);
                    e.Graphics.DrawLine(pen,cx+1.8f*scale,cy-2f*scale,cx+1.8f*scale,cy+4f*scale);
                }
                else if(Symbol=="reset")
                {
                    var r=new RectangleF(cx-5.5f*scale,cy-5.5f*scale,11*scale,11*scale); e.Graphics.DrawArc(pen,r,-55,285);
                    using(var brush=new SolidBrush(Enabled?ForeColor:SystemColors.GrayText))
                        e.Graphics.FillPolygon(brush,new[] {new PointF(cx+5.8f*scale,cy-4.5f*scale),new PointF(cx+1.2f*scale,cy-4.9f*scale),new PointF(cx+4.7f*scale,cy-1.5f*scale)});
                }
            }
            if(Focused) ControlPaint.DrawFocusRectangle(e.Graphics,ClientRectangle);
        }
        protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hot=true;Invalidate();}
        protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hot=false;Invalidate();}
        protected override void OnEnabledChanged(EventArgs e){base.OnEnabledChanged(e);Invalidate();}
        protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space){OnClick(EventArgs.Empty);e.Handled=true;}}
        // Test hook (0.4.45): the same path a mouse click takes.
        internal void PerformTestClick(){OnClick(EventArgs.Empty);}
    }

    sealed class SidebarBar : FlowLayoutPanel
    {
        public SidebarBar()
        {
            Dock=DockStyle.Fill; WrapContents=false; FlowDirection=FlowDirection.LeftToRight;
            Padding=new Padding(2,7,0,0); Margin=Padding.Empty; BackColor=Theme.Navy;
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using(var pen=new Pen(Color.FromArgb(118,145,174))) e.Graphics.DrawLine(pen,0,0,Width,0);
        }
    }
}
