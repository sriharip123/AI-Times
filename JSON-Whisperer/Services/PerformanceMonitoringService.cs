using System.Diagnostics;
using Microsoft.Extensions.Logging;
using JSON_Whisperer.Models;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Service for monitoring application performance and collecting metrics
    /// </summary>
    public class PerformanceMonitoringService
    {
        private readonly ILogger<PerformanceMonitoringService> _logger;
        private readonly AppSettings _settings;
        private readonly Dictionary<string, PerformanceMetric> _metrics;
        private readonly object _metricsLock = new();

        public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger, AppSettings settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _metrics = new Dictionary<string, PerformanceMetric>();
        }

        /// <summary>
        /// Starts timing an operation
        /// </summary>
        public IDisposable StartOperation(string operationName)
        {
            if (!_settings.Performance.EnableTiming)
            {
                return new NoOpDisposable();
            }

            return new OperationTimer(this, operationName);
        }

        /// <summary>
        /// Records the completion of an operation
        /// </summary>
        internal void RecordOperation(string operationName, TimeSpan duration, bool success = true)
        {
            if (!_settings.Performance.EnableTiming)
                return;

            lock (_metricsLock)
            {
                if (!_metrics.TryGetValue(operationName, out var metric))
                {
                    metric = new PerformanceMetric(operationName);
                    _metrics[operationName] = metric;
                }

                metric.RecordExecution(duration, success);
            }

            // Log slow operations
            if (duration.TotalMilliseconds > _settings.Performance.WarnOnSlowOperationsMs)
            {
                _logger.LogWarning("Slow operation detected: {OperationName} took {Duration}ms", 
                    operationName, duration.TotalMilliseconds);
            }

            _logger.LogDebug("Operation {OperationName} completed in {Duration}ms (Success: {Success})", 
                operationName, duration.TotalMilliseconds, success);
        }

        /// <summary>
        /// Records memory usage at a specific point
        /// </summary>
        public void RecordMemoryUsage(string context)
        {
            if (!_settings.Performance.EnableMemoryTracking)
                return;

            var process = Process.GetCurrentProcess();
            var workingSet = process.WorkingSet64;
            var privateMemory = process.PrivateMemorySize64;

            _logger.LogInformation("Memory usage at {Context}: Working Set = {WorkingSet} MB, Private Memory = {PrivateMemory} MB",
                context, 
                workingSet / (1024.0 * 1024.0), 
                privateMemory / (1024.0 * 1024.0));
        }

        /// <summary>
        /// Gets performance metrics summary
        /// </summary>
        public PerformanceMetricsSummary GetMetricsSummary()
        {
            lock (_metricsLock)
            {
                var summary = new PerformanceMetricsSummary
                {
                    TotalOperations = _metrics.Values.Sum(m => m.ExecutionCount),
                    TotalExecutionTime = TimeSpan.FromMilliseconds(_metrics.Values.Sum(m => m.TotalExecutionTime.TotalMilliseconds)),
                    OperationMetrics = _metrics.Values.ToList()
                };

                return summary;
            }
        }

        /// <summary>
        /// Logs performance summary
        /// </summary>
        public void LogPerformanceSummary()
        {
            if (!_settings.Performance.EnableTiming)
                return;

            var summary = GetMetricsSummary();
            
            _logger.LogInformation("Performance Summary:");
            _logger.LogInformation("  Total Operations: {TotalOperations}", summary.TotalOperations);
            _logger.LogInformation("  Total Execution Time: {TotalTime}ms", summary.TotalExecutionTime.TotalMilliseconds);

            foreach (var metric in summary.OperationMetrics.OrderByDescending(m => m.AverageExecutionTime))
            {
                _logger.LogInformation("  {OperationName}: {Count} executions, avg {AvgTime}ms, success rate {SuccessRate:P1}",
                    metric.OperationName,
                    metric.ExecutionCount,
                    metric.AverageExecutionTime.TotalMilliseconds,
                    metric.SuccessRate);
            }
        }

        /// <summary>
        /// Resets all performance metrics
        /// </summary>
        public void ResetMetrics()
        {
            lock (_metricsLock)
            {
                _metrics.Clear();
            }
            _logger.LogDebug("Performance metrics reset");
        }

        private class OperationTimer : IDisposable
        {
            private readonly PerformanceMonitoringService _service;
            private readonly string _operationName;
            private readonly Stopwatch _stopwatch;
            private bool _disposed;

            public OperationTimer(PerformanceMonitoringService service, string operationName)
            {
                _service = service;
                _operationName = operationName;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _stopwatch.Stop();
                    _service.RecordOperation(_operationName, _stopwatch.Elapsed);
                    _disposed = true;
                }
            }
        }

        private class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    /// <summary>
    /// Represents performance metrics for a specific operation
    /// </summary>
    public class PerformanceMetric
    {
        public string OperationName { get; }
        public int ExecutionCount { get; private set; }
        public int SuccessfulExecutions { get; private set; }
        public TimeSpan TotalExecutionTime { get; private set; }
        public TimeSpan MinExecutionTime { get; private set; } = TimeSpan.MaxValue;
        public TimeSpan MaxExecutionTime { get; private set; }
        public TimeSpan AverageExecutionTime => ExecutionCount > 0 ? 
            TimeSpan.FromMilliseconds(TotalExecutionTime.TotalMilliseconds / ExecutionCount) : 
            TimeSpan.Zero;
        public double SuccessRate => ExecutionCount > 0 ? (double)SuccessfulExecutions / ExecutionCount : 0.0;

        public PerformanceMetric(string operationName)
        {
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
        }

        public void RecordExecution(TimeSpan duration, bool success)
        {
            ExecutionCount++;
            if (success)
                SuccessfulExecutions++;

            TotalExecutionTime = TotalExecutionTime.Add(duration);
            
            if (duration < MinExecutionTime)
                MinExecutionTime = duration;
            
            if (duration > MaxExecutionTime)
                MaxExecutionTime = duration;
        }
    }

    /// <summary>
    /// Summary of all performance metrics
    /// </summary>
    public class PerformanceMetricsSummary
    {
        public int TotalOperations { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
        public List<PerformanceMetric> OperationMetrics { get; set; } = new();
    }
}