using System;
using System.Collections.Generic;
using System.Text.Json;

namespace JSON_Whisperer.Models
{
    /// <summary>
    /// Contains the results of JSON structure analysis
    /// </summary>
    public class JsonAnalysisResult
    {
        /// <summary>
        /// Total number of properties in the JSON object
        /// </summary>
        public int TotalProperties { get; set; }

        /// <summary>
        /// Maximum depth of nested objects/arrays
        /// </summary>
        public int MaxDepth { get; set; }

        /// <summary>
        /// Dictionary mapping property names to their JSON value types
        /// </summary>
        public Dictionary<string, JsonValueKind> PropertyTypes { get; set; } = new();

        /// <summary>
        /// List of field names that contain arrays
        /// </summary>
        public List<string> ArrayFields { get; set; } = new();

        /// <summary>
        /// List of field names that contain nested objects
        /// </summary>
        public List<string> ObjectFields { get; set; } = new();

        /// <summary>
        /// Estimated size of the JSON content in bytes
        /// </summary>
        public int EstimatedSize { get; set; }

        /// <summary>
        /// Timestamp when the analysis was performed
        /// </summary>
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    }
}