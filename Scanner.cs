using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DriveScanner
{
    public interface IFileScannerAlgorithm
    {
        void Initialize(); // Initialize the algorithm
        IEnumerable<ScanResult> Scan(string filePath); // Perform the scanning
    }
    public class FileScanner
    {
        private readonly IFileScannerAlgorithm[] algorithms;
        private readonly IReportGenerator reportGenerator;

        public FileScanner(IFileScannerAlgorithm[] algorithms, IReportGenerator reportGenerator)
        {
            this.algorithms = algorithms;
            this.reportGenerator = reportGenerator;
        }

        public void ScanDrive(string drivePath,string outputPath)
        {
             // Ensure the specified drive path exists
            if (!Directory.Exists(drivePath))
            {
                Console.WriteLine($"Drive path not found: {drivePath}");
                return;
            }

            var scanResults = new List<ScanResult>();

            Parallel.ForEach(Directory.EnumerateFiles(drivePath, "*", SearchOption.AllDirectories),
                filePath => ScanFile(filePath, scanResults));

            // Process the scan results as needed
            ProcessScanResults(scanResults, outputPath);
        }

       private void ScanFile(string filePath, List<ScanResult> scanResults)
        {
            try
            {
                foreach (var algorithm in algorithms)
                {
                    var results = algorithm.Scan(filePath);
                    // Process and collect scan results as needed
                    lock (scanResults) // Ensure thread-safe access to the list
                    {
                        scanResults.AddRange(results);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while scanning file {filePath}: {ex.Message}");
            }
        }

        private void ProcessScanResults(IEnumerable<ScanResult> results, string outputPath)
        {
            // Process the scan results, generate reports, etc.
            reportGenerator.GenerateReport(results, outputPath);
        }
    }
}