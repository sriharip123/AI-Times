#!/bin/bash
# JSON-Whisperer Diagnostic Commands Test Script
# This script automates testing of all diagnostic commands

set +e  # Don't exit on error

# Parse arguments
VERBOSE=false
SKIP_SERVICE_CHECKS=false
TEST_GROUP="all"

while [[ $# -gt 0 ]]; do
    case $1 in
        --verbose|-v)
            VERBOSE=true
            shift
            ;;
        --skip-service-checks)
            SKIP_SERVICE_CHECKS=true
            shift
            ;;
        --test-group)
            TEST_GROUP="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--verbose] [--skip-service-checks] [--test-group GROUP]"
            exit 1
            ;;
    esac
done

# Test counters
TESTS_PASSED=0
TESTS_FAILED=0
TESTS_SKIPPED=0

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

# Output functions
write_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

write_failure() {
    echo -e "${RED}✗ $1${NC}"
}

write_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

write_info() {
    echo -e "${CYAN}ℹ $1${NC}"
}

write_test_header() {
    echo -e "\n${MAGENTA}=== $1 ===${NC}"
}

# Test result tracking
record_test_result() {
    local test_name="$1"
    local passed="$2"
    local message="$3"
    
    if [ "$passed" = "true" ]; then
        ((TESTS_PASSED++))
        write_success "$test_name - PASSED"
    else
        ((TESTS_FAILED++))
        write_failure "$test_name - FAILED: $message"
    fi
}

skip_test() {
    local test_name="$1"
    local reason="$2"
    ((TESTS_SKIPPED++))
    write_warning "$test_name - SKIPPED: $reason"
}

# Check if a service is available
test_service_available() {
    local service_name="$1"
    local test_command="$2"
    
    if eval "$test_command" &>/dev/null; then
        return 0
    else
        return 1
    fi
}

# Run a diagnostic command and check exit code
test_diagnostic_command() {
    local test_name="$1"
    local command="$2"
    local expected_exit_code="${3:-0}"
    shift 3
    local expected_output=("$@")
    
    write_info "Running: $command"
    
    # Run command and capture output and exit code
    local output
    output=$(eval "$command" 2>&1)
    local exit_code=$?
    
    if [ "$VERBOSE" = true ]; then
        echo -e "${GRAY}Output:${NC}"
        echo -e "${GRAY}$output${NC}"
        echo -e "${GRAY}Exit Code: $exit_code${NC}"
    fi
    
    # Check exit code
    if [ $exit_code -ne $expected_exit_code ]; then
        record_test_result "$test_name" "false" "Expected exit code $expected_exit_code, got $exit_code"
        return
    fi
    
    # Check expected output
    local all_expected_found=true
    for expected in "${expected_output[@]}"; do
        if ! echo "$output" | grep -q "$expected"; then
            write_failure "Expected output not found: $expected"
            all_expected_found=false
        fi
    done
    
    if [ "$all_expected_found" = true ]; then
        record_test_result "$test_name" "true"
    else
        record_test_result "$test_name" "false" "Output validation failed"
    fi
}

# Main test execution
echo -e "\n${CYAN}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${CYAN}║  JSON-Whisperer Diagnostic Commands Test Suite            ║${NC}"
echo -e "${CYAN}╚════════════════════════════════════════════════════════════╝${NC}\n"

# Check prerequisites
write_test_header "Checking Prerequisites"

if ! test_service_available ".NET" "dotnet --version"; then
    write_failure ".NET SDK not found. Please install .NET 8.0 SDK."
    exit 1
fi
write_success ".NET SDK is available"

# Check if project exists
if [ ! -f "JSON-Whisperer/JSON-Whisperer.csproj" ]; then
    write_failure "JSON-Whisperer project not found. Please run from repository root."
    exit 1
fi
write_success "JSON-Whisperer project found"

# Check services (unless skipped)
OLLAMA_AVAILABLE=false
SCYLLA_AVAILABLE=false

if [ "$SKIP_SERVICE_CHECKS" = false ]; then
    write_info "Checking service availability..."
    
    # Check Ollama
    if curl -s -f http://localhost:11434/api/tags >/dev/null 2>&1; then
        OLLAMA_AVAILABLE=true
        write_success "Ollama is running"
    else
        write_warning "Ollama is not running - some tests will be skipped"
    fi
    
    # Check ScyllaDB
    if timeout 2 bash -c "cat < /dev/null > /dev/tcp/localhost/9042" 2>/dev/null; then
        SCYLLA_AVAILABLE=true
        write_success "ScyllaDB is running"
    else
        write_warning "ScyllaDB is not running - some tests will be skipped"
    fi
else
    write_warning "Service checks skipped - some tests may fail"
fi

# Test Group 1: Help and Basic Functionality
if [ "$TEST_GROUP" = "all" ] || [ "$TEST_GROUP" = "help" ]; then
    write_test_header "Test Group 1: Help and Basic Functionality"
    
    test_diagnostic_command \
        "1.1 Help Display" \
        "dotnet run --project JSON-Whisperer -- --help" \
        0 \
        "--help" "--health-check" "--verbose" "Usage"
    
    test_diagnostic_command \
        "1.2 Short Help Flag" \
        "dotnet run --project JSON-Whisperer -- -h" \
        0 \
        "--help" "--health-check"
    
    test_diagnostic_command \
        "1.3 Unknown Flag Error" \
        "dotnet run --project JSON-Whisperer -- --unknown-flag" \
        5 \
        "ERROR" "Unknown flag"
fi

# Test Group 2: Health Check Command
if [ "$TEST_GROUP" = "all" ] || [ "$TEST_GROUP" = "health" ]; then
    write_test_header "Test Group 2: Health Check Command"
    
    if [ "$OLLAMA_AVAILABLE" = true ] && [ "$SCYLLA_AVAILABLE" = true ]; then
        test_diagnostic_command \
            "2.1 Health Check with All Services Running" \
            "dotnet run --project JSON-Whisperer -- --health-check" \
            0 \
            "Health Check" "Ollama" "ScyllaDB" "Embedding"
        
        test_diagnostic_command \
            "2.2 Health Check with Verbose Mode" \
            "dotnet run --project JSON-Whisperer -- --health-check --verbose" \
            0 \
            "Health Check" "URL:" "Model:"
    else
        skip_test "2.1 Health Check with All Services" "Services not running"
        skip_test "2.2 Health Check with Verbose" "Services not running"
    fi
fi

# Test Group 3: Configuration Validation
if [ "$TEST_GROUP" = "all" ] || [ "$TEST_GROUP" = "config" ]; then
    write_test_header "Test Group 3: Configuration Validation"
    
    test_diagnostic_command \
        "3.1 Validate Valid Configuration" \
        "dotnet run --project JSON-Whisperer -- --validate-config" \
        0 \
        "Configuration Validation" "Ollama" "Application"
    
    test_diagnostic_command \
        "3.2 Validate Configuration with Verbose" \
        "dotnet run --project JSON-Whisperer -- --validate-config --verbose" \
        0 \
        "Configuration Validation" "BaseUrl" "ModelName"
fi

# Test Group 4: Individual Service Testing
if [ "$TEST_GROUP" = "all" ] || [ "$TEST_GROUP" = "services" ]; then
    write_test_header "Test Group 4: Individual Service Testing"
    
    if [ "$OLLAMA_AVAILABLE" = true ]; then
        test_diagnostic_command \
            "4.1 Test Ollama Service" \
            "dotnet run --project JSON-Whisperer -- --test-ollama" \
            0 \
            "Testing Ollama" "available"
        
        test_diagnostic_command \
            "4.5 Test Embedding Service" \
            "dotnet run --project JSON-Whisperer -- --test-embedding" \
            0 \
            "Testing Embedding" "available" "dimensions"
        
        test_diagnostic_command \
            "4.6 Test Embedding with Verbose" \
            "dotnet run --project JSON-Whisperer -- --test-embedding --verbose" \
            0 \
            "Testing Embedding" "First 5 values"
    else
        skip_test "4.1 Test Ollama" "Ollama not running"
        skip_test "4.5 Test Embedding" "Ollama not running"
        skip_test "4.6 Test Embedding Verbose" "Ollama not running"
    fi
    
    if [ "$SCYLLA_AVAILABLE" = true ]; then
        test_diagnostic_command \
            "4.3 Test ScyllaDB" \
            "dotnet run --project JSON-Whisperer -- --test-scylla" \
            0 \
            "Testing ScyllaDB" "connected"
    else
        skip_test "4.3 Test ScyllaDB" "ScyllaDB not running"
    fi
    
    if [ "$OLLAMA_AVAILABLE" = true ] && [ "$SCYLLA_AVAILABLE" = true ]; then
        test_diagnostic_command \
            "4.7 Test Similarity Search" \
            "dotnet run --project JSON-Whisperer -- --test-similarity" \
            0 \
            "Testing Similarity"
    else
        skip_test "4.7 Test Similarity" "Services not running"
    fi
fi

# Test Group 5: Knowledge Base Management
if [ "$TEST_GROUP" = "all" ] || [ "$TEST_GROUP" = "knowledge" ]; then
    write_test_header "Test Group 5: Knowledge Base Management"
    
    if [ "$OLLAMA_AVAILABLE" = true ] && [ "$SCYLLA_AVAILABLE" = true ]; then
        test_diagnostic_command \
            "5.2 Validate Knowledge Base" \
            "dotnet run --project JSON-Whisperer -- --validate-knowledge-base" \
            0 \
            "Validating Knowledge Base" "examples"
        
        test_diagnostic_command \
            "5.3 Validate Knowledge Base with Verbose" \
            "dotnet run --project JSON-Whisperer -- --validate-knowledge-base --verbose" \
            0 \
            "Validating Knowledge Base" "Examples:"
    else
        skip_test "5.2 Validate Knowledge Base" "Services not running"
        skip_test "5.3 Validate KB Verbose" "Services not running"
    fi
fi

# Test Group 6: Benchmarking
if [ "$TEST_GROUP" = "all" ] || [ "$TEST_GROUP" = "benchmark" ]; then
    write_test_header "Test Group 6: Benchmarking"
    
    if [ "$OLLAMA_AVAILABLE" = true ]; then
        test_diagnostic_command \
            "6.4 Benchmark Embedding Generation" \
            "dotnet run --project JSON-Whisperer -- --benchmark-embedding" \
            0 \
            "Benchmark: Embedding" "Iterations" "Average" "Throughput"
    else
        skip_test "6.4 Benchmark Embedding" "Ollama not running"
    fi
    
    if [ "$SCYLLA_AVAILABLE" = true ]; then
        test_diagnostic_command \
            "6.3 Benchmark Vector Operations" \
            "dotnet run --project JSON-Whisperer -- --benchmark-vector-operations" \
            0 \
            "Benchmark: Vector Operations" "Iterations" "Throughput"
    else
        skip_test "6.3 Benchmark Vector" "ScyllaDB not running"
    fi
    
    if [ "$OLLAMA_AVAILABLE" = true ] && [ "$SCYLLA_AVAILABLE" = true ]; then
        test_diagnostic_command \
            "6.1 Benchmark Similarity Search" \
            "dotnet run --project JSON-Whisperer -- --benchmark-similarity" \
            0 \
            "Benchmark: Similarity" "Iterations" "Throughput"
    else
        skip_test "6.1 Benchmark Similarity" "Services not running"
    fi
fi

# Test Group 7: Flag Combinations
if [ "$TEST_GROUP" = "all" ] || [ "$TEST_GROUP" = "flags" ]; then
    write_test_header "Test Group 7: Flag Combinations and Overrides"
    
    test_diagnostic_command \
        "7.1 Verbose Mode Override" \
        "dotnet run --project JSON-Whisperer -- --validate-config --verbose" \
        0 \
        "BaseUrl" "ModelName"
    
    test_diagnostic_command \
        "7.2 Short Verbose Flag" \
        "dotnet run --project JSON-Whisperer -- --validate-config -v" \
        0 \
        "BaseUrl" "ModelName"
    
    test_diagnostic_command \
        "7.4 Conflicting Flags" \
        "dotnet run --project JSON-Whisperer -- --test-scylla --no-similarity" \
        5 \
        "ERROR" "conflicts"
fi

# Test Group 8: Error Handling
if [ "$TEST_GROUP" = "all" ] || [ "$TEST_GROUP" = "errors" ]; then
    write_test_header "Test Group 8: Error Messages and Exit Codes"
    
    test_diagnostic_command \
        "8.1 File Not Found Error" \
        "dotnet run --project JSON-Whisperer -- --file nonexistent.json" \
        5 \
        "ERROR" "File not found"
    
    test_diagnostic_command \
        "8.2 Missing File Argument" \
        "dotnet run --project JSON-Whisperer -- --file" \
        5 \
        "ERROR" "requires"
fi

# Test Summary
echo -e "\n${CYAN}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${CYAN}║  Test Summary                                              ║${NC}"
echo -e "${CYAN}╚════════════════════════════════════════════════════════════╝${NC}\n"

TOTAL_TESTS=$((TESTS_PASSED + TESTS_FAILED + TESTS_SKIPPED))

echo -e "Total Tests:   $TOTAL_TESTS"
write_success "Passed:        $TESTS_PASSED"
write_failure "Failed:        $TESTS_FAILED"
write_warning "Skipped:       $TESTS_SKIPPED"

if [ $TOTAL_TESTS -gt 0 ]; then
    PASS_RATE=$(awk "BEGIN {printf \"%.2f\", ($TESTS_PASSED / $TOTAL_TESTS) * 100}")
else
    PASS_RATE=0
fi

if (( $(echo "$PASS_RATE >= 90" | bc -l) )); then
    COLOR=$GREEN
elif (( $(echo "$PASS_RATE >= 70" | bc -l) )); then
    COLOR=$YELLOW
else
    COLOR=$RED
fi

echo -e "\nPass Rate:     ${COLOR}${PASS_RATE}%${NC}"

# Exit with appropriate code
if [ $TESTS_FAILED -gt 0 ]; then
    echo -e "\n${RED}❌ Some tests failed. Please review the output above.${NC}"
    exit 1
else
    echo -e "\n${GREEN}✅ All tests passed!${NC}"
    exit 0
fi
