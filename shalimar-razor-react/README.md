# Shalimar Razor-React Compiler

Compile Blazor-style `.razor` files into React TSX components. **Razor as the single source of truth.**

## Architecture

```
.razor files → Shalimar Compiler → .tsx files → Vite → wwwroot/dist/
                     ↓
              @server components → RSC wire format → .NET serves
              @client components → Vite bundles → Client hydrates
```

## Key Concepts

| Directive | Behavior |
|-----------|----------|
| `@server` | Renders to RSC wire format on server - no React needed on client |
| `@client` | Bundled by Vite, hydrated on client |

## Project Structure

```
shalimar-razor-react/
├── src/
│   ├── Shalimar.Razor/           # Core library
│   │   ├── RazorParser.cs        # Parse .razor files
│   │   ├── TsxEmitter.cs         # Transform to TSX
│   │   ├── RscWireGenerator.cs   # RSC wire format for @server
│   │   ├── ViteManifest.cs       # Read Vite manifest for chunk IDs
│   │   └── ShalimarCompiler.cs   # Main compiler
│   ├── Shalimar.Razor.Cli/       # CLI tool
│   └── Shalimar.Razor.Tests/     # Unit tests
└── sample-app/                   # Sample application
    ├── Features/                 # Vertical slice features
    │   ├── Users/
    │   │   ├── UserProfile.razor # @server component
    │   │   ├── Avatar.razor      # @client component
    │   │   └── UserList.razor    # @server component
    │   └── Dashboard/
    │       ├── Dashboard.razor   # @client component
    │       ├── StatCard.razor    # @client component
    │       └── Chart.razor       # @client component
    ├── wwwroot/                  # Vite output
    ├── Program.cs                # .NET web host
    ├── vite.config.ts           # Vite configuration
    └── package.json             # npm dependencies
```

## Vertical Slice Features

TSX is generated **alongside** the `.razor` files - not in a separate `src/` folder:

```
Features/
  Users/
    UserProfile.razor     ← You write this
    UserProfile.tsx       ← Shalimar generates this (same folder)
```

## Usage

### 1. Build the Compiler

```bash
cd src
dotnet build
```

### 2. Initialize a Project

```bash
dotnet run --project Shalimar.Razor.Cli -- init -p ./my-app
```

### 3. Compile Razor Files

```bash
# One-time build
dotnet run --project Shalimar.Razor.Cli -- build -s ./Features -m ./shalimar-manifest.json

# Watch mode
dotnet run --project Shalimar.Razor.Cli -- watch -s ./Features
```

### 4. Bundle with Vite

```bash
cd sample-app
npm install
npm run build      # Production build to wwwroot/dist/
npm run dev        # Development with HMR
```

### 5. Run the .NET Server

```bash
cd sample-app
dotnet run
```

## API Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /api/rsc/{feature}/{component}` | Get RSC wire format for a @server component |
| `POST /api/rsc/{feature}/{component}` | Get RSC with props in body |
| `GET /api/components` | List all components |
| `POST /api/compile` | Compile Razor files on demand |

## RSC Wire Format

@server components return React Server Component wire format:

```
0:["$","UserProfile",null,{"Name":"John","Email":"john@example.com"}]
```

The client interprets this format and renders the component without needing React on the server.

## Razor Transformations

| Razor | TSX |
|-------|-----|
| `class="x"` | `className="x"` |
| `@Props.Name` | `{Name}` (destructured) |
| `@if (condition) { }` | `{(condition) && ( )}` |
| `@foreach (var x in items) { }` | `{items.map((x, index) => ( ))}` |
| `style="color: red"` | `style={{color: 'red'}}` |

## Sample Component

```razor
@server

<div className="user-profile">
  <h2>@Props.Name</h2>
  <p>@Props.Email</p>

  @if (Props.IsActive)
  {
    <span className="badge">Active</span>
  }
</div>

@code {
    [Parameter] public string Name { get; set; }
    [Parameter] public string Email { get; set; }
    [Parameter] public bool IsActive { get; set; }
}
```

Compiles to:

```tsx
import React from 'react';

export interface UserProfileProps {
  Name: string;
  Email: string;
  IsActive?: boolean;
}

export function UserProfile({ Name, Email, IsActive }: UserProfileProps) {
  return (
    <div className="user-profile">
      <h2>{Name}</h2>
      <p>{Email}</p>
      {(IsActive) && (
        <span className="badge">Active</span>
      )}
    </div>
  );
}
```

## Requirements

- .NET 8.0 SDK
- Node.js 18+
- npm or pnpm
