using System;
using System.Text.Json;
using NUnit.Framework;
using JSON_Whisperer.Services;
using JSON_Whisperer.Models;

namespace JSON_Whisperer.Tests.Services
{
    [TestFixture]
    public class JsonAnalyzerTests
    {
        private JsonAnalyzer _analyzer;

        [SetUp]
        public void Setup()
        {
            _analyzer = new JsonAnalyzer();
        }

        [Test]
        public void AnalyzeStructure_SimpleObject_ReturnsCorrectAnalysis()
        {
            // Arrange
            var json = """{"name": "John", "age": 30, "active": true}""";

            // Act
            var result = _analyzer.AnalyzeStructure(json);

            // Assert
            Assert.That(result.TotalProperties, Is.EqualTo(3));
            Assert.That(result.MaxDepth, Is.EqualTo(1));
            Assert.That(result.PropertyTypes["name"], Is.EqualTo(JsonValueKind.String));
            Assert.That(result.PropertyTypes["age"], Is.EqualTo(JsonValueKind.Number));
            Assert.That(result.PropertyTypes["active"], Is.EqualTo(JsonValueKind.True));
            Assert.That(result.ObjectFields, Is.Empty);
            Assert.That(result.ArrayFields, Is.Empty);
        }

        [Test]
        public void AnalyzeStructure_NestedObject_ReturnsCorrectDepthAndStructure()
        {
            // Arrange
            var json = """
            {
                "user": {
                    "profile": {
                        "name": "John",
                        "settings": {
                            "theme": "dark"
                        }
                    }
                }
            }
            """;

            // Act
            var result = _analyzer.AnalyzeStructure(json);

            // Assert
            Assert.That(result.MaxDepth, Is.EqualTo(4));
            Assert.That(result.ObjectFields, Contains.Item("user"));
            Assert.That(result.ObjectFields, Contains.Item("user.profile"));
            Assert.That(result.ObjectFields, Contains.Item("user.profile.settings"));
            Assert.That(result.PropertyTypes["user.profile.name"], Is.EqualTo(JsonValueKind.String));
            Assert.That(result.PropertyTypes["user.profile.settings.theme"], Is.EqualTo(JsonValueKind.String));
        }

        [Test]
        public void AnalyzeStructure_ArrayWithMixedTypes_HandlesCorrectly()
        {
            // Arrange
            var json = """
            {
                "items": [
                    "string",
                    42,
                    true,
                    null,
                    {"nested": "object"}
                ]
            }
            """;

            // Act
            var result = _analyzer.AnalyzeStructure(json);

            // Assert
            Assert.That(result.ArrayFields, Contains.Item("items"));
            Assert.That(result.PropertyTypes["items"], Is.EqualTo(JsonValueKind.Array));
            Assert.That(result.PropertyTypes["items[0]"], Is.EqualTo(JsonValueKind.String));
            Assert.That(result.PropertyTypes["items[1]"], Is.EqualTo(JsonValueKind.Number));
            Assert.That(result.PropertyTypes["items[2]"], Is.EqualTo(JsonValueKind.True));
            Assert.That(result.PropertyTypes["items[3]"], Is.EqualTo(JsonValueKind.Null));
            Assert.That(result.ObjectFields, Contains.Item("items[4]"));
        }

        [Test]
        public void AnalyzeStructure_ComplexNestedStructure_ReturnsAccurateMetadata()
        {
            // Arrange
            var json = """
            {
                "users": [
                    {
                        "id": 1,
                        "name": "John",
                        "addresses": [
                            {
                                "type": "home",
                                "street": "123 Main St"
                            }
                        ]
                    }
                ],
                "metadata": {
                    "total": 1,
                    "page": 1
                }
            }
            """;

            // Act
            var result = _analyzer.AnalyzeStructure(json);

            // Assert
            Assert.That(result.ArrayFields, Contains.Item("users"));
            Assert.That(result.ArrayFields, Contains.Item("users[0].addresses"));
            Assert.That(result.ObjectFields, Contains.Item("users[0]"));
            Assert.That(result.ObjectFields, Contains.Item("users[0].addresses[0]"));
            Assert.That(result.ObjectFields, Contains.Item("metadata"));
            Assert.That(result.MaxDepth, Is.GreaterThan(3));
        }

        [Test]
        public void ParseJson_ValidJson_ReturnsJsonDocument()
        {
            // Arrange
            var json = """{"test": "value"}""";

            // Act
            using var document = _analyzer.ParseJson(json);

            // Assert
            Assert.That(document.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(document.RootElement.GetProperty("test").GetString(), Is.EqualTo("value"));
        }

        [Test]
        public void ParseJson_JsonWithTrailingCommas_ParsesSuccessfully()
        {
            // Arrange
            var json = """{"test": "value", "number": 42,}""";

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                using var document = _analyzer.ParseJson(json);
                Assert.That(document.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Object));
            });
        }

        [Test]
        public void AnalyzeStructure_EmptyObject_ReturnsBasicAnalysis()
        {
            // Arrange
            var json = "{}";

            // Act
            var result = _analyzer.AnalyzeStructure(json);

            // Assert
            Assert.That(result.TotalProperties, Is.EqualTo(0));
            Assert.That(result.MaxDepth, Is.EqualTo(0));
            Assert.That(result.PropertyTypes, Is.Empty);
            Assert.That(result.ObjectFields, Is.Empty);
            Assert.That(result.ArrayFields, Is.Empty);
        }

        [Test]
        public void AnalyzeStructure_EmptyArray_ReturnsBasicAnalysis()
        {
            // Arrange
            var json = "[]";

            // Act
            var result = _analyzer.AnalyzeStructure(json);

            // Assert
            Assert.That(result.TotalProperties, Is.EqualTo(0));
            Assert.That(result.MaxDepth, Is.EqualTo(0));
            Assert.That(result.PropertyTypes, Is.Empty);
            Assert.That(result.ObjectFields, Is.Empty);
            Assert.That(result.ArrayFields, Is.Empty);
        }
    }

    [TestFixture]
    public class JsonAnalyzerErrorHandlingTests
    {
        private JsonAnalyzer _analyzer;

        [SetUp]
        public void Setup()
        {
            _analyzer = new JsonAnalyzer();
        }

        [Test]
        public void AnalyzeStructure_NullInput_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _analyzer.AnalyzeStructure(null));
            Assert.That(ex.Message, Contains.Substring("cannot be null"));
        }

        [Test]
        public void AnalyzeStructure_EmptyInput_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _analyzer.AnalyzeStructure(""));
            Assert.That(ex.Message, Contains.Substring("cannot be null"));
        }

        [Test]
        public void AnalyzeStructure_WhitespaceOnlyInput_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _analyzer.AnalyzeStructure("   \t\n  "));
            Assert.That(ex.Message, Contains.Substring("whitespace only"));
        }

        [Test]
        public void ParseJson_MalformedJson_ThrowsJsonException()
        {
            // Arrange
            var malformedJson = """{"name": "John", "age":}""";

            // Act & Assert
            var ex = Assert.Throws<JsonException>(() => _analyzer.ParseJson(malformedJson));
            Assert.That(ex.Message, Contains.Substring("Invalid JSON format"));
        }

        [Test]
        public void ParseJson_UnterminatedString_ThrowsJsonExceptionWithDetails()
        {
            // Arrange
            var malformedJson = """{"name": "John""";

            // Act & Assert
            var ex = Assert.Throws<JsonException>(() => _analyzer.ParseJson(malformedJson));
            Assert.That(ex.Message, Contains.Substring("Invalid JSON format"));
        }

        [Test]
        public void ParseJson_MissingComma_ThrowsJsonExceptionWithDetails()
        {
            // Arrange
            var malformedJson = """{"name": "John" "age": 30}""";

            // Act & Assert
            var ex = Assert.Throws<JsonException>(() => _analyzer.ParseJson(malformedJson));
            Assert.That(ex.Message, Contains.Substring("Invalid JSON format"));
        }

        [Test]
        public void AnalyzeStructure_InvalidJsonStart_ThrowsArgumentException()
        {
            // Arrange
            var invalidJson = "not json at all";

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _analyzer.AnalyzeStructure(invalidJson));
            Assert.That(ex.Message, Contains.Substring("must start with"));
        }

        [Test]
        public void AnalyzeStructure_JsonWithBOM_HandlesCorrectly()
        {
            // Arrange
            var jsonWithBOM = "\uFEFF{\"name\": \"John\"}";

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var result = _analyzer.AnalyzeStructure(jsonWithBOM);
                Assert.That(result.TotalProperties, Is.EqualTo(1));
                Assert.That(result.PropertyTypes["name"], Is.EqualTo(JsonValueKind.String));
            });
        }

        [Test]
        public void AnalyzeStructure_PrettyFormattedJson_HandlesCorrectly()
        {
            // Arrange
            var prettyJson = """
            {
                "name": "John",
                "age": 30,
                "active": true
            }
            """;

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var result = _analyzer.AnalyzeStructure(prettyJson);
                Assert.That(result.TotalProperties, Is.EqualTo(3));
            });
        }

        [Test]
        public void AnalyzeStructure_MinifiedJson_HandlesCorrectly()
        {
            // Arrange
            var minifiedJson = """{"name":"John","age":30,"active":true}""";

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var result = _analyzer.AnalyzeStructure(minifiedJson);
                Assert.That(result.TotalProperties, Is.EqualTo(3));
            });
        }
    }
}