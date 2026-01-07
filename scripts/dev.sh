#!/bin/bash
# Impulse Development Server
# Runs both Vite (TypeScript HMR) and dotnet watch (C# hot reload) in parallel

set -e

GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
INTEGRATION_DIR="$ROOT_DIR/tests/Impulse.IntegrationTests"

export PATH="$HOME/.dotnet:$PATH"

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Impulse Development Server"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo -e "${GREEN}Starting:${NC}"
echo "  • Vite dev server on http://localhost:5173 (TypeScript HMR)"
echo "  • .NET watch on http://localhost:5000 (C# hot reload)"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

cleanup() {
    echo ""
    echo "Shutting down..."
    kill $(jobs -p) 2>/dev/null
    exit 0
}
trap cleanup SIGINT SIGTERM

# Start Vite in background
cd "$INTEGRATION_DIR"
npm run dev &
VITE_PID=$!

# Give Vite a moment to start
sleep 2

# Start dotnet watch
echo -e "${BLUE}Starting .NET watch...${NC}"
dotnet watch run --project "$INTEGRATION_DIR/Impulse.IntegrationTests.csproj" --urls http://localhost:5000 &
DOTNET_PID=$!

# Wait for both
wait $VITE_PID $DOTNET_PID
