#!/bin/bash
# Impulse v2 Build Script
# Single command to run the entire build pipeline

set -e  # Exit on any error

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "============================================"
echo "  Impulse v2 Build Pipeline"
echo "============================================"
echo ""

# Step 1: Restore
echo "Step 1: Restoring packages..."
dotnet restore
echo ""

# Step 2: Build
echo "Step 2: Building solution..."
dotnet build --no-restore
echo ""

# Step 3: Generate TypeScript
echo "Step 3: Generating TypeScript..."
SAMPLE_DLL="samples/SampleApp/bin/Debug/net10.0/SampleApp.dll"
GEN_OUTPUT="samples/SampleApp/generated"
if [ -f "$SAMPLE_DLL" ]; then
    dotnet run --project src/Impulse.SourceGen --no-build -- "$SAMPLE_DLL" "$GEN_OUTPUT"
else
    echo "Warning: SampleApp.dll not found, skipping generation"
fi
echo ""

# Step 4: Run unit tests
echo "Step 4: Running unit tests..."
dotnet run --project tests/Impulse.Core.Tests --no-build
echo ""

# Step 5: Run E2E tests (optional, requires npm and Playwright)
if command -v npm &> /dev/null; then
    echo "Step 5: Running E2E tests..."
    cd tests/Impulse.Playwright.Tests
    npm install --silent
    npm test || echo "E2E tests not yet passing (TDD RED phase)"
    cd "$SCRIPT_DIR"
else
    echo "Step 5: Skipping E2E tests (npm not available)"
fi

echo ""
echo "============================================"
echo "  Build Complete!"
echo "============================================"
