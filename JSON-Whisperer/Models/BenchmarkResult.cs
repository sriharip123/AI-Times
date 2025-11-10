namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Represents the result of a performance benchmark operation
    /// </summary>
    public class BenchmarkResult
    {
        /// <summary>
        /// Name of the benchmark that was executed
        /// </summary>
        public string BenchmarkName { get; set; } = string.Empty;

        /// <summary>
        /// Number of iterations performed in the benchmark
        /// </summary>
        public int Iterations { get; set; }

        /// <summary>
        /// Total duration of all iterations
        /// </summary>
        public TimeSpan TotalDuration { get; set; }

        /// <summary>
        /// Average duration per operation in milliseconds
        /// </summary>
        public double AverageDurationMs { get; set; }

        /// <summary>
        /// Number of operations completed per second
        /// </summary>
        public double OperationsPerSecond { get; set; }

        /// <summary>
        /// Memory used during benchmark execution in bytes
        /// </summary>
        public long MemoryUsedBytes { get; set; }

        /// <summary>
        /// Additional metrics specific to the benchmark type
        /// </summary>
        public Dictionary<string, double> AdditionalMetrics { get; set; } = new();

        /// <summary>
        /// Timestamp when the benchmark was executed
        /// </summary>
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Indicates whether the benchmark completed successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if the benchmark failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
