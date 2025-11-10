namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Represents the health status of an individual service
    /// </summary>
    public class ServiceHealthStatus
    {
        /// <summary>
        /// Name of the service being checked
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the service is healthy
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Status message describing the health check result
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Time taken to perform the health check
        /// </summary>
        public TimeSpan ResponseTime { get; set; }

        /// <summary>
        /// Additional details about the service status
        /// </summary>
        public Dictionary<string, string> Details { get; set; } = new();

        /// <summary>
        /// Exception message if the health check failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Timestamp when the health check was performed
        /// </summary>
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }
}
