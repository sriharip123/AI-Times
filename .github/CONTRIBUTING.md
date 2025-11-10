# Contributing to JSON-Whisperer

Thank you for your interest in contributing! This document provides guidelines for contributing to the project.

## Development Workflow

### 1. Fork and Clone
```bash
git clone https://github.com/sriharip123/AI-Times.git
cd AI-Times
```

### 2. Create a Feature Branch
```bash
git checkout -b feature/your-feature-name
```

### 3. Make Your Changes
- Write clean, maintainable code
- Follow existing code style and conventions
- Add tests for new functionality
- Update documentation as needed

### 4. Run Tests Locally
Before pushing, ensure all tests pass:

```bash
# Restore .NET tools (Paket, coverage tools)
dotnet tool restore

# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run all tests
dotnet test --no-build --configuration Release --verbosity normal
```

All 362 tests should pass before submitting a PR.

### 5. Commit Your Changes
```bash
git add .
git commit -m "feat: add your feature description"
```

Use conventional commit messages:
- `feat:` - New feature
- `fix:` - Bug fix
- `docs:` - Documentation changes
- `test:` - Test additions or modifications
- `refactor:` - Code refactoring
- `chore:` - Maintenance tasks

### 6. Push and Create Pull Request
```bash
git push origin feature/your-feature-name
```

Then create a pull request on GitHub targeting the `main` branch.

## Pull Request Process

1. **Automated Checks**: When you create a PR, GitHub Actions will automatically:
   - Build the solution
   - Run all 362 tests
   - Publish detailed test results
   - Upload test artifacts

2. **Review**: Wait for code review from maintainers

3. **Address Feedback**: Make requested changes if needed

4. **Merge**: Once approved and all checks pass, your PR will be merged

## Continuous Integration

### Test Workflow
The test workflow runs automatically on:
- Every push to `main` branch
- Every pull request to `main` branch

**What it does:**
- ✅ Restores NuGet packages (with caching for faster builds)
- ✅ Builds the solution in Release mode
- ✅ Runs all 362 tests
- ✅ Publishes detailed test results with pass/fail status
- ✅ Uploads test artifacts (retained for 30 days)

### CI Pipeline
The comprehensive CI pipeline includes:
- Build and test job with code coverage
- Code quality checks (formatting validation)
- Test result reporting and artifact management

## Testing Guidelines

### Writing Tests
- Use NUnit framework
- Follow existing test patterns
- Test both success and failure scenarios
- Mock external dependencies (Ollama, ScyllaDB)

### Test Structure
```csharp
[TestFixture]
public class YourServiceTests
{
    [SetUp]
    public void Setup()
    {
        // Initialize test dependencies
    }

    [Test]
    public void MethodName_Scenario_ExpectedBehavior()
    {
        // Arrange
        // Act
        // Assert
    }

    [TearDown]
    public void TearDown()
    {
        // Cleanup
    }
}
```

### Running Specific Tests
```bash
# Run tests for a specific class
dotnet test --filter "FullyQualifiedName~OllamaServiceTests"

# Run a specific test
dotnet test --filter "Name=IsAvailableAsync_InvalidUrl_ReturnsFalse"
```

## Diagnostic Commands

The application includes diagnostic commands for testing and troubleshooting. These are useful for verifying your development environment:

```bash
# Verify all services are operational
dotnet run --project JSON-Whisperer -- --health-check

# Validate configuration settings
dotnet run --project JSON-Whisperer -- --validate-config

# Test Ollama service connectivity
dotnet run --project JSON-Whisperer -- --test-ollama

# Test ScyllaDB connectivity
dotnet run --project JSON-Whisperer -- --test-scylla

# Test embedding generation
dotnet run --project JSON-Whisperer -- --test-embeddings
```

**Exit Codes:**
- `0` = Success/Healthy
- `1` = Failure/Unhealthy

For more details on diagnostic commands, see the [README.md](../README.md#diagnostic-commands).

## Code Style

- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and concise

## Questions?

If you have questions or need help:
1. Check existing issues and discussions
2. Create a new issue with the `question` label
3. Reach out to maintainers

## License

By contributing, you agree that your contributions will be licensed under the same license as the project.
