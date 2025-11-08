using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using JSON_Whisperer.Interfaces;
using JSON_Whisperer.Models;
using Microsoft.Extensions.Logging;


namespace JSON_Whisperer.Services
{
    /// <summary>
    /// Formats and displays output to the user with clear sections and user-friendly layout
    /// </summary>
    public class OutputFormatter : IOutputFormatter
    {
        private readonly ILogger<OutputFormatter> _logger;
        private readonly AppSettings _settings;
        private const string SectionSeparator = "═══════════════════════════════════════════════════════════════════════════════";
        private const string SubSectionSeparator = "───────────────────────────────────────────────────────────────────────────────";

        public OutputFormatter(ILogger<OutputFormatter> logger, AppSettings settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Displays the results including original JSON, summary, and analysis metadata
        /// </summary>
        /// <param name="originalJson">Original JSON input</param>
        /// <param name="summary">Generated AI summary</param>
        /// <param name="analysis">JSON analysis results</param>
        /// <param name="similarityResult">Optional similarity matching results</param>
        public void DisplayResults(string originalJson, string summary, JsonAnalysisResult analysis, SimilarityResult? similarityResult = null)
        {
            try
            {
                var output = new StringBuilder();
                
                // Header
                output.AppendLine();
                output.AppendLine("🔍 JSON-Whisperer Analysis Results");
                output.AppendLine(SectionSeparator);
                output.AppendLine();

                // Original JSON Section
                output.AppendLine("📄 Original JSON Structure:");
                output.AppendLine(SubSectionSeparator);
                output.AppendLine(FormatJsonForDisplay(originalJson));
                output.AppendLine();

                // AI Summary Section
                output.AppendLine("🤖 AI-Generated Summary:");
                output.AppendLine(SubSectionSeparator);
                output.AppendLine(FormatSummary(summary));
                output.AppendLine();

                // Analysis Metadata Section
                output.AppendLine("📊 Analysis Metadata:");
                output.AppendLine(SubSectionSeparator);
                output.AppendLine(FormatAnalysisMetadata(analysis));

                // Similarity Results Section (if available and verbose mode)
                if (similarityResult != null && _settings.Application.VerboseMode)
                {
                    output.AppendLine();
                    output.AppendLine("🔍 Similarity Matching Results:");
                    output.AppendLine(SubSectionSeparator);
                    output.AppendLine(FormatSimilarityResults(similarityResult));
                }

                // Verbose mode additional details
                if (_settings.Application.VerboseMode)
                {
                    output.AppendLine();
                    output.AppendLine("🔬 Detailed Analysis (Verbose Mode):");
                    output.AppendLine(SubSectionSeparator);
                    output.AppendLine(FormatVerboseAnalysis(analysis));
                }

                // Footer
                output.AppendLine();
                output.AppendLine(SectionSeparator);
                output.AppendLine($"✅ Analysis completed at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                output.AppendLine();

                Console.Write(output.ToString());
                _logger.LogDebug("Successfully displayed results with {Length} characters", output.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error displaying results");
                DisplayError($"Failed to display results: {ex.Message}");
            }
        }

        /// <summary>
        /// Displays error messages in a user-friendly format
        /// </summary>
        /// <param name="errorMessage">Error message to display</param>
        public void DisplayError(string errorMessage)
        {
            try
            {
                var output = new StringBuilder();
                
                output.AppendLine();
                output.AppendLine("❌ JSON-Whisperer Error");
                output.AppendLine(SectionSeparator);
                output.AppendLine();
                
                // Format error message with proper line breaks
                var formattedError = FormatErrorMessage(errorMessage);
                output.AppendLine(formattedError);
                
                output.AppendLine();
                output.AppendLine("💡 Troubleshooting Tips:");
                output.AppendLine("   • Ensure your JSON is properly formatted");
                output.AppendLine("   • Check that Ollama is running (http://localhost:11434)");
                output.AppendLine("   • Verify the Mistral model is installed: ollama pull mistral");
                output.AppendLine("   • For file input, ensure the file exists and is readable");
                output.AppendLine();
                output.AppendLine(SectionSeparator);
                output.AppendLine();

                Console.Write(output.ToString());
                _logger.LogDebug("Displayed error message: {Error}", errorMessage);
            }
            catch (Exception ex)
            {
                // Fallback error display if formatting fails
                Console.WriteLine($"\nError: {errorMessage}");
                Console.WriteLine($"Additional error in formatting: {ex.Message}\n");
                _logger.LogError(ex, "Error displaying error message");
            }
        }

        /// <summary>
        /// Formats JSON for display with proper indentation and size limits
        /// </summary>
        /// <param name="jsonContent">Raw JSON content</param>
        /// <returns>Formatted JSON string</returns>
        private string FormatJsonForDisplay(string jsonContent)
        {
            try
            {
                // Parse and pretty-print the JSON
                using var document = JsonDocument.Parse(jsonContent);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var prettyJson = JsonSerializer.Serialize(document.RootElement, options);
                
                // Limit display size for very large JSON
                const int maxDisplayLength = 2000;
                if (prettyJson.Length > maxDisplayLength)
                {
                    var truncated = prettyJson.Substring(0, maxDisplayLength);
                    var lastNewline = truncated.LastIndexOf('\n');
                    if (lastNewline > 0)
                    {
                        truncated = truncated.Substring(0, lastNewline);
                    }
                    
                    return $"{truncated}\n\n... [JSON truncated - showing first {maxDisplayLength} characters of {prettyJson.Length} total]";
                }

                return prettyJson;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to format JSON for display");
                
                // Fallback to original with basic formatting
                var lines = jsonContent.Split('\n');
                if (lines.Length > 50)
                {
                    var preview = string.Join('\n', lines.Take(50));
                    return $"{preview}\n\n... [JSON preview - showing first 50 lines of {lines.Length} total]";
                }
                
                return jsonContent;
            }
        }

        /// <summary>
        /// Formats the AI summary with proper line breaks and readability
        /// </summary>
        /// <param name="summary">Raw AI summary text</param>
        /// <returns>Formatted summary string</returns>
        private string FormatSummary(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary))
            {
                return "No summary available.";
            }

            var formatted = new StringBuilder();
            var lines = summary.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                {
                    formatted.AppendLine();
                    continue;
                }

                // Add proper indentation and line breaks for readability
                if (trimmedLine.Length > 80)
                {
                    var words = trimmedLine.Split(' ');
                    var currentLine = new StringBuilder();
                    
                    foreach (var word in words)
                    {
                        if (currentLine.Length + word.Length + 1 > 80 && currentLine.Length > 0)
                        {
                            formatted.AppendLine(currentLine.ToString());
                            currentLine.Clear();
                        }
                        
                        if (currentLine.Length > 0)
                        {
                            currentLine.Append(' ');
                        }
                        currentLine.Append(word);
                    }
                    
                    if (currentLine.Length > 0)
                    {
                        formatted.AppendLine(currentLine.ToString());
                    }
                }
                else
                {
                    formatted.AppendLine(trimmedLine);
                }
            }

            return formatted.ToString().TrimEnd();
        }

        /// <summary>
        /// Formats analysis metadata in a readable table format
        /// </summary>
        /// <param name="analysis">JSON analysis results</param>
        /// <returns>Formatted metadata string</returns>
        private string FormatAnalysisMetadata(JsonAnalysisResult analysis)
        {
            var metadata = new StringBuilder();
            
            // Processing time calculation
            var processingTime = DateTime.UtcNow - analysis.AnalyzedAt;
            
            metadata.AppendLine($"📏 Size: {FormatFileSize(analysis.EstimatedSize)}");
            metadata.AppendLine($"🏗️  Structure: {analysis.TotalProperties} properties, {analysis.MaxDepth} levels deep");
            metadata.AppendLine($"📋 Arrays: {analysis.ArrayFields.Count} array field(s)");
            metadata.AppendLine($"🗂️  Objects: {analysis.ObjectFields.Count} nested object(s)");
            metadata.AppendLine($"⏱️  Processing Time: {processingTime.TotalMilliseconds:F0}ms");
            metadata.AppendLine($"🕐 Analyzed: {analysis.AnalyzedAt:yyyy-MM-dd HH:mm:ss} UTC");

            return metadata.ToString().TrimEnd();
        }

        /// <summary>
        /// Formats verbose analysis details including property types and field lists
        /// </summary>
        /// <param name="analysis">JSON analysis results</param>
        /// <returns>Formatted verbose analysis string</returns>
        private string FormatVerboseAnalysis(JsonAnalysisResult analysis)
        {
            var verbose = new StringBuilder();

            // Property types breakdown
            if (analysis.PropertyTypes.Any())
            {
                verbose.AppendLine("🔍 Property Types:");
                var typeGroups = analysis.PropertyTypes
                    .GroupBy(kvp => kvp.Value)
                    .OrderBy(g => g.Key.ToString());

                foreach (var group in typeGroups)
                {
                    verbose.AppendLine($"   {GetJsonTypeIcon(group.Key)} {group.Key}: {group.Count()} properties");
                    
                    if (group.Count() <= 10) // Show individual properties for small groups
                    {
                        foreach (var prop in group.Take(10))
                        {
                            verbose.AppendLine($"      • {prop.Key}");
                        }
                    }
                    else
                    {
                        foreach (var prop in group.Take(5))
                        {
                            verbose.AppendLine($"      • {prop.Key}");
                        }
                        verbose.AppendLine($"      ... and {group.Count() - 5} more");
                    }
                }
                verbose.AppendLine();
            }

            // Array fields details
            if (analysis.ArrayFields.Any())
            {
                verbose.AppendLine("📋 Array Fields:");
                foreach (var field in analysis.ArrayFields.Take(20))
                {
                    verbose.AppendLine($"   • {field}");
                }
                if (analysis.ArrayFields.Count > 20)
                {
                    verbose.AppendLine($"   ... and {analysis.ArrayFields.Count - 20} more");
                }
                verbose.AppendLine();
            }

            // Nested object fields details
            if (analysis.ObjectFields.Any())
            {
                verbose.AppendLine("🗂️ Nested Object Fields:");
                foreach (var field in analysis.ObjectFields.Take(20))
                {
                    verbose.AppendLine($"   • {field}");
                }
                if (analysis.ObjectFields.Count > 20)
                {
                    verbose.AppendLine($"   ... and {analysis.ObjectFields.Count - 20} more");
                }
            }

            return verbose.ToString().TrimEnd();
        }

        /// <summary>
        /// Formats error messages with proper line breaks and consistent styling
        /// </summary>
        /// <param name="errorMessage">Raw error message</param>
        /// <returns>Formatted error message</returns>
        private string FormatErrorMessage(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                return "An unknown error occurred.";
            }

            var formatted = new StringBuilder();
            var lines = errorMessage.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                {
                    continue;
                }

                // Add error icon and proper formatting
                formatted.AppendLine($"   {trimmedLine}");
            }

            return formatted.ToString().TrimEnd();
        }

        /// <summary>
        /// Formats file size in human-readable format
        /// </summary>
        /// <param name="sizeInBytes">Size in bytes</param>
        /// <returns>Formatted size string</returns>
        private static string FormatFileSize(int sizeInBytes)
        {
            if (sizeInBytes < 1024)
                return $"{sizeInBytes} bytes";
            
            if (sizeInBytes < 1024 * 1024)
                return $"{sizeInBytes / 1024.0:F1} KB";
            
            return $"{sizeInBytes / (1024.0 * 1024.0):F1} MB";
        }

        /// <summary>
        /// Formats similarity matching results for display
        /// </summary>
        /// <param name="similarityResult">Similarity matching results</param>
        /// <returns>Formatted similarity results string</returns>
        private string FormatSimilarityResults(SimilarityResult similarityResult)
        {
            var output = new StringBuilder();

            try
            {
                // Summary statistics
                output.AppendLine($"🎯 Search Results: {similarityResult.Matches.Count} matches found");
                output.AppendLine($"⚡ Processing Time: {similarityResult.ProcessingTime.TotalMilliseconds:F0}ms");
                output.AppendLine($"🎚️  Threshold Used: {_settings.Vector.SimilarityThreshold:F2}");
                
                if (similarityResult.Matches.Any())
                {
                    output.AppendLine($"🏆 Highest Score: {similarityResult.HighestScore:F3}");
                }

                output.AppendLine();

                // Display individual matches
                if (similarityResult.Matches.Any())
                {
                    output.AppendLine("📋 Similar Examples Found:");
                    output.AppendLine();

                    for (int i = 0; i < similarityResult.Matches.Count; i++)
                    {
                        var match = similarityResult.Matches[i];
                        var rank = i + 1;
                        
                        output.AppendLine($"   {GetRankIcon(rank)} Match #{rank} (Score: {match.SimilarityScore:F3})");
                        output.AppendLine($"      📝 Description: {FormatMatchDescription(match.Description)}");
                        
                        // Show a preview of the matched JSON
                        var jsonPreview = FormatJsonPreview(match.JsonContent);
                        if (!string.IsNullOrEmpty(jsonPreview))
                        {
                            output.AppendLine($"      📄 JSON Preview: {jsonPreview}");
                        }

                        // Add metadata if available
                        if (match.Metadata != null && match.Metadata.Any())
                        {
                            output.AppendLine($"      🏷️  Metadata: {FormatMatchMetadata(match.Metadata)}");
                        }

                        output.AppendLine($"      🆔 ID: {match.Id}");
                        
                        // Add spacing between matches (except for the last one)
                        if (i < similarityResult.Matches.Count - 1)
                        {
                            output.AppendLine();
                        }
                    }
                }
                else
                {
                    output.AppendLine("❌ No similar examples found above the similarity threshold.");
                    output.AppendLine($"   💡 Try lowering the threshold (currently {_settings.Vector.SimilarityThreshold:F2}) to find more matches.");
                }

                // Additional similarity metadata
                if (similarityResult.TotalMatches > similarityResult.Matches.Count)
                {
                    output.AppendLine();
                    output.AppendLine($"ℹ️  Note: Showing top {similarityResult.Matches.Count} of {similarityResult.TotalMatches} total matches");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting similarity results");
                output.Clear();
                output.AppendLine("❌ Error displaying similarity results");
            }

            return output.ToString().TrimEnd();
        }

        /// <summary>
        /// Gets an appropriate icon for match ranking
        /// </summary>
        /// <param name="rank">Match rank (1-based)</param>
        /// <returns>Rank icon string</returns>
        private static string GetRankIcon(int rank)
        {
            return rank switch
            {
                1 => "🥇",
                2 => "🥈", 
                3 => "🥉",
                _ => "🔹"
            };
        }

        /// <summary>
        /// Formats match description with proper line wrapping
        /// </summary>
        /// <param name="description">Raw description text</param>
        /// <returns>Formatted description</returns>
        private string FormatMatchDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return "No description available";
            }

            // Limit description length for display
            const int maxLength = 150;
            if (description.Length > maxLength)
            {
                var truncated = description.Substring(0, maxLength);
                var lastSpace = truncated.LastIndexOf(' ');
                if (lastSpace > maxLength - 20) // Only truncate at word boundary if it's not too far back
                {
                    truncated = truncated.Substring(0, lastSpace);
                }
                return $"{truncated}...";
            }

            return description;
        }

        /// <summary>
        /// Creates a compact preview of JSON content
        /// </summary>
        /// <param name="jsonContent">JSON content to preview</param>
        /// <returns>Formatted JSON preview</returns>
        private string FormatJsonPreview(string jsonContent)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    return "No JSON content";
                }

                // Parse and get basic structure info
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                return root.ValueKind switch
                {
                    JsonValueKind.Object => FormatObjectPreview(root),
                    JsonValueKind.Array => FormatArrayPreview(root),
                    _ => $"{root.ValueKind}: {root.ToString()}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create JSON preview");
                
                // Fallback to simple truncation
                const int maxPreviewLength = 100;
                if (jsonContent.Length > maxPreviewLength)
                {
                    return $"{jsonContent.Substring(0, maxPreviewLength)}...";
                }
                return jsonContent;
            }
        }

        /// <summary>
        /// Creates a preview for JSON objects
        /// </summary>
        /// <param name="element">JSON object element</param>
        /// <returns>Object preview string</returns>
        private static string FormatObjectPreview(JsonElement element)
        {
            var properties = element.EnumerateObject().Take(3).ToList();
            var propertyNames = properties.Select(p => p.Name);
            var preview = $"{{ {string.Join(", ", propertyNames)}";
            
            if (element.EnumerateObject().Count() > 3)
            {
                preview += $", ... +{element.EnumerateObject().Count() - 3} more";
            }
            
            return preview + " }";
        }

        /// <summary>
        /// Creates a preview for JSON arrays
        /// </summary>
        /// <param name="element">JSON array element</param>
        /// <returns>Array preview string</returns>
        private static string FormatArrayPreview(JsonElement element)
        {
            var length = element.GetArrayLength();
            if (length == 0)
            {
                return "[ ]";
            }

            var firstElement = element.EnumerateArray().First();
            var elementType = firstElement.ValueKind;
            
            return $"[ {elementType} array with {length} items ]";
        }

        /// <summary>
        /// Formats match metadata for display
        /// </summary>
        /// <param name="metadata">Metadata dictionary</param>
        /// <returns>Formatted metadata string</returns>
        private static string FormatMatchMetadata(Dictionary<string, string> metadata)
        {
            if (metadata == null || !metadata.Any())
            {
                return "None";
            }

            var items = metadata.Take(3).Select(kvp => $"{kvp.Key}={kvp.Value}");
            var result = string.Join(", ", items);
            
            if (metadata.Count > 3)
            {
                result += $", +{metadata.Count - 3} more";
            }

            return result;
        }

        /// <summary>
        /// Gets an appropriate icon for JSON value types
        /// </summary>
        /// <param name="valueKind">JSON value kind</param>
        /// <returns>Icon string</returns>
        private static string GetJsonTypeIcon(JsonValueKind valueKind)
        {
            return valueKind switch
            {
                JsonValueKind.Object => "🗂️",
                JsonValueKind.Array => "📋",
                JsonValueKind.String => "📝",
                JsonValueKind.Number => "🔢",
                JsonValueKind.True or JsonValueKind.False => "✅",
                JsonValueKind.Null => "❌",
                _ => "❓"
            };
        }
    }
}