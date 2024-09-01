using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;

namespace ConsoleApp1;

internal class Program
{
    public class Options
    {
        [Option('p', "path", Required = true, HelpText = "Choose path to search for log folders")]
        public string Path { get; set; }

        [Option('r', "readkey", Required = false, HelpText = "Awaits a key to be pressed to exit (to run inside VS)")]
        public bool ReadKey { get; set; }

        [Option('d', "daysToKeep", Required = false, HelpText = "How many days from today to keep log folders/files")]
        public int DaysToKeep { get; set; } = 0;
    }

    private static void Main(string[] args)
    {
        string path = "";
        bool readKey = false;
        int daysToKeep = 0;
        Parser.Default.ParseArguments<Options>(args)
               .WithParsed(parsedArgs =>
               {
                   if (!Directory.Exists(parsedArgs.Path))
                   {
                       Console.ForegroundColor = ConsoleColor.Red;
                       Console.WriteLine($"{parsedArgs.Path} doesn't exists.");
                       return;
                   }
                   if (parsedArgs.DaysToKeep == 0)
                   {
                       Console.ForegroundColor = ConsoleColor.White;
                       Console.WriteLine($"DaysToKeep not specified or invalid. Using standard 7");
                       parsedArgs.DaysToKeep = 7;
                   }
                   path = parsedArgs.Path;
                   readKey = parsedArgs.ReadKey;
                   daysToKeep = parsedArgs.DaysToKeep;

               })
               .WithNotParsed(err =>
               {
                   Console.ForegroundColor = ConsoleColor.Red;
                   foreach (var erro in err)
                   {
                       Console.WriteLine($"ERROR: {erro}");
                   }
                   Console.ResetColor();
               });

        if (string.IsNullOrWhiteSpace(path))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Execution aborted...");
            Console.ResetColor();
            return;
        }
        DirectoryInfo directoryInfo = new(path);

        try
        {
            SearchLogs(directoryInfo, 0, daysToKeep);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{directoryInfo.FullName} EXCEPTION {ex.Message}");
        }
        Console.ResetColor();
        if (readKey)
        {
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }

    private static void SearchLogs(DirectoryInfo directoryInfo, int level, int daysToKeep)
    {
        if (level > 4)
        {
            return;
        }
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine($"{"".PadLeft(level * 2)} Searching {directoryInfo.FullName}...");

        List<DirectoryInfo> listaFisica = directoryInfo.GetDirectories()
            .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReadOnly) &&
            !d.Name.StartsWith('.') &&
            !d.Name.StartsWith("obj")).ToList();
        for (int i = 0; i < listaFisica.Count; i++)
        {
            try
            {
                SearchLogs(listaFisica[i], level + 1, daysToKeep);
                CleanLogs(listaFisica[i], level + 1, daysToKeep);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"SEARCH {listaFisica[i].FullName} EXCEPTION {ex.Message}");
            }
        }
    }

    private static void CleanLogs(DirectoryInfo directoryLogs, int level, int daysToKeep)
    {
        var dataCorte = $"{DateTime.Now.Date.AddDays(-daysToKeep):yyyyMMdd}";
        List<DirectoryInfo> dirs = [.. directoryLogs.GetDirectories()];
        for (int i = 0; i < dirs.Count; i++)
        {
            try
            {
                if (ShouldDelete(dirs[i].Name, dataCorte))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{"".PadLeft(level * 2)} Cleaning {dirs[i].FullName}...");
                    Directory.Delete(dirs[i].FullName, true);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"{"".PadLeft(level * 2)} Skipping Cleaning {dirs[i].FullName}...");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"CLEAN DIR {dirs[i].FullName} EXCEPTION {ex.Message}");
            }
        }
        if (!directoryLogs.FullName.Contains("LOG", StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }
        var files = directoryLogs.GetFiles().Where(x => x.Extension is ".txt" or ".log").ToList();
        for (int i = 0; i < files.Count; i++)
        {
            try
            {
                if (files[i].LastWriteTime.Date < DateTime.Now.Date.AddDays(-daysToKeep))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{"".PadLeft(level * 2)} Cleaning File {files[i].FullName}...");
                    File.Delete(files[i].FullName);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"{"".PadLeft(level * 2)} Skipping Cleaning File {files[i].FullName}...");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"CLEAN FILE {files[i].FullName} EXCEPTION {ex.Message}");
            }
        }
    }

    private static bool ShouldDelete(string dirName, string dataCorte)
    {
        //Remove non digits from dirName as Span
        int ixStartYear = dirName.IndexOf('2');
        if (ixStartYear < 0)
        {
            return false;
        }
        if (dirName.Length < ixStartYear + 6)
        {
            return false;
        }
        var spDir = dirName.AsSpan()[ixStartYear..];
        if (spDir.Length < 6)
        {
            return false;
        }
        var spData = dataCorte.AsSpan();

        return MemoryExtensions.CompareTo(spDir, spData, StringComparison.InvariantCultureIgnoreCase) < 0;
    }
}
