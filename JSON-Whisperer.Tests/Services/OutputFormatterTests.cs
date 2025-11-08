using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using JSON_Whisperer.Models;
using JSON_Whisperer.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace JSON_Whisperer.Tests.Services
{
    [TestFixture]
    public class OutputFormatterTests
    {
        private OutputFormatter _outputFormatter;
        private ILogger<OutputFormatter> _logger;
        private AppSettings _settings;
        private StringWriter _consoleOutput;
        private TextWriter _originalConsoleOut;

        [SetUp]
        public void Setup()
        {
            _logger = new LoggerFactory().CreateLogger<OutputFormatter>();
            _settings = new AppSettings 
            { 
                Application = new ApplicationSettings { VerboseMode = false }
            };
            _outputFormatter = new OutputFormatter(_logger, _settings);
            
            // Capture console output for testing
            _originalConsoleOut = Console.Out;
            _consoleOutput = new StringWriter();
            Console.SetOut(_consoleOutput);
        }

        [TearDown]
        public void TearDown()
        {
            // Restore original console output
            Console.SetOut(_originalConsoleOut);
            _consoleOutput?.Dispose();
        }

        [Test]
        public void DisplayResults_WithValidInput_DisplaysFormattedOutput()
        {
            // Arrange
            var originalJson = "{\"name\":\"John\",\"age\":30,\"city\":\"New York\"}";
            var summary = "This JSON represents a person with name, age, and city information.";
            var analysis = CreateSampleAnalysis();

            // Act
            _outputFormatter.DisplayResults(originalJson, summary, analysis);

            // Assert
            var output = _consoleOutput.ToString();
            Assert.That(output, Contains.Substring("JSON-Whisperer Analysis Results"));
            Assert.That(output, Contains.Substring("Original JSON Structure"));
            Assert.That(output, Contains.Substring("AI-Generated Summary"));
            Assert.That(output, Contains.Substring("Analysis Metadata"));
            Assert.That(output, Contains.Substring("John"));
            Assert.That(output, Contains.Substring(summary));
        }

        [Test]
        public void DisplayResults_WithVerboseMode_DisplaysAdditionalDetails()
        {
            // Arrange
            _settings.Application.VerboseMode = true;
            var originalJson = "{\"users\":[{\"id\":1,\"name\":\"Alice\"}],\"count\":1}";
            var summary = "This JSON contains user data with a count.";
            var analysis = CreateSampleAnalysis();
            analysis.PropertyTypes = new Dictionary<string, JsonValueKind>
            {
                { "users", JsonValueKind.Array },
                { "count", JsonValueKind.Number },
                { "id", JsonValueKind.Number },
                { "name", JsonValueKind.String }
            };
            analysis.ArrayFields = new List<string> { "users" };
            analysis.ObjectFields = new List<string> { "user_object" };

            // Act
            _outputFormatter.DisplayResults(originalJson, summary, analysis);

            // Assert
            var output = _consoleOutput.ToString();
            Assert.That(output, Contains.Substring("Detailed Analysis (Verbose Mode)"));
            Assert.That(output, Contains.Substring("Property Types"));
            Assert.That(output, Contains.Substring("Array Fields"));
            Assert.That(output, Contains.Substring("Nested Object Fields"));
        }

        [Test]
        public void DisplayResults_WithLargeJson_TruncatesDisplay()
        {
            // Arrange
            var largeJson = GenerateLargeJson(3000); // Generate JSON larger than display limit
            var summary = "This is a large JSON object.";
            var analysis = CreateSampleAnalysis();

            // Act
            _outputFormatter.DisplayResults(largeJson, summary, analysis);

            // Assert
            var output = _consoleOutput.ToString();
            Assert.That(output, Contains.Substring("JSON truncated"));
        }

        [Test]
        public void DisplayResults_WithLongSummary_FormatsWithLineBreaks()
        {
            // Arrange
            var originalJson = "{\"test\":\"value\"}";
            var longSummary = "This is a very long summary that should be formatted with proper line breaks to ensure readability and user-friendly display when the text exceeds the recommended line length of eighty characters per line.";
            var analysis = CreateSampleAnalysis();

            // Act
            _outputFormatter.DisplayResults(originalJson, longSummary, analysis);

            // Assert
            var output = _consoleOutput.ToString();
            Assert.That(output, Contains.Substring(longSummary.Substring(0, 50))); // Verify content is present
            // Verify line breaks are added (output should contain multiple lines)
            var lines = output.Split('\n');
            Assert.That(lines.Length, Is.GreaterThan(10)); // Should have multiple lines due to formatting
        }

        [Test]
        public void DisplayResults_ShowsProcessingTimeAndStatistics()
        {
            // Arrange
            var originalJson = "{\"data\":{\"items\":[1,2,3]}}";
            var summary = "Test summary";
            var analysis = CreateSampleAnalysis();
            analysis.EstimatedSize = 1024;
            analysis.TotalProperties = 5;
            analysis.MaxDepth = 2;

            // Act
            _outputFormatter.DisplayResults(originalJson, summary, analysis);

            // Assert
            var output = _consoleOutput.ToString();
            Assert.That(output, Contains.Substring("Size: 1.0 KB"));
            Assert.That(output, Contains.Substring("5 properties"));
            Assert.That(output, Contains.Substring("2 levels deep"));
            Assert.That(output, Contains.Substring("Processing Time"));
            Assert.That(output, Contains.Substring("Analyzed:"));
        }

        [Test]
        public void DisplayError_WithSimpleMessage_DisplaysFormattedError()
        {
            // Arrange
            var errorMessage = "Invalid JSON format";

            // Act
            _outputFormatter.DisplayError(errorMessage);

            // Assert
            var output = _consoleOutput.ToString();
            Assert.That(output, Contains.Substring("JSON-Whisperer Error"));
            Assert.That(output, Contains.Substring(errorMessage));
            Assert.That(output, Contains.Substring("Troubleshooting Tips"));
            Assert.That(output, Contains.Substring("Ensure your JSON is properly formatted"));
            Assert.That(output, Contains.Substring("Check that Ollama is running"));
        }

        [Test]
        public void DisplayError_WithMultilineMessage_FormatsCorrectly()
        {
            // Arrange
            var errorMessage = "Multiple errors occurred:\nJSON parsing failed\nConnection timeout";

            // Act
            _outputFormatter.DisplayError(errorMessage);

            // Assert
            var output = _consoleOutput.ToString();
            Assert.That(output, Contains.Substring("Multiple errors occurred"));
            Assert.That(output, Contains.Substring("JSON parsing failed"));
            Assert.That(output, Contains.Substring("Connection timeout"));
        }

        [Test]
        public void DisplayError_WithNullOrEmptyMessage_DisplaysDefaultMessage()
        {
            // Act & Assert for null
            _outputFormatter.DisplayError(null);
            var output1 = _consoleOutput.ToString();
            Assert.That(output1, Contains.Substring("An unknown error occurred"));

            // Reset console output
            _consoleOutput = new StringWriter();
            Console.SetOut(_consoleOutput);

            // Act & Assert for empty
            _outputFormatter.DisplayError("");
            var output2 = _consoleOutput.ToString();
            Assert.That(output2, Contains.Substring("An unknown error occurred"));
        }

        [Test]
        public void DisplayResults_WithInvalidJson_HandlesGracefully()
        {
            // Arrange
            var invalidJson = "{ invalid json content";
            var summary = "Test summary";
            var analysis = CreateSampleAnalysis();

            // Act & Assert - Should not throw exception
            Assert.DoesNotThrow(() => _outputFormatter.DisplayResults(invalidJson, summary, analysis));
            
            var output = _consoleOutput.ToString();
            Assert.That(output, Contains.Substring("JSON-Whisperer Analysis Results"));
        }

        [Test]
        public void DisplayResults_WithEmptyAnalysis_DisplaysBasicInfo()
        {
            // Arrange
            var originalJson = "{}";
            var summary = "Empty object";
            var analysis = new JsonAnalysisResult
            {
                TotalProperties = 0,
                MaxDepth = 1,
                EstimatedSize = 2,
                AnalyzedAt = DateTime.UtcNow
            };

            // Act
            _outputFormatter.DisplayResults(originalJson, summary, analysis);

            // Assert
            var output = _consoleOutput.ToString();
            Assert.That(output, Contains.Substring("0 properties"));
            Assert.That(output, Contains.Substring("1 levels deep"));
            Assert.That(output, Contains.Substring("2 bytes"));
        }

        [Test]
        public void DisplayResults_WithManyArraysAndObjects_DisplaysCorrectCounts()
        {
            // Arrange
            var originalJson = "{\"arrays\":[1,2,3],\"nested\":{\"data\":[]}}";
            var summary = "Complex structure";
            var analysis = CreateSampleAnalysis();
            analysis.ArrayFields = new List<string> { "arrays", "data" };
            analysis.ObjectFields = new List<string> { "nested" };

            // Act
            _outputFormatter.DisplayResults(originalJson, summary, analysis);

            // Assert
            var output = _consoleOutput.ToString();
            Assert.That(output, Contains.Substring("2 array field(s)"));
            Assert.That(output, Contains.Substring("1 nested object(s)"));
        }

        [Test]
        public void DisplayResults_WithSimilarityResultsInVerboseMode_DisplaysSimilaritySection()
        {
            // Arrange
            _settings.Application.VerboseMode = true;
            var originalJson = "{\"user\":{\"name\":\"John\",\"age\":30}}";
            var summary = "User data structure";
            var analysis = CreateSampleAnalysis();
            var similarityResult = CreateSampleSimilarityResult();

            // Act
            _outputFormatter.DisplayResults(originalJson, summary, analysis, similarityResult);

            // Assert
            var output = _consoleOutput.ToString();
            Assert.That(output, Contains.Substring("Similarity Matching Results"));
            Assert.That(output, Contains.Substring("Search Results: 2 matches found"));
            Assert.That(output, Contains.Substring("Processing Time:"));
            Assert.That(output, Contains.Substring("Threshold Used:"));
            Assert.That(output, Contains.Substring("Highest Score:"));
        }

        [Test]
        public void DisplayResults_WithSimilarityResultsInNormalMode_DoesNotDisplaySimilarity()
        {
            // Arrange
            _settings.Application.VerboseMode = false;
            var originalJson = "{\"user\":{\"name\":\"John\",\"age\":30}}";
            var summary = "User data structure";
            var analysis = CreateSampleAnalysis();
            var similarityResult = CreateSampleSimilarityResult();

            // Act
            _outputFormatter.DisplayResults(originalJson, summary, analysis, similarityResult);

            // Assert
            var output = _consoleOutput.ToString();
            Assert.That(output, Does.Not.Contain("Similarity Matching Results"));
        }

        [Test]
        public void DisplayResults_WithSimilarityMatches_DisplaysMatchDetails()
        {
            // Arrange
            _settings.Application.VerboseMode = true;
            var originalJson = "{\"product\":{\"id\":1,\"name\":\"Widget\"}}";
            var summary = "Product data";
            var analysis = CreateSampleAnalysis();
            var similarityResult = CreateSampleSimilarityResult();

            // Act
            _outputFormatter.DisplayResults(originalJson, summary, analysis, similarityResult);

            // Assert
            var output = _consoleOutput.ToString();
            Assert.That(output, Contains.Substring("Similar Examples Found"));
            Assert.That(output, Contains.Substring("🥇 Match #1"));
            Assert.That(output, Contains.Substring("🥈 Match #2"));
            Assert.That(output, Contains.Substring("Score: 0.950"));
            Assert.That(output, Contains.Substring("Score: 0.820"));
            Assert.That(output, Contains.Substring("User profile data"));
            Assert.That(output, Contains.Substring("Product catalog entry"));
            Assert.That(output, Contains.Substring("JSON Preview:"));
            Assert.That(output, Contains.Substring("ID: example-1"));
            Assert.That(output, Contains.Substring("ID: example-2"));
        }

        [Test]
        public void DisplayResults_WithNoSimilarityMatches_DisplaysNoMatchesMessage()
        {
            // Arrange
            _settings.Application.VerboseMode = true;
            var originalJson = "{\"unique\":{\"data\":\"value\"}}";
            var summary = "Unique data structure";
            var analysis = CreateSampleAnalysis();
            var similarityResult = new SimilarityResult
            {
                Matches = new List<SimilarityMatch>(),
                HighestScore = 0.0f,
                TotalMatches = 0,
                ProcessingTime = TimeSpan.FromMilliseconds(50)
            };

            // Act
            _outputFormatter.DisplayResults(originalJson, summary, analysis, similarityResult);

            // Assert
            var output = _consoleOutput.ToString();
            Assert.That(output, Contains.Substring("Similarity Matching Results"));
            Assert.That(output, Contains.Substring("Search Results: 0 matches found"));
            Assert.That(output, Contains.Substring("No similar examples found above the similarity threshold"));
            Assert.That(output, Contains.Substring("Try lowering the threshold"));
        }

        [Test]
        public void DisplayResults_WithSimilarityResultsAndMetadata_DisplaysMetadata()
        {
            // Arrange
            _settings.Application.VerboseMode = true;
            var originalJson = "{\"test\":\"data\"}";
            var summary = "Test data";
            var analysis = CreateSampleAnalysis();
            var similarityResult = new SimilarityResult
            {
                Matches = new List<SimilarityMatch>
                {
                    new SimilarityMatch
                    {
                        Id = "test-1",
                        JsonContent = "{\"user\":{\"name\":\"Alice\"}}",
                        Description = "User data with metadata",
                        SimilarityScore = 0.85f,
                        Metadata = new Dictionary<string, string>
                        {
                            { "source", "api" },
                            { "version", "1.0" },
                            { "category", "user" }
                        }
                    }
                },
                HighestScore = 0.85f,
                TotalMatches = 1,
                ProcessingTime = TimeSpan.FromMilliseconds(75)
            };

            // Act
            _outputFormatter.DisplayResults(originalJson, summary, analysis, similarityResult);

            // Assert
            var output = _consoleOutput.ToString();
            Assert.That(output, Contains.Substring("🏷️  Metadata: source=api, version=1.0, category=user"));
        }

        [Test]
        public void DisplayResults_WithMoreMatchesThanDisplayed_ShowsLimitMessage()
        {
            // Arrange
            _settings.Application.VerboseMode = true;
            var originalJson = "{\"data\":\"test\"}";
            var summary = "Test data";
            var analysis = CreateSampleAnalysis();
            var similarityResult = new SimilarityResult
            {
                Matches = new List<SimilarityMatch>
                {
                    new SimilarityMatch
                    {
                        Id = "match-1",
                        JsonContent = "{\"example\":1}",
                        Description = "First match",
                        SimilarityScore = 0.9f
                    }
                },
                HighestScore = 0.9f,
                TotalMatches = 10, // More than displayed
                ProcessingTime = TimeSpan.FromMilliseconds(100)
            };

            // Act
            _outputFormatter.DisplayResults(originalJson, summary, analysis, similarityResult);

            // Assert
            var output = _consoleOutput.ToString();
            Assert.That(output, Contains.Substring("Showing top 1 of 10 total matches"));
        }

        private JsonAnalysisResult CreateSampleAnalysis()
        {
            return new JsonAnalysisResult
            {
                TotalProperties = 3,
                MaxDepth = 1,
                PropertyTypes = new Dictionary<string, JsonValueKind>
                {
                    { "name", JsonValueKind.String },
                    { "age", JsonValueKind.Number },
                    { "city", JsonValueKind.String }
                },
                ArrayFields = new List<string>(),
                ObjectFields = new List<string>(),
                EstimatedSize = 45,
                AnalyzedAt = DateTime.UtcNow.AddMilliseconds(-100) // Simulate some processing time
            };
        }

        private string GenerateLargeJson(int targetLength)
        {
            var json = "{";
            var counter = 0;
            
            while (json.Length < targetLength)
            {
                if (counter > 0) json += ",";
                json += $"\"property{counter}\":\"This is a long value for property {counter} to make the JSON larger\"";
                counter++;
            }
            
            json += "}";
            return json;
        }

        private SimilarityResult CreateSampleSimilarityResult()
        {
            return new SimilarityResult
            {
                Matches = new List<SimilarityMatch>
                {
                    new SimilarityMatch
                    {
                        Id = "example-1",
                        JsonContent = "{\"user\":{\"id\":1,\"name\":\"Alice\",\"email\":\"alice@example.com\"}}",
                        Description = "User profile data with contact information",
                        SimilarityScore = 0.95f,
                        Metadata = new Dictionary<string, string>
                        {
                            { "source", "user_api" },
                            { "type", "profile" }
                        }
                    },
                    new SimilarityMatch
                    {
                        Id = "example-2",
                        JsonContent = "{\"product\":{\"id\":123,\"name\":\"Widget\",\"price\":29.99}}",
                        Description = "Product catalog entry with pricing",
                        SimilarityScore = 0.82f,
                        Metadata = new Dictionary<string, string>
                        {
                            { "source", "catalog_api" },
                            { "category", "products" }
                        }
                    }
                },
                HighestScore = 0.95f,
                TotalMatches = 2,
                ProcessingTime = TimeSpan.FromMilliseconds(125)
            };
        }
    }
}