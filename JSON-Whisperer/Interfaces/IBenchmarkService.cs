using JSON_Whisperer.Models;

namespace JSON_Whisperer.Interfaces
{
    /// <summary>
    /// Service for executing performance benchmarks on various operations
    /// </summary>
    public interface IBenchmarkService
    {
        /// <summary>
        /// Runs all available benchmarks and returns aggregated results
        /// </summary>
        /// <returns>List of benchmark results for all operations</returns>
        Task<List<BenchmarkResult>> RunAllBenchmarksAsync();

        /// <summary>
        /// Benchmarks similarity search performance
        /// </summary>
        /// <returns>Benchmark result with timing and throughput metrics</returns>
        Task<BenchmarkResult> BenchmarkSimilarityAsync();

        /// <summary>
        /// Benchmarks vector operations (embedding generation and storage)
        /// </summary>
        /// <returns>Benchmark result with timing and throughput metrics</returns>
        Task<BenchmarkResult> BenchmarkVectorOperationsAsync();

        /// <summary>
        /// Benchmarks embedding generation performance
        /// </summary>
        /// <returns>Benchmark result with timing and throughput metrics</returns>
        Task<BenchmarkResult> BenchmarkEmbeddingAsync();
    }
}
