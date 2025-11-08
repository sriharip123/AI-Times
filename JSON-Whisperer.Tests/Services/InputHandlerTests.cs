using System;
using System.IO;
using System.Threading.Tasks;
using JSON_Whisperer.Services;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace JSON_Whisperer.Tests.Services
{
    [TestFixture]
    public class InputHandlerTests
    {
        private InputHandler _inputHandler;
        private ILogger<InputHandler> _logger;
        private string _testDirectory;

        [SetUp]
        public void Setup()
        {
            _logger = new LoggerFactory().CreateLogger<InputHandler>();
            _inputHandler = new InputHandler(_logger);
            _testDirectory = Path.Combine(Path.GetTempPath(), "InputHandlerTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Test]
        public async Task GetJsonInputAsync_WithValidJsonArgument_ReturnsJsonContent()
        {
            // Arrange
            var jsonContent = "{\"name\":\"test\",\"value\":123}";
            var args = new[] { jsonContent };

            // Act
            var result = await _inputHandler.GetJsonInputAsync(args);

            // Assert
            Assert.That(result, Is.EqualTo(jsonContent));
        }

        [Test]
        public async Task GetJsonInputAsync_WithValidFilePath_ReturnsFileContent()
        {
            // Arrange
            var jsonContent = "{\"name\":\"test\",\"value\":123}";
            var filePath = Path.Combine(_testDirectory, "test.json");
            await File.WriteAllTextAsync(filePath, jsonContent);
            var args = new[] { filePath };

            // Act
            var result = await _inputHandler.GetJsonInputAsync(args);

            // Assert
            Assert.That(result, Is.EqualTo(jsonContent));
        }

        [Test]
        public void GetJsonInputAsync_WithNonExistentFile_ThrowsInvalidOperationException()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.json");
            var args = new[] { nonExistentPath };

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await _inputHandler.GetJsonInputAsync(args));
            Assert.That(ex.InnerException, Is.TypeOf<FileNotFoundException>());
        }

        [Test]
        public async Task GetJsonInputAsync_WithDirectoryPath_ThrowsInvalidOperationException()
        {
            // Arrange
            var directoryPath = _testDirectory;
            var args = new[] { directoryPath };

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await _inputHandler.GetJsonInputAsync(args));
        }

        [Test]
        public async Task GetJsonInputAsync_WithEmptyFile_ThrowsInvalidOperationException()
        {
            // Arrange
            var emptyFilePath = Path.Combine(_testDirectory, "empty.json");
            await File.WriteAllTextAsync(emptyFilePath, "");
            var args = new[] { emptyFilePath };

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await _inputHandler.GetJsonInputAsync(args));
        }

        [Test]
        public void ValidateInput_WithValidJson_ReturnsTrue()
        {
            // Arrange
            var validJson = "{\"name\":\"test\",\"value\":123}";

            // Act
            var result = _inputHandler.ValidateInput(validJson);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidateInput_WithValidJsonArray_ReturnsTrue()
        {
            // Arrange
            var validJsonArray = "[{\"name\":\"test1\"},{\"name\":\"test2\"}]";

            // Act
            var result = _inputHandler.ValidateInput(validJsonArray);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidateInput_WithInvalidJson_ReturnsFalse()
        {
            // Arrange
            var invalidJson = "{\"name\":\"test\",\"value\":}";

            // Act
            var result = _inputHandler.ValidateInput(invalidJson);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidateInput_WithEmptyString_ReturnsFalse()
        {
            // Arrange
            var emptyJson = "";

            // Act
            var result = _inputHandler.ValidateInput(emptyJson);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidateInput_WithNullString_ReturnsFalse()
        {
            // Arrange
            string nullJson = null;

            // Act
            var result = _inputHandler.ValidateInput(nullJson);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidateInput_WithNonJsonContent_ReturnsFalse()
        {
            // Arrange
            var nonJsonContent = "This is not JSON content";

            // Act
            var result = _inputHandler.ValidateInput(nonJsonContent);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidateInputWithDetails_WithValidJson_ReturnsTrueAndEmptyError()
        {
            // Arrange
            var validJson = "{\"name\":\"test\",\"value\":123}";

            // Act
            var result = _inputHandler.ValidateInputWithDetails(validJson, out var errorMessage);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(errorMessage, Is.Empty);
        }

        [Test]
        public void ValidateInputWithDetails_WithInvalidJson_ReturnsFalseAndErrorMessage()
        {
            // Arrange
            var invalidJson = "{\"name\":\"test\",\"value\":}";

            // Act
            var result = _inputHandler.ValidateInputWithDetails(invalidJson, out var errorMessage);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errorMessage, Is.Not.Empty);
            Assert.That(errorMessage, Does.Contain("Invalid JSON format"));
        }

        [Test]
        public void ValidateInputWithDetails_WithEmptyContent_ReturnsFalseAndErrorMessage()
        {
            // Arrange
            var emptyContent = "";

            // Act
            var result = _inputHandler.ValidateInputWithDetails(emptyContent, out var errorMessage);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errorMessage, Is.EqualTo("Input is empty or contains only whitespace"));
        }

        [Test]
        public void ValidateInputWithDetails_WithNonJsonContent_ReturnsFalseAndErrorMessage()
        {
            // Arrange
            var nonJsonContent = "This is not JSON";

            // Act
            var result = _inputHandler.ValidateInputWithDetails(nonJsonContent, out var errorMessage);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(errorMessage, Does.Contain("does not appear to be JSON"));
        }

        [Test]
        public async Task GetJsonInputAsync_WithLargeFile_ThrowsInvalidOperationException()
        {
            // Arrange
            var largeFilePath = Path.Combine(_testDirectory, "large.json");
            var largeContent = new string('x', 101 * 1024 * 1024); // 101MB
            await File.WriteAllTextAsync(largeFilePath, largeContent);
            var args = new[] { largeFilePath };

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await _inputHandler.GetJsonInputAsync(args));
        }

        [Test]
        public void ValidateInput_WithEmptyObject_ReturnsTrue()
        {
            // Arrange
            var emptyObject = "{}";

            // Act
            var result = _inputHandler.ValidateInput(emptyObject);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidateInput_WithEmptyArray_ReturnsTrue()
        {
            // Arrange
            var emptyArray = "[]";

            // Act
            var result = _inputHandler.ValidateInput(emptyArray);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidateInput_WithJsonWithComments_ReturnsTrue()
        {
            // Arrange
            var jsonWithComments = "{\n  // This is a comment\n  \"name\": \"test\"\n}";

            // Act
            var result = _inputHandler.ValidateInput(jsonWithComments);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidateInput_WithJsonWithTrailingCommas_ReturnsTrue()
        {
            // Arrange
            var jsonWithTrailingCommas = "{\"name\":\"test\",\"value\":123,}";

            // Act
            var result = _inputHandler.ValidateInput(jsonWithTrailingCommas);

            // Assert
            Assert.That(result, Is.True);
        }
    }
}