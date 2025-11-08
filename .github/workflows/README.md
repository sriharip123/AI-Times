# GitHub Actions Workflows

This directory contains GitHub Actions workflows for continuous integration and testing.

## Workflows

### 1. Test Workflow (`test.yml`)
A simple workflow that runs all tests on every push to main and on pull requests.

**Triggers:**
- Push to `main` branch
- Pull requests targeting `main` branch

**Steps:**
1. Checkout code
2. Setup .NET 9.0
3. Restore .NET tools (Paket, coverage tools)
4. Restore dependencies
5. Build solution
6. Run all tests
7. Publish test results
8. Upload test artifacts

### 2. CI Pipeline (`ci.yml`)
A comprehensive CI pipeline with build, test, and code quality checks.

**Triggers:**
- Push to `main` branch
- Pull requests targeting `main` branch

**Jobs:**

#### Build and Test
- Restores dependencies with caching
- Builds the solution in Release configuration
- Runs all tests with code coverage
- Publishes test results and coverage reports

#### Code Quality
- Checks code formatting
- Runs security scans (optional)

## Test Results

Test results are automatically published and can be viewed in the GitHub Actions UI. Failed tests will cause the workflow to fail, preventing merges of broken code.

## Artifacts

The following artifacts are uploaded after each run:
- **test-results**: Test execution results in TRX format
- **code-coverage**: Code coverage reports in Cobertura XML format

## Status Badges

Status badges are already added to the README.md:

```markdown
![Tests](https://github.com/sriharip123/AI-Times/actions/workflows/test.yml/badge.svg)
![CI](https://github.com/sriharip123/AI-Times/actions/workflows/ci.yml/badge.svg)
```

## Local Testing

Before pushing, you can run the same checks locally:

```bash
# Restore .NET tools (Paket, etc.)
dotnet tool restore

# Restore and build
dotnet restore
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release

# Check formatting
dotnet format --verify-no-changes
```

## Requirements

- .NET 9.0 SDK
- All tests must pass before merging
- Code should follow formatting standards

## Troubleshooting

### Tests failing in CI but passing locally
- Ensure you're using the same .NET version (9.0)
- Check for environment-specific dependencies
- Review test logs in the Actions tab

### Workflow not triggering
- Verify the workflow file is in `.github/workflows/`
- Check that the branch name matches the trigger configuration
- Ensure GitHub Actions is enabled for the repository

## Customization

To modify the workflows:
1. Edit the YAML files in this directory
2. Test changes on a feature branch first
3. Monitor the Actions tab for results
