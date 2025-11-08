# Contributing to JSON-Whisperer

Thank you for your interest in contributing! This document provides guidelines for contributing to the project.

## Development Workflow

### 1. Fork and Clone
```bash
git clone https://github.com/YOUR_USERNAME/JSON-Whisperer.git
cd JSON-Whisperer
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
# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run all tests
dotnet test --configuration Release --verbosity normal
```

All 129 tests should pass before submitting a PR.

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
   - Run all 129 tests
   - Report test results

2. **Review**: Wait for code review from maintainers

3. **Address Feedback**: Make requested changes if needed

4. **Merge**: Once approved and all checks pass, your PR will be merged

## Continuous Integration

### Test Workflow
The test workflow runs automatically on:
- Every push to `main` branch
- Every pull request to `main` branch

**What it does:**
- ✅ Restores NuGet packages (with caching)
- ✅ Builds the solution in Release mode
- ✅ Runs all 129 tests
- ✅ Publishes test results
- ✅ Uploads test artifacts

### CI Pipeline
The comprehensive CI pipeline includes:
- Build and test job
- Code quality checks
- Security scanning (optional)

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
