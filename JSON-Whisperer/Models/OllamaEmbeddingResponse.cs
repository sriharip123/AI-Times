using System.Text.Json.Serialization;

namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Response model for Ollama embedding API
    /// </summary>
    public class OllamaEmbeddingResponse
    {
        /// <summary>
        /// The generated embedding vectors (array of arrays for multiple inputs)
        /// </summary>
        [JsonPropertyName("embeddings")]
        public float[][] Embeddings { get; set; } = Array.Empty<float[]>();

        /// <summary>
        /// The model used for embedding generation
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Total duration of the embedding generation in nanoseconds
        /// </summary>
        [JsonPropertyName("total_duration")]
        public long TotalDuration { get; set; }

        /// <summary>
        /// Load duration in nanoseconds
        /// </summary>
        [JsonPropertyName("load_duration")]
        public long LoadDuration { get; set; }

        /// <summary>
        /// Prompt evaluation count
        /// </summary>
        [JsonPropertyName("prompt_eval_count")]
        public int PromptEvalCount { get; set; }
    }
}