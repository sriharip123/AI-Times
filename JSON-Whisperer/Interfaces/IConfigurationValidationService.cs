using JSON_Whisperer.Models;

namespace JSON_Whisperer.Interfaces
{
    /// <summary>
    /// Service for validating application configuration settings
    /// </summary>
    public interface IConfigurationValidationService
    {
        /// <summary>
        /// Validates all configuration sections and returns a comprehensive result
        /// </summary>
        /// <returns>A task that represents the asynchronous validation operation, containing the validation result</returns>
        Task<ConfigurationValidationResult> ValidateAsync();

        /// <summary>
        /// Validates the Ollama service configuration section
        /// </summary>
        /// <param name="settings">The Ollama settings to validate</param>
        /// <returns>A validation result for the Ollama configuration section</returns>
        ValidationResult ValidateOllamaConfig(OllamaSettings settings);

        /// <summary>
        /// Validates the ScyllaDB database configuration section
        /// </summary>
        /// <param name="settings">The ScyllaDB settings to validate</param>
        /// <returns>A validation result for the ScyllaDB configuration section</returns>
        ValidationResult ValidateScyllaDbConfig(ScyllaDbSettings settings);

        /// <summary>
        /// Validates the Vector similarity configuration section
        /// </summary>
        /// <param name="settings">The Vector settings to validate</param>
        /// <returns>A validation result for the Vector configuration section</returns>
        ValidationResult ValidateVectorConfig(VectorSettings settings);

        /// <summary>
        /// Validates the Application behavior configuration section
        /// </summary>
        /// <param name="settings">The Application settings to validate</param>
        /// <returns>A validation result for the Application configuration section</returns>
        ValidationResult ValidateApplicationConfig(ApplicationSettings settings);
    }
}
