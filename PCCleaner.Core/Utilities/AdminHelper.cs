using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;

namespace PCCleaner.Utilities;

internal static class AdminHelper
{
    public static bool IsRunningAsAdministrator()
    {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            return Environment.UserName == "root";

        if (!OperatingSystem.IsWindows())
            return false;

        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public static bool TryRestartAsAdministrator(IEnumerable<string> args, out string error)
    {
        error = string.Empty;

        if (!OperatingSystem.IsWindows())
        {
            error = Localizer.T("admin.notWindows");
            return false;
        }

        if (IsRunningAsAdministrator())
        {
            error = Localizer.T("admin.already");
            return false;
        }

        try
        {
            string? assemblyPath = Assembly.GetEntryAssembly()?.Location;
            string? processPath = Environment.ProcessPath;

            if (string.IsNullOrWhiteSpace(assemblyPath) && string.IsNullOrWhiteSpace(processPath))
            {
                error = Localizer.T("admin.restartNoPath");
                return false;
            }

            ProcessStartInfo startInfo = CreateElevatedStartInfo(assemblyPath, processPath, args);
            Process.Start(startInfo);
            return true;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            error = Localizer.T("admin.cancelled");
            return false;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or FileNotFoundException)
        {
            error = ex.Message;
            return false;
        }
    }

    private static ProcessStartInfo CreateElevatedStartInfo(string? assemblyPath, string? processPath, IEnumerable<string> args)
    {
        string[] forwardedArgs = args.ToArray();
        bool hasExecutableHost = !string.IsNullOrWhiteSpace(processPath)
            && string.Equals(Path.GetExtension(processPath), ".exe", StringComparison.OrdinalIgnoreCase)
            && !IsDotnetHost(processPath);

        if (hasExecutableHost)
        {
            return new ProcessStartInfo
            {
                FileName = processPath!,
                Arguments = string.Join(' ', forwardedArgs.Select(Quote)),
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Environment.CurrentDirectory
            };
        }

        if (!string.IsNullOrWhiteSpace(assemblyPath)
            && string.Equals(Path.GetExtension(assemblyPath), ".dll", StringComparison.OrdinalIgnoreCase))
        {
            return new ProcessStartInfo
            {
                FileName = string.IsNullOrWhiteSpace(processPath) ? "dotnet" : processPath,
                Arguments = string.Join(' ', new[] { Quote(assemblyPath) }.Concat(forwardedArgs.Select(Quote))),
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Environment.CurrentDirectory
            };
        }

        string executablePath = !string.IsNullOrWhiteSpace(processPath) ? processPath : assemblyPath!;

        return new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = string.Join(' ', forwardedArgs.Select(Quote)),
            UseShellExecute = true,
            Verb = "runas",
            WorkingDirectory = Environment.CurrentDirectory
        };
    }

    private static bool IsDotnetHost(string path)
    {
        return string.Equals(Path.GetFileNameWithoutExtension(path), "dotnet", StringComparison.OrdinalIgnoreCase);
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }
}
