using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner.Cleaners;

internal sealed class ScheduledTaskCleaner : ICleaner
{
    public string Name        => Localizer.T("cleaner.scheduledTask.name");
    public string Description => Localizer.T("cleaner.scheduledTask.description");
    public string Risk        => Localizer.T("cleaner.scheduledTask.risk");

    public CleanerPlatform Platform          => CleanerPlatform.Windows;
    public bool RequiresAdministrator        => false;
    public bool IsRecommended                => false;

    public CleanResult Clean(CleanOptions options)
    {
        var result  = new CleanResult(Name, options.PreviewOnly);
        bool exists = ScheduledTaskManager.Exists();

        if (options.PreviewOnly)
        {
            result.AddNote(exists
                ? $"Task '{ScheduledTaskManager.TaskName}' exists and would be removed."
                : $"No task '{ScheduledTaskManager.TaskName}' found.");
            return result;
        }

        if (!exists)
        {
            result.AddNote($"No task '{ScheduledTaskManager.TaskName}' to remove.");
            return result;
        }

        if (ScheduledTaskManager.Remove(out string error))
            result.AddNote($"Removed task '{ScheduledTaskManager.TaskName}'.");
        else
        {
            result.AddNote($"Could not remove task: {error}");
            result.AddFailure();
        }

        return result;
    }
}
