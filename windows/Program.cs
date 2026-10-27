using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        using (var mutex = new Mutex(true, "MatlabPet.SingleInstance", out bool first))
        {
            if (!first) return; // ya hay un gato corriendo
            Native.SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.Run(new PetForm(args.Contains("--con-matlab")));
        }
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
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hWnd, int attr, out RECT r, int size);

    // Sin los bordes invisibles de Windows 10/11
    public static Rectangle Bounds(IntPtr hWnd)
    {
        if (DwmGetWindowAttribute(hWnd, 9 /* EXTENDED_FRAME_BOUNDS */, out var r, Marshal.SizeOf<RECT>()) != 0)
            GetWindowRect(hWnd, out r);
        return Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
    }
}

enum Mood { Walking, Sitting, Sleeping, Jumping }

class PetForm : Form
{
    // Mismo gato que la versión de macOS: mirando a la derecha, 20x12
    static readonly string[] Body =
    {
        "..............k...k.",
        ".............kok.kok",
        ".k...........koooook",
        ".k...........kooeoek",
        "..k..........kooowpk",
        "..k..kkkkkkkkkoooook",
        "...kkooooooooooooook",
        "....koooooooooooook.",
        "....kowwwwwwwwwwok..",
        "....kkkkkkkkkkkkk...",
    };
    static readonly string[] LegsA = { "....kk...kk..kk..kk.", "....kk...kk..kk..kk." };
    static readonly string[] LegsB = { ".....kk.kk....kk.kk.", "......kkk......kkk.." };
    static readonly string[] Tucked = { "....................", "...................." };

    static readonly Dictionary<string, string[]> Frames = new Dictionary<string, string[]>
    {
        ["walk1"] = Body.Concat(LegsA).ToArray(),
        ["walk2"] = Body.Concat(LegsB).ToArray(),
        ["sit"] = Body.Concat(LegsA).ToArray(),
        ["sleep"] = Tucked.Concat(Body.Select(r => r.Replace('e', 'k'))).ToArray(),
    };

    static readonly Dictionary<char, SolidBrush> Palette = new Dictionary<char, SolidBrush>
    {
        ['k'] = new SolidBrush(Color.FromArgb(20, 20, 20)),    // contorno negro
        ['o'] = new SolidBrush(Color.FromArgb(255, 192, 214)), // rosado pastel
        ['w'] = new SolidBrush(Color.FromArgb(255, 240, 246)),
        ['p'] = new SolidBrush(Color.FromArgb(232, 96, 140)),  // nariz
        ['e'] = new SolidBrush(Color.FromArgb(20, 20, 20)),
    };

    static readonly string[] ProcessNames = { "MATLAB", "MATLABWindow" };
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    readonly int s, extraTop, statusBarOffset;
    readonly Random rnd = new Random();
    readonly int ownPid = Process.GetCurrentProcess().Id;
    readonly NotifyIcon tray;
    readonly bool exitWithMatlab;
    bool seenMatlab;
    HashSet<int> matlabPids = new HashSet<int>();
    IntPtr target;
    float x = 200, jumpY, vy, speed;
    Mood mood = Mood.Walking;
    int moodTicks, tick;
    bool right = true, onlyWithMatlab = true, forcedSleep;
    string frameName = "walk1", bubble;

    public PetForm(bool exitWithMatlab)
    {
        this.exitWithMatlab = exitWithMatlab;
        float dpi;
        using (var g = CreateGraphics()) dpi = g.DpiX / 96f;
        s = Math.Max(2, (int)Math.Round(3 * dpi));
        extraTop = 10 * s;
        statusBarOffset = (int)(22 * dpi); // altura aprox. de la barra de estado de MATLAB
        speed = 0.55f * s;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = TransparencyKey = Color.Magenta;
        DoubleBuffered = true;
        ClientSize = new Size(20 * s, 12 * s + extraTop);
        MouseDown += (_, __) => Pet();

        var menu = new ContextMenuStrip();
        var onlyItem = new ToolStripMenuItem("Solo cuando MATLAB está activo") { Checked = true, CheckOnClick = true };
        onlyItem.CheckedChanged += (_, __) => onlyWithMatlab = onlyItem.Checked;
        var startupItem = new ToolStripMenuItem("Iniciar con Windows") { Checked = StartupEnabled, CheckOnClick = true };
        startupItem.CheckedChanged += (_, __) => SetStartup(startupItem.Checked);
        menu.Items.Add(onlyItem);
        menu.Items.Add("Dormir / Despertar", null, (_, __) => ToggleSleep());
        menu.Items.Add("Acariciar", null, (_, __) => Pet());
        menu.Items.Add(startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Salir", null, (_, __) => Quit());
        tray = new NotifyIcon { Icon = TrayIcon(), Text = "Gatita de MATLAB", ContextMenuStrip = menu, Visible = true };
        tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) Pet(); };

        var timer = new System.Windows.Forms.Timer { Interval = 33 };
        timer.Tick += (_, __) => UpdatePet();
        timer.Start();
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

    static Icon TrayIcon()
    {
        var bmp = new Bitmap(20, 20);
        var rows = Frames["walk1"];
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
                if (Palette.TryGetValue(rows[y][x], out var b)) bmp.SetPixel(x, y + 4, b.Color);
        return Icon.FromHandle(bmp.GetHicon());
    }

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

    void Pet()
    {
        forcedSleep = false;
        Change(Mood.Jumping);
        vy = 2.3f * s;
        bubble = "♥";
    }

    void ToggleSleep()
    {
        forcedSleep = !forcedSleep;
        if (!forcedSleep) { bubble = null; Change(Mood.Walking); }
    }

    void RefreshMatlab()
    {
        var procs = ProcessNames.SelectMany(Process.GetProcessesByName).ToList();
        matlabPids = new HashSet<int>(procs.Select(p => p.Id));
        // Lanzado desde startup.m: se va a dormir de verdad cuando MATLAB se cierra
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
        Invalidate();
    }

    void Think(int minX, int maxX)
    {
        moodTicks++;
        if (forcedSleep && mood != Mood.Jumping) mood = Mood.Sleeping;

        switch (mood)
        {
            case Mood.Walking:
                x += right ? speed : -speed;
                if (x <= minX || x >= maxX - Width) right = !right;
                frameName = (tick / 6) % 2 == 0 ? "walk1" : "walk2";
                if (moodTicks > 30 * rnd.Next(4, 11)) Change(rnd.Next(4) == 0 ? Mood.Sleeping : Mood.Sitting);
                break;
            case Mood.Sitting:
                frameName = "sit";
                if (moodTicks > 30 * 3) Change(Mood.Walking);
                break;
            case Mood.Sleeping:
                frameName = "sleep";
                bubble = new[] { "z", "zZ", "zZz" }[(tick / 20) % 3];
                if (!forcedSleep && moodTicks > 30 * 12) { bubble = null; Change(Mood.Walking); }
                break;
            case Mood.Jumping:
                jumpY += vy;
                vy -= 0.2f * s;
                if (jumpY <= 0) { jumpY = 0; bubble = null; Change(Mood.Walking); }
                break;
        }
    }

    void Change(Mood m)
    {
        mood = m;
        moodTicks = 0;
        if (m != Mood.Walking) return;
        speed = (rnd.Next(5) == 0 ? 1.2f : 0.55f) * s;
        if (rnd.Next(2) == 0) right = !right;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var rows = Frames[frameName];
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
            {
                if (!Palette.TryGetValue(rows[y][x], out var brush)) continue;
                int px = right ? x : rows[y].Length - 1 - x;
                g.FillRectangle(brush, px * s, extraTop + y * s, s, s);
            }
        if (bubble == null) return;
        // Sin antialiasing: el borde suavizado se vería rosa por el TransparencyKey
        g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
        using (var font = new Font("Segoe UI", 11, FontStyle.Bold))
            g.DrawString(bubble, font, Brushes.HotPink, right ? Width - 8 * s : s, 0);
    }
}
