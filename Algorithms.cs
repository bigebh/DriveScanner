using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DriveScanner
{
    public class ScannerAlgorithm : IFileScannerAlgorithm
    {
        private readonly List<Regex> cardPatterns = new List<Regex>
    {
        new Regex(@"\b4[0-9]{12}(?:[0-9]{3})?\b"),         // Visa
        new Regex(@"\b5[1-5][0-9]{14}\b"),                // Mastercard
        new Regex(@"\b3[47][0-9]{13}\b"),                // American Express
        new Regex(@"\b6(?:011|5[0-9]{2})[0-9]{12}\b"),   // Discover
        new Regex(@"\b3(?:0[0-5]|[68][0-9])[0-9]{11}\b"), // Diners Club
        new Regex(@"\b(?:2131|1800|35\d{3})\d{11}\b"),    // JCB
        new Regex(@"\b(?:5[06789]|6)[0-9]{15}\b")        // Maestro
        // Add patterns for other card types as needed
    };

        private readonly Regex ssnPattern = new Regex(@"\b\d{3}-\d{2}-\d{4}\b");

        public string OutputDirectory { get; }

        public ScannerAlgorithm(string outputDirectory)
        {
            OutputDirectory = outputDirectory; // Initialize the output directory
        }
        public void Initialize() { /* Initialization logic */ }

        public class Scanner
        {
            private List<Regex> cardPatterns;
            private Regex ssnPattern;

            public Scanner(List<Regex> cardPatterns, Regex ssnPattern)
            {
                this.cardPatterns = cardPatterns;
                this.ssnPattern = ssnPattern;
            }
        }
        public IEnumerable<ScanResult> Scan(string filePath)
        {
            var results = new List<ScanResult>();
            string fileContent = ReadFile(filePath);

            List<Task<IEnumerable<ScanResult>>> tasks = new List<Task<IEnumerable<ScanResult>>>();

            // Parallel processing for card patterns
            foreach (var cardPattern in cardPatterns)
            {
                tasks.Add(Task.Run(() => ScanPatternAsync(fileContent, cardPattern, ScanResult.ScanResultType.CreditCard, filePath)));
            }

            // Parallel processing for SSN pattern
            tasks.Add(Task.Run(() => ScanPatternAsync(fileContent, ssnPattern, ScanResult.ScanResultType.SSN, filePath)));

            Task.WhenAll(tasks).Wait();

            foreach (var task in tasks)
            {
                results.AddRange(task.Result);
            }

            return results;
        }

        private async Task<IEnumerable<ScanResult>> ScanPatternAsync(string fileContent, Regex pattern, ScanResult.ScanResultType resultType, string filePath)
        {
            return await Task.Run(() =>
       {
           List<ScanResult> results = new List<ScanResult>();

           foreach (Match match in Regex.Matches(fileContent, pattern.ToString()))
           {
               string matchedText = match.Value;
               if (resultType == ScanResult.ScanResultType.CreditCard ? IsCreditCardNumberValid(matchedText) : IsSSNValid(matchedText))
               {
                   results.Add(new ScanResult
                   {
                       FilePath = filePath,
                       MatchedText = matchedText,
                       StartIndex = match.Index.ToString(),
                       EndIndex = (match.Index + matchedText.Length).ToString(),
                       ResultType = resultType
                   });
               }
           }

           return results;
       });
        }


        private bool IsCreditCardNumberValid(string cardNumber)
        {
            // Remove spaces or other separators from the card number
            cardNumber = cardNumber.Replace(" ", "").Replace("-", "");

            // Check if the card number is composed of digits
            if (!Regex.IsMatch(cardNumber, @"^\d+$"))
            {
                return false;
            }

            // Check that the card number has a valid length (typically 13 to 19 digits)
            int cardLength = cardNumber.Length;
            if (cardLength < 13 || cardLength > 19)
            {
                return false;
            }

            // Use the Luhn algorithm to validate the card number
            int sum = 0;
            bool isSecondDigit = false;

            for (int i = cardLength - 1; i >= 0; i--)
            {
                int digit = cardNumber[i] - '0';

                if (isSecondDigit)
                {
                    digit *= 2;

                    if (digit > 9)
                    {
                        digit -= 9;
                    }
                }

                sum += digit;
                isSecondDigit = !isSecondDigit;
            }

            return (sum % 10) == 0;
        }

        private bool IsSSNValid(string ssn)
        {
            // Remove hyphens from the SSN to facilitate validation
            ssn = ssn.Replace("-", "");

            // Check if the SSN consists of 9 digits
            if (!Regex.IsMatch(ssn, @"^\d{9}$"))
            {
                return false;
            }

            // Verify that the SSN doesn't have all the same digits (e.g., 999-99-9999)
            if (ssn[0] == ssn[1] && ssn[1] == ssn[2] &&
                ssn[3] == ssn[4] && ssn[4] == ssn[5] &&
                ssn[6] == ssn[7] && ssn[7] == ssn[8])
            {
                return false;
            }

            // Return true if all checks pass
            return true;
        }

        private string ReadFile(string filePath)
        {
            try
            {
                // Ensure the file exists
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("File not found.", filePath);
                }

                return File.ReadAllText(filePath);

            }
            catch (IOException ex)
            {
                // Handle any file-related exceptions here
                // You might want to log the error or handle it differently
                Console.WriteLine($"Error reading the file: {ex.Message}");
                return string.Empty; // Return an empty string on error
            }
        }
    }

}


