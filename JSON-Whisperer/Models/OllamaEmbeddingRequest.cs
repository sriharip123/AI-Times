using System.Text.Json.Serialization;

namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Request model for Ollama embedding API
    /// </summary>
    public class OllamaEmbeddingRequest
    {
        /// <summary>
        /// The embedding model to use (e.g., "mistral", "nomic-embed-text")
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = "mistral";

        /// <summary>
        /// The text content to generate embeddings for
        /// </summary>
        [JsonPropertyName("input")]
        public string Input { get; set; } = string.Empty;

        /// <summary>
        /// Additional options for embedding generation
        /// </summary>
        [JsonPropertyName("options")]
        public Dictionary<string, object>? Options { get; set; }
    }
}