using Microsoft.Win32;
using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner.Cleaners;

internal sealed class StartupCleaner : ICleaner
{
    private static readonly string[] AppNames =
    {
        "PC Cleaner",
        "PC Cleaner ghetto"
    };

    public string Name => Localizer.T("cleaner.startup.name");

    public string Description => Localizer.T("cleaner.startup.description");

    public string Risk => Localizer.T("cleaner.startup.risk");

    public CleanerPlatform Platform => CleanerPlatform.Windows;

    public bool RequiresAdministrator => false;

    public bool IsRecommended => true;

    public CleanResult Clean(CleanOptions options)
    {
        CleanResult result = new(Name, options.PreviewOnly);

        if (!OperatingSystem.IsWindows())
        {
            result.AddNote(Localizer.T("status.windowsOnly"));
            return result;
        }

        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);
            if (key is null)
            {
                return result;
            }

            foreach (string valueName in key.GetValueNames())
            {
                if (!AppNames.Contains(valueName, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!options.PreviewOnly)
                {
                    key.DeleteValue(valueName, throwOnMissingValue: false);
                }

                result.AddDeletedFile(0);
            }
        }
        catch
        {
            result.AddFailure();
        }

        return result;
    }
}
