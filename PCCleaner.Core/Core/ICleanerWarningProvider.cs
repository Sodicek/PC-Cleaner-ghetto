namespace PCCleaner.Core;

internal interface ICleanerWarningProvider
{
    IReadOnlyList<string> GetWarnings(CleanOptions options);
}
