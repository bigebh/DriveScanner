using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;

namespace DriveScanner
{
    public interface IReportGenerator
    {
        void GenerateReport(IEnumerable<ScanResult> results, string outputPath);
    }

    public class ScanResultMap : ClassMap<ScanResult>
    {
        public ScanResultMap()
        {
            // Map properties to CSV headers
            Map(m => m.FilePath).Name("File Path");
            Map(m => m.MatchedText).Name("Matched Data");
            Map(m => m.StartIndex).Name("Start Index");
            Map(m => m.EndIndex).Name("End Index");
            Map(m => m.ResultType).Name("Result Type");
        }
    }

    public class CsvReportGenerator : IReportGenerator
    {
        public void GenerateReport(IEnumerable<ScanResult> results, string outputPath)
        {
            try
            {
                // If outputPath is a directory, use a default file name
                if (Directory.Exists(outputPath))
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string defaultFileName = $"scan_results_{timestamp}.csv";
                    outputPath = Path.Combine(outputPath, defaultFileName);
                }

                // Create a StreamWriter to write to the CSV file
                using (var writer = new StreamWriter(outputPath))
                using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    csv.Context.RegisterClassMap<ScanResultMap>();
                    // Write the scan results

                    csv.WriteRecords(results);
                }

                Console.WriteLine($"CSV report generated at: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating CSV report: {ex.Message}");
            }
        }
    }
}