using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using JSON_Whisperer.Models;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Service for collecting diagnostic information for troubleshooting
    /// </summary>
    public class DiagnosticService
    {
        private readonly ILogger<DiagnosticService> _logger;
        private readonly AppSettings _settings;

        public DiagnosticService(ILogger<DiagnosticService> logger, AppSettings settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Collects comprehensive diagnostic information
        /// </summary>
        public DiagnosticInfo CollectDiagnosticInfo()
        {
            _logger.LogDebug("Collecting diagnostic information");

            var diagnosticInfo = new DiagnosticInfo
            {
                Timestamp = DateTime.UtcNow,
                ApplicationInfo = GetApplicationInfo(),
                SystemInfo = GetSystemInfo(),
                RuntimeInfo = GetRuntimeInfo(),
                ConfigurationInfo = GetConfigurationInfo(),
                MemoryInfo = GetMemoryInfo(),
                NetworkInfo = GetNetworkInfo()
            };

            _logger.LogDebug("Diagnostic information collected successfully");
            return diagnosticInfo;
        }

        /// <summary>
        /// Logs diagnostic information for troubleshooting
        /// </summary>
        public void LogDiagnosticInfo()
        {
            var info = CollectDiagnosticInfo();

            _logger.LogInformation("=== Diagnostic Information ===");
            _logger.LogInformation("Application: {AppName} v{Version}", info.ApplicationInfo.Name, info.ApplicationInfo.Version);
            _logger.LogInformation("Runtime: {Runtime} on {OS}", info.RuntimeInfo.FrameworkVersion, info.SystemInfo.OperatingSystem);
            _logger.LogInformation("Memory: {WorkingSet} MB working set, {PrivateMemory} MB private", 
                info.MemoryInfo.WorkingSetMB, info.MemoryInfo.PrivateMemoryMB);
            _logger.LogInformation("Configuration: Ollama at {OllamaUrl}, Model: {Model}", 
                info.ConfigurationInfo.OllamaBaseUrl, info.ConfigurationInfo.ModelName);
            _logger.LogInformation("Network: Ollama reachable = {OllamaReachable}", info.NetworkInfo.OllamaReachable);
        }

        /// <summary>
        /// Performs basic health checks
        /// </summary>
        public async Task<HealthCheckResult> PerformHealthCheckAsync()
        {
            _logger.LogDebug("Performing health check");

            var result = new HealthCheckResult
            {
                Timestamp = DateTime.UtcNow,
                OverallStatus = HealthStatus.Healthy
            };

            // Check configuration
            try
            {
                _settings.Validate();
                result.Checks.Add(new HealthCheck("Configuration", HealthStatus.Healthy, "Configuration is valid"));
            }
            catch (Exception ex)
            {
                result.Checks.Add(new HealthCheck("Configuration", HealthStatus.Unhealthy, $"Configuration error: {ex.Message}"));
                result.OverallStatus = HealthStatus.Unhealthy;
            }

            // Check memory usage
            var memoryInfo = GetMemoryInfo();
            var memoryStatus = memoryInfo.WorkingSetMB > 500 ? HealthStatus.Warning : HealthStatus.Healthy;
            var memoryMessage = $"Working set: {memoryInfo.WorkingSetMB} MB";
            result.Checks.Add(new HealthCheck("Memory", memoryStatus, memoryMessage));

            if (memoryStatus == HealthStatus.Warning && result.OverallStatus == HealthStatus.Healthy)
                result.OverallStatus = HealthStatus.Warning;

            // Check Ollama connectivity
            try
            {
                var isOllamaReachable = await CheckOllamaConnectivityAsync();
                var ollamaStatus = isOllamaReachable ? HealthStatus.Healthy : HealthStatus.Unhealthy;
                var ollamaMessage = isOllamaReachable ? "Ollama service is reachable" : "Ollama service is not reachable";
                result.Checks.Add(new HealthCheck("Ollama Connectivity", ollamaStatus, ollamaMessage));

                if (!isOllamaReachable)
                    result.OverallStatus = HealthStatus.Unhealthy;
            }
            catch (Exception ex)
            {
                result.Checks.Add(new HealthCheck("Ollama Connectivity", HealthStatus.Unhealthy, $"Error checking Ollama: {ex.Message}"));
                result.OverallStatus = HealthStatus.Unhealthy;
            }

            _logger.LogInformation("Health check completed with status: {Status}", result.OverallStatus);
            return result;
        }

        private ApplicationInfo GetApplicationInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "Unknown";
            var location = assembly.Location;

            return new ApplicationInfo
            {
                Name = "JSON-Whisperer",
                Version = version,
                Location = location,
                StartTime = Process.GetCurrentProcess().StartTime
            };
        }

        private SystemInfo GetSystemInfo()
        {
            return new SystemInfo
            {
                OperatingSystem = RuntimeInformation.OSDescription,
                Architecture = RuntimeInformation.OSArchitecture.ToString(),
                MachineName = Environment.MachineName,
                UserName = Environment.UserName,
                ProcessorCount = Environment.ProcessorCount,
                SystemDirectory = Environment.SystemDirectory,
                WorkingDirectory = Environment.CurrentDirectory
            };
        }

        private RuntimeInfo GetRuntimeInfo()
        {
            return new RuntimeInfo
            {
                FrameworkVersion = RuntimeInformation.FrameworkDescription,
                RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier,
                ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                Is64BitProcess = Environment.Is64BitProcess,
                Is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
                CLRVersion = Environment.Version.ToString()
            };
        }

        private ConfigurationInfo GetConfigurationInfo()
        {
            return new ConfigurationInfo
            {
                OllamaBaseUrl = _settings.Ollama.BaseUrl,
                ModelName = _settings.Ollama.ModelName,
                TimeoutSeconds = _settings.Ollama.TimeoutSeconds,
                RetryAttempts = _settings.Ollama.RetryAttempts,
                VerboseMode = _settings.Application.VerboseMode,
                MaxJsonSizeBytes = _settings.Application.MaxJsonSizeBytes,
                EnablePerformanceMetrics = _settings.Application.EnablePerformanceMetrics,
                EnableTiming = _settings.Performance.EnableTiming,
                EnableMemoryTracking = _settings.Performance.EnableMemoryTracking
            };
        }

        private MemoryInfo GetMemoryInfo()
        {
            var process = Process.GetCurrentProcess();
            
            return new MemoryInfo
            {
                WorkingSetBytes = process.WorkingSet64,
                WorkingSetMB = process.WorkingSet64 / (1024.0 * 1024.0),
                PrivateMemoryBytes = process.PrivateMemorySize64,
                PrivateMemoryMB = process.PrivateMemorySize64 / (1024.0 * 1024.0),
                VirtualMemoryBytes = process.VirtualMemorySize64,
                VirtualMemoryMB = process.VirtualMemorySize64 / (1024.0 * 1024.0),
                GCTotalMemory = GC.GetTotalMemory(false),
                GCTotalMemoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0)
            };
        }

        private NetworkInfo GetNetworkInfo()
        {
            var networkInfo = new NetworkInfo();

            try
            {
                networkInfo.OllamaReachable = CheckOllamaConnectivityAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check Ollama connectivity");
                networkInfo.OllamaReachable = false;
                networkInfo.OllamaError = ex.Message;
            }

            return networkInfo;
        }

        private async Task<bool> CheckOllamaConnectivityAsync()
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                
                var response = await httpClient.GetAsync($"{_settings.Ollama.BaseUrl}/api/tags");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    #region Diagnostic Data Models

    public class DiagnosticInfo
    {
        public DateTime Timestamp { get; set; }
        public ApplicationInfo ApplicationInfo { get; set; } = new();
        public SystemInfo SystemInfo { get; set; } = new();
        public RuntimeInfo RuntimeInfo { get; set; } = new();
        public ConfigurationInfo ConfigurationInfo { get; set; } = new();
        public MemoryInfo MemoryInfo { get; set; } = new();
        public NetworkInfo NetworkInfo { get; set; } = new();
    }

    public class ApplicationInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
    }

    public class SystemInfo
    {
        public string OperatingSystem { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int ProcessorCount { get; set; }
        public string SystemDirectory { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
    }

    public class RuntimeInfo
    {
        public string FrameworkVersion { get; set; } = string.Empty;
        public string RuntimeIdentifier { get; set; } = string.Empty;
        public string ProcessArchitecture { get; set; } = string.Empty;
        public bool Is64BitProcess { get; set; }
        public bool Is64BitOperatingSystem { get; set; }
        public string CLRVersion { get; set; } = string.Empty;
    }

    public class ConfigurationInfo
    {
        public string OllamaBaseUrl { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; }
        public int RetryAttempts { get; set; }
        public bool VerboseMode { get; set; }
        public long MaxJsonSizeBytes { get; set; }
        public bool EnablePerformanceMetrics { get; set; }
        public bool EnableTiming { get; set; }
        public bool EnableMemoryTracking { get; set; }
    }

    public class MemoryInfo
    {
        public long WorkingSetBytes { get; set; }
        public double WorkingSetMB { get; set; }
        public long PrivateMemoryBytes { get; set; }
        public double PrivateMemoryMB { get; set; }
        public long VirtualMemoryBytes { get; set; }
        public double VirtualMemoryMB { get; set; }
        public long GCTotalMemory { get; set; }
        public double GCTotalMemoryMB { get; set; }
    }

    public class NetworkInfo
    {
        public bool OllamaReachable { get; set; }
        public string? OllamaError { get; set; }
    }

    public class HealthCheckResult
    {
        public DateTime Timestamp { get; set; }
        public HealthStatus OverallStatus { get; set; }
        public List<HealthCheck> Checks { get; set; } = new();
    }

    public class HealthCheck
    {
        public string Name { get; set; }
        public HealthStatus Status { get; set; }
        public string Message { get; set; }

        public HealthCheck(string name, HealthStatus status, string message)
        {
            Name = name;
            Status = status;
            Message = message;
        }
    }

    public enum HealthStatus
    {
        Healthy,
        Warning,
        Unhealthy
    }

    #endregion
}