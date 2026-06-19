namespace PCCleaner.Core;

internal sealed class DiskSpaceSnapshot
{
    public DiskSpaceSnapshot(string driveName, long freeBytes)
    {
        DriveName = driveName;
        FreeBytes = freeBytes;
    }

    public string DriveName { get; }

    public long FreeBytes { get; }
}
