#!/bin/bash
# Simple test runner - runs all test projects
set -e

echo "Building solution..."
dotnet build Impulse.slnx

echo ""
echo "Running Impulse.Core.Tests..."
dotnet run --project tests/Impulse.Core.Tests/Impulse.Core.Tests.csproj

echo ""
echo "Running Impulse.CodeGen.Tests..."
dotnet run --project tests/Impulse.CodeGen.Tests/Impulse.CodeGen.Tests.csproj

echo ""
echo "Running Impulse.Validation.Tests..."
dotnet run --project tests/Impulse.Validation.Tests/Impulse.Validation.Tests.csproj

echo ""
echo "All tests completed!"
