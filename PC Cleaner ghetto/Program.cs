using System;
using System.IO;
using System.Linq;
using System.Security.Principal;
using Microsoft.Win32;

namespace PCCleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!IsUserAdministrator())
            {
                Console.WriteLine("This program must be run as administrator.");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("PC Cleaner - Free up disk space and improve performance");

            CleanTempFolder();

            FindAndRemoveDuplicateFiles();

            OptimizeStartup();

            Console.WriteLine("PC cleaning finished.");

            Console.ReadLine();
        }

        static void CleanTempFolder()
        {
            Console.WriteLine("Cleaning temporary files...");

            string tempPath = Path.GetTempPath();

            try
            {
                DirectoryInfo di = new DirectoryInfo(tempPath);

                foreach (FileInfo file in di.GetFiles())
                {
                    try
                    {
                        file.Delete();
                        Console.WriteLine("Deleted {0}", file.Name);
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine("Failed to delete {0} ({1})", file.Name, ex.Message);
                    }
                }

                Console.WriteLine("Finished cleaning temporary files.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while cleaning temporary files: {0}", ex.Message);
            }
        }

        static void FindAndRemoveDuplicateFiles()
        {
            Console.WriteLine("Finding and removing duplicate files...");

            try
            {
                var duplicateFiles = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "*", SearchOption.AllDirectories)
                                               .GroupBy(file => new FileInfo(file).Length)
                                               .Where(group => group.Count() > 1)
                                               .SelectMany(group => group.OrderBy(file => file)
                                                                        .Skip(1));

                foreach (var file in duplicateFiles)
                {
                    try
                    {
                        File.Delete(file);
                        Console.WriteLine("Deleted duplicate file {0}", file);
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine("Failed to delete {0} ({1})", file, ex.Message);
                    }
                }

                Console.WriteLine("Finished finding and removing duplicate files.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while finding and removing duplicate files: {0}", ex.Message);
            }
        }

        static void OptimizeStartup()
        {
            Console.WriteLine("Optimizing startup...");

            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if (key != null)
                {
                    string[] values = key.GetValueNames();

                    foreach (string value in values)
                    {
                        if (string.Equals(value, "PC Cleaner", StringComparison.OrdinalIgnoreCase))
                        {
                            key.DeleteValue(value);
                            Console.WriteLine("Deleted PC Cleaner from startup.");
                            break;
                        }
                    }
                }

                Console.WriteLine("Finished optimizing startup.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while optimizing startup: {0}", ex.Message);
            }
        }

        static bool IsUserAdministrator()
        {
            bool isAdmin;

            try
            {
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException)
            {
                isAdmin = false;
            }
            catch (Exception)
            {
                isAdmin = false;
            }

            return isAdmin;
        }
    }
}