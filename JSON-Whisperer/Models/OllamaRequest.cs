namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Request model for Ollama API calls
    /// </summary>
    public class OllamaRequest
    {
        /// <summary>
        /// The model to use for generation (default: mistral)
        /// </summary>
        public string Model { get; set; } = "mistral";

        /// <summary>
        /// The prompt to send to the model
        /// </summary>
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Whether to stream the response (default: false for simpler handling)
        /// </summary>
        public bool Stream { get; set; } = false;
    }
}