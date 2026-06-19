using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner;

internal static class Program
{
    private const int DefaultMinimumAgeHours = 24;

    private static readonly IReadOnlyList<ICleaner> Cleaners = CleanerCatalog.CreateCleanersForCurrentPlatform();

    private static void Main(string[] args)
    {
        Console.Title = "PC Cleaner";
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        RuntimeSettings settings = RuntimeSettings.Parse(args, DefaultMinimumAgeHours);

        if (settings.ShowHelp)
        {
            PrintUsage();
            return;
        }

        SelectLanguage(settings.Language);

        if (!settings.SkipIntro)
        {
            ConsoleUi.PlayIntroAnimation();
        }

        if (!ConsoleUi.WaitForUsableWindow())
        {
            ConsoleUi.WriteSuccess(Localizer.T("status.later"));
            return;
        }

        ConsoleUi.PrintHeader();

        if (settings.ForcePreview)
        {
            ConsoleUi.WriteWarning(Localizer.T("status.argumentPreview"));
        }

        if (settings.IncludeRecentFiles)
        {
            ConsoleUi.WriteWarning(Localizer.T("status.includeRecent"));
        }
        else
        {
            ConsoleUi.WriteWarning(Localizer.T("status.ageFilter", settings.MinimumFileAge.TotalHours));
        }

        if (HasSupportedAdminCleaners() && !AdminHelper.IsRunningAsAdministrator())
        {
            ConsoleUi.WriteWarning(Localizer.T("admin.warning"));
        }

        while (true)
        {
            if (!ConsoleUi.WaitForUsableWindow())
            {
                ConsoleUi.WriteSuccess(Localizer.T("status.later"));
                return;
            }

            ConsoleUi.PrintMenu(Cleaners);
            ConsoleUi.WritePrompt(Localizer.T("prompt.choose"));
            string? input = Console.ReadLine();

            if (string.Equals(input, "q", StringComparison.OrdinalIgnoreCase))
            {
                ConsoleUi.WriteSuccess(Localizer.T("status.later"));
                return;
            }

            if (string.Equals(input, "w", StringComparison.OrdinalIgnoreCase))
            {
                ConsoleUi.PlayIntroAnimation();
                if (!ConsoleUi.WaitForUsableWindow())
                {
                    ConsoleUi.WriteSuccess(Localizer.T("status.later"));
                    return;
                }

                ConsoleUi.PrintHeader();
                continue;
            }

            if (string.Equals(input, "r", StringComparison.OrdinalIgnoreCase))
            {
                if (RestartAsAdministrator(args))
                {
                    return;
                }

                continue;
            }

            bool previewOnly = IsPreview(input, settings.ForcePreview);
            IReadOnlyList<ICleaner> selectedCleaners = GetSelectedCleaners(input);
            if (selectedCleaners.Count == 0)
            {
                ConsoleUi.WriteWarning(Localizer.T("status.noValid"));
                continue;
            }

            if (!ConfirmSelection(selectedCleaners, previewOnly))
            {
                ConsoleUi.WriteWarning(Localizer.T("status.cancelled"));
                continue;
            }

            CleanOptions options = new(previewOnly, settings.MinimumFileAge, settings.IncludeRecentFiles);
            CleanRunReport report = RunCleaners(selectedCleaners, options);
            ConsoleUi.PrintSummary(report);
            SaveReport(report, options);
            ConsoleUi.WritePrompt(Localizer.T("prompt.more"));

            if (!IsYes(Console.ReadLine()))
            {
                ConsoleUi.WriteSuccess(Localizer.T("status.done"));
                return;
            }

            ConsoleUi.ClearScreen();
            ConsoleUi.PrintHeader();
        }
    }

    private static bool RestartAsAdministrator(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            ConsoleUi.WriteWarning(Localizer.T("admin.notWindows"));
            return false;
        }

        if (AdminHelper.IsRunningAsAdministrator())
        {
            ConsoleUi.WriteWarning(Localizer.T("admin.already"));
            return false;
        }

        ConsoleUi.WritePrompt(Localizer.T("admin.restartConfirm"));
        if (!IsYes(Console.ReadLine()))
        {
            ConsoleUi.WriteWarning(Localizer.T("admin.restartDeclined"));
            return false;
        }

        if (AdminHelper.TryRestartAsAdministrator(BuildRestartArguments(args), out string error))
        {
            ConsoleUi.WriteSuccess(Localizer.T("admin.restartStarted"));
            return true;
        }

        ConsoleUi.WriteWarning(Localizer.T("admin.restartFailed", error));
        return false;
    }

    private static IReadOnlyList<string> BuildRestartArguments(string[] args)
    {
        List<string> restartArgs = args.ToList();

        if (!HasOption(args, "--lang"))
        {
            restartArgs.Add("--lang");
            restartArgs.Add(Localizer.CurrentLanguage == AppLanguage.Czech ? "cs" : "en");
        }

        return restartArgs;
    }

    private static bool HasOption(IEnumerable<string> args, string option)
    {
        return args.Any(arg =>
            string.Equals(arg, option, StringComparison.OrdinalIgnoreCase)
            || arg.StartsWith(option + "=", StringComparison.OrdinalIgnoreCase));
    }

    private static void SelectLanguage(AppLanguage? language)
    {
        if (language is not null)
        {
            Localizer.SetLanguage(language.Value);
            return;
        }

        ConsoleUi.WritePrompt(Localizer.T("language.choose"));
        string? choice = Console.ReadLine();
        Console.WriteLine();

        if (choice?.Trim() == "2")
        {
            Localizer.SetLanguage(AppLanguage.Czech);
            return;
        }

        if (choice?.Trim() != "1" && !string.IsNullOrWhiteSpace(choice))
        {
            ConsoleUi.WriteWarning(Localizer.T("language.invalid"));
        }
    }

    private static IReadOnlyList<ICleaner> GetSelectedCleaners(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Array.Empty<ICleaner>();
        }

        if (string.Equals(input.Trim(), "a", StringComparison.OrdinalIgnoreCase))
        {
            return Cleaners.Where(cleaner => cleaner.IsRecommended && SystemInfo.IsSupported(cleaner.Platform)).ToList();
        }

        if (string.Equals(input.Trim(), "p", StringComparison.OrdinalIgnoreCase))
        {
            return Cleaners.Where(cleaner => cleaner.IsRecommended && SystemInfo.IsSupported(cleaner.Platform)).ToList();
        }

        if (string.Equals(input.Trim(), "e", StringComparison.OrdinalIgnoreCase))
        {
            return Cleaners.Where(cleaner => SystemInfo.IsSupported(cleaner.Platform)).ToList();
        }

        List<ICleaner> selectedCleaners = new();
        string[] parts = input.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string part in parts)
        {
            if (!int.TryParse(part, out int number))
            {
                continue;
            }

            int index = number - 1;
            if (index >= 0 && index < Cleaners.Count)
            {
                selectedCleaners.Add(Cleaners[index]);
            }
        }

        return selectedCleaners.Distinct().ToList();
    }

    private static bool IsPreview(string? input, bool forcePreview)
    {
        if (forcePreview)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        return input.Trim().StartsWith("p", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ConfirmSelection(IReadOnlyList<ICleaner> cleaners, bool previewOnly)
    {
        IReadOnlyList<ICleaner> manualCleaners = cleaners.Where(cleaner => !cleaner.IsRecommended).ToList();

        ConsoleUi.PrintSelectedCleaners(cleaners);

        if (manualCleaners.Count > 0)
        {
            ConsoleUi.WriteWarning(Localizer.T("status.manualWarning"));
        }

        ConsoleUi.WritePrompt(previewOnly ? Localizer.T("prompt.confirmPreview") : Localizer.T("prompt.confirmClean"));
        if (!IsYes(Console.ReadLine()))
        {
            return false;
        }

        if (previewOnly)
        {
            return true;
        }

        ConsoleUi.WritePrompt(Localizer.T("prompt.typeClean", Localizer.T("prompt.typeCleanWord")));
        string value = Console.ReadLine()?.Trim() ?? string.Empty;

        if (string.Equals(value, Localizer.T("prompt.typeCleanWord"), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        ConsoleUi.WriteWarning(Localizer.T("status.cleanWordMismatch"));
        return false;
    }

    private static bool IsYes(string? input)
    {
        string value = input?.Trim() ?? string.Empty;
        return string.Equals(value, "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "a", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasSupportedAdminCleaners()
    {
        return Cleaners.Any(cleaner => cleaner.RequiresAdministrator && SystemInfo.IsSupported(cleaner.Platform));
    }

    private static CleanRunReport RunCleaners(IReadOnlyList<ICleaner> cleaners, CleanOptions options)
    {
        Console.WriteLine();
        CleanerRunner runner = new();
        CleanerRunCallbacks callbacks = new()
        {
            Warning = ConsoleUi.WriteWarning,
            ConfirmCleaner = cleaner =>
            {
                ConsoleUi.WritePrompt(options.PreviewOnly
                    ? Localizer.T("prompt.confirmCleanerPreview", cleaner.Name)
                    : Localizer.T("prompt.confirmCleanerClean", cleaner.Name));

                return IsYes(Console.ReadLine());
            },
            Starting = cleaner => ConsoleUi.WriteStep(Localizer.T("status.running", cleaner.Name)),
            Completed = ConsoleUi.WriteResult,
            Skipped = (cleaner, reason) =>
            {
                if (string.Equals(reason, Localizer.T("status.adminRequired"), StringComparison.Ordinal))
                {
                    ConsoleUi.WriteWarning(Localizer.T("status.skippingAdmin", cleaner.Name));
                    return;
                }

                if (string.Equals(reason, Localizer.T("status.notConfirmed"), StringComparison.Ordinal))
                {
                    ConsoleUi.WriteWarning(Localizer.T("status.skippedByUser", cleaner.Name));
                    return;
                }

                ConsoleUi.WriteWarning($"{cleaner.Name}: {reason}");
            }
        };

        return runner.Run(cleaners, options, callbacks);
    }

    private static void SaveReport(CleanRunReport report, CleanOptions options)
    {
        if (CleanReportWriter.TrySave(report, options, out string path, out string error))
        {
            ConsoleUi.WriteSuccess(Localizer.T("report.saved", path));
            return;
        }

        ConsoleUi.WriteWarning(Localizer.T("report.failed", error));
    }

    private static void PrintUsage()
    {
        Console.WriteLine("PC Cleaner Ghetto");
        Console.WriteLine("Usage: dotnet \"PC Cleaner ghetto.dll\" [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --preview, --dry-run, -n     Force preview mode; delete nothing.");
        Console.WriteLine("  --age-hours <hours>          Skip files newer than this many hours. Default: 24.");
        Console.WriteLine("  --include-recent             Disable the recent-file safety filter.");
        Console.WriteLine("  --lang <en|cs>               Select language without prompting.");
        Console.WriteLine("  --skip-intro                 Skip the 10-second creator intro.");
        Console.WriteLine("  --help, -h                   Show this help.");
    }

    private sealed class RuntimeSettings
    {
        private RuntimeSettings()
        {
        }

        public bool ForcePreview { get; private set; }

        public bool IncludeRecentFiles { get; private set; }

        public bool ShowHelp { get; private set; }

        public bool SkipIntro { get; private set; }

        public TimeSpan MinimumFileAge { get; private set; }

        public AppLanguage? Language { get; private set; }

        public static RuntimeSettings Parse(string[] args, int defaultMinimumAgeHours)
        {
            RuntimeSettings settings = new()
            {
                MinimumFileAge = TimeSpan.FromHours(defaultMinimumAgeHours)
            };

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (IsAny(arg, "--preview", "--dry-run", "-n"))
                {
                    settings.ForcePreview = true;
                    continue;
                }

                if (IsAny(arg, "--help", "-h", "/?"))
                {
                    settings.ShowHelp = true;
                    continue;
                }

                if (IsAny(arg, "--include-recent"))
                {
                    settings.IncludeRecentFiles = true;
                    continue;
                }

                if (IsAny(arg, "--skip-intro"))
                {
                    settings.SkipIntro = true;
                    continue;
                }

                if (TryReadValue(args, ref i, "--age-hours", arg, out string? ageValue)
                    && ageValue is not null
                    && double.TryParse(ageValue, out double hours)
                    && hours >= 0)
                {
                    settings.MinimumFileAge = TimeSpan.FromHours(hours);
                    continue;
                }

                if (TryReadValue(args, ref i, "--lang", arg, out string? languageValue)
                    && languageValue is not null)
                {
                    settings.Language = ParseLanguage(languageValue);
                }
            }

            return settings;
        }

        private static AppLanguage? ParseLanguage(string value)
        {
            return value.Trim().ToLowerInvariant() switch
            {
                "en" or "english" or "1" => AppLanguage.English,
                "cs" or "cz" or "czech" or "2" => AppLanguage.Czech,
                _ => null
            };
        }

        private static bool TryReadValue(string[] args, ref int index, string name, string arg, out string? value)
        {
            value = null;

            if (arg.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
            {
                value = arg[(name.Length + 1)..];
                return true;
            }

            if (!string.Equals(arg, name, StringComparison.OrdinalIgnoreCase) || index + 1 >= args.Length)
            {
                return false;
            }

            index++;
            value = args[index];
            return true;
        }

        private static bool IsAny(string value, params string[] candidates)
        {
            return candidates.Any(candidate => string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase));
        }
    }
}
