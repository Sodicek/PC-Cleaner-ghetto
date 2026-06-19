using System.Runtime.InteropServices;
using PCCleaner.Core;
using PCCleaner.Utilities;

namespace PCCleaner.Cleaners;

internal sealed class RecycleBinCleaner : ICleaner
{
    private const uint ShrbNoConfirmation = 0x00000001;
    private const uint ShrbNoProgressUi = 0x00000002;
    private const uint ShrbNoSound = 0x00000004;

    public string Name => Localizer.T("cleaner.recycleBin.name");

    public string Description => Localizer.T("cleaner.recycleBin.description");

    public string Risk => Localizer.T("cleaner.recycleBin.risk");

    public CleanerPlatform Platform => CleanerPlatform.Windows;

    public bool RequiresAdministrator => false;

    public bool IsRecommended => true;

    public CleanResult Clean(CleanOptions options)
    {
        CleanResult result = new(Name, options.PreviewOnly);

        if (!OperatingSystem.IsWindows())
        {
            result.AddNote(Localizer.T("status.windowsOnly"));
            return result;
        }

        if (options.PreviewOnly)
        {
            SHQUERYRBINFO info = new();
            info.cbSize = Marshal.SizeOf<SHQUERYRBINFO>();
            int queryResponse = SHQueryRecycleBin(null, ref info);

            if (queryResponse == 0)
            {
                if (info.i64NumItems > 0)
                {
                    result.AddDeletedFile(info.i64Size);
                    for (long i = 1; i < info.i64NumItems; i++)
                    {
                        result.AddDeletedFile(0);
                    }
                }

                result.AddNote(Localizer.T("note.recycleBin", info.i64NumItems, SystemInfo.FormatBytes(info.i64Size)));
            }
            else
            {
                result.AddFailure();
            }

            return result;
        }

        int response = SHEmptyRecycleBin(IntPtr.Zero, null, ShrbNoConfirmation | ShrbNoProgressUi | ShrbNoSound);
        if (response != 0)
        {
            result.AddFailure();
        }

        return result;
    }

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct SHQUERYRBINFO
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

}
