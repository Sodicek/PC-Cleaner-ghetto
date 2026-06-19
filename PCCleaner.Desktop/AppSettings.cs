using System.Text.Json;
using PCCleaner.Utilities;

namespace PCCleaner.Desktop;

internal sealed class AppSettings
{
    public string       Language        { get; set; } = "English";
    public double       AgeHours        { get; set; } = 24;
    public bool         IncludeRecent   { get; set; } = false;
    public List<string> CheckedCleaners { get; set; } = new();

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "pc-cleaner-ghetto", "settings.json");

    private static readonly JsonSerializerOptions SerializerOptions =
        new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            string path = FilePath;
            if (!File.Exists(path)) return new AppSettings();
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch { return new AppSettings(); }
    }

    public void Save()
    {
        try
        {
            string path = FilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, SerializerOptions));
        }
        catch { }
    }

    public AppLanguage GetLanguage() =>
        Language.Equals("Czech", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.Czech
            : AppLanguage.English;

    public void SetLanguage(AppLanguage lang) =>
        Language = lang == AppLanguage.Czech ? "Czech" : "English";
}
