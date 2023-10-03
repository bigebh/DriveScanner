using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
namespace DriveScanner
{
    class Program
    {
        static void Main(string[] args)
        {

            // Check if the user provided an output directory as a command-line argument
            string outputDirectory = GetOutputDirectoryFromUserInput();
            var creditCardAlgorithm = new ScannerAlgorithm(outputDirectory);
            var serviceProvider = new ServiceCollection()
            .AddSingleton<IFileScannerAlgorithm, ScannerAlgorithm>(provider =>
                {
                    return new ScannerAlgorithm(outputDirectory);
                })

           .AddSingleton<IReportGenerator, CsvReportGenerator>()
           .BuildServiceProvider();

            var fileScanner = new FileScanner(
                serviceProvider.GetServices<IFileScannerAlgorithm>().ToArray(),
                serviceProvider.GetRequiredService<IReportGenerator>());
            DriveInfo[] drives = DriveInfo.GetDrives();

            Console.WriteLine("Select drives to scan:");
            Console.WriteLine("0. Scan all drives");
            for (int i = 0; i < drives.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {drives[i].Name}");
            }

            Console.Write("Enter drive numbers (comma-separated) or 0 to scan all: ");
            string driveSelection = Console.ReadLine();

            if (driveSelection == "0")
            {
                // Scan all drives in parallel
                List<Task> scanTasks = new List<Task>();
                foreach (DriveInfo drive in drives)
                {
                    scanTasks.Add(Task.Run(() => ScanDriveIfReady(drive, fileScanner, outputDirectory)));
                }

                Task.WhenAll(scanTasks).Wait();
            }
            else
            {
                // Parse and scan selected drives in parallel
                string[] selectedDriveIndices = driveSelection.Split(',');
                List<Task> scanTasks = new List<Task>();

                foreach (string index in selectedDriveIndices)
                {
                    if (int.TryParse(index, out int driveIndex) && driveIndex > 0 && driveIndex <= drives.Length)
                    {
                        DriveInfo selectedDrive = drives[driveIndex - 1];
                        scanTasks.Add(Task.Run(() => ScanDriveIfReady(selectedDrive, fileScanner, outputDirectory)));
                    }
                    else
                    {
                        Console.WriteLine($"Invalid drive index: {index}");
                    }
                }

                Task.WhenAll(scanTasks).Wait();
            }

        }

        static void ScanDriveIfReady(DriveInfo drive, FileScanner fileScanner, string outputPath)
        {
            if (drive.IsReady)
            {
                string drivePath = drive.RootDirectory.FullName;
                fileScanner.ScanDrive(drivePath, outputPath);
            }
            else
            {
                Console.WriteLine($"Drive {drive.Name} is not ready for scanning.");
            }
        }

        static string GetOutputDirectoryFromUserInput()
        {
            Console.WriteLine("Please enter the output directory where scan results will be saved:");
            string outputDirectory = Console.ReadLine();

            // Validate and handle invalid input (e.g., empty input or invalid directory)
            if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
            {
                Console.WriteLine("Invalid or empty directory. Using default directory : C:\\DefaultOutputDirectory");
                return @"C:\DefaultOutputDirectory"; // Set a default directory here
            }

            return outputDirectory;
        }
    }
}
