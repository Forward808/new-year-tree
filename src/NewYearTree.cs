using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Serialization;

[Serializable]
public class TreeSettings
{
    public string Language = "ru";
    public int Size = 100;
    public int Sway = 45;
    public bool SwayEnabled = true;
    public int LightMode = 0;
    public int SnowIntensity = 0;
    public int Brightness = 85;
    public int Speed = 55;
    public int Palette = 0;
    public bool Lights = true;
    public bool Ornaments = true;
    public bool Snow = true;
    public bool Countdown = true;
    public bool OnTop = true;
    public bool Locked = false;
    public int X = -1;
    public int Y = -1;
}

public static class Ui
{
    static readonly Dictionary<string, string> English = new Dictionary<string, string>
    {
        { "Ёлочка · До Нового года", "Christmas Tree · New Year Countdown" },
        { "Настройки…", "Settings…" }, { "Показать / скрыть ёлочку", "Show / hide tree" },
        { "Вернуть на экран", "Bring tree back on screen" }, { "Выход", "Exit" },
        { "Ёлочка — настройки и выход", "Christmas Tree — settings and exit" },
        { "Настройки ёлочки", "Christmas Tree Settings" }, { "Размер", "Size" },
        { "Покачивать игрушки", "Sway ornaments" }, { "Размах покачивания", "Sway amount" },
        { "Яркость гирлянды", "Light brightness" }, { "Скорость гирлянды", "Light speed" },
        { "Режим гирлянды", "Light mode" }, { "Мерцание", "Twinkle" },
        { "Попеременное мигание", "Alternating lights" }, { "Бегущие огни", "Chase" },
        { "Плавная пульсация", "Slow glow" }, { "Постоянный свет", "Steady lights" },
        { "Цвета гирлянды", "Light colors" }, { "Разноцветная", "Multicolor" },
        { "Тёплое золото", "Warm gold" }, { "Ледяная сине-белая", "Ice blue and white" },
        { "Включить гирлянду", "Enable lights" }, { "Ёлочные игрушки", "Show ornaments" },
        { "Падающий снег", "Falling snow" }, { "Сила снегопада", "Snowfall intensity" },
        { "Лёгкий", "Light" }, { "Средний", "Medium" }, { "Сильный", "Heavy" },
        { "Обратный отсчёт до Нового года", "New Year countdown" },
        { "Поверх других окон", "Always on top" }, { "Закрепить положение ёлочки", "Lock tree position" },
        { "Готово", "Done" },
        { "Изменения сохраняются автоматически.\nПеретаскивайте ёлочку левой кнопкой мыши.\nПравая кнопка — меню. Двойной щелчок — настройки.\nДата и время берутся из Windows.",
          "Changes are saved automatically.\nDrag the tree with the left mouse button.\nRight-click: menu. Double-click: settings.\nDate and time come from Windows." }
    };
    public static string Text(TreeSettings settings, string russian)
    { string english; return settings.Language == "en" && English.TryGetValue(russian, out english) ? english : russian; }
}

public static class Program
{
    public static bool SmokeTest;
    public static DateTime NextNewYear(DateTime now) { return new DateTime(now.Year + 1, 1, 1); }
    [STAThread]
    public static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        SmokeTest = args.Length > 0 && args[0] == "--smoke-test";
        if (args.Length > 0 && args[0] == "--self-test")
        {
            if (NextNewYear(new DateTime(2026, 12, 31, 23, 59, 59)) != new DateTime(2027, 1, 1)) Environment.Exit(1);
            if (NextNewYear(new DateTime(2027, 1, 1)) != new DateTime(2028, 1, 1)) Environment.Exit(2);
            if ((NextNewYear(new DateTime(2028, 1, 1)) - new DateTime(2028, 1, 1)).Days != 366) Environment.Exit(3);
            TreeSettings original = new TreeSettings(); original.Size = 125; original.Snow = false; original.Language = "en";
            XmlSerializer serializer = new XmlSerializer(typeof(TreeSettings));
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.Serialize(stream, original); stream.Position = 0;
                TreeSettings copy = (TreeSettings)serializer.Deserialize(stream);
                if (copy.Size != 125 || copy.Snow || copy.Language != "en") Environment.Exit(4);
            }
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-result.txt"), "PASS: year rollover, January 1, leap year, settings roundtrip.");
            return;
        }
        using (TreeWindow tree = new TreeWindow())
        {
            if (args.Length > 0 && args[0] == "--language-test") { tree.VerifyLanguages(); return; }
            if (args.Length > 0 && args[0] == "--render-test") { tree.VerifyRendering(); return; }
            if (args.Length > 0 && args[0] == "--preview")
            {
                using (Bitmap preview = tree.RenderPreview()) preview.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preview.png"), ImageFormat.Png);
                using (Bitmap preview = tree.RenderPreview(3.5)) preview.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preview-motion.png"), ImageFormat.Png);
                using (Bitmap preview = tree.RenderPreview(2, true)) preview.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preview-transparent.png"), ImageFormat.Png);
                using (Bitmap transparent = tree.RenderPreview(2, true))
                using (Bitmap white = new Bitmap(transparent.Width, transparent.Height))
                { using (Graphics g = Graphics.FromImage(white)) { g.Clear(Color.White); g.DrawImageUnscaled(transparent, 0, 0); } white.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "preview-white.png"), ImageFormat.Png); }
                return;
            }
            Application.Run(tree);
        }
    }
}

public class TreeWindow : Form
{
    const int W = 400, H = 665;
    readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NewYearTree", "settings.xml");
    TreeSettings settings;
    Bitmap foliage;
    readonly Timer animation = new Timer();
    readonly System.Diagnostics.Stopwatch elapsed = System.Diagnostics.Stopwatch.StartNew();
    readonly List<PointF> bulbs = new List<PointF>();
    readonly List<PointF> ornaments = new List<PointF>();
    readonly List<Bitmap> sprites = new List<Bitmap>();
    readonly float[] toySizes = { 72, 76, 69, 74, 78, 65, 76, 67, 66, 71, 73, 64 };
    readonly Font numbers = new Font("Segoe UI", 18, FontStyle.Bold);
    static readonly Dictionary<long, Bitmap> glowCache = new Dictionary<long, Bitmap>();
    Bitmap renderSurface;
    Bitmap renderBackdrop;
    long frameTicks;
    int frameCount;
    readonly NotifyIcon tray = new NotifyIcon();
    bool dragging;
    Point mouseOrigin, windowOrigin;
    SettingsWindow settingsWindow;

    public TreeWindow()
    {
        settings = LoadSettings();
        Text = "Ёлочка · До Нового года";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        BackColor = Color.Black;
        StartPosition = FormStartPosition.Manual;
        Icon = SystemIcons.Application;
        ApplySize();
        Rectangle area = Screen.PrimaryScreen.WorkingArea;
        Location = new Point(settings.X < 0 ? area.Right - Width - 32 : settings.X, settings.Y < 0 ? area.Bottom - Height - 20 : settings.Y);
        KeepVisible();
        TopMost = settings.OnTop;
        BuildTree();
        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Items.Add("Настройки…", null, delegate { OpenSettings(); });
        menu.Items.Add("Показать / скрыть ёлочку", null, delegate { Visible = !Visible; });
        menu.Items.Add("Вернуть на экран", null, delegate { Location = new Point(Screen.PrimaryScreen.WorkingArea.Right - Width - 30, Screen.PrimaryScreen.WorkingArea.Bottom - Height - 20); KeepVisible(); Show(); SaveSettings(); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, delegate { Close(); });
        ContextMenuStrip = menu;
        tray.Icon = Icon; tray.Text = "Ёлочка — настройки и выход"; tray.ContextMenuStrip = menu; tray.Visible = true;
        RefreshLanguage();
        tray.DoubleClick += delegate { Show(); OpenSettings(); };
        MouseDown += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left && !settings.Locked) { dragging = true; mouseOrigin = Cursor.Position; windowOrigin = Location; Capture = true; } };
        MouseMove += delegate { if (dragging) Location = new Point(windowOrigin.X + Cursor.Position.X - mouseOrigin.X, windowOrigin.Y + Cursor.Position.Y - mouseOrigin.Y); };
        MouseUp += delegate { if (dragging) { dragging = false; Capture = false; KeepVisible(); SaveSettings(); } };
        MouseDoubleClick += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) OpenSettings(); };
        Shown += delegate
        {
            DrawFrame();
            if (Program.SmokeTest)
            {
                using (SettingsWindow dialog = new SettingsWindow(settings, delegate { })) { dialog.CreateControl(); }
                Timer finish = new Timer { Interval = 1200 };
                finish.Tick += delegate { finish.Stop(); finish.Dispose(); File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "smoke-result.txt"), "PASS: native transparent window, animated frames, settings controls, graceful shutdown. Frames: " + frameCount + "; average draw: " + (1000.0 * frameTicks / System.Diagnostics.Stopwatch.Frequency / Math.Max(1, frameCount)).ToString("F1", CultureInfo.InvariantCulture) + " ms."); Close(); };
                finish.Start();
            }
        };
        animation.Interval = 15; animation.Tick += delegate { if (Visible) DrawFrame(); }; animation.Start();
        FormClosing += delegate { SaveSettings(); };
    }

    TreeSettings LoadSettings()
    {
        try { using (FileStream stream = File.OpenRead(settingsPath)) { TreeSettings s = (TreeSettings)new XmlSerializer(typeof(TreeSettings)).Deserialize(stream); s.Size = Math.Max(55, Math.Min(150, s.Size)); s.Palette = Math.Max(0, Math.Min(2, s.Palette)); s.Brightness = Math.Max(15, Math.Min(100, s.Brightness)); s.Speed = Math.Max(0, Math.Min(100, s.Speed)); s.Sway = Math.Max(0, Math.Min(100, s.Sway)); s.LightMode = Math.Max(0, Math.Min(4, s.LightMode)); s.SnowIntensity = Math.Max(0, Math.Min(2, s.SnowIntensity)); return s; } }
        catch (Exception e) { if (!(e is IOException || e is InvalidOperationException || e is UnauthorizedAccessException)) throw; return new TreeSettings(); }
    }
    void SaveSettings()
    {
        if (Program.SmokeTest) return;
        settings.X = Left; settings.Y = Top;
        try { Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)); using (FileStream stream = File.Create(settingsPath)) new XmlSerializer(typeof(TreeSettings)).Serialize(stream, settings); }
        catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
    void ApplySize() { ClientSize = new Size(W * settings.Size / 100, H * settings.Size / 100); }
    void KeepVisible()
    {
        Rectangle area = Screen.FromRectangle(Bounds).WorkingArea;
        Location = new Point(Math.Max(area.Left, Math.Min(Left, area.Right - Width)), Math.Max(area.Top, Math.Min(Top, area.Bottom - Height)));
    }
    void OpenSettings()
    {
        if (settingsWindow != null && !settingsWindow.IsDisposed) { settingsWindow.Activate(); return; }
        settingsWindow = new SettingsWindow(settings, delegate { ApplySize(); TopMost = settings.OnTop; KeepVisible(); RefreshLanguage(); SaveSettings(); Invalidate(); });
        settingsWindow.Show();
    }
    void RefreshLanguage()
    {
        Text = Ui.Text(settings, "Ёлочка · До Нового года");
        tray.Text = Ui.Text(settings, "Ёлочка — настройки и выход");
        string[] captions = { "Настройки…", "Показать / скрыть ёлочку", "Вернуть на экран", "", "Выход" };
        for (int i = 0; i < captions.Length; i++) if (captions[i].Length > 0) ContextMenuStrip.Items[i].Text = Ui.Text(settings, captions[i]);
    }
    public void VerifyLanguages()
    {
        using (SettingsWindow dialog = new SettingsWindow(settings, RefreshLanguage))
        {
            dialog.SetLanguage("en");
            if (dialog.Text != "Christmas Tree Settings" || ContextMenuStrip.Items[0].Text != "Settings…" || settings.Language != "en") throw new Exception("English switch failed");
            if (!dialog.ContainsCaption("Sway ornaments") || !dialog.ContainsCaption("Light mode") || !dialog.ContainsCaption("Snowfall intensity") || !dialog.ContainsCaption("Done")) throw new Exception("English controls missing");
            dialog.SetLanguage("ru");
            if (dialog.Text != "Настройки ёлочки" || ContextMenuStrip.Items[0].Text != "Настройки…" || !dialog.ContainsCaption("Покачивать игрушки")) throw new Exception("Russian switch failed");
        }
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language-result.txt"), "PASS: live Russian/English switch, controls, settings title, tray menu, language setting.");
    }
    static void Line(Graphics g, Color c, float width, float x1, float y1, float x2, float y2)
    { using (Pen p = new Pen(c, width)) { p.StartCap = p.EndCap = LineCap.Round; g.DrawLine(p, x1, y1, x2, y2); } }
    static float Rand(Random r, float lo, float hi) { return lo + (float)r.NextDouble() * (hi - lo); }
    static Bitmap ReadAsset(string name)
    {
        using (Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
        {
            if (stream == null) throw new InvalidOperationException("Missing embedded asset: " + name);
            using (Bitmap image = new Bitmap(stream)) return new Bitmap(image);
        }
    }
    void BuildTree()
    {
        foliage = new Bitmap(W, H, PixelFormat.Format32bppPArgb);
        using (Bitmap source = ReadAsset("tree.png"))
        using (Graphics g = Graphics.FromImage(foliage))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(source, new Rectangle(0, 0, W, 610));
        }
        using (Bitmap atlas = ReadAsset("ornaments.png"))
        {
            for (int i = 0; i < 12; i++)
            {
                int x0 = (i % 4) * atlas.Width / 4, y0 = (i / 4) * atlas.Height / 3;
                int x1 = (i % 4 + 1) * atlas.Width / 4, y1 = (i / 4 + 1) * atlas.Height / 3;
                int left = x1, top = y1, right = x0, bottom = y0;
                for (int yy = y0; yy < y1; yy++) for (int xx = x0; xx < x1; xx++)
                    if (atlas.GetPixel(xx, yy).A > 15) { left = Math.Min(left, xx); top = Math.Min(top, yy); right = Math.Max(right, xx); bottom = Math.Max(bottom, yy); }
                if (right <= left || bottom <= top) throw new InvalidOperationException("Empty ornament sprite " + i);
                using (Bitmap cropped = atlas.Clone(Rectangle.FromLTRB(left, top, right + 1, bottom + 1), PixelFormat.Format32bppPArgb))
                {
                    float targetWidth = toySizes[i] * 2, targetHeight = targetWidth * cropped.Height / cropped.Width;
                    if (targetHeight > 172) { targetWidth *= 172 / targetHeight; targetHeight = 172; }
                    Bitmap sprite = new Bitmap((int)Math.Ceiling(targetWidth), (int)Math.Ceiling(targetHeight), PixelFormat.Format32bppPArgb);
                    using (Graphics g = Graphics.FromImage(sprite)) { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.DrawImage(cropped, new Rectangle(0, 0, sprite.Width, sprite.Height)); }
                    sprites.Add(sprite);
                }
            }
        }
        for (int row = 0; row < 8; row++)
        {
            float y = 111 + row * 60, width = 26 + row * 21;
            int count = 7 + row * 3;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                bulbs.Add(new PointF(200 - width + width * 2 * t, y + (float)Math.Sin(t * Math.PI) * 23 + t * 14));
            }
        }
        ornaments.AddRange(new PointF[] {
            new PointF(162, 133), new PointF(244, 165), new PointF(122, 223), new PointF(282, 240),
            new PointF(101, 309), new PointF(202, 285), new PointF(292, 340), new PointF(88, 408),
            new PointF(319, 418), new PointF(98, 506), new PointF(202, 522), new PointF(307, 508) });
        AddBranchSnow();
        using (Bitmap gifts = ReadAsset("winter-base.png"))
        {
            int left = gifts.Width, top = gifts.Height, right = 0, bottom = 0;
            for (int y = 0; y < gifts.Height; y++) for (int x = 0; x < gifts.Width; x++)
                if (gifts.GetPixel(x, y).A > 12) { left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x); bottom = Math.Max(bottom, y); }
            if (right <= left || bottom <= top) throw new InvalidOperationException("Gift asset is empty");
            float scale = Math.Min(365f / (right - left + 1), 100f / (bottom - top + 1));
            float width = (right - left + 1) * scale, height = (bottom - top + 1) * scale;
            RectangleF destination = new RectangleF((W - width) / 2, 659 - height, width, height);
            RectangleF source = new RectangleF(left, top, right - left + 1, bottom - top + 1);
            Bitmap combined = new Bitmap(W, H, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(combined))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(gifts, destination, source, GraphicsUnit.Pixel);
                g.DrawImageUnscaled(foliage, 0, 0);
                g.SetClip(new Rectangle(0, 590, W, H - 590));
                g.DrawImage(gifts, destination, source, GraphicsUnit.Pixel);
            }
            foliage.Dispose(); foliage = combined;
        }
    }
    void AddBranchSnow()
    {
        Random random = new Random(730);
        using (Bitmap coverage = new Bitmap(W, H))
        {
            using (Graphics g = Graphics.FromImage(coverage))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                for (int tier = 0; tier < 12; tier++)
                {
                    float y = 115 + tier * 39, reach = 18 + tier * 14;
                    for (int side = -1; side <= 1; side += 2)
                    for (int patch = 0; patch < 4; patch++)
                    {
                        float x = 200 + side * reach * Rand(random, .35f, 1);
                        float yy = y + Rand(random, -9, 9), length = Rand(random, 5, 15);
                        Line(g, Color.FromArgb(175, 177, 205, 219), 3.3f, x - length / 2, yy + 1.3f, x + length / 2, yy - .8f);
                        Line(g, Color.FromArgb(225, 242, 249, 251), 2.4f, x - length / 2, yy, x + length / 2, yy - 1.4f);
                        for (int dust = 0; dust < 4; dust++)
                        {
                            using (SolidBrush b = new SolidBrush(Color.FromArgb(random.Next(110, 230), 244, 250, 255)))
                                g.FillEllipse(b, x + Rand(random, -length, length), yy + Rand(random, -3, 5), Rand(random, .8f, 2), Rand(random, .7f, 1.5f));
                        }
                    }
                }
            }
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                Color snow = coverage.GetPixel(x, y), tree = foliage.GetPixel(x, y);
                if (snow.A > 0 && (tree.A < 200 || tree.G < tree.R * 1.1)) coverage.SetPixel(x, y, Color.Transparent);
            }
            using (Graphics g = Graphics.FromImage(foliage)) g.DrawImageUnscaled(coverage, 0, 0);
        }
    }
    static GraphicsPath Rounded(RectangleF rect, float radius)
    {
        GraphicsPath p = new GraphicsPath(); float d = radius * 2;
        p.AddArc(rect.X, rect.Y, d, d, 180, 90); p.AddArc(rect.Right - d, rect.Y, d, d, 270, 90); p.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); p.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p;
    }
    static void Glow(Graphics g, PointF p, Color color, float radius, float strength)
    {
        int level = Math.Max(0, Math.Min(32, (int)(strength * 32)));
        if (level == 0) return;
        long key = ((long)(uint)color.ToArgb() << 6) | (uint)level;
        Bitmap glow;
        if (!glowCache.TryGetValue(key, out glow))
        {
            glow = new Bitmap(48, 48, PixelFormat.Format32bppPArgb);
            using (Graphics cg = Graphics.FromImage(glow))
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(0, 0, 48, 48);
                using (PathGradientBrush b = new PathGradientBrush(path)) { b.CenterColor = Color.FromArgb(160 * level / 32, color); b.SurroundColors = new Color[] { Color.FromArgb(0, color) }; cg.FillPath(b, path); }
            }
            glowCache.Add(key, glow);
        }
        g.DrawImage(glow, new RectangleF(p.X - radius, p.Y - radius, radius * 2, radius * 2));
    }
    public static double LightLevel(int mode, int i, double time, int speed)
    {
        double t = time * (.35 + speed / 32.0);
        switch (mode)
        {
            case 1: return Math.Pow(.5 + .5 * Math.Sin(t * Math.PI * 2 + (i % 2) * Math.PI), 3);
            case 2: double phase = ((i / 7.0 - t * 4) % 10 + 10) % 10; return Math.Exp(-phase * phase / 2);
            case 3: return Math.Pow(.5 + .5 * Math.Sin(t * 2 + i * .0001), 2);
            case 4: return 1;
            default: return Math.Pow(.5 + .5 * Math.Sin(t * (2.4 + (i % 5) * .11) + i * 2.399), 5);
        }
    }
    Color LightColor(int i)
    {
        if (settings.Palette == 1) return i % 2 == 0 ? Color.FromArgb(255, 201, 80) : Color.FromArgb(255, 241, 180);
        if (settings.Palette == 2) return i % 2 == 0 ? Color.FromArgb(91, 192, 255) : Color.FromArgb(218, 240, 255);
        return new Color[] { Color.FromArgb(255, 51, 194), Color.FromArgb(65, 165, 255), Color.FromArgb(255, 209, 75) }[(i / 8) % 3];
    }
    void DrawLights(Graphics g, double time, bool front)
    {
        GraphicsState quality = g.Save();
        g.InterpolationMode = InterpolationMode.Bilinear;
        int index = 0;
        for (int row = 0; row < 8; row++)
        {
            int count = 7 + row * 3;
            if (!front)
                for (int j = 1; j < count; j++)
                {
                    PointF a = bulbs[index + j - 1], b = bulbs[index + j];
                    Line(g, Color.FromArgb(172, 142, 84), .55f, a.X, a.Y, b.X, b.Y);
                }
            for (int j = 0; j < count; j++)
            {
                PointF p = bulbs[index + j];
                Color color = LightColor(index + j);
                double wave = LightLevel(settings.LightMode, index + j, time, settings.Speed);
                float strength = (float)wave * settings.Brightness / 100f;
                if (strength < .025f) continue;
                if (front)
                {
                    if (j % 3 != 0) continue;
                    Glow(g, p, color, 11, strength * .32f);
                }
                else
                {
                    Glow(g, p, color, 14, strength);
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb((int)(230 * strength), color))) g.FillEllipse(brush, p.X - 2, p.Y - 2, 4, 4);
                    if (j % 3 == 0)
                    {
                        float ray = (float)(5 + wave * 5);
                        Color rayColor = Color.FromArgb((int)(220 * strength), color);
                        Line(g, rayColor, 1.15f, p.X - ray, p.Y, p.X + ray, p.Y);
                        Line(g, rayColor, 1.15f, p.X, p.Y - ray, p.X, p.Y + ray);
                        Line(g, Color.FromArgb((int)(245 * strength), Color.White), .8f, p.X - ray * .5f, p.Y, p.X + ray * .5f, p.Y);
                        Line(g, Color.FromArgb((int)(245 * strength), Color.White), .8f, p.X, p.Y - ray * .5f, p.X, p.Y + ray * .5f);
                        Line(g, Color.FromArgb((int)(150 * strength), Color.White), .6f, p.X - ray * .55f, p.Y - ray * .55f, p.X + ray * .55f, p.Y + ray * .55f);
                        Line(g, Color.FromArgb((int)(150 * strength), Color.White), .6f, p.X + ray * .55f, p.Y - ray * .55f, p.X - ray * .55f, p.Y + ray * .55f);
                    }
                    using (SolidBrush brush = new SolidBrush(Color.FromArgb((int)(255 * strength), Color.White))) g.FillEllipse(brush, p.X - .9f, p.Y - .9f, 1.8f, 1.8f);
                }
            }
            index += count;
        }
        g.Restore(quality);
    }
    void PaintScene(Graphics g, DateTime now, double time, bool backgroundReady = false)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.Bilinear;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        if (!backgroundReady) g.DrawImageUnscaled(foliage, 0, 0);
        if (settings.Lights) DrawLights(g, time, false);
        if (settings.Ornaments)
        {
            for (int i = 0; i < ornaments.Count; i++)
            {
                PointF anchor = ornaments[i]; Bitmap sprite = sprites[i];
                float width = toySizes[i], height = width * sprite.Height / sprite.Width;
                if (height > 86) { width *= 86 / height; height = 86; }
                GraphicsState saved = g.Save();
                g.TranslateTransform(anchor.X, anchor.Y);
                g.RotateTransform((float)(settings.SwayEnabled ? Math.Sin(time * (.57 + i * .014) + i * 2.13) * 3.4 * settings.Sway / 100.0 : 0));
                Line(g, Color.FromArgb(216, 174, 77), .8f, 0, -6, 0, 5);
                g.DrawImage(sprite, new RectangleF(-width / 2, 0, width, height));
                g.Restore(saved);
            }
        }
        if (settings.Countdown) DrawCountdown(g, now);
        if (settings.Lights)
        {
            float power = (float)(.72 + .28 * Math.Sin(time * 1.8)) * settings.Brightness / 100f;
            Glow(g, new PointF(200, 34), Color.Gold, 21, power);
        }
        if (settings.Snow)
        {
            Random r = new Random(141);
            int count = settings.SnowIntensity == 0 ? 35 : settings.SnowIntensity == 1 ? 85 : 170;
            for (int i = 0; i < count; i++)
            {
                float depth = Rand(r, .35f, 1), speed = (9 + depth * 19) * (1 + settings.SnowIntensity * .18f);
                float x = Rand(r, 10, 390), y = (Rand(r, 0, H) + (float)time * speed) % H;
                float size = 3 + depth * 5.2f; x += (float)Math.Sin(time * .32 + i) * (3 + depth * 6);
                using (SolidBrush brush = new SolidBrush(Color.FromArgb((int)(110 + depth * 130), 236, 247, 255))) g.FillEllipse(brush, x, y, size, size);
            }
        }
    }
    void DrawCountdown(Graphics g, DateTime now)
    {
        DateTime target = Program.NextNewYear(now); TimeSpan left = target - now;
        GraphicsState state = g.Save();
        g.TranslateTransform(200, 453);
        const int x = -82, y = 0, width = 164, height = 44;
        using (Pen ring = new Pen(Color.FromArgb(232, 180, 57), 1.7f)) { g.DrawEllipse(ring, -5, y - 15, 10, 13); }
        using (LinearGradientBrush gold = new LinearGradientBrush(new Rectangle(x, y, width, height), Color.FromArgb(255, 245, 177), Color.FromArgb(187, 105, 17), 85f))
        using (GraphicsPath outer = Rounded(new RectangleF(x, y, width, height), 14)) g.FillPath(gold, outer);
        using (Pen edge = new Pen(Color.FromArgb(125, 65, 13), 1.6f))
        using (GraphicsPath outer = Rounded(new RectangleF(x, y, width, height), 14)) g.DrawPath(edge, outer);
        using (Pen edge = new Pen(Color.FromArgb(255, 241, 157), 1.4f))
        using (GraphicsPath shine = Rounded(new RectangleF(x + 3, y + 3, width - 6, height - 6), 11)) g.DrawPath(edge, shine);
        using (GraphicsPath inset = Rounded(new RectangleF(x + 6, y + 6, width - 12, height - 12), 9))
        using (LinearGradientBrush dark = new LinearGradientBrush(new Rectangle(x, y, width, height), Color.FromArgb(48, 49, 45), Color.FromArgb(15, 20, 20), 80f)) g.FillPath(dark, inset);
        using (StringFormat center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        using (SolidBrush white = new SolidBrush(Color.FromArgb(255, 248, 222)))
        {
            string digits = left.Days.ToString(CultureInfo.InvariantCulture) + ":" + left.Hours.ToString("00") + ":" + left.Minutes.ToString("00");
            g.DrawString(digits, numbers, white, new RectangleF(x + 5, y + 4, width - 10, height - 8), center);
        }
        g.Restore(state);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        DrawFrame();
    }
    protected override CreateParams CreateParams
    {
        get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x80000 | 0x80; return cp; }
    }
    [StructLayout(LayoutKind.Sequential)] struct NativePoint { public int X, Y; public NativePoint(int x, int y) { X = x; Y = y; } }
    [StructLayout(LayoutKind.Sequential)] struct NativeSize { public int W, H; public NativeSize(int w, int h) { W = w; H = h; } }
    [StructLayout(LayoutKind.Sequential, Pack = 1)] struct Blend { public byte Operation, Flags, Alpha, Format; }
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
    [DllImport("user32.dll", SetLastError = true)] static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr screen, ref NativePoint position, ref NativeSize size, IntPtr source, ref NativePoint origin, int key, ref Blend blend, int flags);
    void DrawFrame()
    {
        if (!IsHandleCreated || foliage == null || ClientSize.Width < 1 || ClientSize.Height < 1) return;
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        using (Bitmap frame = new Bitmap(ClientSize.Width, ClientSize.Height, PixelFormat.Format32bppArgb))
        {
            if (renderSurface == null || renderSurface.Width != ClientSize.Width * 2 || renderSurface.Height != ClientSize.Height * 2)
            {
                if (renderSurface != null) renderSurface.Dispose(); if (renderBackdrop != null) renderBackdrop.Dispose();
                renderSurface = new Bitmap(ClientSize.Width * 2, ClientSize.Height * 2, PixelFormat.Format32bppPArgb);
                renderBackdrop = new Bitmap(renderSurface.Width, renderSurface.Height, PixelFormat.Format32bppPArgb);
                using (Graphics g = Graphics.FromImage(renderBackdrop)) { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.DrawImage(foliage, new Rectangle(0, 0, renderBackdrop.Width, renderBackdrop.Height)); }
            }
            using (Graphics g = Graphics.FromImage(renderSurface)) { g.CompositingMode = CompositingMode.SourceCopy; g.DrawImageUnscaled(renderBackdrop, 0, 0); g.CompositingMode = CompositingMode.SourceOver; g.ScaleTransform(renderSurface.Width / (float)W, renderSurface.Height / (float)H); PaintScene(g, DateTime.Now, elapsed.Elapsed.TotalSeconds, true); }
            using (Graphics g = Graphics.FromImage(frame)) { g.CompositingMode = CompositingMode.SourceCopy; g.InterpolationMode = InterpolationMode.Bilinear; g.PixelOffsetMode = PixelOffsetMode.HighQuality; g.DrawImage(renderSurface, new Rectangle(0, 0, frame.Width, frame.Height)); }
            IntPtr screen = GetDC(IntPtr.Zero), dc = CreateCompatibleDC(screen), bitmap = IntPtr.Zero, old = IntPtr.Zero;
            try
            {
                bitmap = frame.GetHbitmap(Color.FromArgb(0)); old = SelectObject(dc, bitmap);
                NativePoint position = new NativePoint(Left, Top), origin = new NativePoint(0, 0);
                NativeSize size = new NativeSize(frame.Width, frame.Height);
                Blend blend = new Blend { Operation = 0, Flags = 0, Alpha = 255, Format = 1 };
                if (!UpdateLayeredWindow(Handle, screen, ref position, ref size, dc, ref origin, 0, ref blend, 2)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
            finally { if (old != IntPtr.Zero) SelectObject(dc, old); if (bitmap != IntPtr.Zero) DeleteObject(bitmap); DeleteDC(dc); ReleaseDC(IntPtr.Zero, screen); }
        }
        frameTicks += System.Diagnostics.Stopwatch.GetTimestamp() - started;
        frameCount++;
    }
    public Bitmap RenderPreview(double time = 2, bool transparent = false)
    {
        Bitmap b = new Bitmap(W, H);
        using (Bitmap large = new Bitmap(W * 2, H * 2))
        {
            using (Graphics g = Graphics.FromImage(large)) { g.Clear(Color.Transparent); g.ScaleTransform(2, 2); PaintScene(g, DateTime.Now, time); }
            using (Graphics g = Graphics.FromImage(b)) { g.Clear(transparent ? Color.Transparent : Color.FromArgb(34, 43, 55)); g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.PixelOffsetMode = PixelOffsetMode.HighQuality; g.DrawImage(large, new Rectangle(0, 0, W, H)); }
        }
        return b;
    }
    long FrameHash(double time)
    {
        using (Bitmap frame = new Bitmap(W, H))
        {
            using (Graphics g = Graphics.FromImage(frame)) PaintScene(g, new DateTime(2026, 10, 26, 12, 30, 0), time);
            long hash = 17;
            unchecked { for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) hash = hash * 31 + frame.GetPixel(x, y).ToArgb(); }
            return hash;
        }
    }
    public void VerifyRendering()
    {
        settings.Snow = false; settings.Countdown = true; settings.Ornaments = true; settings.Lights = false; settings.Sway = 100; settings.SwayEnabled = true;
        if (FrameHash(0) == FrameHash(1.5)) throw new Exception("Ornaments do not move");
        settings.Sway = 0;
        if (FrameHash(0) != FrameHash(1.5)) throw new Exception("Sway off does not stop ornaments");
        settings.Sway = 100; settings.SwayEnabled = false;
        if (FrameHash(0) != FrameHash(1.5)) throw new Exception("Sway checkbox does not stop ornaments");
        settings.Lights = true; settings.Speed = 55;
        if (FrameHash(0) == FrameHash(1.5)) throw new Exception("Lights do not animate");
        settings.Speed = 0;
        settings.Lights = false;
        using (Bitmap frame = RenderPreview(0, true)) if (frame.GetPixel(0, 0).A != 0) throw new Exception("Background is opaque");
        if (sprites.Count != 12) throw new Exception("Missing ornaments");
        for (int mode = 0; mode < 4; mode++)
        {
            double lo = 1, hi = 0;
            for (int t = 0; t < 200; t++) { double level = LightLevel(mode, 9, t * .1, 55); lo = Math.Min(lo, level); hi = Math.Max(hi, level); }
            if (lo > .03 || hi < .9) throw new Exception("Light mode does not blink fully: " + mode);
        }
        if (LightLevel(4, 9, 0, 55) != LightLevel(4, 9, 5, 55)) throw new Exception("Steady mode changes");
        settings.Snow = true; settings.SnowIntensity = 0; long lightSnow = FrameHash(2);
        settings.SnowIntensity = 1; long mediumSnow = FrameHash(2);
        settings.SnowIntensity = 2; long heavySnow = FrameHash(2);
        if (lightSnow == mediumSnow || mediumSnow == heavySnow) throw new Exception("Snow strengths are identical");
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "render-result.txt"), "PASS: ornament motion and checkbox, four blinking modes and steady mode, three snowfall strengths, true alpha, twelve sprites.");
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { animation.Dispose(); tray.Visible = false; tray.Dispose(); if (settingsWindow != null) settingsWindow.Dispose(); if (foliage != null) foliage.Dispose(); if (renderSurface != null) renderSurface.Dispose(); if (renderBackdrop != null) renderBackdrop.Dispose(); foreach (Bitmap sprite in sprites) sprite.Dispose(); numbers.Dispose(); if (ContextMenuStrip != null) ContextMenuStrip.Dispose(); }
        base.Dispose(disposing);
    }
}

public class SettingsWindow : Form
{
    readonly TreeSettings settings;
    readonly Action changed;
    readonly Dictionary<Control, string> captions = new Dictionary<Control, string>();
    readonly Dictionary<ComboBox, string[]> choices = new Dictionary<ComboBox, string[]>();
    bool translating;
    int y = 20;
    public SettingsWindow(TreeSettings s, Action onChanged)
    {
        settings = s; changed = onChanged;
        Text = "Настройки ёлочки"; Font = new Font("Segoe UI", 10); FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false; AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(370, 630); StartPosition = FormStartPosition.CenterScreen; TopMost = true;
        BackColor = Color.FromArgb(245, 247, 244);
        AddChoice("Язык / Language", new string[] { "Русский", "English" }, s.Language == "en" ? 1 : 0, delegate(int v) { s.Language = v == 1 ? "en" : "ru"; RefreshTexts(); });
        AddSlider("Размер", 55, 150, s.Size, delegate(int v) { s.Size = v; });
        AddCheck("Покачивать игрушки", s.SwayEnabled, delegate(bool v) { s.SwayEnabled = v; });
        AddSlider("Размах покачивания", 0, 100, s.Sway, delegate(int v) { s.Sway = v; });
        AddSlider("Яркость гирлянды", 15, 100, s.Brightness, delegate(int v) { s.Brightness = v; });
        AddSlider("Скорость гирлянды", 0, 100, s.Speed, delegate(int v) { s.Speed = v; });
        AddChoice("Режим гирлянды", new string[] { "Мерцание", "Попеременное мигание", "Бегущие огни", "Плавная пульсация", "Постоянный свет" }, s.LightMode, delegate(int v) { s.LightMode = v; });
        Label label = new Label { Text = "Цвета гирлянды", Left = 20, Top = y, Width = 320, Height = 24 }; Controls.Add(label); y += 27;
        ComboBox palette = new ComboBox { Left = 23, Top = y, Width = 321, DropDownStyle = ComboBoxStyle.DropDownList };
        palette.Items.AddRange(new object[] { "Разноцветная", "Тёплое золото", "Ледяная сине-белая" }); palette.SelectedIndex = s.Palette;
        palette.SelectedIndexChanged += delegate { if (translating) return; s.Palette = palette.SelectedIndex; changed(); }; Controls.Add(palette); y += 42;
        AddCheck("Включить гирлянду", s.Lights, delegate(bool v) { s.Lights = v; });
        AddCheck("Ёлочные игрушки", s.Ornaments, delegate(bool v) { s.Ornaments = v; });
        AddCheck("Падающий снег", s.Snow, delegate(bool v) { s.Snow = v; });
        AddChoice("Сила снегопада", new string[] { "Лёгкий", "Средний", "Сильный" }, s.SnowIntensity, delegate(int v) { s.SnowIntensity = v; });
        AddCheck("Обратный отсчёт до Нового года", s.Countdown, delegate(bool v) { s.Countdown = v; });
        AddCheck("Поверх других окон", s.OnTop, delegate(bool v) { s.OnTop = v; });
        AddCheck("Закрепить положение ёлочки", s.Locked, delegate(bool v) { s.Locked = v; });
        Label hint = new Label { Text = "Изменения сохраняются автоматически.\nПеретаскивайте ёлочку левой кнопкой мыши.\nПравая кнопка — меню. Двойной щелчок — настройки.\nДата и время берутся из Windows.", Left = 23, Top = y + 8, Width = 327, Height = 82, ForeColor = Color.FromArgb(83, 100, 90), Font = new Font("Segoe UI", 9) }; Controls.Add(hint); y += 100;
        Button done = new Button { Text = "Готово", Left = 243, Top = y, Width = 100, Height = 32 }; done.Click += delegate { Close(); }; Controls.Add(done);
        AutoScroll = true;
        AutoScrollMinSize = new Size(370, y + 48);
        ClientSize = new Size(390, Math.Min(y + 48, Screen.PrimaryScreen.WorkingArea.Height - 85)); AcceptButton = done; CancelButton = done;
        foreach (Control control in Controls)
        {
            ComboBox box = control as ComboBox;
            if (box != null) { string[] items = new string[box.Items.Count]; for (int i = 0; i < items.Length; i++) items[i] = box.Items[i].ToString(); choices.Add(box, items); }
            else if (!(control is TrackBar)) captions.Add(control, control.Text);
        }
        RefreshTexts();
    }
    void AddSlider(string title, int min, int max, int value, Action<int> setter)
    {
        Label label = new Label { Text = title + ": " + value, Left = 20, Top = y, Width = 335, Height = 24 }; Controls.Add(label); y += 24;
        TrackBar slider = new TrackBar { Left = 16, Top = y, Width = 335, Height = 36, Minimum = min, Maximum = max, Value = value, TickStyle = TickStyle.None };
        slider.ValueChanged += delegate { setter(slider.Value); captions[label] = title + ": " + slider.Value; label.Text = Ui.Text(settings, title) + ": " + slider.Value; changed(); }; Controls.Add(slider); y += 43;
    }
    void AddChoice(string title, string[] options, int value, Action<int> setter)
    {
        Controls.Add(new Label { Text = title, Left = 23, Top = y, Width = 325, Height = 24 }); y += 25;
        ComboBox box = new ComboBox { Left = 23, Top = y, Width = 321, DropDownStyle = ComboBoxStyle.DropDownList };
        if (title == "Язык / Language") box.Name = "LanguageSelector";
        box.Items.AddRange(options); box.SelectedIndex = Math.Max(0, Math.Min(options.Length - 1, value));
        box.SelectedIndexChanged += delegate { if (translating) return; setter(box.SelectedIndex); changed(); }; Controls.Add(box); y += 42;
    }
    void AddCheck(string title, bool value, Action<bool> setter)
    {
        CheckBox box = new CheckBox { Text = title, Left = 23, Top = y, Width = 330, Height = 29, Checked = value };
        box.CheckedChanged += delegate { setter(box.Checked); changed(); }; Controls.Add(box); y += 30;
    }
    void RefreshTexts()
    {
        translating = true;
        try
        {
            Text = Ui.Text(settings, "Настройки ёлочки");
            foreach (KeyValuePair<Control, string> item in captions)
            {
                int separator = item.Value.IndexOf(": ", StringComparison.Ordinal);
                item.Key.Text = separator < 0 ? Ui.Text(settings, item.Value) : Ui.Text(settings, item.Value.Substring(0, separator)) + item.Value.Substring(separator);
            }
            foreach (KeyValuePair<ComboBox, string[]> item in choices)
            {
                int index = item.Key.SelectedIndex;
                item.Key.BeginUpdate();
                try { item.Key.Items.Clear(); foreach (string option in item.Value) item.Key.Items.Add(Ui.Text(settings, option)); item.Key.SelectedIndex = index; }
                finally { item.Key.EndUpdate(); }
            }
        }
        finally { translating = false; }
    }
    public void SetLanguage(string language)
    { ((ComboBox)Controls["LanguageSelector"]).SelectedIndex = language == "en" ? 1 : 0; }
    public bool ContainsCaption(string caption)
    { foreach (Control control in Controls) if (control.Text == caption) return true; return false; }
}
