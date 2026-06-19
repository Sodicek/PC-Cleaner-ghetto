namespace PCCleaner.Core;

internal interface ICleaner
{
    string Name { get; }

    string Description { get; }

    string Risk { get; }

    CleanerPlatform Platform { get; }

    bool RequiresAdministrator { get; }

    bool IsRecommended { get; }

    CleanResult Clean(CleanOptions options);
}
