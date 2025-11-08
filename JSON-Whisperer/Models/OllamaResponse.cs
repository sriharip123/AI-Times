namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Response model for Ollama API calls
    /// </summary>
    public class OllamaResponse
    {
        /// <summary>
        /// The generated response text from the model
        /// </summary>
        public string Response { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the response is complete
        /// </summary>
        public bool Done { get; set; }
    }
}