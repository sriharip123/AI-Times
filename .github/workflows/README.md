# GitHub Actions Workflows

This directory contains GitHub Actions workflows for continuous integration and testing.

## Workflows

### 1. Test Workflow (`test.yml`)
A focused workflow that runs all tests on every push to main and on pull requests. This workflow provides fast feedback on test status.

**Triggers:**
- Push to `main` branch
- Pull requests targeting `main` branch

**Steps:**
1. Checkout code using `actions/checkout@v4`
2. Setup .NET 9.0.x using `actions/setup-dotnet@v4`
3. Restore .NET tools (Paket)
4. Cache NuGet packages using `actions/cache@v4` with paket.lock hash
5. Restore dependencies with `dotnet restore`
6. Build solution in Release configuration with `--no-restore`
7. Run all tests with `--no-build` and TRX logger
8. Publish test results using `dorny/test-reporter@v1`
9. Upload test artifacts with 30-day retention

**Performance Optimizations:**
- NuGet package caching reduces restore time
- `--no-restore` flag avoids redundant package restoration
- `--no-build` flag reuses build artifacts from previous step

### 2. CI Pipeline (`ci.yml`)
A comprehensive CI pipeline with build, test, code coverage, and code quality checks. This workflow provides complete validation of code changes.

**Triggers:**
- Push to `main` branch
- Pull requests targeting `main` branch

**Jobs:**

#### Build and Test
- Checks out code with full history (`fetch-depth: 0`)
- Restores dependencies with NuGet package caching
- Builds the solution in Release configuration
- Runs all tests with code coverage collection
- Publishes test results and coverage reports
- Uploads test results and coverage artifacts (30-day retention)

#### Code Quality
- Checks code formatting with `dotnet format --verify-no-changes`
- Runs with `continue-on-error: true` to not block merges on formatting issues

## Test Results

Test results are automatically published and can be viewed in the GitHub Actions UI. Failed tests will cause the workflow to fail, preventing merges of broken code.

The `dorny/test-reporter@v1` action provides:
- Visual test result summaries in the Actions UI
- Annotations on failed tests
- Test execution statistics

## Artifacts

The following artifacts are uploaded after each workflow run and retained for 30 days:

**test.yml artifacts:**
- **test-results**: Test execution results in TRX format (30-day retention)

**ci.yml artifacts:**
- **test-results**: Test execution results in TRX format (30-day retention)
- **code-coverage**: Code coverage reports in Cobertura XML format (30-day retention)

**Artifact Retention Policy:**
- All artifacts are retained for 30 days to balance storage costs with debugging needs
- Artifacts can be downloaded from the Actions tab for local analysis
- Older artifacts are automatically deleted after the retention period

## Status Badges

Status badges are already added to the README.md:

```markdown
![Tests](https://github.com/sriharip123/AI-Times/actions/workflows/test.yml/badge.svg)
![CI](https://github.com/sriharip123/AI-Times/actions/workflows/ci.yml/badge.svg)
```

## Local Testing

Before pushing, you can run the same checks locally to match the CI workflow:

```bash
# Restore .NET tools (Paket)
dotnet tool restore

# Restore dependencies
dotnet restore

# Build solution
dotnet build --no-restore --configuration Release

# Run tests (matches test.yml)
dotnet test --no-build --configuration Release --verbosity normal

# Run tests with coverage (matches ci.yml)
dotnet test --no-build --configuration Release --verbosity normal --collect:"XPlat Code Coverage"

# Check code formatting (matches ci.yml code quality job)
dotnet format --verify-no-changes --verbosity diagnostic
```

**Diagnostic Commands:**

The project includes diagnostic commands for validating configuration and service health:

```bash
# Health check (validates Ollama and ScyllaDB connectivity)
dotnet run --project JSON-Whisperer -- --health-check

# Configuration validation
dotnet run --project JSON-Whisperer -- --validate-config

# View all available commands
dotnet run --project JSON-Whisperer -- --help
```

For comprehensive diagnostic testing, use the automated test scripts:

```bash
# Windows
.\scripts\test-diagnostics.ps1

# Linux/macOS
./scripts/test-diagnostics.sh
```

See [TROUBLESHOOTING.md](../../TROUBLESHOOTING.md) and [MANUAL_TESTING_GUIDE.md](../../MANUAL_TESTING_GUIDE.md) for more diagnostic commands and testing procedures.

## Requirements

- .NET 9.0 SDK
- All tests must pass before merging
- Code should follow formatting standards

## Troubleshooting

### Tests failing in CI but passing locally
- **Verify .NET version**: Ensure you're using .NET 9.0.x locally (check with `dotnet --version`)
- **Check dependencies**: Run `dotnet tool restore` and `dotnet restore` to ensure all dependencies are current
- **Match build configuration**: Use `--configuration Release` to match CI settings
- **Review test logs**: Check the Actions tab for detailed test output and error messages
- **Environment differences**: CI runs on Ubuntu; check for platform-specific issues
- **Service dependencies**: Ensure tests don't require external services (Ollama, ScyllaDB) unless properly mocked

### Workflow not triggering
- **Verify file location**: Workflow files must be in `.github/workflows/` directory
- **Check branch configuration**: Ensure your branch name matches the trigger configuration (main)
- **Confirm Actions enabled**: Verify GitHub Actions is enabled in repository Settings > Actions
- **YAML syntax**: Validate YAML syntax using a linter or GitHub's workflow editor
- **Push vs PR**: Remember that workflows trigger on push to main and PRs targeting main

### Build or restore failures
- **Clear local cache**: Delete `~/.nuget/packages` or `%USERPROFILE%\.nuget\packages` and retry
- **Paket issues**: Run `dotnet tool restore` to ensure Paket is installed
- **Lock file mismatch**: Ensure `paket.lock` is committed and up to date
- **Action version issues**: Verify all GitHub Actions versions (v4) are available and not deprecated

### Artifact upload failures
- **Check artifact paths**: Ensure test results are generated in expected locations (`**/test-results.trx`)
- **Retention limits**: Artifacts are retained for 30 days; older artifacts are automatically deleted
- **Storage quota**: Check repository storage quota hasn't been exceeded

### Code formatting failures
- **Run locally first**: Execute `dotnet format --verify-no-changes` before pushing
- **Auto-fix formatting**: Run `dotnet format` (without --verify-no-changes) to fix issues
- **Note**: Code quality job uses `continue-on-error: true`, so formatting issues won't block merges

### Cache not working
- **Verify cache key**: Cache key is based on `paket.lock` hash; changes to this file invalidate cache
- **Check cache size**: Very large caches may not be saved
- **Cache scope**: Caches are scoped to branches; new branches won't have cache initially

### Need more help?
- Review [TROUBLESHOOTING.md](../../TROUBLESHOOTING.md) for application-specific issues
- Check [MANUAL_TESTING_GUIDE.md](../../MANUAL_TESTING_GUIDE.md) for diagnostic procedures
- Examine workflow run logs in the Actions tab for detailed error messages

## Customization

To modify the workflows:
1. Edit the YAML files in this directory
2. Test changes on a feature branch first
3. Monitor the Actions tab for results
