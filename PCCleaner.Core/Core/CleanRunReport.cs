namespace PCCleaner.Core;

internal sealed class CleanRunReport
{
    private readonly List<CleanResult> _results = new();
    private readonly List<string> _skippedCleaners = new();

    public CleanRunReport(CleanResult total)
    {
        Total = total;
    }

    public CleanResult Total { get; }

    public DiskSpaceSnapshot? DiskBefore { get; private set; }

    public DiskSpaceSnapshot? DiskAfter { get; private set; }

    public IReadOnlyList<CleanResult> Results => _results;

    public IReadOnlyList<string> SkippedCleaners => _skippedCleaners;

    public void AddResult(CleanResult result)
    {
        _results.Add(result);
        Total.Merge(result);
    }

    public void AddSkipped(string cleanerName, string reason)
    {
        _skippedCleaners.Add($"{cleanerName}: {reason}");
    }

    public void SetDiskBefore(DiskSpaceSnapshot? snapshot)
    {
        DiskBefore = snapshot;
    }

    public void SetDiskAfter(DiskSpaceSnapshot? snapshot)
    {
        DiskAfter = snapshot;
    }
}
