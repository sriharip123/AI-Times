using System.Text.Json.Serialization;

namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Represents a vector embedding stored in the database
    /// </summary>
    public class VectorEmbedding
    {
        /// <summary>
        /// Unique identifier for the embedding
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The vector embedding as a float array
        /// </summary>
        public float[] Embedding { get; set; } = Array.Empty<float>();

        /// <summary>
        /// The original JSON content that was embedded
        /// </summary>
        public string JsonContent { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable description of the JSON content
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// When the embedding was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional metadata about the embedding source
        /// </summary>
        public Dictionary<string, string>? Metadata { get; set; }
    }
}