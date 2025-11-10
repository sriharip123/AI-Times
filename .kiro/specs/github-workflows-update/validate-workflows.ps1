# Workflow Validation Script
# This script validates GitHub workflow files for syntax and configuration

Write-Host "=== GitHub Workflows Validation ===" -ForegroundColor Cyan
Write-Host ""

$validationResults = @()

# Function to validate YAML basic syntax
function Test-YamlBasicSyntax {
    param([string]$FilePath)
    
    $content = Get-Content $FilePath -Raw
    $issues = @()
    
    # Check for basic YAML syntax issues
    if ($content -match '\t') {
        $issues += "Contains tabs (YAML requires spaces)"
    }
    
    # Check for consistent indentation (should be 2 spaces)
    $lines = Get-Content $FilePath
    foreach ($line in $lines) {
        if ($line -match '^( +)') {
            $spaces = $matches[1].Length
            if ($spaces % 2 -ne 0) {
                $issues += "Inconsistent indentation detected (not multiple of 2)"
                break
            }
        }
    }
    
    return $issues
}

# Function to extract and validate action versions
function Get-ActionVersions {
    param([string]$FilePath)
    
    $content = Get-Content $FilePath -Raw
    $actions = @()
    
    # Extract all uses: statements
    if ($content -match '(?m)^\s+uses:\s+(.+)$') {
        $matches = [regex]::Matches($content, '(?m)^\s+uses:\s+(.+)$')
        foreach ($match in $matches) {
            $actions += $match.Groups[1].Value.Trim()
        }
    }
    
    return $actions
}

# Function to extract environment variables
function Get-EnvironmentVariables {
    param([string]$FilePath)
    
    $content = Get-Content $FilePath -Raw
    $envVars = @{}
    
    # Extract env section
    if ($content -match '(?s)^env:\s*\n((?:  \w+:.*\n)+)') {
        $envSection = $matches[1]
        $envLines = $envSection -split '\n'
        foreach ($line in $envLines) {
            if ($line -match '^\s+(\w+):\s*(.+)$') {
                $envVars[$matches[1]] = $matches[2].Trim()
            }
        }
    }
    
    return $envVars
}

# Validate test.yml
Write-Host "Validating test.yml..." -ForegroundColor Yellow
$testYmlPath = ".github/workflows/test.yml"

if (Test-Path $testYmlPath) {
    $syntaxIssues = Test-YamlBasicSyntax -FilePath $testYmlPath
    if ($syntaxIssues.Count -eq 0) {
        Write-Host "  [PASS] YAML syntax: VALID" -ForegroundColor Green
        $validationResults += @{File="test.yml"; Check="YAML Syntax"; Status="PASS"}
    } else {
        Write-Host "  [FAIL] YAML syntax issues found:" -ForegroundColor Red
        foreach ($issue in $syntaxIssues) {
            Write-Host "    - $issue" -ForegroundColor Red
        }
        $validationResults += @{File="test.yml"; Check="YAML Syntax"; Status="FAIL"}
    }
    
    $actions = Get-ActionVersions -FilePath $testYmlPath
    Write-Host "  [PASS] Actions found: $($actions.Count)" -ForegroundColor Green
    foreach ($action in $actions) {
        Write-Host "    - $action" -ForegroundColor Gray
    }
    $validationResults += @{File="test.yml"; Check="Actions Detected"; Status="PASS"}
    
    $envVars = Get-EnvironmentVariables -FilePath $testYmlPath
    Write-Host "  [PASS] Environment variables: $($envVars.Count)" -ForegroundColor Green
    foreach ($key in $envVars.Keys) {
        Write-Host "    - $key = $($envVars[$key])" -ForegroundColor Gray
    }
    $validationResults += @{File="test.yml"; Check="Env Variables"; Status="PASS"}
} else {
    Write-Host "  [FAIL] File not found: $testYmlPath" -ForegroundColor Red
    $validationResults += @{File="test.yml"; Check="File Exists"; Status="FAIL"}
}

Write-Host ""

# Validate ci.yml
Write-Host "Validating ci.yml..." -ForegroundColor Yellow
$ciYmlPath = ".github/workflows/ci.yml"

if (Test-Path $ciYmlPath) {
    $syntaxIssues = Test-YamlBasicSyntax -FilePath $ciYmlPath
    if ($syntaxIssues.Count -eq 0) {
        Write-Host "  [PASS] YAML syntax: VALID" -ForegroundColor Green
        $validationResults += @{File="ci.yml"; Check="YAML Syntax"; Status="PASS"}
    } else {
        Write-Host "  [FAIL] YAML syntax issues found:" -ForegroundColor Red
        foreach ($issue in $syntaxIssues) {
            Write-Host "    - $issue" -ForegroundColor Red
        }
        $validationResults += @{File="ci.yml"; Check="YAML Syntax"; Status="FAIL"}
    }
    
    $actions = Get-ActionVersions -FilePath $ciYmlPath
    Write-Host "  [PASS] Actions found: $($actions.Count)" -ForegroundColor Green
    foreach ($action in $actions) {
        Write-Host "    - $action" -ForegroundColor Gray
    }
    $validationResults += @{File="ci.yml"; Check="Actions Detected"; Status="PASS"}
    
    $envVars = Get-EnvironmentVariables -FilePath $ciYmlPath
    Write-Host "  [PASS] Environment variables: $($envVars.Count)" -ForegroundColor Green
    foreach ($key in $envVars.Keys) {
        Write-Host "    - $key = $($envVars[$key])" -ForegroundColor Gray
    }
    $validationResults += @{File="ci.yml"; Check="Env Variables"; Status="PASS"}
} else {
    Write-Host "  [FAIL] File not found: $ciYmlPath" -ForegroundColor Red
    $validationResults += @{File="ci.yml"; Check="File Exists"; Status="FAIL"}
}

Write-Host ""

# Compare environment variables between workflows
Write-Host "Checking environment variable consistency..." -ForegroundColor Yellow
$testEnv = Get-EnvironmentVariables -FilePath $testYmlPath
$ciEnv = Get-EnvironmentVariables -FilePath $ciYmlPath

$consistent = $true
foreach ($key in $testEnv.Keys) {
    if ($ciEnv.ContainsKey($key)) {
        if ($testEnv[$key] -eq $ciEnv[$key]) {
            Write-Host "  [PASS] $key matches: $($testEnv[$key])" -ForegroundColor Green
        } else {
            Write-Host "  [FAIL] $key mismatch: test.yml=$($testEnv[$key]), ci.yml=$($ciEnv[$key])" -ForegroundColor Red
            $consistent = $false
        }
    } else {
        Write-Host "  [WARN] $key only in test.yml" -ForegroundColor Yellow
    }
}

foreach ($key in $ciEnv.Keys) {
    if (-not $testEnv.ContainsKey($key)) {
        Write-Host "  [WARN] $key only in ci.yml" -ForegroundColor Yellow
    }
}

if ($consistent) {
    $validationResults += @{File="Both"; Check="Env Consistency"; Status="PASS"}
} else {
    $validationResults += @{File="Both"; Check="Env Consistency"; Status="FAIL"}
}

Write-Host ""

# Validate action versions are pinned
Write-Host "Validating action version pinning..." -ForegroundColor Yellow
$allActions = @()
$allActions += Get-ActionVersions -FilePath $testYmlPath
$allActions += Get-ActionVersions -FilePath $ciYmlPath

$unpinnedActions = @()
foreach ($action in $allActions | Select-Object -Unique) {
    if ($action -notmatch '@v\d+' -and $action -notmatch '@[a-f0-9]{40}') {
        $unpinnedActions += $action
    }
}

if ($unpinnedActions.Count -eq 0) {
    Write-Host "  [PASS] All actions are properly pinned to versions" -ForegroundColor Green
    $validationResults += @{File="Both"; Check="Action Pinning"; Status="PASS"}
} else {
    Write-Host "  [FAIL] Unpinned actions found:" -ForegroundColor Red
    foreach ($action in $unpinnedActions) {
        Write-Host "    - $action" -ForegroundColor Red
    }
    $validationResults += @{File="Both"; Check="Action Pinning"; Status="FAIL"}
}

Write-Host ""
Write-Host "=== Validation Summary ===" -ForegroundColor Cyan
$passCount = ($validationResults | Where-Object { $_.Status -eq "PASS" }).Count
$failCount = ($validationResults | Where-Object { $_.Status -eq "FAIL" }).Count
Write-Host "Passed: $passCount" -ForegroundColor Green
Write-Host "Failed: $failCount" -ForegroundColor Red

if ($failCount -eq 0) {
    Write-Host ""
    Write-Host "All workflow validations passed!" -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "Some validations failed. Please review the issues above." -ForegroundColor Red
    exit 1
}
