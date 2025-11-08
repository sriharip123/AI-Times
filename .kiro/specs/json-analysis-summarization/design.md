# Design Document

## Overview

The JSON-Whisperer system follows a modular architecture with clear separation of concerns. The application consists of input handling, JSON analysis, AI integration, vector similarity matching, and output formatting components. The design emphasizes reliability, extensibility, and user experience while maintaining simplicity for a console application. The system now includes vector embedding capabilities using ScyllaDB for high-performance storage and Ollama for embedding generation to provide contextual similarity matching.

## Architecture

The system uses a layered architecture pattern:

```
┌─────────────────────────────────────────────────────────┐
│                Console Interface                        │
├─────────────────────────────────────────────────────────┤
│              Application Layer                          │
├─────────────────────────────────────────────────────────┤
│  JSON Analysis  │  AI Service  │  Vector Similarity     │
├─────────────────┼──────────────┼────────────────────────┤
│  Input Handler  │ Output Format│  Embedding Service     │
├─────────────────┼──────────────┼────────────────────────┤
│  Knowledge Base │  ScyllaDB    │  Similarity Service    │
├─────────────────┼──────────────┼────────────────────────┤
│              Infrastructure Layer                       │
└─────────────────────────────────────────────────────────┘
```

### Core Components

1. **Program Entry Point**: Handles command line parsing and application orchestration
2. **Input Handler**: Manages different input sources (CLI args, files, stdin)
3. **JSON Analyzer**: Parses and analyzes JSON structure
4. **AI Service**: Interfaces with Ollama API for summary generation
5. **Embedding Service**: Generates vector embeddings using Ollama's embedding API
6. **Vector Database Service**: Manages ScyllaDB database operations for embeddings
7. **Similarity Service**: Performs cosine similarity matching and retrieval
8. **Knowledge Base Service**: Loads and manages JSON examples from AppData
9. **Output Formatter**: Presents results in user-friendly format

## Components and Interfaces

### IInputHandler
```csharp
public interface IInputHandler
{
    Task<string> GetJsonInputAsync(string[] args);
    bool ValidateInput(string jsonContent);
}
```

### IJsonAnalyzer
```csharp
public interface IJsonAnalyzer
{
    JsonAnalysisResult AnalyzeStructure(string jsonContent);
    JsonDocument ParseJson(string jsonContent);
}
```

### IAiService
```csharp
public interface IAiService
{
    Task<bool> IsAvailableAsync();
    Task<string> GenerateSummaryAsync(JsonAnalysisResult analysis, string originalJson);
}
```

### IOutputFormatter
```csharp
public interface IOutputFormatter
{
    void DisplayResults(string originalJson, string summary, JsonAnalysisResult analysis, SimilarityResult? similarityResult = null);
    void DisplayError(string errorMessage);
}
```

### IEmbeddingService
```csharp
public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string jsonContent);
    Task<bool> IsEmbeddingServiceAvailableAsync();
}
```

### IVectorDatabaseService
```csharp
public interface IVectorDatabaseService
{
    Task<bool> InitializeAsync();
    Task StoreEmbeddingAsync(string id, float[] embedding, string jsonContent, string description);
    Task<List<SimilarityMatch>> FindSimilarAsync(float[] queryEmbedding, int maxResults = 5, float threshold = 0.7f);
    Task<bool> IsConnectedAsync();
}
```

### ISimilarityService
```csharp
public interface ISimilarityService
{
    Task<SimilarityResult> FindSimilarJsonAsync(string inputJson);
    float CalculateCosineSimilarity(float[] vector1, float[] vector2);
}
```

### IKnowledgeBaseService
```csharp
public interface IKnowledgeBaseService
{
    Task<List<JsonExample>> LoadExamplesAsync();
    Task InitializeVectorDatabaseAsync();
}
```

## Data Models

### JsonAnalysisResult
```csharp
public class JsonAnalysisResult
{
    public int TotalProperties { get; set; }
    public int MaxDepth { get; set; }
    public Dictionary<string, JsonValueKind> PropertyTypes { get; set; }
    public List<string> ArrayFields { get; set; }
    public List<string> ObjectFields { get; set; }
    public int EstimatedSize { get; set; }
    public DateTime AnalyzedAt { get; set; }
}
```

### OllamaRequest/Response Models
```csharp
public class OllamaRequest
{
    public string Model { get; set; } = "mistral";
    public string Prompt { get; set; }
    public bool Stream { get; set; } = false;
}

public class OllamaResponse
{
    public string Response { get; set; }
    public bool Done { get; set; }
}

public class OllamaEmbeddingRequest
{
    public string Model { get; set; } = "nomic-embed-text";
    public string Prompt { get; set; }
}

public class OllamaEmbeddingResponse
{
    public float[] Embedding { get; set; }
}
```

### Vector Similarity Models
```csharp
public class JsonExample
{
    public string Id { get; set; }
    public string JsonContent { get; set; }
    public string Description { get; set; }
    public string FilePath { get; set; }
}

public class SimilarityMatch
{
    public string Id { get; set; }
    public string JsonContent { get; set; }
    public string Description { get; set; }
    public float SimilarityScore { get; set; }
}

public class SimilarityResult
{
    public List<SimilarityMatch> Matches { get; set; } = new();
    public float HighestScore { get; set; }
    public int TotalMatches { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}

public class VectorEmbedding
{
    public string Id { get; set; }
    public float[] Embedding { get; set; }
    public string JsonContent { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## Implementation Details

### JSON Analysis Strategy
- Use System.Text.Json for parsing and validation
- Implement recursive traversal for structure analysis
- Collect metadata about types, depth, and complexity
- Generate structured analysis data for AI prompt creation

### AI Integration Approach
- Use HttpClient for Ollama API communication
- Implement retry logic with exponential backoff
- Create structured prompts that include JSON analysis metadata and similar examples
- Handle streaming responses if needed for large summaries
- Support both text generation and embedding models

### Vector Similarity Strategy
- Use Ollama's embedding API (nomic-embed-text model) for generating embeddings
- Store embeddings in ScyllaDB with JSON content and descriptions for high-performance retrieval
- Implement cosine similarity calculation for matching
- Load knowledge base from AppData directory on startup
- Cache embeddings to avoid regeneration
- Provide configurable similarity thresholds and result limits
- Leverage ScyllaDB's performance advantages for fast vector similarity searches

### Input Handling Strategy
- Command line arguments take precedence
- File path validation and error handling
- Stdin reading with timeout for interactive use
- Support for both compressed and formatted JSON

### Error Handling Strategy
- Graceful degradation when Ollama is unavailable
- Fallback to basic analysis when vector database is unavailable
- Clear error messages for common issues (ScyllaDB connection, embedding failures)
- Validation at each layer with specific error types
- Logging for debugging and monitoring
- Continue operation without similarity matching if vector services fail

## Configuration

### Application Settings
```csharp
public class AppSettings
{
    public OllamaSettings Ollama { get; set; } = new();
    public ApplicationSettings Application { get; set; } = new();
    public VectorSettings Vector { get; set; } = new();
    public ScyllaDbSettings ScyllaDb { get; set; } = new();
}

public class OllamaSettings
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string ModelName { get; set; } = "mistral";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    public int TimeoutSeconds { get; set; } = 30;
}

public class VectorSettings
{
    public float SimilarityThreshold { get; set; } = 0.7f;
    public int MaxSimilarResults { get; set; } = 5;
    public bool EnableSimilarityMatching { get; set; } = true;
    public string AppDataPath { get; set; } = "AppData";
}

public class ScyllaDbSettings
{
    public string ContactPoints { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 9042;
    public string Keyspace { get; set; } = "json_whisperer";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string DataCenter { get; set; } = "datacenter1";
}
```

### Dependency Injection Setup
- Register services with appropriate lifetimes
- Configure HttpClient with proper timeouts
- Setup logging and configuration providers

## Error Handling

### Error Categories
1. **Input Errors**: Invalid JSON, file not found, permission issues
2. **Service Errors**: Ollama unavailable, model not found, API timeouts
3. **Processing Errors**: Analysis failures, memory issues, unexpected exceptions

### Error Response Strategy
- Provide actionable error messages
- Include suggestions for resolution
- Maintain consistent error format
- Log errors for debugging while keeping user output clean

## Testing Strategy

### Unit Testing
- Mock external dependencies (Ollama API, file system)
- Test JSON analysis logic with various input types
- Validate error handling scenarios
- Test input parsing and validation

### Integration Testing
- Test with real Ollama instance when available
- Validate end-to-end workflows
- Test with various JSON complexity levels
- Performance testing with large JSON files

### Test Data Strategy
- Create sample JSON files of varying complexity
- Include edge cases (empty objects, deep nesting, large arrays)
- Test with real-world JSON examples (API responses, config files)

## Performance Considerations

### Memory Management
- Stream large JSON files instead of loading entirely into memory
- Dispose of JsonDocument instances properly
- Implement size limits for processing

### API Optimization
- Implement connection pooling for Ollama requests
- Cache model availability checks
- Optimize prompt size to reduce processing time

### Scalability
- Design for potential future features (batch processing, web interface)
- Modular architecture allows for easy extension
- Configuration-driven behavior for flexibility