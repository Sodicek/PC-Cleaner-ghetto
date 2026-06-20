namespace PCCleaner.Core;

using PCCleaner.Utilities;

internal sealed class CleanResult
{
    private readonly List<string> _notes = new();
    private int _failureNoteCount;

    public CleanResult(string cleanerName, bool previewOnly = false)
    {
        CleanerName = cleanerName;
        PreviewOnly = previewOnly;
    }

    public string CleanerName { get; }

    public bool PreviewOnly { get; }

    public int FilesDeleted { get; private set; }

    public int FilesSkipped { get; private set; }

    public int DirectoriesDeleted { get; private set; }

    public long BytesFreed { get; private set; }

    public int Failures { get; private set; }

    public IReadOnlyList<string> Notes => _notes;

    public void AddDeletedFile(long bytes)
    {
        FilesDeleted++;
        BytesFreed += Math.Max(0, bytes);
    }

    public void AddSkippedFile()
    {
        FilesSkipped++;
    }

    public void AddDeletedDirectory()
    {
        DirectoriesDeleted++;
    }

    private const int MaxFailureNotes = 20;

    public void AddFailure(string filePath = "", string reason = "")
    {
        Failures++;
        if (!string.IsNullOrEmpty(filePath))
        {
            if (_failureNoteCount < MaxFailureNotes)
            {
                string name = Path.GetFileName(filePath);
                if (string.IsNullOrEmpty(name)) name = filePath;
                _notes.Add($"✗ {name}: {reason}");
                _failureNoteCount++;
            }
            else if (_failureNoteCount == MaxFailureNotes)
            {
                _notes.Add("✗ … (more errors not shown — see total count above)");
                _failureNoteCount++;
            }
        }
    }

    public void AddNote(string note)
    {
        if (!string.IsNullOrWhiteSpace(note))
        {
            _notes.Add(note);
        }
    }

    public void Merge(CleanResult other)
    {
        FilesDeleted += other.FilesDeleted;
        FilesSkipped += other.FilesSkipped;
        DirectoriesDeleted += other.DirectoriesDeleted;
        BytesFreed += other.BytesFreed;
        Failures += other.Failures;
        _failureNoteCount += other._failureNoteCount;
        _notes.AddRange(other.Notes);
    }

    public string ToConsoleMessage()
    {
        string action = PreviewOnly ? Localizer.T("result.wouldDelete") : Localizer.T("result.deleted");
        return Localizer.T("result.message", CleanerName, action, FilesDeleted, DirectoriesDeleted, FilesSkipped, SystemInfo.FormatBytes(BytesFreed), Failures);
    }
}
