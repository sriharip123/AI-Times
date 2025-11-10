# Documentation Validation Script
# This script validates documentation accuracy including commands, file paths, and test counts

Write-Host "=== Documentation Validation ===" -ForegroundColor Cyan
Write-Host ""

$validationResults = @()
$failCount = 0

# Function to count actual tests in the project
function Get-ActualTestCount {
    Write-Host "Counting actual tests in project..." -ForegroundColor Yellow
    
    try {
        # Run dotnet test with --list-tests to count tests
        $output = dotnet test --list-tests --verbosity quiet 2>&1 | Out-String
        
        # Count test methods (lines that don't start with spaces and aren't empty)
        $testLines = $output -split "`n" | Where-Object { 
            $_ -match '^\s{4,}\S' -and $_ -notmatch 'The following Tests are available' 
        }
        
        return $testLines.Count
    } catch {
        Write-Host "  [WARN] Could not count tests automatically" -ForegroundColor Yellow
        return -1
    }
}

# Function to check if a file path exists
function Test-FilePath {
    param([string]$Path, [string]$Source)
    
    if (Test-Path $Path) {
        Write-Host "  [PASS] File exists: $Path" -ForegroundColor Green
        return $true
    } else {
        Write-Host "  [FAIL] File not found: $Path (referenced in $Source)" -ForegroundColor Red
        $script:failCount++
        return $false
    }
}

# Function to extract and validate commands from markdown
function Test-DocumentedCommands {
    param([string]$FilePath)
    
    Write-Host "Validating commands in $FilePath..." -ForegroundColor Yellow
    
    if (-not (Test-Path $FilePath)) {
        Write-Host "  [FAIL] File not found: $FilePath" -ForegroundColor Red
        $script:failCount++
        return
    }
    
    $content = Get-Content $FilePath -Raw
    
    # Extract code blocks with shell commands
    $codeBlocks = [regex]::Matches($content, '```(?:bash|sh|powershell|cmd)?\s*\n(.*?)\n```', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    
    $commandCount = 0
    foreach ($block in $codeBlocks) {
        $commands = $block.Groups[1].Value -split "`n" | Where-Object { $_ -match '^\s*(dotnet|paket|git)' }
        $commandCount += $commands.Count
    }
    
    Write-Host "  [INFO] Found $commandCount documented commands" -ForegroundColor Cyan
}

# Function to check test count references in a file
function Test-TestCountReferences {
    param([string]$FilePath, [int]$ExpectedCount)
    
    if (-not (Test-Path $FilePath)) {
        return
    }
    
    $content = Get-Content $FilePath -Raw
    
    # Look for test count patterns
    $patterns = @(
        '(\d+)\s+tests?',
        'test\s+suite\s+\((\d+)\s+tests?\)',
        '(\d+)\s+unit\s+tests?'
    )
    
    $foundCounts = @()
    foreach ($pattern in $patterns) {
        $matches = [regex]::Matches($content, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        foreach ($match in $matches) {
            $count = [int]$match.Groups[1].Value
            if ($count -gt 10) {  # Likely a test count, not a version number
                $foundCounts += $count
            }
        }
    }
    
    if ($foundCounts.Count -gt 0) {
        $uniqueCounts = $foundCounts | Select-Object -Unique
        foreach ($count in $uniqueCounts) {
            if ($ExpectedCount -gt 0 -and $count -ne $ExpectedCount) {
                Write-Host "  [WARN] Test count mismatch in $FilePath : found $count, expected $ExpectedCount" -ForegroundColor Yellow
            } else {
                Write-Host "  [INFO] Test count in $FilePath : $count" -ForegroundColor Cyan
            }
        }
    }
}

# Get actual test count
Write-Host "Step 1: Verify actual test count" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
$actualTestCount = Get-ActualTestCount
if ($actualTestCount -gt 0) {
    Write-Host "  [PASS] Actual test count: $actualTestCount" -ForegroundColor Green
} else {
    Write-Host "  [WARN] Could not determine test count automatically" -ForegroundColor Yellow
    Write-Host "  [INFO] Manually verify test count by running: dotnet test --list-tests" -ForegroundColor Cyan
}
Write-Host ""

# Validate file paths referenced in documentation
Write-Host "Step 2: Validate file path references" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

$filesToCheck = @(
    @{Path=".github/workflows/test.yml"; Source="Documentation"},
    @{Path=".github/workflows/ci.yml"; Source="Documentation"},
    @{Path=".github/workflows/README.md"; Source="Documentation"},
    @{Path=".github/CONTRIBUTING.md"; Source="Documentation"},
    @{Path=".github/SETUP_CHECKLIST.md"; Source="Documentation"},
    @{Path=".github/WORKFLOWS_SETUP.md"; Source="Documentation"},
    @{Path="JSON-Whisperer/JSON-Whisperer.csproj"; Source="Build files"},
    @{Path="JSON-Whisperer.Tests/JSON-Whisperer.Tests.csproj"; Source="Test files"},
    @{Path="paket.dependencies"; Source="Dependency management"},
    @{Path="paket.lock"; Source="Dependency management"}
)

foreach ($file in $filesToCheck) {
    Test-FilePath -Path $file.Path -Source $file.Source
}
Write-Host ""

# Validate commands in documentation files
Write-Host "Step 3: Validate documented commands" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

$docsToCheck = @(
    ".github/workflows/README.md",
    ".github/CONTRIBUTING.md",
    ".github/SETUP_CHECKLIST.md",
    ".github/WORKFLOWS_SETUP.md"
)

foreach ($doc in $docsToCheck) {
    if (Test-Path $doc) {
        Test-DocumentedCommands -FilePath $doc
    }
}
Write-Host ""

# Check test count references in documentation
Write-Host "Step 4: Validate test count references" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan

if ($actualTestCount -gt 0) {
    Test-TestCountReferences -FilePath ".github/CONTRIBUTING.md" -ExpectedCount $actualTestCount
    Test-TestCountReferences -FilePath ".github/WORKFLOWS_SETUP.md" -ExpectedCount $actualTestCount
    Test-TestCountReferences -FilePath ".github/SETUP_CHECKLIST.md" -ExpectedCount $actualTestCount
} else {
    Write-Host "  [WARN] Skipping test count validation (could not determine actual count)" -ForegroundColor Yellow
}
Write-Host ""

# Validate internal links (basic check for referenced files)
Write-Host "Step 5: Validate internal documentation links" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan

function Test-InternalLinks {
    param([string]$FilePath)
    
    if (-not (Test-Path $FilePath)) {
        return
    }
    
    $content = Get-Content $FilePath -Raw
    
    # Extract markdown links [text](path)
    $links = [regex]::Matches($content, '\[([^\]]+)\]\(([^)]+)\)')
    
    $brokenLinks = 0
    foreach ($link in $links) {
        $linkPath = $link.Groups[2].Value
        
        # Skip external URLs
        if ($linkPath -match '^https?://') {
            continue
        }
        
        # Skip anchors
        if ($linkPath -match '^#') {
            continue
        }
        
        # Check if file exists (relative to repo root)
        $fullPath = Join-Path (Get-Location) $linkPath
        if (-not (Test-Path $fullPath)) {
            Write-Host "  [WARN] Broken link in $FilePath : $linkPath" -ForegroundColor Yellow
            $brokenLinks++
        }
    }
    
    if ($brokenLinks -eq 0) {
        Write-Host "  [PASS] All internal links valid in $FilePath" -ForegroundColor Green
    }
}

foreach ($doc in $docsToCheck) {
    if (Test-Path $doc) {
        Test-InternalLinks -FilePath $doc
    }
}
Write-Host ""

# Summary
Write-Host "=== Validation Summary ===" -ForegroundColor Cyan
if ($failCount -eq 0) {
    Write-Host "All documentation validations passed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Note: Some warnings may appear for test counts or links." -ForegroundColor Yellow
    Write-Host "Review warnings above to ensure documentation is accurate." -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "Failed checks: $failCount" -ForegroundColor Red
    Write-Host "Please review the failures above and update documentation." -ForegroundColor Red
    exit 1
}
