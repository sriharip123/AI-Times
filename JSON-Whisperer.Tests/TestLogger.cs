using Microsoft.Extensions.Logging;

namespace JSON_Whisperer.Tests
{
    /// <summary>
    /// Simple test logger implementation for unit tests
    /// </summary>
    /// <typeparam name="T">The type being logged</typeparam>
    public class TestLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // For tests, we typically don't need to actually log anything
            // This could be extended to capture log messages for verification if needed
        }
    }
}