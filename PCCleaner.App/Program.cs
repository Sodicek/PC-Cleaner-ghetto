using PCCleaner.Core;
using PCCleaner.App;
using PCCleaner.Utilities;

var settings      = AppSettings.Load();
bool initialPrev  = false;
bool autoClean    = false;
double ageOverride = -1;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--lang" or "-l" when i + 1 < args.Length:
            bool isCzech = args[++i].StartsWith("cs", StringComparison.OrdinalIgnoreCase);
            settings.SetLanguage(isCzech ? AppLanguage.Czech : AppLanguage.English);
            settings.Save();
            break;
        case "--preview" or "--dry-run" or "-n":
            initialPrev = true;
            break;
        case "--age-hours" when i + 1 < args.Length:
            if (double.TryParse(args[++i],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double h))
                ageOverride = h;
            break;
        case "--include-recent":
            settings.IncludeRecent = true;
            break;
        case "--auto-clean":
            autoClean = true;
            break;
    }
}

if (ageOverride >= 0) settings.AgeHours = ageOverride;

Localizer.SetLanguage(settings.GetLanguage());

if (autoClean)
{
    var cleaners = CleanerCatalog.CreateCleanersForCurrentPlatform()
        .Where(c => c.IsRecommended && !c.RequiresAdministrator)
        .ToList();
    var opts      = new CleanOptions(false, TimeSpan.FromHours(settings.AgeHours), settings.IncludeRecent);
    var callbacks = new CleanerRunCallbacks { ConfirmCleaner = _ => true };
    var report    = new CleanerRunner().Run(cleaners, opts, callbacks);
    CleanReportWriter.TrySave(report, opts, out _, out _);
    return;
}

new TuiApp(settings, initialPrev).Run();
