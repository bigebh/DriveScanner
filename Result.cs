namespace DriveScanner
{
    public class ScanResult
    {
        public string FilePath {get; set;}
        public string MatchedText { get; set; }
        public string StartIndex { get; set; }
        public string EndIndex { get; set; }
        public ScanResultType ResultType { get; set; } // The type of scan result (e.g., CreditCard, SSN, etc.)

        // Additional properties and methods as needed

        public enum ScanResultType
        {
            Unknown,      // Default value
            CreditCard,   // Indicates that a credit card number was found
            SSN,          // Indicates that a Social Security Number (SSN) was found
                          // Add more types as needed
        }
    }
}