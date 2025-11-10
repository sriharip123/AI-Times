using JSON_Whisperer.Models;
using JSON_Whisperer.Services;
using NUnit.Framework;

namespace JSON_Whisperer.Tests.Services
{
    [TestFixture]
    public class CommandLineParserTests
    {
        private CommandLineParser _parser = null!;

        [SetUp]
        public void Setup()
        {
            _parser = new CommandLineParser();
        }

        #region Diagnostic Flag Parsing Tests

        [Test]
        public void Parse_WithHealthCheckFlag_SetsDiagnosticModeAndCommand()
        {
            // Arrange
            var args = new[] { "--health-check" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Diagnostic));
            Assert.That(result.DiagnosticCommand, Is.EqualTo(DiagnosticCommand.HealthCheck));
        }

        [Test]
        public void Parse_WithValidateConfigFlag_SetsDiagnosticModeAndCommand()
        {
            // Arrange
            var args = new[] { "--validate-config" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Diagnostic));
            Assert.That(result.DiagnosticCommand, Is.EqualTo(DiagnosticCommand.ValidateConfig));
        }

        [Test]
        public void Parse_WithTestOllamaFlag_SetsDiagnosticModeAndCommand()
        {
            // Arrange
            var args = new[] { "--test-ollama" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Diagnostic));
            Assert.That(result.DiagnosticCommand, Is.EqualTo(DiagnosticCommand.TestOllama));
        }

        [Test]
        public void Parse_WithTestScyllaFlag_SetsDiagnosticModeAndCommand()
        {
            // Arrange
            var args = new[] { "--test-scylla" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Diagnostic));
            Assert.That(result.DiagnosticCommand, Is.EqualTo(DiagnosticCommand.TestScylla));
        }

        [Test]
        public void Parse_WithTestEmbeddingFlag_SetsDiagnosticModeAndCommand()
        {
            // Arrange
            var args = new[] { "--test-embedding" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Diagnostic));
            Assert.That(result.DiagnosticCommand, Is.EqualTo(DiagnosticCommand.TestEmbedding));
        }

        [Test]
        public void Parse_WithTestSimilarityFlag_SetsDiagnosticModeAndCommand()
        {
            // Arrange
            var args = new[] { "--test-similarity" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Diagnostic));
            Assert.That(result.DiagnosticCommand, Is.EqualTo(DiagnosticCommand.TestSimilarity));
        }

        [Test]
        public void Parse_WithReinitializeKnowledgeBaseFlag_SetsDiagnosticModeAndCommand()
        {
            // Arrange
            var args = new[] { "--reinitialize-knowledge-base" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Diagnostic));
            Assert.That(result.DiagnosticCommand, Is.EqualTo(DiagnosticCommand.ReinitializeKnowledgeBase));
        }

        [Test]
        public void Parse_WithValidateKnowledgeBaseFlag_SetsDiagnosticModeAndCommand()
        {
            // Arrange
            var args = new[] { "--validate-knowledge-base" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Diagnostic));
            Assert.That(result.DiagnosticCommand, Is.EqualTo(DiagnosticCommand.ValidateKnowledgeBase));
        }

        [Test]
        public void Parse_WithBenchmarkAllFlag_SetsDiagnosticModeAndCommand()
        {
            // Arrange
            var args = new[] { "--benchmark-all" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Diagnostic));
            Assert.That(result.DiagnosticCommand, Is.EqualTo(DiagnosticCommand.BenchmarkAll));
        }

        [Test]
        public void Parse_WithBenchmarkSimilarityFlag_SetsDiagnosticModeAndCommand()
        {
            // Arrange
            var args = new[] { "--benchmark-similarity" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Diagnostic));
            Assert.That(result.DiagnosticCommand, Is.EqualTo(DiagnosticCommand.BenchmarkSimilarity));
        }

        [Test]
        public void Parse_WithBenchmarkVectorOperationsFlag_SetsDiagnosticModeAndCommand()
        {
            // Arrange
            var args = new[] { "--benchmark-vector-operations" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Diagnostic));
            Assert.That(result.DiagnosticCommand, Is.EqualTo(DiagnosticCommand.BenchmarkVectorOperations));
        }

        [Test]
        public void Parse_WithBenchmarkEmbeddingFlag_SetsDiagnosticModeAndCommand()
        {
            // Arrange
            var args = new[] { "--benchmark-embedding" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Diagnostic));
            Assert.That(result.DiagnosticCommand, Is.EqualTo(DiagnosticCommand.BenchmarkEmbedding));
        }

        #endregion

        #region Input Flag Parsing Tests

        [Test]
        public void Parse_WithHelpFlag_SetsHelpMode()
        {
            // Arrange
            var args = new[] { "--help" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Help));
            Assert.That(result.HelpRequested, Is.True);
        }

        [Test]
        public void Parse_WithShortHelpFlag_SetsHelpMode()
        {
            // Arrange
            var args = new[] { "-h" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Help));
            Assert.That(result.HelpRequested, Is.True);
        }

        [Test]
        public void Parse_WithVerboseFlag_SetsVerboseMode()
        {
            // Arrange
            var args = new[] { "--verbose" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.VerboseMode, Is.True);
        }

        [Test]
        public void Parse_WithShortVerboseFlag_SetsVerboseMode()
        {
            // Arrange
            var args = new[] { "-v" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.VerboseMode, Is.True);
        }

        [Test]
        public void Parse_WithNoSimilarityFlag_SetsNoSimilarity()
        {
            // Arrange
            var args = new[] { "--no-similarity" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.NoSimilarity, Is.True);
        }

        [Test]
        public void Parse_WithFileFlag_SetsFilePath()
        {
            // Arrange
            var args = new[] { "--file", "test.json" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.FilePath, Is.EqualTo("test.json"));
        }

        [Test]
        public void Parse_WithShortFileFlag_SetsFilePath()
        {
            // Arrange
            var args = new[] { "-f", "test.json" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.FilePath, Is.EqualTo("test.json"));
        }

        [Test]
        public void Parse_WithFileFlagButNoPath_MarksInvalid()
        {
            // Arrange
            var args = new[] { "--file" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("requires a file path"));
        }

        [Test]
        public void Parse_WithJsonContent_SetsJsonContent()
        {
            // Arrange
            var jsonContent = "{\"test\":\"value\"}";
            var args = new[] { jsonContent };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.JsonContent, Is.EqualTo(jsonContent));
        }

        #endregion

        #region Execution Mode Detection Tests

        [Test]
        public void Parse_WithNoArguments_SetsNormalMode()
        {
            // Arrange
            var args = new string[] { };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Normal));
        }

        [Test]
        public void Parse_WithNullArguments_SetsNormalMode()
        {
            // Arrange
            string[]? args = null;

            // Act
            var result = _parser.Parse(args!);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Normal));
        }

        [Test]
        public void Parse_WithOnlyVerboseFlag_SetsNormalMode()
        {
            // Arrange
            var args = new[] { "--verbose" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Normal));
            Assert.That(result.VerboseMode, Is.True);
        }

        [Test]
        public void Parse_WithDiagnosticFlag_SetsDiagnosticMode()
        {
            // Arrange
            var args = new[] { "--health-check" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Diagnostic));
        }

        [Test]
        public void Parse_WithHelpFlag_SetsHelpModeOverDiagnostic()
        {
            // Arrange
            var args = new[] { "--help", "--health-check" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Help));
        }

        #endregion

        #region Multiple Diagnostic Flags Tests

        [Test]
        public void Parse_WithMultipleDiagnosticFlags_UsesFirstFlag()
        {
            // Arrange
            var args = new[] { "--health-check", "--validate-config" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Diagnostic));
            Assert.That(result.DiagnosticCommand, Is.EqualTo(DiagnosticCommand.HealthCheck));
        }

        [Test]
        public void Parse_WithDiagnosticFlagAndJsonContent_UsesDiagnosticMode()
        {
            // Arrange
            var args = new[] { "--health-check", "{\"test\":\"value\"}" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Diagnostic));
            Assert.That(result.DiagnosticCommand, Is.EqualTo(DiagnosticCommand.HealthCheck));
        }

        #endregion

        #region Combined Flags Tests

        [Test]
        public void Parse_WithVerboseAndDiagnosticFlags_SetsBothOptions()
        {
            // Arrange
            var args = new[] { "--verbose", "--health-check" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Diagnostic));
            Assert.That(result.VerboseMode, Is.True);
            Assert.That(result.DiagnosticCommand, Is.EqualTo(DiagnosticCommand.HealthCheck));
        }

        [Test]
        public void Parse_WithNoSimilarityAndJsonContent_SetsBothOptions()
        {
            // Arrange
            var args = new[] { "--no-similarity", "{\"test\":\"value\"}" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Normal));
            Assert.That(result.NoSimilarity, Is.True);
            Assert.That(result.JsonContent, Is.EqualTo("{\"test\":\"value\"}"));
        }

        [Test]
        public void Parse_WithFileAndVerboseFlags_SetsBothOptions()
        {
            // Arrange
            var args = new[] { "--file", "test.json", "--verbose" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Normal));
            Assert.That(result.FilePath, Is.EqualTo("test.json"));
            Assert.That(result.VerboseMode, Is.True);
        }

        #endregion

        #region Unknown Flag Tests

        [Test]
        public void Parse_WithUnknownFlag_MarksInvalid()
        {
            // Arrange
            var args = new[] { "--unknown-flag" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("Unknown flag"));
        }

        [Test]
        public void Parse_WithUnknownShortFlag_MarksInvalid()
        {
            // Arrange
            var args = new[] { "-x" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("Unknown flag"));
        }

        #endregion

        #region Validation Tests

        [Test]
        public void IsValid_WithValidHelpMode_ReturnsTrue()
        {
            // Arrange
            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Help,
                HelpRequested = true
            };

            // Act
            var result = _parser.IsValid(options, out var errorMessage);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(errorMessage, Is.Empty);
        }

        [Test]
        public void IsValid_WithValidDiagnosticMode_ReturnsTrue()
        {
            // Arrange
            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.HealthCheck
            };

            // Act
            var result = _parser.IsValid(options, out var errorMessage);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(errorMessage, Is.Empty);
        }

        [Test]
        public void IsValid_WithDiagnosticModeButNoCommand_ReturnsFalse()
        {
            // Arrange
            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = null
            };

            // Act
            var result = _parser.IsValid(options, out var errorMessage);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errorMessage, Does.Contain("requires a diagnostic command"));
        }

        [Test]
        public void IsValid_WithInvalidOptionsFromParsing_ReturnsFalse()
        {
            // Arrange
            var options = new CommandLineOptions
            {
                IsValid = false,
                ErrorMessage = "Test error"
            };

            // Act
            var result = _parser.IsValid(options, out var errorMessage);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errorMessage, Is.EqualTo("Test error"));
        }

        [Test]
        public void IsValid_WithValidNormalMode_ReturnsTrue()
        {
            // Arrange
            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Normal,
                JsonContent = "{\"test\":\"value\"}"
            };

            // Act
            var result = _parser.IsValid(options, out var errorMessage);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(errorMessage, Is.Empty);
        }

        #endregion

        #region Conflicting Flags Tests

        [Test]
        public void IsValid_WithNoSimilarityAndTestScylla_ReturnsFalse()
        {
            // Arrange
            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.TestScylla,
                NoSimilarity = true
            };

            // Act
            var result = _parser.IsValid(options, out var errorMessage);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errorMessage, Does.Contain("--no-similarity"));
            Assert.That(errorMessage, Does.Contain("conflicts"));
        }

        [Test]
        public void IsValid_WithNoSimilarityAndTestSimilarity_ReturnsFalse()
        {
            // Arrange
            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.TestSimilarity,
                NoSimilarity = true
            };

            // Act
            var result = _parser.IsValid(options, out var errorMessage);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errorMessage, Does.Contain("--no-similarity"));
            Assert.That(errorMessage, Does.Contain("conflicts"));
        }

        [Test]
        public void IsValid_WithNoSimilarityAndReinitializeKnowledgeBase_ReturnsFalse()
        {
            // Arrange
            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.ReinitializeKnowledgeBase,
                NoSimilarity = true
            };

            // Act
            var result = _parser.IsValid(options, out var errorMessage);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errorMessage, Does.Contain("--no-similarity"));
            Assert.That(errorMessage, Does.Contain("conflicts"));
        }

        [Test]
        public void IsValid_WithNoSimilarityAndValidateKnowledgeBase_ReturnsFalse()
        {
            // Arrange
            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.ValidateKnowledgeBase,
                NoSimilarity = true
            };

            // Act
            var result = _parser.IsValid(options, out var errorMessage);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errorMessage, Does.Contain("--no-similarity"));
            Assert.That(errorMessage, Does.Contain("conflicts"));
        }

        [Test]
        public void IsValid_WithNoSimilarityAndBenchmarkSimilarity_ReturnsFalse()
        {
            // Arrange
            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.BenchmarkSimilarity,
                NoSimilarity = true
            };

            // Act
            var result = _parser.IsValid(options, out var errorMessage);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errorMessage, Does.Contain("--no-similarity"));
            Assert.That(errorMessage, Does.Contain("conflicts"));
        }

        [Test]
        public void IsValid_WithNoSimilarityAndBenchmarkVectorOperations_ReturnsFalse()
        {
            // Arrange
            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.BenchmarkVectorOperations,
                NoSimilarity = true
            };

            // Act
            var result = _parser.IsValid(options, out var errorMessage);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errorMessage, Does.Contain("--no-similarity"));
            Assert.That(errorMessage, Does.Contain("conflicts"));
        }

        [Test]
        public void IsValid_WithNoSimilarityAndHealthCheck_ReturnsTrue()
        {
            // Arrange
            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.HealthCheck,
                NoSimilarity = true
            };

            // Act
            var result = _parser.IsValid(options, out var errorMessage);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(errorMessage, Is.Empty);
        }

        [Test]
        public void IsValid_WithNoSimilarityAndTestOllama_ReturnsTrue()
        {
            // Arrange
            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Diagnostic,
                DiagnosticCommand = DiagnosticCommand.TestOllama,
                NoSimilarity = true
            };

            // Act
            var result = _parser.IsValid(options, out var errorMessage);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(errorMessage, Is.Empty);
        }

        #endregion

        #region File Path Validation Tests

        [Test]
        public void IsValid_WithBothFilePathAndJsonContent_ReturnsFalse()
        {
            // Arrange
            var options = new CommandLineOptions
            {
                Mode = ExecutionMode.Normal,
                FilePath = "test.json",
                JsonContent = "{\"test\":\"value\"}"
            };

            // Act
            var result = _parser.IsValid(options, out var errorMessage);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errorMessage, Does.Contain("Cannot specify both"));
        }

        #endregion

        #region Case Insensitivity Tests

        [Test]
        public void Parse_WithUpperCaseFlags_ParsesCorrectly()
        {
            // Arrange
            var args = new[] { "--HEALTH-CHECK" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Diagnostic));
            Assert.That(result.DiagnosticCommand, Is.EqualTo(DiagnosticCommand.HealthCheck));
        }

        [Test]
        public void Parse_WithMixedCaseFlags_ParsesCorrectly()
        {
            // Arrange
            var args = new[] { "--Health-Check" };

            // Act
            var result = _parser.Parse(args);

            // Assert
            Assert.That(result.Mode, Is.EqualTo(ExecutionMode.Diagnostic));
            Assert.That(result.DiagnosticCommand, Is.EqualTo(DiagnosticCommand.HealthCheck));
        }

        #endregion
    }
}
