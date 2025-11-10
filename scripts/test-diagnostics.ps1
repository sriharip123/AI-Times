# JSON-Whisperer Diagnostic Commands Test Script
# This script automates testing of all diagnostic commands

param(
    [switch]$Verbose,
    [switch]$SkipServiceChecks,
    [string]$TestGroup = "all"
)

$ErrorActionPreference = "Continue"
$script:TestsPassed = 0
$script:TestsFailed = 0
$script:TestsSkipped = 0

# Colors for output
function Write-Success { param($Message) Write-Host "✓ $Message" -ForegroundColor Green }
function Write-Failure { param($Message) Write-Host "✗ $Message" -ForegroundColor Red }
function Write-Warning { param($Message) Write-Host "⚠ $Message" -ForegroundColor Yellow }
function Write-Info { param($Message) Write-Host "ℹ $Message" -ForegroundColor Cyan }
function Write-TestHeader { param($Message) Write-Host "`n=== $Message ===" -ForegroundColor Magenta }

# Test result tracking
function Record-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Message = ""
    )
    
    if ($Passed) {
        $script:TestsPassed++
        Write-Success "$TestName - PASSED"
    } else {
        $script:TestsFailed++
        Write-Failure "$TestName - FAILED: $Message"
    }
}

function Skip-Test {
    param([string]$TestName, [string]$Reason)
    $script:TestsSkipped++
    Write-Warning "$TestName - SKIPPED: $Reason"
}

# Check if a service is running
function Test-ServiceAvailable {
    param(
        [string]$ServiceName,
        [string]$TestCommand
    )
    
    try {
        $result = Invoke-Expression $TestCommand 2>&1
        return $true
    } catch {
        return $false
    }
}

# Run a diagnostic command and check exit code
function Test-DiagnosticCommand {
    param(
        [string]$TestName,
        [string]$Command,
        [int]$ExpectedExitCode = 0,
        [string[]]$ExpectedOutput = @(),
        [string[]]$UnexpectedOutput = @()
    )
    
    Write-Info "Running: $Command"
    
    try {
        $output = Invoke-Expression $Command 2>&1 | Out-String
        $exitCode = $LASTEXITCODE
        
        if ($Verbose) {
            Write-Host "Output:" -ForegroundColor Gray
            Write-Host $output -ForegroundColor Gray
            Write-Host "Exit Code: $exitCode" -ForegroundColor Gray
        }
        
        # Check exit code
        if ($exitCode -ne $ExpectedExitCode) {
            Record-TestResult -TestName $TestName -Passed $false -Message "Expected exit code $ExpectedExitCode, got $exitCode"
            return
        }
        
        # Check expected output
        $allExpectedFound = $true
        foreach ($expected in $ExpectedOutput) {
            if ($output -notmatch [regex]::Escape($expected)) {
                Write-Failure "Expected output not found: $expected"
                $allExpectedFound = $false
            }
        }
        
        # Check unexpected output
        $noUnexpectedFound = $true
        foreach ($unexpected in $UnexpectedOutput) {
            if ($output -match [regex]::Escape($unexpected)) {
                Write-Failure "Unexpected output found: $unexpected"
                $noUnexpectedFound = $false
            }
        }
        
        if ($allExpectedFound -and $noUnexpectedFound) {
            Record-TestResult -TestName $TestName -Passed $true
        } else {
            Record-TestResult -TestName $TestName -Passed $false -Message "Output validation failed"
        }
        
    } catch {
        Record-TestResult -TestName $TestName -Passed $false -Message $_.Exception.Message
    }
}

# Main test execution
Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  JSON-Whisperer Diagnostic Commands Test Suite            ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# Check prerequisites
Write-TestHeader "Checking Prerequisites"

$dotnetAvailable = Test-ServiceAvailable -ServiceName ".NET" -TestCommand "dotnet --version"
if (-not $dotnetAvailable) {
    Write-Failure ".NET SDK not found. Please install .NET 8.0 SDK."
    exit 1
}
Write-Success ".NET SDK is available"

# Check if project exists
if (-not (Test-Path "JSON-Whisperer/JSON-Whisperer.csproj")) {
    Write-Failure "JSON-Whisperer project not found. Please run from repository root."
    exit 1
}
Write-Success "JSON-Whisperer project found"

# Check services (unless skipped)
$ollamaAvailable = $false
$scyllaAvailable = $false

if (-not $SkipServiceChecks) {
    Write-Info "Checking service availability..."
    
    # Check Ollama
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -Method GET -TimeoutSec 2 -ErrorAction SilentlyContinue
        $ollamaAvailable = $true
        Write-Success "Ollama is running"
    } catch {
        Write-Warning "Ollama is not running - some tests will be skipped"
    }
    
    # Check ScyllaDB (try to connect on default port)
    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $tcpClient.Connect("localhost", 9042)
        $tcpClient.Close()
        $scyllaAvailable = $true
        Write-Success "ScyllaDB is running"
    } catch {
        Write-Warning "ScyllaDB is not running - some tests will be skipped"
    }
} else {
    Write-Warning "Service checks skipped - some tests may fail"
}

# Test Group 1: Help and Basic Functionality
if ($TestGroup -eq "all" -or $TestGroup -eq "help") {
    Write-TestHeader "Test Group 1: Help and Basic Functionality"
    
    Test-DiagnosticCommand `
        -TestName "1.1 Help Display" `
        -Command "dotnet run --project JSON-Whisperer -- --help" `
        -ExpectedExitCode 0 `
        -ExpectedOutput @("--help", "--health-check", "--verbose", "Usage")
    
    Test-DiagnosticCommand `
        -TestName "1.2 Short Help Flag" `
        -Command "dotnet run --project JSON-Whisperer -- -h" `
        -ExpectedExitCode 0 `
        -ExpectedOutput @("--help", "--health-check")
    
    Test-DiagnosticCommand `
        -TestName "1.3 Unknown Flag Error" `
        -Command "dotnet run --project JSON-Whisperer -- --unknown-flag" `
        -ExpectedExitCode 5 `
        -ExpectedOutput @("ERROR", "Unknown flag")
}

# Test Group 2: Health Check Command
if ($TestGroup -eq "all" -or $TestGroup -eq "health") {
    Write-TestHeader "Test Group 2: Health Check Command"
    
    if ($ollamaAvailable -and $scyllaAvailable) {
        Test-DiagnosticCommand `
            -TestName "2.1 Health Check with All Services Running" `
            -Command "dotnet run --project JSON-Whisperer -- --health-check" `
            -ExpectedExitCode 0 `
            -ExpectedOutput @("Health Check", "Ollama", "ScyllaDB", "Embedding")
        
        Test-DiagnosticCommand `
            -TestName "2.2 Health Check with Verbose Mode" `
            -Command "dotnet run --project JSON-Whisperer -- --health-check --verbose" `
            -ExpectedExitCode 0 `
            -ExpectedOutput @("Health Check", "URL:", "Model:")
    } else {
        Skip-Test -TestName "2.1 Health Check with All Services" -Reason "Services not running"
        Skip-Test -TestName "2.2 Health Check with Verbose" -Reason "Services not running"
    }
}

# Test Group 3: Configuration Validation
if ($TestGroup -eq "all" -or $TestGroup -eq "config") {
    Write-TestHeader "Test Group 3: Configuration Validation"
    
    Test-DiagnosticCommand `
        -TestName "3.1 Validate Valid Configuration" `
        -Command "dotnet run --project JSON-Whisperer -- --validate-config" `
        -ExpectedExitCode 0 `
        -ExpectedOutput @("Configuration Validation", "Ollama", "Application")
    
    Test-DiagnosticCommand `
        -TestName "3.2 Validate Configuration with Verbose" `
        -Command "dotnet run --project JSON-Whisperer -- --validate-config --verbose" `
        -ExpectedExitCode 0 `
        -ExpectedOutput @("Configuration Validation", "BaseUrl", "ModelName")
}

# Test Group 4: Individual Service Testing
if ($TestGroup -eq "all" -or $TestGroup -eq "services") {
    Write-TestHeader "Test Group 4: Individual Service Testing"
    
    if ($ollamaAvailable) {
        Test-DiagnosticCommand `
            -TestName "4.1 Test Ollama Service" `
            -Command "dotnet run --project JSON-Whisperer -- --test-ollama" `
            -ExpectedExitCode 0 `
            -ExpectedOutput @("Testing Ollama", "available")
        
        Test-DiagnosticCommand `
            -TestName "4.5 Test Embedding Service" `
            -Command "dotnet run --project JSON-Whisperer -- --test-embedding" `
            -ExpectedExitCode 0 `
            -ExpectedOutput @("Testing Embedding", "available", "dimensions")
        
        Test-DiagnosticCommand `
            -TestName "4.6 Test Embedding with Verbose" `
            -Command "dotnet run --project JSON-Whisperer -- --test-embedding --verbose" `
            -ExpectedExitCode 0 `
            -ExpectedOutput @("Testing Embedding", "First 5 values")
    } else {
        Skip-Test -TestName "4.1 Test Ollama" -Reason "Ollama not running"
        Skip-Test -TestName "4.5 Test Embedding" -Reason "Ollama not running"
        Skip-Test -TestName "4.6 Test Embedding Verbose" -Reason "Ollama not running"
    }
    
    if ($scyllaAvailable) {
        Test-DiagnosticCommand `
            -TestName "4.3 Test ScyllaDB" `
            -Command "dotnet run --project JSON-Whisperer -- --test-scylla" `
            -ExpectedExitCode 0 `
            -ExpectedOutput @("Testing ScyllaDB", "connected")
    } else {
        Skip-Test -TestName "4.3 Test ScyllaDB" -Reason "ScyllaDB not running"
    }
    
    if ($ollamaAvailable -and $scyllaAvailable) {
        Test-DiagnosticCommand `
            -TestName "4.7 Test Similarity Search" `
            -Command "dotnet run --project JSON-Whisperer -- --test-similarity" `
            -ExpectedExitCode 0 `
            -ExpectedOutput @("Testing Similarity")
    } else {
        Skip-Test -TestName "4.7 Test Similarity" -Reason "Services not running"
    }
}

# Test Group 5: Knowledge Base Management
if ($TestGroup -eq "all" -or $TestGroup -eq "knowledge") {
    Write-TestHeader "Test Group 5: Knowledge Base Management"
    
    if ($ollamaAvailable -and $scyllaAvailable) {
        Test-DiagnosticCommand `
            -TestName "5.2 Validate Knowledge Base" `
            -Command "dotnet run --project JSON-Whisperer -- --validate-knowledge-base" `
            -ExpectedExitCode 0 `
            -ExpectedOutput @("Validating Knowledge Base", "examples")
        
        Test-DiagnosticCommand `
            -TestName "5.3 Validate Knowledge Base with Verbose" `
            -Command "dotnet run --project JSON-Whisperer -- --validate-knowledge-base --verbose" `
            -ExpectedExitCode 0 `
            -ExpectedOutput @("Validating Knowledge Base", "Examples:")
    } else {
        Skip-Test -TestName "5.2 Validate Knowledge Base" -Reason "Services not running"
        Skip-Test -TestName "5.3 Validate KB Verbose" -Reason "Services not running"
    }
}

# Test Group 6: Benchmarking
if ($TestGroup -eq "all" -or $TestGroup -eq "benchmark") {
    Write-TestHeader "Test Group 6: Benchmarking"
    
    if ($ollamaAvailable) {
        Test-DiagnosticCommand `
            -TestName "6.4 Benchmark Embedding Generation" `
            -Command "dotnet run --project JSON-Whisperer -- --benchmark-embedding" `
            -ExpectedExitCode 0 `
            -ExpectedOutput @("Benchmark: Embedding", "Iterations", "Average", "Throughput")
    } else {
        Skip-Test -TestName "6.4 Benchmark Embedding" -Reason "Ollama not running"
    }
    
    if ($scyllaAvailable) {
        Test-DiagnosticCommand `
            -TestName "6.3 Benchmark Vector Operations" `
            -Command "dotnet run --project JSON-Whisperer -- --benchmark-vector-operations" `
            -ExpectedExitCode 0 `
            -ExpectedOutput @("Benchmark: Vector Operations", "Iterations", "Throughput")
    } else {
        Skip-Test -TestName "6.3 Benchmark Vector" -Reason "ScyllaDB not running"
    }
    
    if ($ollamaAvailable -and $scyllaAvailable) {
        Test-DiagnosticCommand `
            -TestName "6.1 Benchmark Similarity Search" `
            -Command "dotnet run --project JSON-Whisperer -- --benchmark-similarity" `
            -ExpectedExitCode 0 `
            -ExpectedOutput @("Benchmark: Similarity", "Iterations", "Throughput")
    } else {
        Skip-Test -TestName "6.1 Benchmark Similarity" -Reason "Services not running"
    }
}

# Test Group 7: Flag Combinations
if ($TestGroup -eq "all" -or $TestGroup -eq "flags") {
    Write-TestHeader "Test Group 7: Flag Combinations and Overrides"
    
    Test-DiagnosticCommand `
        -TestName "7.1 Verbose Mode Override" `
        -Command "dotnet run --project JSON-Whisperer -- --validate-config --verbose" `
        -ExpectedExitCode 0 `
        -ExpectedOutput @("BaseUrl", "ModelName")
    
    Test-DiagnosticCommand `
        -TestName "7.2 Short Verbose Flag" `
        -Command "dotnet run --project JSON-Whisperer -- --validate-config -v" `
        -ExpectedExitCode 0 `
        -ExpectedOutput @("BaseUrl", "ModelName")
    
    Test-DiagnosticCommand `
        -TestName "7.4 Conflicting Flags" `
        -Command "dotnet run --project JSON-Whisperer -- --test-scylla --no-similarity" `
        -ExpectedExitCode 5 `
        -ExpectedOutput @("ERROR", "conflicts")
}

# Test Group 8: Error Handling
if ($TestGroup -eq "all" -or $TestGroup -eq "errors") {
    Write-TestHeader "Test Group 8: Error Messages and Exit Codes"
    
    Test-DiagnosticCommand `
        -TestName "8.1 File Not Found Error" `
        -Command "dotnet run --project JSON-Whisperer -- --file nonexistent.json" `
        -ExpectedExitCode 5 `
        -ExpectedOutput @("ERROR", "File not found")
    
    Test-DiagnosticCommand `
        -TestName "8.2 Missing File Argument" `
        -Command "dotnet run --project JSON-Whisperer -- --file" `
        -ExpectedExitCode 5 `
        -ExpectedOutput @("ERROR", "requires")
}

# Test Summary
Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  Test Summary                                              ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

$totalTests = $script:TestsPassed + $script:TestsFailed + $script:TestsSkipped

Write-Host "Total Tests:   $totalTests" -ForegroundColor White
Write-Success "Passed:        $script:TestsPassed"
Write-Failure "Failed:        $script:TestsFailed"
Write-Warning "Skipped:       $script:TestsSkipped"

$passRate = if ($totalTests -gt 0) { [math]::Round(($script:TestsPassed / $totalTests) * 100, 2) } else { 0 }
Write-Host "`nPass Rate:     $passRate%" -ForegroundColor $(if ($passRate -ge 90) { "Green" } elseif ($passRate -ge 70) { "Yellow" } else { "Red" })

# Exit with appropriate code
if ($script:TestsFailed -gt 0) {
    Write-Host "`n❌ Some tests failed. Please review the output above." -ForegroundColor Red
    exit 1
} else {
    Write-Host "`n✅ All tests passed!" -ForegroundColor Green
    exit 0
}
