#!/bin/bash
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Impulse Framework - Complete Build & Test"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Ensure .NET is in PATH
export PATH="$HOME/.dotnet:$PATH"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
cd "$ROOT_DIR"

# Track overall status
FAILED=0
TOTAL_TESTS=0
PASSED_TESTS=0

step() {
    echo -e "\n${YELLOW}▸ $1${NC}"
}

success() {
    echo -e "${GREEN}✓ $1${NC}"
}

fail() {
    echo -e "${RED}✗ $1${NC}"
    FAILED=1
}

info() {
    echo -e "${BLUE}  $1${NC}"
}

# ═══════════════════════════════════════════════════════
# Step 0: Ensure local NuGet packages exist
# ═══════════════════════════════════════════════════════
step "Checking local NuGet packages..."
if [ ! -f "packages/Microsoft.CodeAnalysis.CSharp.4.12.0.nupkg" ]; then
    info "Creating local packages from SDK..."
    python3 scripts/create-local-packages.py
fi

if [ -f "packages/Microsoft.CodeAnalysis.CSharp.4.12.0.nupkg" ]; then
    success "Local NuGet packages ready"
else
    fail "Failed to create local NuGet packages"
    exit 1
fi

# ═══════════════════════════════════════════════════════
# Step 1: Build Core Libraries
# ═══════════════════════════════════════════════════════
step "Building Impulse.Core..."
if dotnet build src/Impulse.Core/Impulse.Core.csproj -c Release --verbosity quiet; then
    success "Impulse.Core built"
else
    fail "Impulse.Core build failed"
    exit 1
fi

step "Building Impulse.CodeGen..."
if dotnet build src/Impulse.CodeGen/Impulse.CodeGen.csproj -c Release --verbosity quiet; then
    success "Impulse.CodeGen built"
else
    fail "Impulse.CodeGen build failed"
    exit 1
fi

step "Building Impulse.SourceGen..."
if dotnet build src/Impulse.SourceGen/Impulse.SourceGen.csproj -c Release --verbosity quiet; then
    success "Impulse.SourceGen built"
else
    fail "Impulse.SourceGen build failed"
    exit 1
fi

# ═══════════════════════════════════════════════════════
# Step 2: Run Unit Tests
# ═══════════════════════════════════════════════════════
step "Running Impulse.Core.Tests..."
if dotnet build tests/Impulse.Core.Tests/Impulse.Core.Tests.csproj -c Release --verbosity quiet; then
    if dotnet run --project tests/Impulse.Core.Tests/Impulse.Core.Tests.csproj -c Release --no-build 2>&1; then
        success "Impulse.Core.Tests passed"
    else
        fail "Impulse.Core.Tests failed"
    fi
else
    fail "Impulse.Core.Tests build failed"
fi

step "Running Impulse.CodeGen.Tests..."
if dotnet build tests/Impulse.CodeGen.Tests/Impulse.CodeGen.Tests.csproj -c Release --verbosity quiet; then
    if dotnet run --project tests/Impulse.CodeGen.Tests/Impulse.CodeGen.Tests.csproj -c Release --no-build 2>&1; then
        success "Impulse.CodeGen.Tests passed"
    else
        fail "Impulse.CodeGen.Tests failed"
    fi
else
    fail "Impulse.CodeGen.Tests build failed"
fi

# ═══════════════════════════════════════════════════════
# Step 3: Build Integration Tests
# ═══════════════════════════════════════════════════════
step "Building Impulse.IntegrationTests..."
if dotnet build tests/Impulse.IntegrationTests/Impulse.IntegrationTests.csproj -c Release --verbosity quiet; then
    success "Impulse.IntegrationTests built"
else
    fail "Impulse.IntegrationTests build failed"
fi

# ═══════════════════════════════════════════════════════
# Step 4: TypeScript Type Check (if npm is available)
# ═══════════════════════════════════════════════════════
if command -v npm &> /dev/null; then
    step "TypeScript type check..."
    cd tests/Impulse.IntegrationTests
    if [ -f "package.json" ]; then
        npm install --silent 2>/dev/null || true
        if npx tsc --noEmit 2>/dev/null; then
            success "TypeScript type check passed"
        else
            fail "TypeScript type check failed"
        fi
    fi
    cd "$ROOT_DIR"
fi

# ═══════════════════════════════════════════════════════
# Summary
# ═══════════════════════════════════════════════════════
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Build Summary:"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  Impulse.Core       - Built"
echo "  Impulse.CodeGen    - Built"
echo "  Impulse.SourceGen  - Built"
echo "  Core.Tests         - Tested"
echo "  CodeGen.Tests      - Tested"
echo "  IntegrationTests   - Built"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}All checks passed!${NC}"
    exit 0
else
    echo -e "${RED}Some checks failed!${NC}"
    exit 1
fi
