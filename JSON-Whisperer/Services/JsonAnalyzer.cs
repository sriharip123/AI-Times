using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;

namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Service for analyzing JSON structure and extracting metadata
    /// </summary>
    public class JsonAnalyzer : IJsonAnalyzer
    {
        /// <summary>
        /// Analyzes the structure of JSON content and returns analysis results
        /// </summary>
        /// <param name="jsonContent">JSON content to analyze</param>
        /// <returns>Analysis results containing structure metadata</returns>
        public JsonAnalysisResult AnalyzeStructure(string jsonContent)
        {
            // Basic null/empty validation first
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                throw new ArgumentException("JSON content cannot be null, empty, or whitespace only", nameof(jsonContent));
            }

            // Normalize JSON content (handle different formats)
            var normalizedJson = NormalizeJsonContent(jsonContent);
            
            // Validate normalized input
            ValidateJsonInput(normalizedJson);

            var result = new JsonAnalysisResult
            {
                EstimatedSize = System.Text.Encoding.UTF8.GetByteCount(normalizedJson),
                AnalyzedAt = DateTime.UtcNow
            };

            try
            {
                using var document = ParseJson(normalizedJson);
                AnalyzeElement(document.RootElement, result, "", 0);
                return result;
            }
            catch (JsonException ex)
            {
                throw new JsonException($"JSON analysis failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error during JSON analysis: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses JSON content into a JsonDocument for further processing
        /// </summary>
        /// <param name="jsonContent">JSON content to parse</param>
        /// <returns>Parsed JsonDocument</returns>
        public JsonDocument ParseJson(string jsonContent)
        {
            // Basic null/empty validation first
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                throw new ArgumentException("JSON content cannot be null, empty, or whitespace only", nameof(jsonContent));
            }

            // Normalize and validate
            var normalizedJson = NormalizeJsonContent(jsonContent);
            ValidateJsonInput(normalizedJson);

            try
            {
                var options = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip,
                    MaxDepth = 1000 // Prevent stack overflow on deeply nested JSON
                };

                return JsonDocument.Parse(normalizedJson, options);
            }
            catch (JsonException ex)
            {
                var errorDetails = GetDetailedJsonError(normalizedJson, ex);
                throw new JsonException($"Invalid JSON format: {errorDetails}", ex);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"JSON parsing error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Recursively analyzes a JSON element and updates the analysis result
        /// </summary>
        /// <param name="element">JSON element to analyze</param>
        /// <param name="result">Analysis result to update</param>
        /// <param name="propertyPath">Current property path for nested elements</param>
        /// <param name="currentDepth">Current depth in the JSON structure</param>
        private void AnalyzeElement(JsonElement element, JsonAnalysisResult result, string propertyPath, int currentDepth)
        {
            // Update maximum depth
            result.MaxDepth = Math.Max(result.MaxDepth, currentDepth);

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    AnalyzeObject(element, result, propertyPath, currentDepth);
                    break;

                case JsonValueKind.Array:
                    AnalyzeArray(element, result, propertyPath, currentDepth);
                    break;

                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    // Record primitive types
                    if (!string.IsNullOrEmpty(propertyPath))
                    {
                        result.PropertyTypes[propertyPath] = element.ValueKind;
                    }
                    break;
            }
        }

        /// <summary>
        /// Analyzes a JSON object and its properties
        /// </summary>
        /// <param name="element">JSON object element</param>
        /// <param name="result">Analysis result to update</param>
        /// <param name="propertyPath">Current property path</param>
        /// <param name="currentDepth">Current depth in the JSON structure</param>
        private void AnalyzeObject(JsonElement element, JsonAnalysisResult result, string propertyPath, int currentDepth)
        {
            if (!string.IsNullOrEmpty(propertyPath))
            {
                result.ObjectFields.Add(propertyPath);
                result.PropertyTypes[propertyPath] = JsonValueKind.Object;
            }

            foreach (var property in element.EnumerateObject())
            {
                result.TotalProperties++;
                
                var newPath = string.IsNullOrEmpty(propertyPath) 
                    ? property.Name 
                    : $"{propertyPath}.{property.Name}";

                AnalyzeElement(property.Value, result, newPath, currentDepth + 1);
            }
        }

        /// <summary>
        /// Analyzes a JSON array and its elements
        /// </summary>
        /// <param name="element">JSON array element</param>
        /// <param name="result">Analysis result to update</param>
        /// <param name="propertyPath">Current property path</param>
        /// <param name="currentDepth">Current depth in the JSON structure</param>
        private void AnalyzeArray(JsonElement element, JsonAnalysisResult result, string propertyPath, int currentDepth)
        {
            if (!string.IsNullOrEmpty(propertyPath))
            {
                result.ArrayFields.Add(propertyPath);
                result.PropertyTypes[propertyPath] = JsonValueKind.Array;
            }

            var arrayIndex = 0;
            foreach (var arrayElement in element.EnumerateArray())
            {
                var arrayPath = string.IsNullOrEmpty(propertyPath) 
                    ? $"[{arrayIndex}]" 
                    : $"{propertyPath}[{arrayIndex}]";

                AnalyzeElement(arrayElement, result, arrayPath, currentDepth + 1);
                arrayIndex++;
            }
        }

        /// <summary>
        /// Validates JSON input for basic requirements
        /// </summary>
        /// <param name="jsonContent">JSON content to validate</param>
        /// <exception cref="ArgumentException">Thrown when input is invalid</exception>
        private static void ValidateJsonInput(string jsonContent)
        {
            // Check for reasonable size limits (prevent memory issues)
            const int maxSizeBytes = 100 * 1024 * 1024; // 100MB limit
            var sizeBytes = System.Text.Encoding.UTF8.GetByteCount(jsonContent);
            if (sizeBytes > maxSizeBytes)
            {
                throw new ArgumentException($"JSON content is too large ({sizeBytes:N0} bytes). Maximum allowed size is {maxSizeBytes:N0} bytes", nameof(jsonContent));
            }

            // Basic format validation
            var trimmed = jsonContent.Trim();
            if (!IsValidJsonStart(trimmed))
            {
                throw new ArgumentException("JSON content must start with '{' (object) or '[' (array)", nameof(jsonContent));
            }
        }

        /// <summary>
        /// Normalizes JSON content to handle different formats
        /// </summary>
        /// <param name="jsonContent">Original JSON content</param>
        /// <returns>Normalized JSON content</returns>
        private static string NormalizeJsonContent(string jsonContent)
        {
            // Trim whitespace
            var normalized = jsonContent.Trim();

            // Handle BOM (Byte Order Mark) if present
            if (normalized.StartsWith('\uFEFF'))
            {
                normalized = normalized.Substring(1);
            }

            // Remove any leading/trailing whitespace again after BOM removal
            return normalized.Trim();
        }

        /// <summary>
        /// Checks if JSON content starts with valid JSON characters
        /// </summary>
        /// <param name="jsonContent">JSON content to check</param>
        /// <returns>True if starts with valid JSON, false otherwise</returns>
        private static bool IsValidJsonStart(string jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent))
                return false;

            var firstChar = jsonContent[0];
            return firstChar == '{' || firstChar == '[';
        }

        /// <summary>
        /// Provides detailed error information for JSON parsing failures
        /// </summary>
        /// <param name="jsonContent">Original JSON content</param>
        /// <param name="ex">JSON exception that occurred</param>
        /// <returns>Detailed error message</returns>
        private static string GetDetailedJsonError(string jsonContent, JsonException ex)
        {
            var message = ex.Message;

            // Try to extract line and position information if available
            var lineNumberMatch = Regex.Match(message, @"line (\d+)");
            var positionMatch = Regex.Match(message, @"position (\d+)");

            if (lineNumberMatch.Success || positionMatch.Success)
            {
                var details = new List<string> { message };

                if (lineNumberMatch.Success && int.TryParse(lineNumberMatch.Groups[1].Value, out var lineNumber))
                {
                    var lines = jsonContent.Split('\n');
                    if (lineNumber > 0 && lineNumber <= lines.Length)
                    {
                        details.Add($"Problematic line: {lines[lineNumber - 1].Trim()}");
                    }
                }

                return string.Join(" | ", details);
            }

            // Provide common error suggestions
            var suggestions = new List<string>();
            
            if (message.Contains("unexpected character", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add("Check for unescaped quotes, missing commas, or invalid characters");
            }
            
            if (message.Contains("unterminated", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add("Check for missing closing brackets, braces, or quotes");
            }

            if (suggestions.Count > 0)
            {
                return $"{message}. Suggestions: {string.Join(", ", suggestions)}";
            }

            return message;
        }
    }
}