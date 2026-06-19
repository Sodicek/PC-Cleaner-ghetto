namespace PCCleaner.Core;

internal sealed class CleanerRunCallbacks
{
    public Action<string>? Warning { get; init; }

    public Action<ICleaner>? Starting { get; init; }

    public Action<CleanResult>? Completed { get; init; }

    public Action<ICleaner, string>? Skipped { get; init; }

    public Func<ICleaner, bool>? ConfirmCleaner { get; init; }
}
