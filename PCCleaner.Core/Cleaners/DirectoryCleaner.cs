using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner.Cleaners;

internal sealed class DirectoryCleaner : ICleaner
{
    private readonly IReadOnlyList<string> _directoryPaths;
    private readonly string _descriptionKey;
    private readonly string _nameKey;
    private readonly string _riskKey;

    public DirectoryCleaner(
        string nameKey,
        string descriptionKey,
        string riskKey,
        IReadOnlyList<string> directoryPaths,
        CleanerPlatform platform = CleanerPlatform.All,
        bool requiresAdministrator = false,
        bool isRecommended = true)
    {
        _nameKey = nameKey;
        _descriptionKey = descriptionKey;
        _riskKey = riskKey;
        _directoryPaths = directoryPaths;
        Platform = platform;
        RequiresAdministrator = requiresAdministrator;
        IsRecommended = isRecommended;
    }

    public string Name => Localizer.T(_nameKey);

    public string Description => Localizer.T(_descriptionKey);

    public string Risk => Localizer.T(_riskKey);

    public CleanerPlatform Platform { get; }

    public bool RequiresAdministrator { get; }

    public bool IsRecommended { get; }

    public CleanResult Clean(CleanOptions options)
    {
        CleanResult result = new(Name, options.PreviewOnly);

        foreach (string directoryPath in _directoryPaths)
        {
            result.Merge(FileDeleteHelper.CleanDirectory(Name, directoryPath, options));
        }

        return result;
    }
}
