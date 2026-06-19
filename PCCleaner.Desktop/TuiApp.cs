using System.Text;
using PCCleaner.Core;
using PCCleaner.Utilities;
using Terminal.Gui;

namespace PCCleaner.Desktop;

internal sealed class TuiApp
{
    // ── Palette ───────────────────────────────────────────────────────────────
    // Soft purple / indigo chrome on black — vibey, not hacker
    private static readonly ColorScheme SchemePurple = Cs(Color.Magenta,       Color.Black, Color.BrightMagenta);
    private static readonly ColorScheme SchemeAccent = Cs(Color.BrightMagenta, Color.Black, Color.White);
    private static readonly ColorScheme SchemeWhite  = Cs(Color.White,         Color.Black, Color.White);
    private static readonly ColorScheme SchemeDim    = Cs(Color.Gray,          Color.Black, Color.White);

    private static ColorScheme Cs(Color normal, Color bg, Color focused)
    {
        var s = new ColorScheme();
        s.Normal    = Terminal.Gui.Attribute.Make(normal,  bg);
        s.Focus     = Terminal.Gui.Attribute.Make(focused, Color.DarkGray);
        s.HotNormal = Terminal.Gui.Attribute.Make(normal,  bg);
        s.HotFocus  = Terminal.Gui.Attribute.Make(focused, Color.DarkGray);
        s.Disabled  = Terminal.Gui.Attribute.Make(Color.DarkGray, bg);
        return s;
    }

    // ── State ──────────────────────────────────────────────────────────────────
    private readonly IReadOnlyList<ICleaner> _cleaners;
    private readonly List<CheckBox> _checkBoxes = new();
    private TextField _ageField         = null!;
    private CheckBox  _includeRecentBox = null!;
    private TextView  _outputView       = null!;

    public TuiApp()
    {
        _cleaners = CleanerCatalog.CreateCleanersForCurrentPlatform();
    }

    // ── Entry point ────────────────────────────────────────────────────────────
    public void Run()
    {
        Application.Init();
        BuildUi();
        Application.Run();
        Application.Shutdown();
    }

    // ── UI ─────────────────────────────────────────────────────────────────────
    private void BuildUi()
    {
        var top = Application.Top;
        top.ColorScheme = SchemeWhite;

        // Menu bar ─────────────────────────────────────────────────────────────
        var menu = new MenuBar(new MenuBarItem[]
        {
            new("_File", new MenuItem[]
            {
                new("_Quit", "Ctrl+Q", () => Application.RequestStop())
            })
        })
        { ColorScheme = SchemePurple };
        top.Add(menu);

        // Main window ──────────────────────────────────────────────────────────
        var win = new Window("◆  PC Cleaner Ghetto  ◆")
        {
            X = 0, Y = 1,
            Width  = Dim.Fill(),
            Height = Dim.Fill() - 1,
            ColorScheme = SchemePurple
        };

        // Subtitle + separator
        string sub  = $"  {SystemInfo.LocalAuthor}   ·   {SystemInfo.CurrentPlatformName}   ·   {SystemInfo.MachineName}";
        string rule = "  " + new string('·', 120);
        win.Add(new Label(sub)  { X = 0, Y = 0, Width = Dim.Fill(), ColorScheme = SchemeDim });
        win.Add(new Label(rule) { X = 0, Y = 1, Width = Dim.Fill(), ColorScheme = SchemeDim });

        // ── Cleaners panel ─────────────────────────────────────────────────────
        var cleanersFrame = new FrameView("  Cleaners  ")
        {
            X = 0, Y = 2,
            Width  = Dim.Percent(62),
            Height = Dim.Fill() - 9,
            ColorScheme = SchemePurple
        };

        var scroll = new ScrollView
        {
            X = 0, Y = 0,
            Width  = Dim.Fill(),
            Height = Dim.Fill(),
            ShowHorizontalScrollIndicator = false,
            ShowVerticalScrollIndicator   = true,
            ColorScheme = SchemeWhite
        };

        for (int i = 0; i < _cleaners.Count; i++)
        {
            ICleaner cleaner   = _cleaners[i];
            bool     supported = SystemInfo.IsSupported(cleaner.Platform);

            string badge = cleaner.RequiresAdministrator ? "admin  "
                         : cleaner.IsRecommended        ? "safe   "
                         : "manual ";

            string label = $"  {badge}  {cleaner.Name}";

            ColorScheme scheme = !supported                    ? SchemeDim
                               : cleaner.RequiresAdministrator ? SchemeAccent
                               : !cleaner.IsRecommended        ? SchemeDim
                               : SchemeWhite;

            var cb = new CheckBox(label)
            {
                X = 0, Y = i,
                Checked     = supported && cleaner.IsRecommended,
                Enabled     = supported,
                ColorScheme = scheme
            };
            _checkBoxes.Add(cb);
            scroll.Add(cb);
        }

        scroll.ContentSize = new Size(130, _cleaners.Count + 1);
        cleanersFrame.Add(scroll);
        win.Add(cleanersFrame);

        // ── Controls panel ─────────────────────────────────────────────────────
        var controlsFrame = new FrameView("  Controls  ")
        {
            X = Pos.Right(cleanersFrame) + 1,
            Y = 2,
            Width  = Dim.Fill(),
            Height = Dim.Fill() - 9,
            ColorScheme = SchemePurple
        };

        // Primary actions first
        var btnScan  = new Button("Scan (preview)") { X = 1, Y = 0, ColorScheme = SchemeAccent };
        var btnClean = new Button("Run clean")      { X = 1, Y = 2, ColorScheme = SchemeAccent };

        controlsFrame.Add(new Label("· · · · · · · · · · · ·") { X = 1, Y = 4, ColorScheme = SchemeDim });
        controlsFrame.Add(new Label("Age (hours)") { X = 1, Y = 5, ColorScheme = SchemePurple });
        _ageField         = new TextField("24")                  { X = 1, Y = 6, Width = Dim.Fill(2) };
        _includeRecentBox = new CheckBox("Include recent files") { X = 1, Y = 7, ColorScheme = SchemeWhite };

        controlsFrame.Add(new Label("· · · · · · · · · · · ·") { X = 1, Y = 9, ColorScheme = SchemeDim });
        var btnRecommended = new Button("Recommended")    { X = 1, Y = 10, ColorScheme = SchemeWhite };
        var btnAll         = new Button("All for this OS") { X = 1, Y = 12, ColorScheme = SchemeWhite };
        var btnClear       = new Button("Clear")            { X = 1, Y = 14, ColorScheme = SchemeWhite };

        btnScan.Clicked        += () => LaunchCleaners(previewOnly: true);
        btnClean.Clicked       += () => LaunchCleaners(previewOnly: false);
        btnRecommended.Clicked += SelectRecommended;
        btnAll.Clicked         += SelectAll;
        btnClear.Clicked       += ClearSelection;

        controlsFrame.Add(btnScan, btnClean, _ageField, _includeRecentBox,
            btnRecommended, btnAll, btnClear);
        win.Add(controlsFrame);

        // ── Output panel ───────────────────────────────────────────────────────
        var outputFrame = new FrameView("  Output  ")
        {
            X = 0,
            Y = Pos.Bottom(cleanersFrame),
            Width  = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = SchemePurple
        };

        _outputView = new TextView
        {
            X = 0, Y = 0,
            Width    = Dim.Fill(),
            Height   = Dim.Fill(),
            ReadOnly = true,
            WordWrap = false,
            ColorScheme = SchemeDim
        };
        outputFrame.Add(_outputView);
        win.Add(outputFrame);

        SetOutput("Ready. Select cleaners and press Scan or Run clean.");
        top.Add(win);

        // Status bar ───────────────────────────────────────────────────────────
        top.Add(new StatusBar(new StatusItem[]
        {
            new(Key.Q | Key.CtrlMask, "~Ctrl+Q~ Quit",     () => Application.RequestStop()),
            new(Key.Null, $"{_cleaners.Count} cleaners   ·   {SystemInfo.MachineName}", null)
        })
        { ColorScheme = SchemePurple });
    }

    // ── Selection helpers ──────────────────────────────────────────────────────
    private void SelectRecommended()
    {
        for (int i = 0; i < _cleaners.Count; i++)
        {
            if (_checkBoxes[i].Enabled)
                _checkBoxes[i].Checked = _cleaners[i].IsRecommended;
        }
    }

    private void SelectAll()
    {
        foreach (CheckBox cb in _checkBoxes)
        {
            if (cb.Enabled)
                cb.Checked = true;
        }
    }

    private void ClearSelection()
    {
        foreach (CheckBox cb in _checkBoxes)
            cb.Checked = false;
    }

    // ── Cleaning logic ─────────────────────────────────────────────────────────
    private void LaunchCleaners(bool previewOnly)
    {
        List<ICleaner> selected = GetSelectedCleaners();

        if (selected.Count == 0)
        {
            SetOutput("No supported cleaners selected.");
            return;
        }

        if (!TryParseAge(out double ageHours))
        {
            SetOutput("Age filter must be a non-negative number (e.g. 24).");
            return;
        }

        if (!previewOnly && !ConfirmClean())
        {
            SetOutput("Clean cancelled.");
            return;
        }

        CleanOptions options = new(previewOnly, TimeSpan.FromHours(ageHours), _includeRecentBox.Checked);
        SetOutput(previewOnly ? "Scanning..." : "Cleaning...");

        ThreadPool.QueueUserWorkItem(_ =>
        {
            string output = RunCleaners(selected, options);
            Application.MainLoop.Invoke(() => SetOutput(output));
        });
    }

    private List<ICleaner> GetSelectedCleaners()
    {
        List<ICleaner> result = new();
        for (int i = 0; i < _cleaners.Count; i++)
        {
            if (_checkBoxes[i].Checked && _checkBoxes[i].Enabled)
                result.Add(_cleaners[i]);
        }
        return result;
    }

    private bool TryParseAge(out double ageHours)
    {
        return double.TryParse(
            _ageField.Text.ToString(),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out ageHours) && ageHours >= 0;
    }

    private static bool ConfirmClean()
    {
        bool confirmed = false;
        var dialog = new Dialog("Confirm deletion", 60, 9);
        var label  = new Label("Type CLEAN to confirm deleting files:") { X = 1, Y = 1 };
        var input  = new TextField("") { X = 1, Y = 3, Width = Dim.Fill(2) };

        var ok = new Button("OK", is_default: true);
        ok.Clicked += () =>
        {
            confirmed = string.Equals(input.Text.ToString(), "CLEAN", StringComparison.OrdinalIgnoreCase);
            Application.RequestStop();
        };

        var cancel = new Button("Cancel");
        cancel.Clicked += () => Application.RequestStop();

        dialog.Add(label, input);
        dialog.AddButton(cancel);
        dialog.AddButton(ok);
        Application.Run(dialog);
        return confirmed;
    }

    private static string RunCleaners(IReadOnlyList<ICleaner> cleaners, CleanOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine(options.PreviewOnly
            ? "Scan mode — no files will be deleted."
            : "Clean mode — files can be deleted.");
        sb.AppendLine();

        var callbacks = new CleanerRunCallbacks
        {
            Warning   = w        => sb.AppendLine($"Warning: {w}"),
            Starting  = c        => sb.AppendLine($"Running {c.Name}..."),
            Completed = r        =>
            {
                sb.AppendLine(r.ToConsoleMessage());
                foreach (string note in r.Notes)
                    sb.AppendLine($"  {note}");
            },
            Skipped   = (c, why) => sb.AppendLine($"{c.Name}: skipped — {why}")
        };

        var report = new CleanerRunner().Run(cleaners, options, callbacks);
        sb.AppendLine();
        sb.AppendLine(report.Total.ToConsoleMessage());

        if (CleanReportWriter.TrySave(report, options, out string path, out string error))
            sb.AppendLine($"Report saved: {path}");
        else
            sb.AppendLine($"Could not save report: {error}");

        return sb.ToString();
    }

    private void SetOutput(string text)
    {
        _outputView.Text = text;
    }
}
