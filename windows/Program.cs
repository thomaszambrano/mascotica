using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

static class Program
{
    [STAThread]
    public const string PipeName = "MatlabPet.Events";

    // `MatlabPet.exe mascota grogu` → "mascota:grogu", igual que `zedpet` en Mac
    static void Main(string[] args)
    {
        var words = args.Where(a => a != "--con-matlab").ToArray();
        var ev = words.Length == 0 ? null : words.Length == 1 ? words[0] : $"{words[0]}:{string.Join(" ", words.Skip(1))}";
        using (var mutex = new Mutex(true, "MatlabPet.SingleInstance", out bool first))
        {
            if (!first)
            {
                // Ya hay una mascota corriendo: le pasa el comando y le presta el foco por si abre el selector
                if (ev != null) { Native.AllowSetForegroundWindow(-1 /* ASFW_ANY */); Send(ev); }
                return;
            }
            Native.SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.Run(new PetForm(args.Contains("--con-matlab"), ev));
        }
    }

    static void Send(string ev)
    {
        try
        {
            using (var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
            {
                pipe.Connect(2000);
                using (var w = new StreamWriter(pipe)) w.WriteLine(ev);
            }
        }
        catch (Exception) { } // si la mascota se está cerrando, no hay a quién avisarle
    }
}

static class Native
{
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool AllowSetForegroundWindow(int pid);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hWnd, int attr, out RECT r, int size);

    // Sin los bordes invisibles de Windows 10/11
    public static Rectangle Bounds(IntPtr hWnd)
    {
        if (DwmGetWindowAttribute(hWnd, 9 /* EXTENDED_FRAME_BOUNDS */, out var r, Marshal.SizeOf<RECT>()) != 0)
            GetWindowRect(hWnd, out r);
        return Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
    }
}

// MARK: - Sprites (compartidos con macOS en sprites/*.json, van embebidos en el .exe)

class Anim
{
    public string[] frames { get; set; }
    public int ticksPerFrame { get; set; }
}

class SpriteSheet
{
    public string id { get; set; }
    public string name { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public Dictionary<string, Dictionary<string, string>> palettes { get; set; }
    public Dictionary<string, string> blink { get; set; }
    public Dictionary<string, Anim> animations { get; set; }
    public Dictionary<string, string[]> frames { get; set; }
}

class Skin
{
    static readonly string[] PalettePreference = { "rosado", "default" };

    // Si un personaje no trae alguna animación, usa la más parecida
    static readonly Dictionary<string, string> Fallback = new Dictionary<string, string>
    {
        ["run"] = "walk", ["sit"] = "idle", ["yawn"] = "idle", ["stretch"] = "idle", ["happy"] = "jump",
        ["scared"] = "jump", ["jump"] = "idle", ["sleep"] = "idle", ["idle"] = "walk",
    };

    public readonly SpriteSheet Sheet;
    public readonly Dictionary<char, SolidBrush> Palette;
    public readonly Dictionary<char, char> Blink;

    public Skin(SpriteSheet sheet)
    {
        Sheet = sheet;
        var colors = PalettePreference.Where(sheet.palettes.ContainsKey).Select(p => sheet.palettes[p]).FirstOrDefault()
                     ?? sheet.palettes.Values.First();
        Palette = colors.ToDictionary(kv => kv.Key[0], kv => new SolidBrush(ColorTranslator.FromHtml(kv.Value)));
        Blink = (sheet.blink ?? new Dictionary<string, string>()).ToDictionary(kv => kv.Key[0], kv => kv.Value[0]);
    }

    public string Frame(string anim, int t)
    {
        while (!Sheet.animations.ContainsKey(anim) && Fallback.TryGetValue(anim, out var f)) anim = f;
        if (!Sheet.animations.TryGetValue(anim, out var a)) return Sheet.frames.Keys.First();
        return a.frames[t / Math.Max(a.ticksPerFrame, 1) % a.frames.Length];
    }

    /// Dibujo cuadrado de la mascota quieta, un pixel por celda (icono y selector)
    public Bitmap Preview()
    {
        var rows = Sheet.frames[Frame("idle", 0)];
        int size = Math.Max(Sheet.width, Sheet.height), ox = (size - Sheet.width) / 2, oy = (size - Sheet.height) / 2;
        var bmp = new Bitmap(size, size);
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
                if (Palette.TryGetValue(rows[y][x], out var b)) bmp.SetPixel(x + ox, y + oy, b.Color);
        return bmp;
    }

    public static List<Skin> LoadAll()
    {
        var asm = Assembly.GetExecutingAssembly();
        var json = new JavaScriptSerializer();
        return asm.GetManifestResourceNames()
            .Where(n => n.StartsWith("pets."))
            .Select(n =>
            {
                using (var r = new StreamReader(asm.GetManifestResourceStream(n)))
                    return new Skin(json.Deserialize<SpriteSheet>(r.ReadToEnd()));
            })
            .OrderBy(k => k.Sheet.id == "cat" ? 0 : 1).ThenBy(k => k.Sheet.name)
            .ToList();
    }
}

enum Mood { Walking, Idle, Sleeping, OneShot, Airborne }

class PetForm : Form
{
    static readonly string[] ProcessNames = { "MATLAB", "MATLABWindow" };
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string SettingsKey = @"Software\MatlabPet";

    readonly List<Skin> skins = Skin.LoadAll();
    readonly int s, extraTop, statusBarOffset;
    readonly float u; // velocidad escalada al DPI
    readonly Random rnd = new Random();
    readonly int ownPid = Process.GetCurrentProcess().Id;
    readonly NotifyIcon tray;
    readonly ToolStripMenuItem petsMenu;
    readonly bool exitWithMatlab;
    bool seenMatlab;
    HashSet<int> matlabPids = new HashSet<int>();
    IntPtr target;

    Skin skin;
    float x = 200, jumpY, vy, vx, speed;
    Mood mood = Mood.Walking, next = Mood.Walking;
    int moodTicks, moodLength = 150, tick, animStart, blinkTicks, bubbleTicks;
    bool right = true, onlyWithMatlab = true, forcedSleep;
    string anim = "walk", frameName = "walk1", bubble;
    Brush bubbleBrush = Brushes.HotPink;

    public PetForm(bool exitWithMatlab, string firstEvent = null)
    {
        this.exitWithMatlab = exitWithMatlab;
        float dpi;
        using (var g = CreateGraphics()) dpi = g.DpiX / 96f;
        s = Math.Max(2, (int)Math.Round(3 * dpi));
        u = s / 3f;
        extraTop = 10 * s;
        statusBarOffset = (int)(22 * dpi); // altura aprox. de la barra de estado de MATLAB
        speed = 1.6f * u;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = TransparencyKey = Color.Magenta;
        DoubleBuffered = true;
        MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) OnEvent("feliz"); };

        var menu = new ContextMenuStrip();
        petsMenu = new ToolStripMenuItem("Mascota");
        foreach (var k in skins)
        {
            var id = k.Sheet.id;
            petsMenu.DropDownItems.Add(new ToolStripMenuItem(k.Sheet.name, null, (_, __) => OnEvent("mascota:" + id)) { Tag = id });
        }
        var onlyItem = new ToolStripMenuItem("Solo cuando MATLAB está activo") { Checked = true, CheckOnClick = true };
        onlyItem.CheckedChanged += (_, __) => onlyWithMatlab = onlyItem.Checked;
        var startupItem = new ToolStripMenuItem("Iniciar con Windows") { Checked = StartupEnabled, CheckOnClick = true };
        startupItem.CheckedChanged += (_, __) => SetStartup(startupItem.Checked);
        menu.Items.Add(petsMenu);
        menu.Items.Add("Elegir mascota…", null, (_, __) => ShowPicker());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(onlyItem);
        menu.Items.Add("Dormir / Despertar", null, (_, __) => OnEvent(forcedSleep ? "despertar" : "dormir"));
        menu.Items.Add("Acariciar", null, (_, __) => OnEvent("feliz"));
        menu.Items.Add(startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Salir", null, (_, __) => Quit());
        tray = new NotifyIcon { Text = "Mascota de MATLAB", ContextMenuStrip = menu, Visible = true };
        tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) OnEvent("feliz"); };
        tray.MouseDoubleClick += (_, __) => ShowPicker();
        ContextMenuStrip = menu; // clic derecho sobre la mascota abre el mismo menú
        MouseDoubleClick += (_, __) => ShowPicker();

        string saved;
        using (var k = Registry.CurrentUser.OpenSubKey(SettingsKey)) saved = k?.GetValue("pet") as string;
        SetSkin(skins.FirstOrDefault(k => k.Sheet.id == saved) ?? skins[0]);

        var timer = new System.Windows.Forms.Timer { Interval = 33 };
        timer.Tick += (_, __) => UpdatePet();
        timer.Start();

        new Thread(ListenForCommands) { IsBackground = true }.Start();
        if (firstEvent != null) Shown += (_, __) => OnEvent(firstEvent);
    }

    PetPicker picker;

    // Mini menú con todas las mascotas: por si el comando no reconoce el nombre o los menús fallan
    void ShowPicker()
    {
        if (picker == null || picker.IsDisposed)
            picker = new PetPicker(skins, skin, k => OnEvent("mascota:" + k.Sheet.id));
        picker.Show();
        picker.Activate();
    }

    // Recibe los comandos que manda `MatlabPet.exe <comando>` desde la terminal o MATLAB
    void ListenForCommands()
    {
        while (true)
        {
            try
            {
                using (var pipe = new NamedPipeServerStream(Program.PipeName, PipeDirection.In))
                {
                    pipe.WaitForConnection();
                    var ev = new StreamReader(pipe).ReadLine();
                    if (ev != null && IsHandleCreated) BeginInvoke((Action)(() => OnEvent(ev)));
                }
            }
            catch (Exception) { Thread.Sleep(500); }
        }
    }

    // Que no robe el foco a MATLAB ni salga en Alt+Tab
    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x80 /* TOOLWINDOW */ | 0x08000000 /* NOACTIVATE */ | 0x8 /* TOPMOST */;
            return cp;
        }
    }

    void SetSkin(Skin k)
    {
        skin = k;
        ClientSize = new Size(k.Sheet.width * s, k.Sheet.height * s + extraTop);
        tray.Icon = TrayIcon(k);
        foreach (ToolStripMenuItem i in petsMenu.DropDownItems) i.Checked = (string)i.Tag == k.Sheet.id;
        using (var key = Registry.CurrentUser.CreateSubKey(SettingsKey)) key.SetValue("pet", k.Sheet.id);
    }

    static Icon TrayIcon(Skin k) => Icon.FromHandle(k.Preview().GetHicon());

    static bool StartupEnabled
    {
        get { using (var k = Registry.CurrentUser.OpenSubKey(RunKey)) return k?.GetValue("MatlabPet") != null; }
    }

    static void SetStartup(bool on)
    {
        using (var k = Registry.CurrentUser.OpenSubKey(RunKey, true))
        {
            if (on) k.SetValue("MatlabPet", $"\"{Application.ExecutablePath}\"");
            else k.DeleteValue("MatlabPet", false);
        }
    }

    void Quit()
    {
        tray.Visible = false;
        Application.Exit();
    }

    // MARK: Eventos

    void OnEvent(string ev)
    {
        if (ev.StartsWith("mascota:"))
        {
            // Acepta el id o el nombre como sea que se escriba: "Iron Man", "ironman", "Dr. Doom"…
            string Norm(string t) => new string(t.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
            var wanted = Norm(ev.Substring("mascota:".Length));
            var k = skins.FirstOrDefault(x => Norm(x.Sheet.id) == wanted || Norm(x.Sheet.name) == wanted);
            if (k != null) { SetSkin(k); OnEvent("feliz"); }
            else { Say("?", Brushes.Orange, 40); ShowPicker(); } // nombre que no conoce: que la elija con el mouse
            return;
        }
        switch (ev.ToLowerInvariant())
        {
            case "mascota": case "mascotas": case "elegir": case "menu": ShowPicker(); break;
            case "feliz": case "acariciar": forcedSleep = false; Jump(7, "happy", "♥", Brushes.HotPink); break;
            case "exito": case "ok": forcedSleep = false; Jump(8, "happy", "★", Brushes.Gold); break;
            case "error": case "fallo": forcedSleep = false; Jump(5, "scared", "!", Brushes.Red, right ? -2 : 2, Mood.Idle); break;
            case "salir": Quit(); break;
            case "dormir": forcedSleep = true; break;
            case "despertar": forcedSleep = false; Play("stretch", 45, Mood.Walking); break;
        }
    }

    void Jump(float v, string a, string text, Brush color, float dx = 0, Mood then = Mood.Walking)
    {
        vy = v * u;
        vx = dx * u;
        anim = a;
        next = then;
        Say(text, color, 40);
        Set(Mood.Airborne, int.MaxValue);
    }

    void Play(string a, int ticks, Mood then)
    {
        anim = a;
        next = then;
        Set(Mood.OneShot, ticks);
    }

    void Say(string text, Brush color, int ticks)
    {
        bubble = text;
        bubbleBrush = color;
        bubbleTicks = ticks;
    }

    void Set(Mood m, int length)
    {
        mood = m;
        moodTicks = 0;
        moodLength = length;
        animStart = tick;
        switch (m)
        {
            case Mood.Walking:
                bool running = rnd.Next(5) == 0;
                speed = (running ? 3.5f : 1.6f) * u;
                anim = running ? "run" : "walk";
                if (rnd.Next(2) == 0) right = !right;
                break;
            case Mood.Idle: anim = "idle"; break;
            case Mood.Sleeping: anim = "sleep"; break;
        }
    }

    int WalkLength() => 30 * rnd.Next(4, 11);

    // MARK: Ventana de MATLAB

    void RefreshMatlab()
    {
        var procs = ProcessNames.SelectMany(Process.GetProcessesByName).ToList();
        matlabPids = new HashSet<int>(procs.Select(p => p.Id));
        // Lanzado desde startup.m: se va de verdad cuando MATLAB se cierra
        if (matlabPids.Count > 0) seenMatlab = true;
        else if (exitWithMatlab && seenMatlab) Quit();
        if (target == IntPtr.Zero)
            target = procs.Select(p => p.MainWindowHandle).FirstOrDefault(h => h != IntPtr.Zero);
        procs.ForEach(p => p.Dispose());
    }

    void UpdatePet()
    {
        if (tick++ % 30 == 0) RefreshMatlab();
        if (IsDisposed) return;

        var fg = Native.GetForegroundWindow();
        Native.GetWindowThreadProcessId(fg, out uint fgPid);
        bool matlabActive = matlabPids.Contains((int)fgPid);
        if (matlabActive)
        {
            var r = Native.Bounds(fg);
            if (r.Width > 400 && r.Height > 200) target = fg; // sigue a la ventana de MATLAB (o figura) activa
        }

        Rectangle? win = null;
        if (target != IntPtr.Zero && Native.IsWindow(target) && Native.IsWindowVisible(target) && !Native.IsIconic(target))
            win = Native.Bounds(target);
        else
            target = IntPtr.Zero;

        if (onlyWithMatlab && (win == null || !(matlabActive || fgPid == ownPid)))
        {
            if (Visible) Hide();
            return;
        }

        var area = win ?? Screen.PrimaryScreen.WorkingArea;
        int floor = area.Bottom - (win != null ? statusBarOffset : 0);
        Think(area.Left, area.Right);
        x = Math.Min(Math.Max(x, area.Left), area.Right - Width);
        Location = new Point((int)x, (int)(floor - Height - jumpY));
        if (!Visible) Show();
        Render();
    }

    void Render()
    {
        frameName = skin.Frame(anim, tick - animStart);
        if (blinkTicks > 0) blinkTicks--;
        else if (mood != Mood.Sleeping && rnd.Next(120) == 0) blinkTicks = 5;
        if (bubbleTicks > 0 && --bubbleTicks == 0) bubble = null;
        Invalidate();
    }

    void Think(int minX, int maxX)
    {
        moodTicks++;
        if (forcedSleep && mood != Mood.Sleeping && mood != Mood.Airborne) Set(Mood.Sleeping, int.MaxValue);

        switch (mood)
        {
            case Mood.Walking:
                x += right ? speed : -speed;
                if (x <= minX || x >= maxX - Width) right = !right;
                if (moodTicks > moodLength)
                {
                    int r = rnd.Next(20);
                    if (r < 10) Set(Mood.Idle, 30 * rnd.Next(3, 7));
                    else if (r < 13) Set(Mood.Sleeping, 30 * rnd.Next(10, 21));
                    else if (r < 16) Play("yawn", 40, Mood.Idle);
                    else if (r < 18) Play("stretch", 45, Mood.Walking);
                    else Set(Mood.Walking, WalkLength());
                }
                break;
            case Mood.Idle:
                if (moodTicks > moodLength)
                {
                    if (rnd.Next(4) == 0) Play("yawn", 40, Mood.Walking);
                    else Set(Mood.Walking, WalkLength());
                }
                break;
            case Mood.Sleeping:
                Say(new[] { "z", "zZ", "zZz" }[(tick / 20) % 3], Brushes.Gray, 2);
                if (!forcedSleep && moodTicks > moodLength) Play("stretch", 45, Mood.Walking);
                break;
            case Mood.OneShot:
                if (moodTicks > moodLength) Set(next, next == Mood.Idle ? 90 : WalkLength());
                break;
            case Mood.Airborne:
                jumpY += vy;
                x += vx;
                vy -= 0.6f * u;
                if (jumpY <= 0)
                {
                    jumpY = 0;
                    vx = 0;
                    if (next == Mood.Idle) Play("sit", 60, Mood.Walking);
                    else Set(next, WalkLength());
                }
                break;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        if (!skin.Sheet.frames.TryGetValue(frameName, out var rows)) return;
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
            {
                var ch = rows[y][x];
                if (blinkTicks > 0 && skin.Blink.TryGetValue(ch, out var closed)) ch = closed;
                if (!skin.Palette.TryGetValue(ch, out var brush)) continue;
                int px = right ? x : rows[y].Length - 1 - x;
                g.FillRectangle(brush, px * s, extraTop + y * s, s, s);
            }
        if (bubble == null) return;
        // Sin antialiasing: el borde suavizado se vería rosa por el TransparencyKey
        g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
        using (var font = new Font("Segoe UI", 11, FontStyle.Bold))
            g.DrawString(bubble, font, bubbleBrush, right ? Width - 8 * s : s, 0);
    }
}

// MARK: - Selector: una ventanita con un botón por mascota

class PetPicker : Form
{
    public PetPicker(List<Skin> skins, Skin current, Action<Skin> choose)
    {
        Text = "Elige tu mascota";
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };

        var grid = new FlowLayoutPanel { AutoSize = true, MaximumSize = new Size(460, 0), Padding = new Padding(8) };
        foreach (var k in skins)
        {
            var b = new Button
            {
                Text = k.Sheet.name,
                Image = Pixelated(k.Preview(), 48),
                TextImageRelation = TextImageRelation.ImageAboveText,
                Size = new Size(100, 90),
                FlatStyle = FlatStyle.Flat,
                BackColor = k == current ? Color.MistyRose : SystemColors.Control,
            };
            b.Click += (_, __) => { choose(k); Close(); };
            grid.Controls.Add(b);
        }
        Controls.Add(grid);
    }

    // Agranda sin suavizar, para que el pixel art no se vea borroso
    static Bitmap Pixelated(Bitmap src, int size)
    {
        var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(src, 0, 0, size, size);
        }
        return bmp;
    }
}
