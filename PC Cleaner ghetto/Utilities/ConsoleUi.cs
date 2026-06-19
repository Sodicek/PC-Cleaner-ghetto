using System.Diagnostics;
using PCCleaner.Core;

namespace PCCleaner.Utilities;

internal static class ConsoleUi
{
    private const int MinimumWindowWidth = 50;
    private const int MinimumWindowHeight = 18;
    private const int WideMenuWidth = 88;
    private const int MaximumContentWidth = 120;

    public static void PlayIntroAnimation()
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            Console.WriteLine(Localizer.T("app.credit"));
            Console.WriteLine();
            return;
        }

        try
        {
            ClearScreen();

            Stopwatch stopwatch = Stopwatch.StartNew();
            int frame = 0;

            while (stopwatch.Elapsed < TimeSpan.FromSeconds(10))
            {
                DrawWaveFrame(frame, stopwatch.Elapsed.TotalSeconds / 10);
                Thread.Sleep(55);
                frame++;
            }
        }
        catch (IOException)
        {
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (InvalidOperationException)
        {
        }
        finally
        {
            Console.ResetColor();
            ClearScreen();
        }
    }

    public static bool WaitForUsableWindow()
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            return true;
        }

        try
        {
            while (Console.WindowWidth < MinimumWindowWidth || Console.WindowHeight < MinimumWindowHeight)
            {
                ClearScreen();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(Localizer.T("console.tooSmallTitle"));
                Console.ResetColor();
                Console.WriteLine(Localizer.T(
                    "console.tooSmallSize",
                    Console.WindowWidth,
                    Console.WindowHeight,
                    MinimumWindowWidth,
                    MinimumWindowHeight));
                Console.WriteLine(Localizer.T("console.resizeOrQuit"));

                for (int i = 0; i < 6; i++)
                {
                    Thread.Sleep(100);

                    if (Console.WindowWidth >= MinimumWindowWidth && Console.WindowHeight >= MinimumWindowHeight)
                    {
                        ClearScreen();
                        return true;
                    }

                    if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Q)
                    {
                        return false;
                    }
                }
            }
        }
        catch (IOException)
        {
            return true;
        }
        catch (InvalidOperationException)
        {
            return true;
        }

        return true;
    }

    public static void ClearScreen()
    {
        try
        {
            if (!Console.IsOutputRedirected)
            {
                Console.Clear();
            }
        }
        catch (IOException)
        {
        }
    }

    private static void PrintCreditSplash(int frameNumber, int frameCount)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==============================================");
        Console.WriteLine("              PC CLEANER GHETTO               ");
        Console.WriteLine($"              {Localizer.T("app.credit")}              ");
        Console.WriteLine("==============================================");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(Localizer.T("animation.hold", frameNumber, frameCount));
        Console.WriteLine();
        Console.ResetColor();
    }

    private static void DrawWaveFrame(int frame, double progress)
    {
        int width = Math.Clamp(GetWindowWidth() - 1, MinimumWindowWidth - 1, MaximumContentWidth);
        int waveHeight = Math.Clamp(GetWindowHeight() - 12, 8, 20);
        int center = waveHeight / 2;
        double phase = frame * 0.16;

        Console.SetCursorPosition(0, 0);
        WriteCentered("~ PC CLEANER GHETTO ~", width, ConsoleColor.Cyan);
        WriteCentered("soda144p // wave mode", width, ConsoleColor.Magenta);
        WriteCentered(Localizer.T("app.menuCredit", SystemInfo.LocalAuthor), width, ConsoleColor.Green);
        WriteFoamBorder(width, frame);

        char[,] canvas = new char[waveHeight, width];
        ConsoleColor[,] colors = new ConsoleColor[waveHeight, width];

        for (int y = 0; y < waveHeight; y++)
        {
            for (int x = 0; x < width; x++)
            {
                canvas[y, x] = ' ';
                colors[y, x] = ConsoleColor.Black;
            }
        }

        DrawWaveLayer(canvas, colors, width, waveHeight, center - 2, 0.125, 0.95, phase, '~', ConsoleColor.Cyan);
        DrawWaveLayer(canvas, colors, width, waveHeight, center + 1, 0.095, -1.35, phase * 1.25, '=', ConsoleColor.Blue);
        DrawWaveLayer(canvas, colors, width, waveHeight, center + 3, 0.055, 1.85, phase * 0.82, '-', ConsoleColor.DarkCyan);
        DrawFoam(canvas, colors, width, waveHeight, frame);
        DrawNickname(canvas, colors, width, waveHeight, frame);

        for (int y = 0; y < waveHeight; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Console.ForegroundColor = colors[y, x];
                Console.Write(canvas[y, x]);
            }

            Console.WriteLine();
        }

        Console.ResetColor();
        WriteFoamBorder(width, frame + 7);

        int filled = (int)Math.Round((width - 20) * Math.Clamp(progress, 0, 1));
        string bar = new string('~', filled).PadRight(width - 20, '.');
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write("[");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(bar);
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"] {(progress * 100):0}%");
        Console.ResetColor();
    }

    private static void DrawWaveLayer(
        char[,] canvas,
        ConsoleColor[,] colors,
        int width,
        int height,
        int baseline,
        double frequency,
        double direction,
        double phase,
        char glyph,
        ConsoleColor color)
    {
        for (int x = 0; x < width; x++)
        {
            double wave = Math.Sin((x * frequency) + (phase * direction)) * 2.4
                + Math.Sin((x * frequency * 0.43) - (phase * 1.7 * direction)) * 1.2;

            int y = Math.Clamp(baseline + (int)Math.Round(wave), 0, height - 1);
            canvas[y, x] = glyph;
            colors[y, x] = color;

            if (y + 1 < height && canvas[y + 1, x] == ' ')
            {
                canvas[y + 1, x] = glyph == '~' ? '\'' : '.';
                colors[y + 1, x] = ConsoleColor.DarkCyan;
            }
        }
    }

    private static void DrawFoam(char[,] canvas, ConsoleColor[,] colors, int width, int height, int frame)
    {
        for (int x = 0; x < width; x += 5)
        {
            int y = Math.Abs((x * 3 + frame) % (height * 2) - height);

            if (y >= 0 && y < height && canvas[y, x] == ' ')
            {
                canvas[y, x] = (x + frame) % 3 == 0 ? '*' : '.';
                colors[y, x] = ConsoleColor.White;
            }
        }
    }

    private static void DrawNickname(char[,] canvas, ConsoleColor[,] colors, int width, int height, int frame)
    {
        const string nickname = " soda144p ";
        int travelWidth = Math.Max(1, width + nickname.Length);
        int startX = width - (frame * 2 % travelWidth);
        int y = Math.Clamp((height / 2) + (int)Math.Round(Math.Sin(frame * 0.18) * 2), 1, height - 2);

        for (int i = 0; i < nickname.Length; i++)
        {
            int x = startX + i;

            if (x < 0 || x >= width)
            {
                continue;
            }

            canvas[y, x] = nickname[i];
            colors[y, x] = ConsoleColor.Magenta;
        }
    }

    private static void WriteFoamBorder(int width, int frame)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;

        for (int i = 0; i < width; i++)
        {
            char glyph = ((i + frame) % 9) switch
            {
                0 => '~',
                1 or 2 => '.',
                3 => '`',
                4 => '-',
                _ => (char)' '
            };

            Console.Write(glyph);
        }

        Console.WriteLine();
        Console.ResetColor();
    }

    private static void WriteCentered(string text, int width, ConsoleColor color)
    {
        string value = text.Length > width ? text[..width] : text;
        int leftPadding = Math.Max(0, (width - value.Length) / 2);
        Console.ForegroundColor = color;
        Console.WriteLine(new string(' ', leftPadding) + value);
        Console.ResetColor();
    }

    public static void PrintHeader()
    {
        int width = GetContentWidth();

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;

        if (width >= 64)
        {
            Console.WriteLine(@"  ____   ____    ____ _");
            Console.WriteLine(@" |  _ \ / ___|  / ___| | ___  __ _ _ __   ___ _ __");
            Console.WriteLine(@" | |_) | |     | |   | |/ _ \/ _` | '_ \ / _ \ '__|");
            Console.WriteLine(@" |  __/| |___  | |___| |  __/ (_| | | | |  __/ |");
            Console.WriteLine(@" |_|    \____|  \____|_|\___|\__,_|_| |_|\___|_|");
        }
        else
        {
            WriteCentered("PC CLEANER GHETTO", width, ConsoleColor.Cyan);
            WriteCentered("~ soda144p ~", width, ConsoleColor.Magenta);
        }

        Console.ResetColor();
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.White;
        WriteWrapped(Localizer.T("app.subtitle"), 2, ConsoleColor.White);
        Console.ResetColor();
        Console.WriteLine();

        PrintDashboard();
        Console.WriteLine();
    }

    private static void PrintDashboard()
    {
        WriteSection(Localizer.T("menu.dashboard"));
        WriteInfoRow(ConsoleColor.Cyan, Localizer.T("app.credit"));
        WriteInfoRow(ConsoleColor.Magenta, Localizer.T("app.menuCredit", SystemInfo.LocalAuthor));
        WriteInfoRow(ConsoleColor.Green, Localizer.T("system.loggedIn", SystemInfo.LoggedInUser));
        WriteInfoRow(ConsoleColor.DarkCyan, Localizer.T("system.machine", SystemInfo.MachineName));
        WriteInfoRow(ConsoleColor.DarkGray, Localizer.T("system.detected", SystemInfo.Description));
    }

    public static void PrintMenu(IReadOnlyList<ICleaner> cleaners)
    {
        int width = GetContentWidth();

        WriteSection(Localizer.T("menu.cleaners"));

        if (width >= WideMenuWidth)
        {
            PrintWideMenu(cleaners, width);
        }
        else
        {
            PrintCompactMenu(cleaners, width);
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        WriteWrapped(Localizer.T("menu.help"), 0, ConsoleColor.DarkGray);
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void PrintWideMenu(IReadOnlyList<ICleaner> cleaners, int width)
    {
        int nameWidth = Math.Clamp(width - 54, 28, 38);
        string cleanerHeader = TrimToWidth(Localizer.T("menu.cleaner"), nameWidth).PadRight(nameWidth);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($" {Localizer.T("menu.number"),-4} {cleanerHeader} {Localizer.T("menu.details")}");
        Console.WriteLine(" " + new string('-', Math.Max(30, width - 2)));
        Console.ResetColor();

        for (int i = 0; i < cleaners.Count; i++)
        {
            ICleaner cleaner = cleaners[i];
            bool supported = SystemInfo.IsSupported(cleaner.Platform);

            Console.ForegroundColor = supported ? ConsoleColor.White : ConsoleColor.DarkGray;
            Console.Write($" {i + 1,2}. ");
            Console.ResetColor();
            WritePaddedName(cleaner.Name, nameWidth, supported);
            WriteCleanerBadges(cleaner, supported);
            Console.WriteLine();

            WriteWrapped(cleaner.Description, 6, ConsoleColor.DarkGray);
            WriteWrapped($"{Localizer.T("menu.risk")}: {cleaner.Risk}", 6, ConsoleColor.Yellow);
            Console.WriteLine();
        }
    }

    private static void PrintCompactMenu(IReadOnlyList<ICleaner> cleaners, int width)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(Localizer.T("console.compactMenu", width));
        Console.ResetColor();
        Console.WriteLine();

        for (int i = 0; i < cleaners.Count; i++)
        {
            ICleaner cleaner = cleaners[i];
            bool supported = SystemInfo.IsSupported(cleaner.Platform);

            Console.ForegroundColor = supported ? ConsoleColor.White : ConsoleColor.DarkGray;
            WriteWrapped($"{i + 1}. {cleaner.Name}", 1, supported ? ConsoleColor.White : ConsoleColor.DarkGray);
            Console.Write("   ");
            WriteCleanerBadges(cleaner, supported);
            Console.WriteLine();
            WriteWrapped(cleaner.Description, 3, ConsoleColor.DarkGray);
            WriteWrapped($"{Localizer.T("menu.risk")}: {cleaner.Risk}", 3, ConsoleColor.Yellow);
            Console.WriteLine();
        }
    }

    public static void PrintSelectedCleaners(IReadOnlyList<ICleaner> cleaners)
    {
        WriteSection(Localizer.T("section.ready"));

        foreach (ICleaner cleaner in cleaners)
        {
            WriteWrapped($"- {cleaner.Name}", 1, ConsoleColor.White);
            Console.ForegroundColor = SystemInfo.IsSupported(cleaner.Platform) ? ConsoleColor.DarkGray : ConsoleColor.Red;
            WriteWrapped(SystemInfo.PlatformName(cleaner.Platform), 3, SystemInfo.IsSupported(cleaner.Platform) ? ConsoleColor.DarkGray : ConsoleColor.Red);
            WriteWrapped($"{Localizer.T("menu.risk")}: {cleaner.Risk}", 3, ConsoleColor.Yellow);
            Console.ResetColor();
        }

        Console.WriteLine();
    }

    public static void PrintSummary(CleanRunReport report)
    {
        WriteSection(Localizer.T("section.summary"));
        WriteResult(report.Total);
        PrintDiskChange(report);
        Console.WriteLine();
    }

    private static void PrintDiskChange(CleanRunReport report)
    {
        if (report.DiskBefore is null || report.DiskAfter is null)
        {
            return;
        }

        long delta = report.DiskAfter.FreeBytes - report.DiskBefore.FreeBytes;

        Console.ForegroundColor = delta >= 0 ? ConsoleColor.Green : ConsoleColor.Yellow;
        Console.WriteLine(Localizer.T(
            "report.diskChange",
            report.DiskBefore.DriveName,
            SystemInfo.FormatBytes(report.DiskBefore.FreeBytes),
            SystemInfo.FormatBytes(report.DiskAfter.FreeBytes),
            SystemInfo.FormatBytes(Math.Abs(delta)),
            delta >= 0 ? "+" : "-"));
        Console.ResetColor();
        Console.WriteLine();
    }

    public static void WriteStep(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(">> ");
        Console.ResetColor();
        Console.WriteLine(message);
    }

    public static void WriteResult(CleanResult result)
    {
        Console.ForegroundColor = result.Failures == 0 ? ConsoleColor.Green : ConsoleColor.Yellow;
        Console.WriteLine(result.ToConsoleMessage());
        Console.ResetColor();

        foreach (string note in result.Notes)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"   {note}");
            Console.ResetColor();
        }

        Console.WriteLine();
    }

    public static void WritePrompt(string message)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(message);
        Console.ResetColor();
    }

    public static void WriteWarning(string message)
    {
        WriteWrapped(message, 0, ConsoleColor.Yellow);
        Console.WriteLine();
    }

    public static void WriteSuccess(string message)
    {
        WriteWrapped(message, 0, ConsoleColor.Green);
    }

    private static void WriteSection(string title)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"== {title} ==");
        Console.ResetColor();
    }

    private static void WriteInfoRow(ConsoleColor color, string message)
    {
        List<string> lines = WrapText(message, Math.Max(10, GetContentWidth() - 4)).ToList();

        if (lines.Count == 0)
        {
            return;
        }

        Console.ForegroundColor = color;
        Console.Write("  * ");
        Console.ResetColor();
        Console.WriteLine(lines[0]);

        for (int i = 1; i < lines.Count; i++)
        {
            Console.WriteLine($"    {lines[i]}");
        }
    }

    private static void WriteCleanerBadges(ICleaner cleaner, bool supported)
    {
        WriteBadge(cleaner.IsRecommended ? Localizer.T("menu.safe") : Localizer.T("menu.manual"), cleaner.IsRecommended ? ConsoleColor.Green : ConsoleColor.Yellow);

        if (cleaner.RequiresAdministrator)
        {
            WriteBadge(Localizer.T("menu.admin"), ConsoleColor.DarkYellow);
        }

        WriteBadge(SystemInfo.PlatformName(cleaner.Platform), supported ? ConsoleColor.DarkCyan : ConsoleColor.DarkGray);

        if (!supported)
        {
            WriteBadge(Localizer.T("menu.unsupported"), ConsoleColor.Red);
        }
    }

    private static void WriteBadge(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write($"[{text}] ");
        Console.ResetColor();
    }

    private static void WritePaddedName(string text, int width, bool supported)
    {
        Console.ForegroundColor = supported ? ConsoleColor.White : ConsoleColor.DarkGray;
        Console.Write(TrimToWidth(text, width).PadRight(width));
        Console.ResetColor();
    }

    private static string TrimToWidth(string text, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        return text.Length > width ? text[..Math.Max(0, width - 1)] + "." : text;
    }

    private static void WriteWrapped(string text, int indent, ConsoleColor color)
    {
        int lineWidth = Math.Max(10, GetContentWidth() - indent);
        string padding = new(' ', Math.Max(0, indent));

        foreach (string line in WrapText(text, lineWidth))
        {
            Console.ForegroundColor = color;
            Console.Write(padding);
            Console.WriteLine(line);
            Console.ResetColor();
        }
    }

    private static IEnumerable<string> WrapText(string text, int width)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        width = Math.Max(10, width);
        string current = string.Empty;
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (string rawWord in words)
        {
            string word = rawWord;

            while (word.Length > width)
            {
                if (!string.IsNullOrEmpty(current))
                {
                    yield return current;
                    current = string.Empty;
                }

                yield return word[..width];
                word = word[width..];
            }

            if (string.IsNullOrEmpty(current))
            {
                current = word;
                continue;
            }

            if (current.Length + 1 + word.Length > width)
            {
                yield return current;
                current = word;
                continue;
            }

            current += " " + word;
        }

        if (!string.IsNullOrEmpty(current))
        {
            yield return current;
        }
    }

    private static int GetContentWidth()
    {
        return Math.Clamp(GetWindowWidth() - 1, MinimumWindowWidth, MaximumContentWidth);
    }

    private static int GetWindowWidth()
    {
        if (Console.IsOutputRedirected)
        {
            return 100;
        }

        try
        {
            return Math.Max(1, Console.WindowWidth);
        }
        catch (IOException)
        {
            return 100;
        }
        catch (InvalidOperationException)
        {
            return 100;
        }
    }

    private static int GetWindowHeight()
    {
        if (Console.IsOutputRedirected)
        {
            return 30;
        }

        try
        {
            return Math.Max(1, Console.WindowHeight);
        }
        catch (IOException)
        {
            return 30;
        }
        catch (InvalidOperationException)
        {
            return 30;
        }
    }
}
