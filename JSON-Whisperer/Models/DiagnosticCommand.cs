namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Defines available diagnostic commands
    /// </summary>
    public enum DiagnosticCommand
    {
        /// <summary>
        /// Perform health check on all services
        /// </summary>
        HealthCheck,

        /// <summary>
        /// Validate configuration settings
        /// </summary>
        ValidateConfig,

        /// <summary>
        /// Test Ollama service connectivity
        /// </summary>
        TestOllama,

        /// <summary>
        /// Test ScyllaDB connectivity
        /// </summary>
        TestScylla,

        /// <summary>
        /// Test embedding generation service
        /// </summary>
        TestEmbedding,

        /// <summary>
        /// Test similarity search functionality
        /// </summary>
        TestSimilarity,

        /// <summary>
        /// Reinitialize the knowledge base
        /// </summary>
        ReinitializeKnowledgeBase,

        /// <summary>
        /// Validate knowledge base integrity
        /// </summary>
        ValidateKnowledgeBase,

        /// <summary>
        /// Run all benchmarks
        /// </summary>
        BenchmarkAll,

        /// <summary>
        /// Benchmark similarity search performance
        /// </summary>
        BenchmarkSimilarity,

        /// <summary>
        /// Benchmark vector operations performance
        /// </summary>
        BenchmarkVectorOperations,

        /// <summary>
        /// Benchmark embedding generation performance
        /// </summary>
        BenchmarkEmbedding
    }
}
