# Impulse Framework

A server-driven UI framework bridging .NET 10 backend with React 19 frontend, featuring automatic TypeScript type generation, FluentValidation → Zod schema generation, and TanStack Router integration.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        Impulse Framework                         │
├─────────────────────────────────────────────────────────────────┤
│  .NET 10 Backend                    │  React 19 Frontend        │
│  ─────────────────                  │  ──────────────────       │
│  • Impulse.Core                     │  • @impulse/react         │
│  • Impulse.CodeGen                  │  • TanStack Router        │
│  • Impulse.Validation               │  • Generated Types        │
│  • Sample Server                    │  • Sample App             │
└─────────────────────────────────────────────────────────────────┘
```

## Prerequisites

- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Node.js 22+** with npm
- **tsgo** (optional) - `npm install -g @typescript/native-preview` for 10x faster TypeScript builds

## Quick Start

### 1. Install .NET 10 SDK

```bash
# Using the install script
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0

# Add to PATH
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
```

### 2. Build the Solution

```bash
# Restore and build all projects
dotnet restore Impulse.slnx
dotnet build Impulse.slnx
```

### 3. Run the Sample Server

```bash
cd src/Impulse.Sample.Server
dotnet run
# Server runs on http://localhost:5000
```

### 4. Run the Sample React App

```bash
cd client/sample-app
npm install
npm run dev
# App runs on http://localhost:3000
```

## Project Structure

```
├── src/
│   ├── Impulse.Core/           # Core server-side library
│   ├── Impulse.CodeGen/        # C# → TypeScript type generation
│   ├── Impulse.Validation/     # FluentValidation → Zod generation
│   └── Impulse.Sample.Server/  # Sample API demonstrating usage
├── client/
│   ├── impulse-react/          # React runtime library
│   └── sample-app/             # Sample React application
├── tests/
│   ├── Impulse.Core.Tests/
│   ├── Impulse.CodeGen.Tests/
│   └── Impulse.Validation.Tests/
└── Impulse.slnx                # .NET 10 solution file
```

## Running Tests

### .NET Tests

```bash
# Run all .NET tests
dotnet test Impulse.slnx

# Run specific test project
dotnet test tests/Impulse.Core.Tests

# Run with coverage
dotnet test Impulse.slnx --collect:"XPlat Code Coverage"

# Run tests with detailed output
dotnet test Impulse.slnx --logger "console;verbosity=detailed"
```

### React Tests

```bash
# Run impulse-react tests
cd client/impulse-react
npm install
npm test

# Run with watch mode
npm run test:watch

# Run with coverage
npm test -- --coverage
```

### Sample App Tests

```bash
cd client/sample-app
npm install
npm test
```

## Component Documentation

### Impulse.Core

Core server-side library providing:

- **Component Registration** - `AsComponent<TProps>()` extension method
- **Mutation Registration** - `AsMutation<TRequest, TResponse>()` extension method
- **Deferred/Lazy Loading** - `.Deferred<T>()` and `.Lazy<T>()` methods
- **Result Builders** - `Impulse.Ok()`, `Impulse.Created()`, etc.
- **Shell Rendering** - `ImpulseShellRenderer` for initial HTML

```csharp
// Example endpoint registration
app.MapGet("/residents/{id:int}", handler)
   .AsComponent<ResidentDetailProps>("./Residents/ResidentDetail")
   .Deferred<MedicationsProps>("medications", "/residents/{id}/medications")
   .Lazy<DocumentsProps>("documents", "/residents/{id}/documents");
```

### Impulse.CodeGen

Generates TypeScript interfaces from C# types:

| C# Type | TypeScript Type |
|---------|-----------------|
| `int`, `long`, `double` | `number` |
| `string`, `Guid` | `string` |
| `bool` | `boolean` |
| `DateTime`, `DateOnly` | `string` |
| `T?` | `T \| null` |
| `IReadOnlyList<T>` | `readonly T[]` |
| `Dictionary<K,V>` | `Record<K, V>` |
| `enum` | String literal union |

### Impulse.Validation

Generates Zod schemas from FluentValidation:

| FluentValidation | Zod |
|------------------|-----|
| `NotEmpty()` | `.min(1)` |
| `MaximumLength(n)` | `.max(n)` |
| `EmailAddress()` | `.email()` |
| `GreaterThan(n)` | `.gt(n)` |
| `Matches(regex)` | `.regex()` |
| `IsInEnum<T>()` | `.enum([...])` |

### @impulse/react

React runtime library providing:

- **`mount()`** - Initialize app from shell payload
- **`useDeferred<T>(key)`** - Auto-fetch after hydration
- **`useLazy<T>(key)`** - Fetch on demand
- **`useImpulseContext<T>()`** - Access app context
- **`useMutation<TRequest, TResponse>()`** - Execute mutations

```tsx
// Example component
function ResidentDetail() {
  const context = useImpulseContext<AppContext>();
  const medications = useDeferred<MedicationsProps>('medications', deferredUrls);
  const [documents, loadDocuments] = useLazy<DocumentsProps>('documents', lazyUrls);

  return (
    <div>
      {medications.status === 'success' && <MedicationList data={medications.data} />}
      <button onClick={loadDocuments}>Load Documents</button>
    </div>
  );
}
```

## Development Workflow

### Using tsgo for Faster Builds

[tsgo](https://devblogs.microsoft.com/typescript/typescript-native-port/) is Microsoft's Go-based TypeScript compiler offering ~10x faster builds:

```bash
# Install tsgo
npm install -g @typescript/native-preview

# Use in projects
tsgo --project tsconfig.json

# Or via npm scripts (already configured)
npm run build  # Uses tsgo
npm run build:tsc  # Falls back to tsc
```

### Code Generation

Generate TypeScript types from your C# models:

```bash
# From the sample server project
cd src/Impulse.Sample.Server
dotnet run -- --generate-types --output ../../client/sample-app/src/generated
```

### Live Development

Terminal 1 - Backend:
```bash
cd src/Impulse.Sample.Server
dotnet watch run
```

Terminal 2 - Frontend:
```bash
cd client/sample-app
npm run dev
```

## API Reference

### Server Shell Format

```html
<div id="app" data-impulse='{
  "url": "/residents/1",
  "version": "abc123",
  "props": { "id": 1, "name": "Margaret Chen" },
  "context": { "user": {...}, "permissions": [...] },
  "deferred": { "medications": "/residents/1/medications" },
  "lazy": { "documents": "/residents/1/documents" }
}'></div>
```

### Navigation Headers

- Request: `X-Impulse: true`, `X-Impulse-Version: <version>`
- Response (version mismatch): `X-Impulse-Reload: true`

### Validation Error Format

```json
{
  "errors": {
    "name": ["Name is required", "Name already exists"],
    "dosage": ["Dosage is required"]
  }
}
```

## Technologies

- **.NET 10** - Latest LTS release with C# 14
- **React 19** - Latest with Server Components support
- **TanStack Router** - Type-safe routing
- **tsgo** - 10x faster TypeScript compilation
- **FluentValidation** - Server-side validation
- **Zod** - Client-side schema validation
- **Vitest** - Fast test runner for React
- **xUnit** - .NET test framework

## License

MIT
