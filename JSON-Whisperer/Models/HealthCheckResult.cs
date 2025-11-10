namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Represents the result of a comprehensive health check across all services
    /// </summary>
    public class HealthCheckResult
    {
        /// <summary>
        /// Dictionary of service names to their health status
        /// </summary>
        public Dictionary<string, ServiceHealthStatus> ServiceStatuses { get; set; } = new();

        /// <summary>
        /// Indicates whether all services are healthy
        /// </summary>
        public bool AllHealthy => ServiceStatuses.Values.All(s => s.IsHealthy);

        /// <summary>
        /// Total duration of all health checks
        /// </summary>
        public TimeSpan TotalCheckDuration { get; set; }

        /// <summary>
        /// Timestamp when the health check was performed
        /// </summary>
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }
}
