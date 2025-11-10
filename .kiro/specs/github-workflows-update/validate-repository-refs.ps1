# Repository References Validation Script
# This script validates GitHub repository URLs, workflow badge URLs, and job names

Write-Host "=== Repository References Validation ===" -ForegroundColor Cyan
Write-Host ""

$validationResults = @()
$failCount = 0

# Expected repository information
$expectedRepo = "sriharip123/AI-Times"
$expectedBranch = "main"

# Function to extract repository references from a file
function Get-RepositoryReferences {
    param([string]$FilePath)
    
    if (-not (Test-Path $FilePath)) {
        return @()
    }
    
    $content = Get-Content $FilePath -Raw
    $refs = @()
    
    # Pattern for GitHub URLs
    $githubPattern = 'https://github\.com/([^/\s\)]+/[^/\s\)]+)'
    $matches = [regex]::Matches($content, $githubPattern)
    
    foreach ($match in $matches) {
        $refs += @{
            Type = "GitHub URL"
            Value = $match.Groups[1].Value
            FullMatch = $match.Value
        }
    }
    
    # Pattern for badge URLs
    $badgePattern = 'https://github\.com/([^/\s\)]+/[^/\s\)]+)/(?:actions/)?workflows/([^/\s\)]+)/badge\.svg'
    $matches = [regex]::Matches($content, $badgePattern)
    
    foreach ($match in $matches) {
        $refs += @{
            Type = "Badge URL"
            Value = $match.Groups[1].Value
            Workflow = $match.Groups[2].Value
            FullMatch = $match.Value
        }
    }
    
    return $refs
}

# Function to extract job names from workflow files
function Get-WorkflowJobNames {
    param([string]$FilePath)
    
    if (-not (Test-Path $FilePath)) {
        return @()
    }
    
    $content = Get-Content $FilePath -Raw
    $jobs = @()
    
    # Extract job names from YAML
    $jobPattern = '(?m)^jobs:\s*\n((?:  \w+:.*\n(?:    .*\n)*)+)'
    if ($content -match $jobPattern) {
        $jobsSection = $matches[1]
        $jobNamePattern = '(?m)^  (\w+):'
        $jobMatches = [regex]::Matches($jobsSection, $jobNamePattern)
        
        foreach ($match in $jobMatches) {
            $jobs += $match.Groups[1].Value
        }
    }
    
    return $jobs
}

# Step 1: Validate repository URLs in documentation
Write-Host "Step 1: Validate GitHub repository URLs" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$docsToCheck = @(
    ".github/workflows/README.md",
    ".github/CONTRIBUTING.md",
    ".github/SETUP_CHECKLIST.md",
    ".github/WORKFLOWS_SETUP.md",
    "README.md"
)

$allRepoRefs = @()
foreach ($doc in $docsToCheck) {
    if (Test-Path $doc) {
        $refs = Get-RepositoryReferences -FilePath $doc
        if ($refs.Count -gt 0) {
            Write-Host "  Checking $doc..." -ForegroundColor Yellow
            foreach ($ref in $refs) {
                $allRepoRefs += $ref
                # Normalize by removing .git suffix if present
                $normalizedValue = $ref.Value -replace '\.git$', ''
                if ($normalizedValue -eq $expectedRepo) {
                    Write-Host "    [PASS] $($ref.Type): $($ref.Value)" -ForegroundColor Green
                } else {
                    Write-Host "    [FAIL] $($ref.Type): $($ref.Value) (expected: $expectedRepo)" -ForegroundColor Red
                    $script:failCount++
                }
            }
        }
    }
}

if ($allRepoRefs.Count -eq 0) {
    Write-Host "  [WARN] No repository references found in documentation" -ForegroundColor Yellow
}
Write-Host ""

# Step 2: Validate workflow badge URLs
Write-Host "Step 2: Validate workflow badge URLs" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

$badgeRefs = $allRepoRefs | Where-Object { $_.Type -eq "Badge URL" }
if ($badgeRefs.Count -gt 0) {
    foreach ($badge in $badgeRefs) {
        Write-Host "  Badge for workflow: $($badge.Workflow)" -ForegroundColor Yellow
        
        # Check if workflow file exists
        $workflowFile = ".github/workflows/$($badge.Workflow)"
        if (Test-Path $workflowFile) {
            Write-Host "    [PASS] Workflow file exists: $workflowFile" -ForegroundColor Green
        } else {
            Write-Host "    [FAIL] Workflow file not found: $workflowFile" -ForegroundColor Red
            $script:failCount++
        }
        
        # Check repository in badge URL
        if ($badge.Value -eq $expectedRepo) {
            Write-Host "    [PASS] Repository correct: $($badge.Value)" -ForegroundColor Green
        } else {
            Write-Host "    [FAIL] Repository incorrect: $($badge.Value) (expected: $expectedRepo)" -ForegroundColor Red
            $script:failCount++
        }
    }
} else {
    Write-Host "  [INFO] No workflow badge URLs found" -ForegroundColor Cyan
}
Write-Host ""

# Step 3: Extract and display job names for branch protection
Write-Host "Step 3: Validate workflow job names for branch protection" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$workflowFiles = @(
    @{Path=".github/workflows/test.yml"; Name="test.yml"},
    @{Path=".github/workflows/ci.yml"; Name="ci.yml"}
)

$allJobNames = @{}
foreach ($workflow in $workflowFiles) {
    if (Test-Path $workflow.Path) {
        $jobs = Get-WorkflowJobNames -FilePath $workflow.Path
        $allJobNames[$workflow.Name] = $jobs
        
        Write-Host "  Workflow: $($workflow.Name)" -ForegroundColor Yellow
        if ($jobs.Count -gt 0) {
            Write-Host "    [PASS] Found $($jobs.Count) job(s):" -ForegroundColor Green
            foreach ($job in $jobs) {
                Write-Host "      - $job" -ForegroundColor Gray
            }
        } else {
            Write-Host "    [WARN] No jobs found" -ForegroundColor Yellow
        }
    }
}
Write-Host ""

# Step 4: Check if job names are referenced in setup documentation
Write-Host "Step 4: Validate job names in setup documentation" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

$setupDoc = ".github/SETUP_CHECKLIST.md"
if (Test-Path $setupDoc) {
    $content = Get-Content $setupDoc -Raw
    
    Write-Host "  Checking $setupDoc for job name references..." -ForegroundColor Yellow
    
    $foundJobRefs = $false
    foreach ($workflowName in $allJobNames.Keys) {
        foreach ($jobName in $allJobNames[$workflowName]) {
            if ($content -match [regex]::Escape($jobName)) {
                Write-Host "    [PASS] Job '$jobName' referenced in documentation" -ForegroundColor Green
                $foundJobRefs = $true
            }
        }
    }
    
    if (-not $foundJobRefs) {
        Write-Host "    [INFO] No specific job names found in documentation" -ForegroundColor Cyan
        Write-Host "    [INFO] This is acceptable if documentation uses general descriptions" -ForegroundColor Cyan
    }
} else {
    Write-Host "  [WARN] Setup checklist not found: $setupDoc" -ForegroundColor Yellow
}
Write-Host ""

# Step 5: Validate monitoring URLs and instructions
Write-Host "Step 5: Validate monitoring URLs and instructions" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

$monitoringDocs = @(
    ".github/SETUP_CHECKLIST.md",
    ".github/WORKFLOWS_SETUP.md"
)

foreach ($doc in $monitoringDocs) {
    if (Test-Path $doc) {
        $content = Get-Content $doc -Raw
        
        Write-Host "  Checking $doc..." -ForegroundColor Yellow
        
        # Check for Actions tab reference
        if ($content -match 'Actions\s+tab' -or $content -match 'github\.com/[^/]+/[^/]+/actions') {
            Write-Host "    [PASS] Contains monitoring/Actions references" -ForegroundColor Green
        } else {
            Write-Host "    [INFO] No explicit Actions tab references found" -ForegroundColor Cyan
        }
        
        # Check for workflow status references
        if ($content -match 'workflow\s+status' -or $content -match 'badge') {
            Write-Host "    [PASS] Contains workflow status references" -ForegroundColor Green
        }
    }
}
Write-Host ""

# Summary
Write-Host "=== Validation Summary ===" -ForegroundColor Cyan

# Display all unique repository references found
$uniqueRepos = @()
foreach ($ref in $allRepoRefs) {
    if ($ref.Value -and $uniqueRepos -notcontains $ref.Value) {
        $uniqueRepos += $ref.Value
    }
}

Write-Host "Repository references found:" -ForegroundColor Yellow
if ($uniqueRepos.Count -gt 0) {
    foreach ($repo in $uniqueRepos) {
        if ($repo -eq $expectedRepo) {
            Write-Host "  [PASS] $repo" -ForegroundColor Green
        } else {
            Write-Host "  [FAIL] $repo (expected: $expectedRepo)" -ForegroundColor Red
        }
    }
} else {
    Write-Host "  [INFO] No repository references found" -ForegroundColor Cyan
}
Write-Host ""

# Display job names for branch protection setup
Write-Host "Job names for branch protection:" -ForegroundColor Yellow
foreach ($workflowName in $allJobNames.Keys) {
    Write-Host "  $workflowName :" -ForegroundColor Cyan
    foreach ($job in $allJobNames[$workflowName]) {
        Write-Host "    - $job" -ForegroundColor Gray
    }
}
Write-Host ""

if ($failCount -eq 0) {
    Write-Host "All repository reference validations passed!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "Failed checks: $failCount" -ForegroundColor Red
    Write-Host "Please review the failures above and update repository references." -ForegroundColor Red
    exit 1
}
