using System.Text;
using PCCleaner.Core;
using PCCleaner.Utilities;
using Terminal.Gui;

namespace PCCleaner.Desktop;

internal sealed class TuiApp
{
    // ── Palette ───────────────────────────────────────────────────────────────
    private static readonly ColorScheme SchemePrimary = Cs(Color.BrightCyan,  Color.Black, Color.White);
    private static readonly ColorScheme SchemeAccent  = Cs(Color.White,       Color.Black, Color.BrightCyan);
    private static readonly ColorScheme SchemeContent = Cs(Color.Cyan,        Color.Black, Color.BrightCyan);
    private static readonly ColorScheme SchemeDim     = Cs(Color.DarkGray,    Color.Black, Color.White);

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
    private readonly List<CheckBox>          _checkBoxes      = new();
    private readonly bool                    _initialPreview;
    private readonly bool                    _isAdmin;
    private          AppSettings             _settings;
    private          AppLanguage             _language;

    private TextField  _ageField         = null!;
    private CheckBox   _includeRecentBox = null!;
    private TextView   _outputView       = null!;
    private StatusItem _freedItem        = null!;
    private StatusItem _totalFreedItem  = null!;
    private StatusBar  _statusBar        = null!;
    private Button     _btnScan          = null!;
    private Button     _btnClean         = null!;

    private readonly StringBuilder _outputBuf = new();

    // Cancel support
    private volatile bool _isRunning       = false;
    private volatile bool _cancelRequested = false;

    // Update
    private UpdateInfo? _pendingUpdate  = null;
    private bool        _updateChecked  = false;

    // ── Starfield ─────────────────────────────────────────────────────────────
    private static readonly char[] BrightPool = { '*', '+', '•', '✦' };
    private static readonly char[] DimPool    = { '·', '·', '.', ' ', ' ', ' ', ' ', ' ' };
    private readonly Random  _rng      = new();
    private          char[]  _fastRow  = Array.Empty<char>();
    private          char[]  _slowRow  = Array.Empty<char>();
    private          int     _slowTick = 0;
    private          Label   _animFast = null!;
    private          Label   _animSlow = null!;

    internal TuiApp(AppSettings settings, bool initialPreview = false)
    {
        _settings       = settings;
        _initialPreview = initialPreview;
        _language       = settings.GetLanguage();
        _isAdmin        = AdminHelper.IsRunningAsAdministrator();
        _cleaners       = CleanerCatalog.CreateCleanersForCurrentPlatform();
    }

    // ── Entry point ────────────────────────────────────────────────────────────
    public void Run()
    {
        Application.Init();
        // Save Application.Top immediately after Init — on Linux, Application.Run(splash)
        // clears it so it's null by the time we reach BuildUi.
        var mainTop = Application.Top ?? new Toplevel
        {
            X = 0, Y = 0,
            Width  = Dim.Fill(),
            Height = Dim.Fill()
        };
        ShowSplash();
        BuildUi(mainTop);
        Application.Run(mainTop);
        Application.Shutdown();
    }

    // ── Splash screen ──────────────────────────────────────────────────────────
    private void ShowSplash()
    {
        // Toplevel (not Window) is the correct root-view type; Window adds a border we don't want
        var splash = new Toplevel
        {
            X = 0, Y = 0,
            Width  = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = new ColorScheme
            {
                Normal    = Terminal.Gui.Attribute.Make(Color.BrightCyan, Color.Black),
                Focus     = Terminal.Gui.Attribute.Make(Color.BrightCyan, Color.Black),
                HotNormal = Terminal.Gui.Attribute.Make(Color.BrightCyan, Color.Black),
                HotFocus  = Terminal.Gui.Attribute.Make(Color.BrightCyan, Color.Black),
                Disabled  = Terminal.Gui.Attribute.Make(Color.DarkGray,   Color.Black)
            }
        };

        // timerToken is assigned below; key-press cancels the timer so it can't fire
        // RequestStop() a second time and close the main window.
        object? timerToken = null;
        splash.KeyPress += (e) =>
        {
            if (timerToken != null) Application.MainLoop.RemoveTimeout(timerToken);
            Application.RequestStop();
            e.Handled = true;
        };

        string[] logo =
        {
            @"  ██████╗  ██████╗    ██████╗██╗     ███████╗ █████╗ ███╗   ██╗███████╗██████╗ ",
            @"  ██╔══██╗██╔════╝   ██╔════╝██║     ██╔════╝██╔══██╗████╗  ██║██╔════╝██╔══██╗",
            @"  ██████╔╝██║        ██║     ██║     █████╗  ███████║██╔██╗ ██║█████╗  ██████╔╝",
            @"  ██╔═══╝ ██║        ██║     ██║     ██╔══╝  ██╔══██║██║╚██╗██║██╔══╝  ██╔══██╗",
            @"  ██║     ╚██████╗   ╚██████╗███████╗███████╗██║  ██║██║ ╚████║███████╗██║  ██║",
            @"  ╚═╝      ╚═════╝    ╚═════╝╚══════╝╚══════╝╚═╝  ╚═╝╚═╝  ╚═══╝╚══════╝╚═╝  ╚═╝"
        };

        // Star particles — use actual terminal width, not hardcoded 79
        int starCount = 40;
        int termW     = Math.Max(Console.WindowWidth - 4, 40);
        int termH     = Math.Max(Console.WindowHeight - 4, 20);
        var sx    = new float[starCount];
        var sy    = new float[starCount];
        var sspd  = new float[starCount];
        var sch   = new char[starCount];
        char[] spool = { '*', '·', '+', '.', '•' };

        var rng2 = new Random();
        for (int i = 0; i < starCount; i++)
        {
            sx[i]   = rng2.Next(termW);
            sy[i]   = rng2.NextSingle() * termH;
            sspd[i] = 0.15f + rng2.NextSingle() * 0.25f;
            sch[i]  = spool[rng2.Next(spool.Length)];
        }

        Label[] starLabels = new Label[starCount];
        for (int i = 0; i < starCount; i++)
        {
            starLabels[i] = new Label(" ") { X = (int)sx[i], Y = (int)sy[i], ColorScheme = SchemeDim };
            splash.Add(starLabels[i]);
        }

        // Logo (added after stars → draws on top)
        int logoY = Math.Max(2, (termH - logo.Length - 6) / 2);
        Label[] logoLabels = new Label[logo.Length];
        for (int r = 0; r < logo.Length; r++)
        {
            logoLabels[r] = new Label(logo[r])
            {
                X = Pos.Center(), Y = logoY + r,
                ColorScheme = SchemePrimary
            };
            splash.Add(logoLabels[r]);
        }

        int tlY = logoY + logo.Length + 1;
        splash.Add(new Label("sweeping your drive clean since forever")
            { X = Pos.Center(), Y = tlY,     ColorScheme = SchemeDim });
        splash.Add(new Label($"by soda144p  ·  {SystemInfo.MachineName}  ·  {SystemInfo.CurrentPlatformName}")
            { X = Pos.Center(), Y = tlY + 1, ColorScheme = SchemeDim });
        splash.Add(new Label("press any key to skip")
            { X = Pos.Center(), Y = tlY + 2, ColorScheme = SchemeDim });

        // Progress bar — both labels same initial width so centering is stable
        int barY     = tlY + 4;
        const string BarEmpty = "[                            ]"; // 30 chars
        var barShell = new Label(BarEmpty) { X = Pos.Center(), Y = barY, ColorScheme = SchemeDim };
        var barFill  = new Label(BarEmpty) { X = Pos.Center(), Y = barY, ColorScheme = SchemePrimary };
        splash.Add(barShell, barFill);

        string[] bootMsgs =
        {
            "booting cleaner core...",
            "scanning dusty corners...",
            "warming up cache broom...",
            "loading cleaners...",
            "almost shiny...",
            "ready ✦"
        };
        var statusLbl = new Label(bootMsgs[0]) { X = Pos.Center(), Y = barY + 1, ColorScheme = SchemeDim };
        splash.Add(statusLbl);

        int wave = 0, tick = 0, totalTicks = 28;

        timerToken = Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(80), _ =>
        {
            tick++;

            // Move stars
            for (int i = 0; i < starCount; i++)
            {
                sy[i] += sspd[i];
                if (sy[i] >= termH) { sy[i] = 0; sx[i] = rng2.Next(termW); }
                starLabels[i].X    = (int)sx[i];
                starLabels[i].Y    = (int)sy[i];
                starLabels[i].Text = sch[i].ToString();
            }

            // Wave colour on logo rows
            wave = (wave + 1) % (logo.Length * 2);
            for (int r = 0; r < logo.Length; r++)
                logoLabels[r].ColorScheme = ((wave + r) % (logo.Length * 2)) < logo.Length
                    ? SchemePrimary : SchemeContent;

            // Progress bar
            int filled = Math.Min(28, (tick * 28) / totalTicks);
            barFill.Text = "[" + new string('█', filled) + new string(' ', 28 - filled) + "]";

            // Boot message
            statusLbl.Text = bootMsgs[Math.Min(bootMsgs.Length - 1, (tick * bootMsgs.Length) / totalTicks)];

            if (tick >= totalTicks) { Application.RequestStop(); return false; }
            return true;
        });

        // FIX: run splash directly as its own toplevel — do NOT add it to Application.Top
        Application.Run(splash);
    }

    // ── Main UI ────────────────────────────────────────────────────────────────
    private void BuildUi(Toplevel top)
    {
        top.ColorScheme = SchemeContent;

        // ── Menu bar ──────────────────────────────────────────────────────────
        var menuLang     = new MenuItem("_Language: EN / CS", "", ToggleLanguage);
        var menuUpdates  = new MenuItem("_Check for Updates", "", ShowUpdateDialog);
        var menuOverview = new MenuItem("_Disk Overview...", "", ShowDiskOverviewDialog);
        var menuQuit     = new MenuItem("_Quit", "Ctrl+Q", () => Application.RequestStop());
        var menuSchedule = new MenuItem("_Schedule Auto-Clean...", "", ShowScheduleDialog);
        MenuItem[] fileItems;
        if (SystemInfo.IsWindows)
        {
            var menuAdmin = new MenuItem("_Restart as Administrator", "", RestartAsAdmin);
#pragma warning disable CS8625
            fileItems = new MenuItem[] { menuLang, null, menuSchedule, null, menuAdmin, null, menuOverview, null, menuUpdates, null, menuQuit };
#pragma warning restore CS8625
        }
        else
        {
#pragma warning disable CS8625
            fileItems = new MenuItem[] { menuLang, null, menuSchedule, null, menuOverview, null, menuUpdates, null, menuQuit };
#pragma warning restore CS8625
        }

        top.Add(new MenuBar(new MenuBarItem[] { new("_File", fileItems) })
            { ColorScheme = SchemePrimary });

        // ── Main window ───────────────────────────────────────────────────────
        var win = new Window("◆  PC Cleaner  ◆")
        {
            X = 0, Y = 1,
            Width  = Dim.Fill(),
            Height = Dim.Fill() - 1,
            ColorScheme = SchemePrimary
        };

        win.Add(new Label($"  {SystemInfo.LocalAuthor}   ·   {SystemInfo.CurrentPlatformName}   ·   {SystemInfo.MachineName}")
            { X = 0, Y = 0, Width = Dim.Fill(), ColorScheme = SchemeDim });

        // Starfield strip
        InitStarfield(200);
        _animSlow = new Label("") { X = 0, Y = 1, Width = Dim.Fill(), ColorScheme = SchemeDim };
        _animFast = new Label("") { X = 0, Y = 1, Width = Dim.Fill(), ColorScheme = SchemePrimary };
        win.Add(_animSlow, _animFast);
        Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(70), StarfieldTick);

        // ── Cleaners panel ────────────────────────────────────────────────────
        var cleanersFrame = new FrameView("  Cleaners  ")
        {
            X = 0, Y = 3,
            Width  = Dim.Percent(62),
            Height = Dim.Fill() - 9,
            ColorScheme = SchemePrimary
        };

        var scroll = new ScrollView
        {
            X = 0, Y = 0,
            Width  = Dim.Fill(),
            Height = Dim.Fill(),
            ShowHorizontalScrollIndicator = false,
            ShowVerticalScrollIndicator   = true,
            ColorScheme = SchemeContent
        };

        for (int i = 0; i < _cleaners.Count; i++)
        {
            ICleaner cleaner   = _cleaners[i];
            bool     supported = SystemInfo.IsSupported(cleaner.Platform);
            bool     canRun    = supported && (!cleaner.RequiresAdministrator || _isAdmin);

            string badge = cleaner.RequiresAdministrator && !_isAdmin ? "admin! "
                         : cleaner.RequiresAdministrator              ? "admin  "
                         : cleaner.IsRecommended                      ? "safe   "
                         : "manual ";

            ColorScheme scheme = !canRun                       ? SchemeDim
                               : cleaner.RequiresAdministrator ? SchemeAccent
                               : !cleaner.IsRecommended        ? SchemeDim
                               : SchemeContent;

            bool initialChecked = canRun && (
                _settings.CheckedCleaners.Count > 0
                    ? _settings.CheckedCleaners.Contains(cleaner.GetType().Name)
                    : cleaner.IsRecommended);

            var cb = new CheckBox($"  {badge}  {cleaner.Name}")
            {
                X = 0, Y = i,
                Checked     = initialChecked,
                Enabled     = canRun,
                ColorScheme = scheme
            };
            // FIX: wire toggle → save settings
            cb.Toggled += _ => SaveSettings();
            _checkBoxes.Add(cb);
            scroll.Add(cb);
        }

        scroll.ContentSize = new Size(130, _cleaners.Count + 1);
        cleanersFrame.Add(scroll);
        win.Add(cleanersFrame);

        // ── Controls panel ────────────────────────────────────────────────────
        var controlsFrame = new FrameView("  Controls  ")
        {
            X = Pos.Right(cleanersFrame) + 1,
            Y = 3,
            Width  = Dim.Fill(),
            Height = Dim.Fill() - 9,
            ColorScheme = SchemePrimary
        };

        _btnScan  = new Button("Scan (preview)") { X = 1, Y = 0, ColorScheme = SchemeAccent };
        _btnClean = new Button("Run clean")      { X = 1, Y = 2, ColorScheme = SchemeAccent };

        _btnScan.Clicked  += () => { if (_isRunning) _cancelRequested = true; else LaunchCleaners(previewOnly: true); };
        _btnClean.Clicked += () => { if (_isRunning) _cancelRequested = true; else LaunchCleaners(previewOnly: false); };

        controlsFrame.Add(new Label("· · · · · · · · · · · ·") { X = 1, Y = 4,  ColorScheme = SchemeDim });
        controlsFrame.Add(new Label("Age (hours)")              { X = 1, Y = 5,  ColorScheme = SchemePrimary });

        string ageStr = _settings.AgeHours.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        _ageField         = new TextField(ageStr)               { X = 1, Y = 6,  Width = Dim.Fill(2) };
        _includeRecentBox = new CheckBox("Include recent files") { X = 1, Y = 7,  Checked = _settings.IncludeRecent, ColorScheme = SchemeContent };

        controlsFrame.Add(new Label("· · · · · · · · · · · ·") { X = 1, Y = 9,  ColorScheme = SchemeDim });
        var btnRecommended = new Button("Recommended")     { X = 1, Y = 10, ColorScheme = SchemeContent };
        var btnAll         = new Button("All for this OS") { X = 1, Y = 12, ColorScheme = SchemeContent };
        var btnClear       = new Button("Clear")           { X = 1, Y = 14, ColorScheme = SchemeContent };

        btnRecommended.Clicked += () => { SelectRecommended(); SaveSettings(); };
        btnAll.Clicked         += () => { SelectAll();         SaveSettings(); };
        btnClear.Clicked       += () => { ClearSelection();    SaveSettings(); };

        controlsFrame.Add(_btnScan, _btnClean, _ageField, _includeRecentBox,
            btnRecommended, btnAll, btnClear);
        win.Add(controlsFrame);

        // ── Output panel ──────────────────────────────────────────────────────
        // FIX: output frame starts at Pos.Bottom(cleanersFrame) — no overlapping label here
        var outputFrame = new FrameView("  Output  ")
        {
            X = 0,
            Y = Pos.Bottom(cleanersFrame),
            Width  = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = SchemePrimary
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

        // FIX: admin note goes into initial output text — not a floating label that overlaps
        var initLines = new StringBuilder();
        initLines.AppendLine(_initialPreview
            ? "Preview mode active. Select cleaners and press Scan or Run clean."
            : "Ready. Select cleaners and press Scan or Run clean.");
        if (!_isAdmin && _cleaners.Any(c => c.RequiresAdministrator && SystemInfo.IsSupported(c.Platform)))
        {
            initLines.AppendLine();
            initLines.AppendLine("○ Not running as administrator.");
            initLines.Append("  Admin cleaners are disabled — use File › Restart as Administrator.");
        }
        SetOutput(initLines.ToString());

        top.Add(win);

        // ── Background update check ───────────────────────────────────────────
        _ = Task.Run(async () =>
        {
            var info = await UpdateChecker.CheckAsync();
            Application.MainLoop.Invoke(() =>
            {
                _pendingUpdate = info;
                _updateChecked = true;
                if (info is not null)
                {
                    AppendOutput($"\n★  Update available: v{info.LatestVersion}  —  File › Check for Updates\n");
                    _freedItem.Title = $"★ v{info.LatestVersion} ready";
                    _statusBar.SetNeedsDisplay();
                }
            });
        });

        // ── Status bar ────────────────────────────────────────────────────────
        string adminBadge = _isAdmin ? "● admin" : "○ user";
        _freedItem      = new StatusItem(Key.Null, "Freed: —", null);
        string totalLabel = _settings.TotalBytesFreed > 0
            ? $"All-time: {SystemInfo.FormatBytes(_settings.TotalBytesFreed)}"
            : "All-time: —";
        _totalFreedItem = new StatusItem(Key.Null, totalLabel, null);
        _statusBar = new StatusBar(new StatusItem[]
        {
            new(Key.Q | Key.CtrlMask, "~Ctrl+Q~ Quit", () => Application.RequestStop()),
            new(Key.Null, $"{_cleaners.Count} cleaners   ·   {SystemInfo.MachineName}   ·   {adminBadge}", null),
            _freedItem,
            _totalFreedItem
        })
        { ColorScheme = SchemePrimary };
        top.Add(_statusBar);
    }

    // ── Starfield ──────────────────────────────────────────────────────────────
    private void InitStarfield(int width)
    {
        _fastRow = new char[Math.Max(width, 40)];
        _slowRow = new char[Math.Max(width, 40)];
        for (int i = 0; i < _fastRow.Length; i++)
        {
            _fastRow[i] = ' ';
            _slowRow[i] = DimPool[_rng.Next(DimPool.Length)];
        }
        for (int i = 0; i < _fastRow.Length / 10; i++)
            _fastRow[_rng.Next(_fastRow.Length)] = BrightPool[_rng.Next(BrightPool.Length)];
    }

    private bool StarfieldTick(MainLoop _)
    {
        char first = _fastRow[0];
        Array.Copy(_fastRow, 1, _fastRow, 0, _fastRow.Length - 1);
        _fastRow[^1] = first;
        for (int i = 0; i < _fastRow.Length; i++)
        {
            int r = _rng.Next(300);
            if (r < 1)      _fastRow[i] = BrightPool[_rng.Next(BrightPool.Length)];
            else if (r < 3) _fastRow[i] = ' ';
        }

        _slowTick++;
        if (_slowTick >= 3)
        {
            _slowTick = 0;
            char last = _slowRow[^1];
            Array.Copy(_slowRow, 0, _slowRow, 1, _slowRow.Length - 1);
            _slowRow[0] = last;
            for (int i = 0; i < _slowRow.Length; i++)
                if (_rng.Next(500) < 1)
                    _slowRow[i] = DimPool[_rng.Next(DimPool.Length)];
        }

        var merged = new char[_fastRow.Length];
        for (int i = 0; i < merged.Length; i++)
            merged[i] = _fastRow[i] != ' ' ? _fastRow[i] : _slowRow[i];

        _animSlow.Text = new string(_slowRow);
        _animFast.Text = new string(merged);
        return true;
    }

    // ── Output helpers ─────────────────────────────────────────────────────────
    private void SetOutput(string text)
    {
        _outputBuf.Clear();
        _outputBuf.Append(text);
        _outputView.Text = text;
    }

    private void AppendOutput(string text)
    {
        _outputBuf.Append(text);
        _outputView.Text = _outputBuf.ToString();
        // FIX: scroll to bottom so latest output is always visible
        _outputView.MoveEnd();
    }

    private void UpdateFreed(long bytes)
    {
        _freedItem.Title = bytes == 0 ? "Freed: —" : $"Freed: {SystemInfo.FormatBytes(bytes)}";
        _statusBar.SetNeedsDisplay();
    }

    private void UpdateTotalFreed()
    {
        _totalFreedItem.Title = _settings.TotalBytesFreed > 0
            ? $"All-time: {SystemInfo.FormatBytes(_settings.TotalBytesFreed)}"
            : "All-time: —";
        _statusBar.SetNeedsDisplay();
    }

    // ── Language / Admin / Schedule ────────────────────────────────────────────
    private void ToggleLanguage()
    {
        _language = _language == AppLanguage.English ? AppLanguage.Czech : AppLanguage.English;
        Localizer.SetLanguage(_language);
        _settings.SetLanguage(_language);
        _settings.Save();
        string name = _language == AppLanguage.Czech ? "Czech" : "English";
        SetOutput($"[Language] Switched to {name}.\nCleaner output will use the new language. Restart to update UI labels.");
    }

    private static void RestartAsAdmin()
    {
        if (AdminHelper.IsRunningAsAdministrator())
        {
            MessageBox.Query("Admin", "Already running as administrator.", "OK");
            return;
        }
        string[] forwarded = Environment.GetCommandLineArgs().Skip(1).ToArray();
        if (AdminHelper.TryRestartAsAdministrator(forwarded, out string error))
            Application.RequestStop();
        else
            MessageBox.ErrorQuery("Error", $"Could not restart as administrator:\n{error}", "OK");
    }

    private void ShowUpdateDialog()
    {
        if (!_updateChecked)
        {
            MessageBox.Query("Updates", $"Checking for updates...\nCurrent version: v{AppVersion.Current}\n\nTry again in a moment.", "OK");
            return;
        }

        if (_pendingUpdate is null)
        {
            MessageBox.Query("Up to date", $"You are running the latest version.\n\nv{AppVersion.Current}", "OK");
            return;
        }

        var update = _pendingUpdate;
        var dialog = new Dialog($"Update available: v{update.LatestVersion}", 62, 12);
        dialog.Add(new Label($"Current version : v{AppVersion.Current}")   { X = 2, Y = 1, ColorScheme = SchemeDim });
        dialog.Add(new Label($"Latest version  : v{update.LatestVersion}") { X = 2, Y = 2, ColorScheme = SchemePrimary });
        dialog.Add(new Label(update.DownloadUrl is not null
            ? "Ready to download and install automatically."
            : "No binary for this platform — visit the release page.")
            { X = 2, Y = 4, ColorScheme = SchemeDim });

        var btnInstall = new Button(update.DownloadUrl is not null ? "Download & Install" : "Open release page")
            { ColorScheme = SchemeAccent };
        var btnCancel  = new Button("Later") { ColorScheme = SchemeDim };

        btnInstall.Clicked += () =>
        {
            Application.RequestStop();

            if (update.DownloadUrl is null)
            {
                // No binary — open browser
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    { FileName = update.ReleasePageUrl, UseShellExecute = true }); }
                catch { SetOutput($"Visit: {update.ReleasePageUrl}"); }
                return;
            }

            SetOutput($"Updating to v{update.LatestVersion}...\n");
            _ = Task.Run(async () =>
            {
                void Progress(string msg) => Application.MainLoop.Invoke(() => AppendOutput(msg + "\n"));
                var (ok, err) = await Updater.DownloadAndApplyAsync(update, Progress);
                if (!ok)
                    Application.MainLoop.Invoke(() => AppendOutput($"[X] {err}\n"));
            });
        };

        btnCancel.Clicked += () => Application.RequestStop();
        dialog.AddButton(btnCancel);
        dialog.AddButton(btnInstall);
        Application.Run(dialog);
    }

    private static void ShowScheduleDialog()
    {
        string? processPath = Environment.ProcessPath;
        // Reject dotnet host — can't schedule schtasks/systemd with a `dotnet run` path
        bool isDirectExe = !string.IsNullOrEmpty(processPath)
            && !Path.GetFileNameWithoutExtension(processPath)
                    .Equals("dotnet", StringComparison.OrdinalIgnoreCase);

        if (!isDirectExe)
        {
            MessageBox.Query("Schedule",
                "Scheduled tasks require running the compiled binary directly.\n" +
                "Build Release and launch the published PCCleaner executable.",
                "OK");
            return;
        }

        if (SystemInfo.IsLinux)
        {
            ShowLinuxScheduleDialog(processPath!);
            return;
        }

        // Windows — schtasks-based dialog
        bool   exists     = ScheduledTaskManager.Exists();
        string statusText = exists ? "Status: Scheduled (weekly, Sunday 09:00)" : "Status: Not scheduled";

        var dialog = new Dialog("Auto-Clean Schedule", 62, 11);
        dialog.Add(new Label($"Task:   {ScheduledTaskManager.TaskName}") { X = 2, Y = 1, ColorScheme = SchemeContent });
        dialog.Add(new Label(statusText)                                  { X = 2, Y = 2, ColorScheme = exists ? SchemePrimary : SchemeDim });
        dialog.Add(new Label("Runs recommended cleaners silently via --auto-clean.") { X = 2, Y = 4, ColorScheme = SchemeDim });

        var btnCreate = new Button("Create weekly task") { ColorScheme = SchemeAccent };
        var btnRemove = new Button("Remove task")        { ColorScheme = SchemeDim };
        var btnClose  = new Button("Close");

        btnCreate.Clicked += () =>
        {
            if (ScheduledTaskManager.Create(processPath!, out string err))
                MessageBox.Query("Scheduled", "Task created.\nRuns every Sunday at 09:00.", "OK");
            else
                MessageBox.ErrorQuery("Error", $"Could not create task:\n{err}", "OK");
            Application.RequestStop();
        };

        btnRemove.Clicked += () =>
        {
            if (!exists) { MessageBox.Query("Schedule", "No task to remove.", "OK"); return; }
            if (ScheduledTaskManager.Remove(out string err))
                MessageBox.Query("Schedule", "Task removed.", "OK");
            else
                MessageBox.ErrorQuery("Error", $"Could not remove task:\n{err}", "OK");
            Application.RequestStop();
        };

        btnClose.Clicked += () => Application.RequestStop();
        dialog.AddButton(btnCreate);
        dialog.AddButton(btnRemove);
        dialog.AddButton(btnClose);
        Application.Run(dialog);
    }

    private static void ShowLinuxScheduleDialog(string processPath)
    {
        bool   exists     = SystemdTimerManager.Exists();
        string statusText = exists ? "Status: Enabled (weekly, Sunday 09:00)" : "Status: Not scheduled";

        var dialog = new Dialog("Auto-Clean — systemd timer", 64, 12);
        dialog.Add(new Label("Unit:   pc-cleaner.timer  (~/.config/systemd/user/)") { X = 2, Y = 1, ColorScheme = SchemeContent });
        dialog.Add(new Label(statusText) { X = 2, Y = 2, ColorScheme = exists ? SchemePrimary : SchemeDim });
        dialog.Add(new Label("Runs recommended cleaners silently via --auto-clean.")  { X = 2, Y = 4, ColorScheme = SchemeDim });
        dialog.Add(new Label("Requires a running systemd --user session (GNOME/KDE).") { X = 2, Y = 5, ColorScheme = SchemeDim });

        var btnCreate = new Button("Enable weekly timer") { ColorScheme = SchemeAccent };
        var btnRemove = new Button("Remove timer")        { ColorScheme = SchemeDim };
        var btnClose  = new Button("Close");

        btnCreate.Clicked += () =>
        {
            if (SystemdTimerManager.Create(processPath, out string err))
                MessageBox.Query("Scheduled", "systemd timer enabled.\nRuns every Sunday at 09:00.", "OK");
            else
                MessageBox.ErrorQuery("Error", $"Could not enable timer:\n{err}", "OK");
            Application.RequestStop();
        };

        btnRemove.Clicked += () =>
        {
            if (!exists) { MessageBox.Query("Schedule", "No timer to remove.", "OK"); return; }
            if (SystemdTimerManager.Remove(out string err))
                MessageBox.Query("Schedule", "Timer removed.", "OK");
            else
                MessageBox.ErrorQuery("Error", $"Could not remove timer:\n{err}", "OK");
            Application.RequestStop();
        };

        btnClose.Clicked += () => Application.RequestStop();
        dialog.AddButton(btnCreate);
        dialog.AddButton(btnRemove);
        dialog.AddButton(btnClose);
        Application.Run(dialog);
    }

    private void ShowDiskOverviewDialog()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var cts     = new System.Threading.CancellationTokenSource();

        var dialog = new Dialog("  Disk Overview  ", 72, 24);
        var output = new TextView
        {
            X = 1, Y = 1,
            Width    = Dim.Fill(1),
            Height   = Dim.Fill(4),
            ReadOnly = true,
            WordWrap = false,
            ColorScheme = SchemeDim,
            Text = "Scanning..."
        };
        dialog.Add(output);

        var btnClose = new Button("Close");
        btnClose.Clicked += () => { cts.Cancel(); Application.RequestStop(); };
        dialog.AddButton(btnClose);

        var snapshot  = SystemInfo.GetCurrentDriveSnapshot();
        string header = BuildOverviewHeader(home, snapshot);

        // Parallel scan — each completed dir triggers a live UI refresh
        var results  = new System.Collections.Concurrent.ConcurrentBag<(string name, long size)>();
        var lockObj  = new object();
        string[]   dirs = Directory.Exists(home) ? Directory.GetDirectories(home) : Array.Empty<string>();
        int        total = dirs.Length;

        void Refresh(bool done)
        {
            if (cts.IsCancellationRequested) return;
            List<(string name, long size)> snap;
            lock (lockObj) snap = results.OrderByDescending(r => r.size).ToList();
            long maxSz = snap.Count > 0 ? Math.Max(snap[0].size, 1) : 1;
            string progress = done ? $"Done — {snap.Count} folders" : $"Scanning... {snap.Count}/{total}";

            var sb = new StringBuilder();
            sb.AppendLine(header);
            sb.AppendLine($"Top folders in {home}  [{progress}]");
            sb.AppendLine(new string('─', 60));
            foreach (var (name, sz) in snap.Take(16))
            {
                int    barLen  = (int)((double)sz / maxSz * 22);
                string bar     = new string('█', barLen) + new string('░', 22 - barLen);
                string sizeStr = SystemInfo.FormatBytes(sz).PadLeft(10);
                sb.AppendLine($"  {bar}  {sizeStr}  {name}");
            }
            string text = sb.ToString();
            Application.MainLoop.Invoke(() => { if (!cts.IsCancellationRequested) output.Text = text; });
        }

        _ = Task.WhenAll(dirs.Select(dir => Task.Run(() =>
        {
            long sz = DirSize(dir);
            lock (lockObj) results.Add((Path.GetFileName(dir), sz));
            Refresh(done: false);
        }, cts.Token))).ContinueWith(_ => Refresh(done: true), cts.Token);

        Application.Run(dialog);
        cts.Cancel();
    }

    private string BuildOverviewHeader(string home, DiskSpaceSnapshot? snapshot)
    {
        var sb = new StringBuilder();
        if (snapshot != null)
            sb.AppendLine($"Drive free  : {SystemInfo.FormatBytes(snapshot.FreeBytes)}");
        if (_settings.TotalBytesFreed > 0)
            sb.AppendLine($"Freed by PC Cleaner (all-time): {SystemInfo.FormatBytes(_settings.TotalBytesFreed)}");
        return sb.ToString().TrimEnd();
    }

    private static long DirSize(string path)
    {
        long total = 0;
        try
        {
            foreach (string f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try { total += new FileInfo(f).Length; }
                catch { }
            }
        }
        catch { }
        return total;
    }

    // ── Selection helpers ──────────────────────────────────────────────────────
    private void SelectRecommended()
    {
        for (int i = 0; i < _cleaners.Count; i++)
            if (_checkBoxes[i].Enabled)
                _checkBoxes[i].Checked = _cleaners[i].IsRecommended;
    }

    private void SelectAll()
    {
        foreach (CheckBox cb in _checkBoxes)
            if (cb.Enabled) cb.Checked = true;
    }

    private void ClearSelection()
    {
        foreach (CheckBox cb in _checkBoxes)
            cb.Checked = false;
    }

    private void SaveSettings()
    {
        if (TryParseAge(out double h)) _settings.AgeHours = h;
        _settings.IncludeRecent   = _includeRecentBox?.Checked ?? _settings.IncludeRecent;
        _settings.CheckedCleaners = _cleaners
            .Zip(_checkBoxes, (c, cb) => (c, cb))
            .Where(t => t.cb.Checked)
            .Select(t => t.c.GetType().Name)
            .ToList();
        _settings.Save();
    }

    // ── Cleaning logic ─────────────────────────────────────────────────────────
    private void LaunchCleaners(bool previewOnly)
    {
        // FIX: if already running, treat button press as cancel
        if (_isRunning)
        {
            _cancelRequested = true;
            return;
        }

        List<ICleaner> selected = GetSelectedCleaners();
        if (selected.Count == 0) { SetOutput("No supported cleaners selected."); return; }
        if (!TryParseAge(out double ageHours)) { SetOutput("Age filter must be a non-negative number."); return; }
        if (!previewOnly && !ConfirmClean()) { SetOutput("Clean cancelled."); return; }

        SaveSettings();
        UpdateFreed(0);

        _isRunning       = true;
        _cancelRequested = false;
        _btnScan.Text    = "[X] Cancel";
        _btnClean.Text   = "[X] Cancel";

        SetOutput(previewOnly ? "Scanning...\n\n" : "Cleaning...\n\n");

        var options = new CleanOptions(previewOnly, TimeSpan.FromHours(ageHours), _includeRecentBox.Checked);

        ThreadPool.QueueUserWorkItem(_ =>
        {
            void   Append(string text) => Application.MainLoop.Invoke(() => AppendOutput(text));
            void   OnFreed(long b)     => Application.MainLoop.Invoke(() => UpdateFreed(b));
            bool   IsCancelled()       => _cancelRequested;

            long freed = ExecuteClean(selected, options, Append, OnFreed, IsCancelled);

            Application.MainLoop.Invoke(() =>
            {
                if (!options.PreviewOnly && freed > 0 && !_cancelRequested)
                {
                    _settings.TotalBytesFreed += freed;
                    _settings.Save();
                    UpdateTotalFreed();
                }
                _isRunning     = false;
                _btnScan.Text  = "Scan (preview)";
                _btnClean.Text = "Run clean";
            });
        });
    }

    private List<ICleaner> GetSelectedCleaners()
    {
        var result = new List<ICleaner>();
        for (int i = 0; i < _cleaners.Count; i++)
            if (_checkBoxes[i].Checked && _checkBoxes[i].Enabled)
                result.Add(_cleaners[i]);
        return result;
    }

    private bool TryParseAge(out double ageHours) =>
        double.TryParse(
            _ageField?.Text.ToString() ?? "24",
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out ageHours) && ageHours >= 0;

    private static bool ConfirmClean()
    {
        string confirmWord = Localizer.T("prompt.typeCleanWord"); // "CLEAN" or "CISTIT"
        bool   confirmed   = false;

        var dialog = new Dialog("Confirm deletion", 60, 9);
        dialog.Add(new Label($"Type {confirmWord} to confirm deleting files:") { X = 1, Y = 1 });
        var input = new TextField("") { X = 1, Y = 3, Width = Dim.Fill(2) };
        dialog.Add(input);

        var ok = new Button("OK", is_default: true);
        ok.Clicked += () =>
        {
            confirmed = string.Equals(input.Text.ToString(), confirmWord, StringComparison.OrdinalIgnoreCase);
            Application.RequestStop();
        };
        var cancel = new Button("Cancel");
        cancel.Clicked += () => Application.RequestStop();

        dialog.AddButton(cancel);
        dialog.AddButton(ok);
        Application.Run(dialog);
        return confirmed;
    }

    private static long ExecuteClean(
        IReadOnlyList<ICleaner> cleaners,
        CleanOptions            options,
        Action<string>          append,
        Action<long>            onFreed,
        Func<bool>              isCancelled)
    {
        append(options.PreviewOnly ? "Scan mode — no files will be deleted.\n" : "Clean mode — files will be deleted.\n");
        append("\n");

        long totalFreed = 0;

        var callbacks = new CleanerRunCallbacks
        {
            Warning   = w        => append($"  ⚠  {w}\n"),
            Starting  = c        => append($"→ {c.Name}...\n"),
            Completed = r        =>
            {
                append($"  {r.ToConsoleMessage()}\n");
                foreach (string note in r.Notes)
                    append($"     {note}\n");
                if (r.BytesFreed > 0)
                {
                    totalFreed += r.BytesFreed;
                    onFreed(totalFreed);
                }
            },
            Skipped        = (c, why) => append($"  {c.Name}: skipped ({why})\n"),
            // FIX: check cancel flag before each cleaner; CleanerRunner calls this between cleaners
            ConfirmCleaner = _ =>
            {
                if (!isCancelled()) return true;
                append("\n[X] Cancelled by user.\n");
                return false;
            }
        };

        var report = new CleanerRunner().Run(cleaners, options, callbacks);
        append("\n");
        append($"━━━ {report.Total.ToConsoleMessage()} ━━━\n");

        if (!isCancelled())
        {
            if (CleanReportWriter.TrySave(report, options, out string path, out string saveError))
                append($"Report saved → {path}\n");
            else if (!string.IsNullOrEmpty(saveError))
                append($"Report error: {saveError}\n");
        }

        return totalFreed;
    }
}
