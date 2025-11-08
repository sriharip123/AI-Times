# Requirements Document

## Introduction

The JSON-Whisperer system is a C# .NET 9 console application that analyzes JSON objects and generates human-readable summaries in plain English using the Ollama Mistral language model. The system provides users with an intuitive way to understand complex JSON data structures through natural language descriptions, enhanced with vector similarity matching to provide contextual examples from a knowledge base stored in Cassandra database.

## Glossary

- **JSON-Whisperer**: The main console application system that processes JSON input and generates summaries
- **Ollama**: The local AI inference engine that hosts and runs language models
- **Mistral Model**: The specific language model used for generating natural language summaries
- **JSON Object**: A structured data format consisting of key-value pairs, arrays, and nested objects
- **Summary**: A plain English description that explains the structure, content, and purpose of a JSON object
- **Analysis Engine**: The component responsible for parsing and understanding JSON structure
- **AI Service**: The component that interfaces with Ollama to generate summaries
- **Vector Database**: ScyllaDB database that stores JSON embeddings and their approved descriptions
- **Embedding Service**: The component that generates vector embeddings using Ollama's embedding API
- **Similarity Service**: The component that performs cosine similarity matching against stored embeddings
- **Knowledge Base**: Collection of JSON examples with approved descriptions stored in AppData directory
- **Context Enhancement**: Process of finding similar JSON patterns to improve summary generation

## Requirements

### Requirement 1

**User Story:** As a developer, I want to input a JSON object to the application, so that I can receive a plain English summary of its structure and content.

#### Acceptance Criteria

1. WHEN a user provides a JSON string as input, THE JSON-Whisperer SHALL parse and validate the JSON structure
2. THE JSON-Whisperer SHALL accept JSON input through command line arguments or file paths
3. IF the JSON input is malformed, THEN THE JSON-Whisperer SHALL display a clear error message with details about the parsing failure
4. THE JSON-Whisperer SHALL support nested JSON objects of arbitrary depth
5. THE JSON-Whisperer SHALL handle JSON arrays containing mixed data types

### Requirement 2

**User Story:** As a user, I want the application to connect to Ollama with the Mistral model, so that I can generate AI-powered summaries of my JSON data.

#### Acceptance Criteria

1. THE JSON-Whisperer SHALL establish a connection to the local Ollama service
2. THE JSON-Whisperer SHALL verify that the Mistral model is available before processing
3. IF Ollama is not running or accessible, THEN THE JSON-Whisperer SHALL display an informative error message
4. IF the Mistral model is not installed, THEN THE JSON-Whisperer SHALL provide instructions for model installation
5. THE JSON-Whisperer SHALL handle API timeouts and connection failures gracefully

### Requirement 3

**User Story:** As a user, I want to receive a comprehensive plain English summary of my JSON data, so that I can quickly understand its structure and purpose without manually parsing it.

#### Acceptance Criteria

1. THE JSON-Whisperer SHALL generate a summary that describes the overall structure of the JSON object
2. THE JSON-Whisperer SHALL identify and describe key data fields and their types
3. THE JSON-Whisperer SHALL explain relationships between nested objects and arrays
4. THE JSON-Whisperer SHALL provide insights about the likely purpose or domain of the data
5. THE JSON-Whisperer SHALL present the summary in clear, non-technical language suitable for business users

### Requirement 4

**User Story:** As a user, I want the application to handle various input methods, so that I can use it flexibly with different JSON sources.

#### Acceptance Criteria

1. THE JSON-Whisperer SHALL accept JSON input as a command line argument
2. THE JSON-Whisperer SHALL read JSON from a specified file path
3. THE JSON-Whisperer SHALL read JSON from standard input when no arguments are provided
4. THE JSON-Whisperer SHALL support both single-line and pretty-formatted JSON input
5. THE JSON-Whisperer SHALL validate file existence and readability before processing

### Requirement 5

**User Story:** As a user, I want clear and helpful output formatting, so that I can easily read and understand the generated summaries.

#### Acceptance Criteria

1. THE JSON-Whisperer SHALL display the original JSON structure in a readable format
2. THE JSON-Whisperer SHALL clearly separate the input JSON from the generated summary
3. THE JSON-Whisperer SHALL format the summary with appropriate line breaks and sections
4. THE JSON-Whisperer SHALL include metadata such as processing time and JSON size statistics
5. WHERE verbose mode is enabled, THE JSON-Whisperer SHALL display additional analysis details

### Requirement 6

**User Story:** As a system administrator, I want to initialize a vector database with JSON examples, so that the system can provide contextual similarity matching for better summaries.

#### Acceptance Criteria

1. THE JSON-Whisperer SHALL connect to a ScyllaDB database for vector storage
2. THE JSON-Whisperer SHALL read JSON examples from the AppData directory during initialization
3. THE JSON-Whisperer SHALL generate vector embeddings for each JSON example using Ollama's embedding API
4. THE JSON-Whisperer SHALL store embeddings along with their approved descriptions in ScyllaDB
5. THE JSON-Whisperer SHALL handle database connection failures gracefully with informative error messages

### Requirement 7

**User Story:** As a user, I want the system to find similar JSON patterns from the knowledge base, so that I receive more accurate and contextual summaries.

#### Acceptance Criteria

1. THE JSON-Whisperer SHALL generate vector embeddings for input JSON using Ollama's embedding API
2. THE JSON-Whisperer SHALL perform cosine similarity search against stored embeddings in ScyllaDB
3. THE JSON-Whisperer SHALL retrieve the most similar JSON examples with similarity scores above a configurable threshold
4. THE JSON-Whisperer SHALL include similar examples' descriptions as context in the AI prompt
5. WHERE verbose mode is enabled, THE JSON-Whisperer SHALL display similarity scores and matched examples

### Requirement 8

**User Story:** As a developer, I want configurable similarity matching parameters, so that I can tune the system's performance and accuracy.

#### Acceptance Criteria

1. THE JSON-Whisperer SHALL support configurable similarity threshold for matching
2. THE JSON-Whisperer SHALL support configurable maximum number of similar examples to retrieve
3. THE JSON-Whisperer SHALL support configurable embedding model selection
4. THE JSON-Whisperer SHALL validate configuration parameters at startup
5. THE JSON-Whisperer SHALL provide default values for all similarity matching parameters