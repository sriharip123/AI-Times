namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Represents the result of a service test operation
    /// </summary>
    public class TestResult
    {
        /// <summary>
        /// Name of the test that was executed
        /// </summary>
        public string TestName { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the test passed successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Human-readable message describing the test result
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Duration of the test execution
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Additional metrics and details collected during the test
        /// </summary>
        public Dictionary<string, object> Metrics { get; set; } = new();

        /// <summary>
        /// Error message if the test failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Timestamp when the test was executed
        /// </summary>
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    }
}
