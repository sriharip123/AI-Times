namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Represents a JSON example from the knowledge base with its description
    /// </summary>
    public class JsonExample
    {
        /// <summary>
        /// Unique identifier for the JSON example
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The JSON content as a string
        /// </summary>
        public string JsonContent { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable description of what this JSON represents
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// File path where this example was loaded from
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
    }
}