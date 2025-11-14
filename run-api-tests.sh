#!/bin/bash

# API Test Runner Script for Maltalist .NET API
# This script runs the API test suite

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to print header
print_header() {
    echo -e "${BLUE}"
    echo "======================================================================"
    echo "           Maltalist API Test Suite Runner"
    echo "======================================================================"
    echo -e "${NC}"
    echo "🧪 Test Projects: MaltalistApi.Tests"
    echo "🔧 Services: ListingsService"
    echo "📝 Models: Listing, User, Promotion, CreateListingRequest"
    echo "🎯 Coverage: Model validation, Service layer testing"
    echo ""
}

# Function to check dependencies
check_dependencies() {
    print_info "Checking dependencies..."
    
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK is not installed"
        exit 1
    fi
    
    print_success "All dependencies are available"
}

# Main execution
main() {
    print_header
    check_dependencies
    
    print_info "Starting API test suite..."
    echo ""
    
    # Navigate to test directory
    cd "$(dirname "$0")/MaltalistApi.Tests"
    
    # Run tests with detailed output
    print_info "Running tests with dotnet test..."
    if dotnet test --verbosity normal; then
        echo ""
        print_success "All API tests passed! ✅"
        exit 0
    else
        echo ""
        print_error "Some tests failed! ❌"
        exit 1
    fi
}

# Run main function
main
