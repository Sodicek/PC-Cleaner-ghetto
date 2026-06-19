namespace PCCleaner.Core;

internal sealed class CleanOptions
{
    public CleanOptions(bool previewOnly, TimeSpan minimumFileAge, bool includeRecentFiles)
    {
        PreviewOnly = previewOnly;
        MinimumFileAge = minimumFileAge;
        IncludeRecentFiles = includeRecentFiles;
    }

    public bool PreviewOnly { get; }

    public TimeSpan MinimumFileAge { get; }

    public bool IncludeRecentFiles { get; }

    public DateTime OldestAllowedWriteTimeUtc => DateTime.UtcNow.Subtract(MinimumFileAge);

    public bool ShouldSkipBecauseRecent(FileInfo file)
    {
        return !IncludeRecentFiles && file.LastWriteTimeUtc > OldestAllowedWriteTimeUtc;
    }
}
