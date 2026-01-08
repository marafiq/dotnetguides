# Impulse v2 - Complete Roadmap

## Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Developer Experience                      │
│  dotnet new impulse → write code → dotnet run → it works   │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────┬──────────────┬──────────────┬────────────────┐
│ 1. Core      │ 2. Source    │ 3. MSBuild   │ 4. Runtime     │
│ Abstractions │ Generator    │ Task         │ & Assets       │
└──────────────┴──────────────┴──────────────┴────────────────┘
```

---

## 1. Core Abstractions (Developer API)

**What developers write:**

### 1.1 Endpoint Definition

```csharp
// Program.cs - clean, no magic
app.MapGet("/residents", ResidentsHandler.List)
   .Impulse<ResidentListProps>("./Residents/List");

app.MapGet("/residents/{id:int}", ResidentsHandler.Detail)
   .Impulse<ResidentDetailProps>("./Residents/Detail")
   .Deferred<MedicationList>("medications", "/residents/{id}/meds");

app.MapPost("/residents", ResidentsHandler.Create);
```

### 1.2 Handler Pattern

```csharp
// Handlers/ResidentsHandler.cs
public static class ResidentsHandler
{
    public static async Task<Results<Ok<ResidentListProps>, NotFound>> List(
        IResidentService service, CancellationToken ct)
    {
        var residents = await service.GetAllAsync(ct);
        return TypedResults.Ok(new ResidentListProps(residents));
    }
}
```

### 1.3 Mutation with DI

```csharp
// Handlers/CreateResidentHandler.cs
[ImpulseHandler]
public class CreateResidentHandler(IResidentService service, IValidator<CreateRequest> validator)
{
    public async Task<Results<Ok<CreateResponse>, ValidationProblem>> Handle(
        CreateRequest request, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid)
            return TypedResults.ValidationProblem(result.ToDictionary());

        var id = await service.CreateAsync(request, ct);
        return TypedResults.Ok(new CreateResponse(id));
    }
}
```

### 1.4 Validation

```csharp
// Validators/CreateResidentValidator.cs
public class CreateResidentValidator : AbstractValidator<CreateRequest>
{
    public CreateResidentValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

### 1.5 React Component (user writes)

```tsx
// Features/Residents/List.tsx
import { useLoaderData } from '@tanstack/react-router'
import type { ResidentListProps } from '@/generated/types'

export default function ResidentList() {
  const { residents } = useLoaderData<ResidentListProps>()
  return <ul>{residents.map(r => <li key={r.id}>{r.name}</li>)}</ul>
}
```

### 1.6 Form with Validation (user writes)

```tsx
// Features/Residents/Create.tsx
import { useCreateResident } from '@/generated/forms'

export default function CreateResident() {
  const { register, errors, submit, isSubmitting } = useCreateResident()

  return (
    <form onSubmit={submit}>
      <input {...register('name')} />
      {errors.name && <span>{errors.name}</span>}
      <button disabled={isSubmitting}>Create</button>
    </form>
  )
}
```

---

## 2. Source Generator

**Runs at compile time via Roslyn.**

### 2.1 What It Analyzes

```
Input: C# Source Code
  ├── MapGet/Post/Put/Delete invocations
  ├── .Impulse<T>() extensions
  ├── .Deferred<T>() / .Lazy<T>() calls
  ├── [ImpulseHandler] classes
  ├── AbstractValidator<T> implementations
  └── TypedResults return types
```

### 2.2 What It Generates (C#)

```
Output: Generated/*.g.cs
  ├── Routes.g.cs           → Route constants + path builders
  ├── Handlers.g.cs         → MapImpulseHandlers() extension
  └── ImpulseModel.g.cs     → Serialized model for MSBuild task
```

### 2.3 Source Generator Project

```
src/Impulse.SourceGenerator/
├── ImpulseGenerator.cs           # Main incremental generator
├── Analyzers/
│   ├── EndpointAnalyzer.cs       # Find MapGet/Post calls
│   ├── HandlerAnalyzer.cs        # Find [ImpulseHandler]
│   ├── ValidatorAnalyzer.cs      # Find AbstractValidator<T>
│   └── TypeAnalyzer.cs           # Extract TypeModel from symbols
├── Emitters/
│   ├── RoutesEmitter.cs          # Emit Routes.g.cs
│   └── HandlersEmitter.cs        # Emit handler registration
└── Model/
    └── ImpulseModel.cs           # Shared model definition
```

---

## 3. MSBuild Task

**Runs after compile, generates TypeScript.**

### 3.1 Pipeline

```
dotnet build
    ↓
[Compile] → Source Generator runs → *.g.cs + model.json
    ↓
[AfterBuild] → MSBuild Task runs → reads model.json
    ↓
[Generate TS] → types/ routes/ validation/ forms/
    ↓
[Vite/esbuild] → bundle for dev/prod
```

### 3.2 MSBuild Integration

```xml
<!-- Impulse.Sdk.targets -->
<Target Name="ImpulseGenerateTypeScript" AfterTargets="Build">
  <ImpulseGenerateTask
    ModelPath="$(IntermediateOutputPath)impulse-model.json"
    OutputDir="$(MSBuildProjectDirectory)/ClientApp/generated"
    TreeShakeable="true" />
</Target>
```

### 3.3 Task Project

```
src/Impulse.MSBuild/
├── ImpulseGenerateTask.cs        # MSBuild task entry
├── Generators/
│   ├── TypesGenerator.cs         # → types/*.ts
│   ├── RoutesGenerator.cs        # → routes/*.tsx (TanStack)
│   ├── ZodGenerator.cs           # → validation/*.ts
│   └── FormGenerator.cs          # → forms/*.ts
└── Impulse.MSBuild.targets       # MSBuild integration
```

---

## 4. Runtime & Assets

### 4.1 Development (HMR)

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Vite      │────▶│  .NET Kestrel│────▶│   Browser   │
│   (HMR)     │     │  (API proxy) │     │   (React)   │
└─────────────┘     └─────────────┘     └─────────────┘
     :5173              :5000
```

**Dev experience:**
- `dotnet run` starts both Kestrel + Vite
- React HMR via Vite
- API calls proxy to Kestrel
- File changes → regenerate TS → HMR updates

### 4.2 Production (Hashed Assets)

```
dotnet publish
    ↓
[Vite build] → dist/assets/
    ├── index-[hash].js
    ├── index-[hash].css
    └── .vite/manifest.json
    ↓
[Copy to wwwroot] → wwwroot/assets/
    ↓
[Runtime] → AssetManifest reads manifest.json
    ↓
[Serve] → Static files middleware serves hashed assets
```

### 4.3 Production Asset Serving Options

**Option A: wwwroot (Recommended)**
```
wwwroot/
├── assets/
│   ├── index-a1b2c3d4.js
│   ├── index-e5f6g7h8.css
│   └── .vite/
│       └── manifest.json
```

```csharp
// Program.cs
app.UseStaticFiles(); // Serves from wwwroot

// Assets have content hash → infinite cache
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name.Contains("-"))  // Hashed filename
            ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    }
});
```

**Option B: Embedded Resources**
```csharp
// Assets embedded in DLL at publish time
[assembly: EmbeddedResource("assets/index-a1b2c3.js")]

// Served via middleware
app.UseImpulseAssets(); // Reads from embedded resources
```

### 4.4 Asset Manifest

```json
// .vite/manifest.json (generated by Vite)
{
  "index.html": {
    "file": "assets/index-a1b2c3d4.js",
    "css": ["assets/index-e5f6g7h8.css"],
    "isEntry": true
  }
}
```

```csharp
// AssetManifest.cs
public class AssetManifest
{
    private readonly Dictionary<string, ManifestEntry> _entries;

    public AssetManifest(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.WebRootPath, "assets", ".vite", "manifest.json");
        _entries = JsonSerializer.Deserialize<...>(File.ReadAllText(path));
    }

    public string GetScript() => $"/assets/{_entries["index.html"].File}";
    public IEnumerable<string> GetStyles() => _entries["index.html"].Css
        .Select(css => $"/assets/{css}");
}
```

### 4.5 HTML Rendering

```csharp
// Inject hashed asset paths into HTML
public class ImpulseHtmlMiddleware(AssetManifest assets)
{
    public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
    {
        if (ctx.Request.Path == "/" || !Path.HasExtension(ctx.Request.Path))
        {
            var html = $"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    {string.Join("\n", assets.GetStyles().Select(s =>
                        $"<link rel=\"stylesheet\" href=\"{s}\">"))}
                </head>
                <body>
                    <div id="root"></div>
                    <script type="module" src="{assets.GetScript()}"></script>
                </body>
                </html>
                """;
            ctx.Response.ContentType = "text/html";
            await ctx.Response.WriteAsync(html);
            return;
        }
        await next(ctx);
    }
}
```

### 4.6 Publish Pipeline

```xml
<!-- In .csproj -->
<Target Name="BuildClientApp" BeforeTargets="Publish">
  <Exec Command="npm run build" WorkingDirectory="ClientApp" />
</Target>

<Target Name="CopyClientAssets" AfterTargets="BuildClientApp">
  <ItemGroup>
    <ClientAssets Include="ClientApp/dist/**/*" />
  </ItemGroup>
  <Copy SourceFiles="@(ClientAssets)"
        DestinationFolder="$(PublishDir)wwwroot/%(RecursiveDir)" />
</Target>
```

**Resulting publish output:**
```
publish/
├── MyApp.dll
├── MyApp.deps.json
├── wwwroot/
│   └── assets/
│       ├── index-a1b2c3d4.js      # Hashed, cacheable forever
│       ├── index-e5f6g7h8.css
│       ├── chunk-vendor-xyz123.js  # Code-split chunks
│       └── .vite/
│           └── manifest.json
└── appsettings.json
```

### 4.7 Runtime Project

```
src/Impulse.Runtime/
├── AssetManifest.cs              # Read Vite manifest
├── ImpulseMiddleware.cs          # Serve SPA, handle fallback
├── DevProxy.cs                   # Proxy to Vite in dev mode
└── Extensions/
    └── ImpulseExtensions.cs      # app.UseImpulse()
```

---

## 5. dotnet new impulse

### 5.1 Template Experience

```bash
dotnet new impulse -n MyApp
cd MyApp
dotnet run
# Browser opens → working app with example resident CRUD
```

### 5.2 Generated Structure

```
MyApp/
├── MyApp.csproj
├── Program.cs                    # Minimal API setup
├── appsettings.json
├── Handlers/
│   └── ResidentsHandler.cs       # Example handler
├── Validators/
│   └── CreateResidentValidator.cs
├── Models/
│   ├── Resident.cs
│   └── Requests.cs
├── ClientApp/
│   ├── package.json
│   ├── vite.config.ts
│   ├── tsconfig.json
│   ├── index.html
│   ├── src/
│   │   ├── main.tsx
│   │   ├── App.tsx
│   │   └── Features/
│   │       └── Residents/
│   │           ├── List.tsx
│   │           ├── Detail.tsx
│   │           └── Create.tsx
│   └── generated/                # Auto-generated, gitignored
│       ├── types/
│       ├── routes/
│       ├── validation/
│       └── forms/
└── Generated/                    # C# generated, gitignored
    ├── Routes.g.cs
    └── Handlers.g.cs
```

### 5.3 Template Project

```
templates/
└── Impulse.Template/
    ├── template.json             # dotnet new metadata
    ├── MyApp.csproj
    ├── Program.cs
    └── ... (all template files)
```

---

## 6. NuGet Packages

```
Impulse.Core           → Core abstractions (ImpulseAttribute, extensions)
Impulse.SourceGenerator → Roslyn source generator (Routes.g.cs, Handlers.g.cs)
Impulse.MSBuild        → TypeScript generation task
Impulse.Runtime        → Asset manifest, middleware, dev proxy
Impulse.Templates      → dotnet new templates
```

**Single package for simplicity:**
```xml
<PackageReference Include="Impulse" Version="1.0.0" />
```
*Meta-package that references all above.*

---

## 7. Implementation Order

| Phase | Component | Deliverable |
|-------|-----------|-------------|
| 1 | Core Abstractions | `ImpulseAttribute`, extension methods |
| 2 | TypeScript Generators | Pure functions, tested |
| 3 | Source Generator | Routes.g.cs, model.json |
| 4 | MSBuild Task | Generate TS on build |
| 5 | Runtime | Asset manifest, middleware |
| 6 | Dev Experience | Vite integration, HMR |
| 7 | Template | dotnet new impulse |
| 8 | Package | NuGet publish |

---

## 8. Developer Experience & Local Dev Loop

### 8.1 Repo Structure (Vertical Slices + Colocated React)

```
impulse/
├── src/
│   ├── Impulse.Core/                 # Core abstractions
│   ├── Impulse.SourceGenerator/      # Roslyn generator
│   ├── Impulse.MSBuild/              # TS generation task
│   └── Impulse.Runtime/              # Asset serving, middleware
├── samples/
│   └── Impulse.Sample/               # Dogfooding app
│       ├── Features/                 # Vertical slices
│       │   ├── Residents/
│       │   │   ├── ResidentsHandler.cs
│       │   │   ├── CreateResidentValidator.cs
│       │   │   ├── Models.cs
│       │   │   └── Components/       # Colocated React
│       │   │       ├── List.tsx
│       │   │       ├── Detail.tsx
│       │   │       └── Create.tsx
│       │   └── Medications/
│       │       ├── MedicationsHandler.cs
│       │       └── Components/
│       │           └── List.tsx
│       ├── Program.cs
│       └── ClientApp/
│           ├── src/
│           │   └── main.tsx          # Entry point only
│           └── generated/            # Auto-generated
├── tests/
│   └── Impulse.Tests/
├── nuget.config                      # Local feed
└── Directory.Build.props             # Shared settings
```

### 8.2 Local NuGet Package Dev Loop

**nuget.config:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="./artifacts/packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

**Directory.Build.props:**
```xml
<Project>
  <PropertyGroup>
    <Version>0.0.1-local</Version>
    <PackageOutputPath>$(MSBuildThisFileDirectory)artifacts/packages</PackageOutputPath>
  </PropertyGroup>
</Project>
```

**Dev workflow script (dev.ps1 / dev.sh):**
```bash
#!/bin/bash
set -e

# 1. Pack all Impulse packages
dotnet pack src/Impulse.Core -o artifacts/packages
dotnet pack src/Impulse.SourceGenerator -o artifacts/packages
dotnet pack src/Impulse.MSBuild -o artifacts/packages
dotnet pack src/Impulse.Runtime -o artifacts/packages

# 2. Clear NuGet cache for local packages
dotnet nuget locals all --clear

# 3. Restore and build sample
dotnet restore samples/Impulse.Sample
dotnet build samples/Impulse.Sample

# 4. Run sample with hot reload
dotnet watch run --project samples/Impulse.Sample
```

### 8.3 Fast Iteration Targets

**In Impulse.Sample.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <!-- Reference local projects during dev, packages in release -->
  <ItemGroup Condition="'$(Configuration)' == 'Debug'">
    <ProjectReference Include="..\..\src\Impulse.Core\Impulse.Core.csproj" />
    <ProjectReference Include="..\..\src\Impulse.Runtime\Impulse.Runtime.csproj" />
  </ItemGroup>

  <ItemGroup Condition="'$(Configuration)' == 'Release'">
    <PackageReference Include="Impulse" Version="*" />
  </ItemGroup>

  <!-- Source generator always via project reference for debugging -->
  <ItemGroup>
    <ProjectReference Include="..\..\src\Impulse.SourceGenerator\Impulse.SourceGenerator.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

### 8.4 Vertical Slice File Discovery

**ComponentPath convention:**
```csharp
// In handler registration
app.MapGet("/residents", ResidentsHandler.List)
   .Impulse<ResidentListProps>();  // No path needed!

// Generator discovers component by convention:
// 1. Handler is in Features/Residents/ResidentsHandler.cs
// 2. Component is Features/Residents/Components/List.tsx
// 3. Route name "ResidentsList" derived from handler method

// Or explicit override:
app.MapGet("/residents/{id:int}", ResidentsHandler.Detail)
   .Impulse<ResidentDetailProps>("./Detail");  // Relative to slice
```

**Generated route uses slice-relative paths:**
```typescript
// routes/residents/$id.tsx
export const Route = createFileRoute('/residents/$id')({
  component: () => import('@/Features/Residents/Components/Detail'),
})
```

### 8.5 Watch Mode for Full Stack

**Makefile / justfile:**
```makefile
.PHONY: dev pack restore

# Full dev loop
dev: pack restore
	@echo "Starting dev servers..."
	@trap 'kill 0' EXIT; \
	dotnet watch run --project samples/Impulse.Sample & \
	cd samples/Impulse.Sample/ClientApp && npm run dev & \
	wait

# Pack local packages
pack:
	@rm -rf artifacts/packages/*.nupkg
	@dotnet pack src/Impulse.Core -o artifacts/packages -c Debug
	@dotnet pack src/Impulse.SourceGenerator -o artifacts/packages -c Debug
	@dotnet pack src/Impulse.MSBuild -o artifacts/packages -c Debug
	@dotnet pack src/Impulse.Runtime -o artifacts/packages -c Debug

# Force restore from local
restore:
	@dotnet nuget locals all --clear
	@dotnet restore samples/Impulse.Sample --force
```

### 8.6 Source Generator Debugging

**launchSettings.json for generator debugging:**
```json
{
  "profiles": {
    "Debug Generator": {
      "commandName": "DebugRoslynComponent",
      "targetProject": "../samples/Impulse.Sample/Impulse.Sample.csproj"
    }
  }
}
```

**Or attach debugger manually:**
```csharp
// In ImpulseGenerator.cs
public void Initialize(IncrementalGeneratorInitializationContext context)
{
#if DEBUG
    if (!System.Diagnostics.Debugger.IsAttached)
        System.Diagnostics.Debugger.Launch();
#endif
    // ...
}
```

### 8.7 Colocated Component Imports

**vite.config.ts alias:**
```typescript
export default defineConfig({
  resolve: {
    alias: {
      '@': path.resolve(__dirname, '../'),  // Points to project root
      '@features': path.resolve(__dirname, '../Features'),
    },
  },
})
```

**tsconfig.json paths:**
```json
{
  "compilerOptions": {
    "baseUrl": ".",
    "paths": {
      "@/*": ["../*"],
      "@features/*": ["../Features/*"]
    }
  }
}
```

**Usage in components:**
```tsx
// Features/Residents/Components/Detail.tsx
import type { ResidentDetailProps } from '@/ClientApp/generated/types'
import { useUpdateResident } from '@/ClientApp/generated/forms'

// Or with shorter alias:
import type { ResidentDetailProps } from '@generated/types'
```

---

## 9. Unified "Just Works" Experience

**Goal:** Same experience for end users AND Impulse developers.

### 9.1 End User Experience

```bash
dotnet new impulse -n MyApp
cd MyApp
dotnet run
# → Browser opens at localhost:5000
# → Working CRUD app with React + .NET
# → Edit C# → hot reload
# → Edit .tsx → HMR
# → Everything just works
```

### 9.2 Impulse Developer Experience (This Repo)

```bash
git clone https://github.com/user/impulse
cd impulse
./dev.sh    # or: make dev
# → Same experience as end user
# → Uses local packages instead of NuGet.org
```

### 9.3 The Magic: MSBuild Targets

**Impulse.Sdk.targets** (ships with package):
```xml
<Project>
  <!-- Auto npm install if node_modules missing -->
  <Target Name="ImpulseRestoreNpm"
          BeforeTargets="Build"
          Condition="!Exists('$(MSBuildProjectDirectory)/ClientApp/node_modules')">
    <Exec Command="npm install" WorkingDirectory="$(MSBuildProjectDirectory)/ClientApp" />
  </Target>

  <!-- Generate TypeScript on every build -->
  <Target Name="ImpulseGenerateTS" AfterTargets="Build">
    <ImpulseGenerateTask
      ModelPath="$(IntermediateOutputPath)impulse-model.json"
      OutputDir="$(MSBuildProjectDirectory)/ClientApp/generated" />
  </Target>

  <!-- Start Vite dev server with dotnet run -->
  <Target Name="ImpulseStartVite"
          BeforeTargets="Run"
          Condition="'$(ASPNETCORE_ENVIRONMENT)' == 'Development'">
    <Exec Command="npm run dev"
          WorkingDirectory="$(MSBuildProjectDirectory)/ClientApp"
          ContinueOnError="true" />
  </Target>

  <!-- Build client for publish -->
  <Target Name="ImpulseBuildClient" BeforeTargets="Publish">
    <Exec Command="npm run build" WorkingDirectory="$(MSBuildProjectDirectory)/ClientApp" />
  </Target>

  <Target Name="ImpulseCopyAssets" AfterTargets="ImpulseBuildClient">
    <ItemGroup>
      <ClientAssets Include="$(MSBuildProjectDirectory)/ClientApp/dist/**/*" />
    </ItemGroup>
    <Copy SourceFiles="@(ClientAssets)"
          DestinationFolder="$(PublishDir)wwwroot/%(RecursiveDir)" />
  </Target>
</Project>
```

### 9.4 Single Line Setup

**Program.cs:**
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddImpulse();  // Registers services, validators, handlers

var app = builder.Build();

app.UseImpulse();  // Static files, SPA fallback, dev proxy

// Your endpoints (source generator adds Routes.g.cs)
app.MapResidentsEndpoints();  // Generated extension method

app.Run();
```

**What `AddImpulse()` does:**
```csharp
public static IServiceCollection AddImpulse(this WebApplicationBuilder builder)
{
    // Auto-discover and register validators
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // Auto-discover and register [ImpulseHandler] classes
    builder.Services.AddImpulseHandlers();

    // Asset manifest for production
    builder.Services.AddSingleton<AssetManifest>();

    return builder.Services;
}
```

**What `UseImpulse()` does:**
```csharp
public static IApplicationBuilder UseImpulse(this WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        // Proxy non-API requests to Vite dev server
        app.UseImpulseDevProxy("http://localhost:5173");
    }
    else
    {
        // Serve static files with caching
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                if (ctx.File.Name.Contains("-"))
                    ctx.Context.Response.Headers.CacheControl =
                        "public, max-age=31536000, immutable";
            }
        });
    }

    // SPA fallback - serve index.html for client routes
    app.UseImpulseSpaFallback();

    return app;
}
```

### 9.5 Zero Config vite.config.ts

**Template includes pre-configured Vite:**
```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { TanStackRouterVite } from '@tanstack/router-plugin/vite'
import path from 'path'

export default defineConfig({
  plugins: [
    TanStackRouterVite({
      routesDirectory: './generated/routes',
      generatedRouteTree: './generated/routeTree.gen.ts',
    }),
    react(),
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, '../'),
      '@generated': path.resolve(__dirname, './generated'),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5000',  // API calls go to .NET
    },
  },
  build: {
    outDir: 'dist',
    manifest: true,
  },
})
```

### 9.6 This Repo: Identical Experience

**Directory.Build.props** (at repo root):
```xml
<Project>
  <PropertyGroup>
    <!-- Use local packages, not NuGet.org -->
    <RestorePackagesPath>$(MSBuildThisFileDirectory)artifacts/packages</RestorePackagesPath>
    <Version>0.0.1-local</Version>
  </PropertyGroup>

  <!-- Import Impulse targets from local source -->
  <Import Project="$(MSBuildThisFileDirectory)src/Impulse.MSBuild/Impulse.Sdk.targets"
          Condition="Exists('...')" />
</Project>
```

**dev.sh:**
```bash
#!/bin/bash
set -e

echo "🔧 Building Impulse packages..."
dotnet build src/Impulse.sln

echo "🚀 Starting sample app..."
cd samples/Impulse.Sample
dotnet run

# That's it. Same as end user experience.
# MSBuild targets handle:
# - npm install (if needed)
# - TypeScript generation
# - Vite dev server
# - Hot reload
```

### 9.7 First Run Flow

```
dotnet new impulse -n MyApp
         │
         ▼
┌─────────────────────────────────┐
│ Template creates:               │
│ - MyApp.csproj (with Impulse)  │
│ - Program.cs (AddImpulse/Use)  │
│ - ClientApp/ (package.json)    │
│ - Features/Residents/ (example)│
└─────────────────────────────────┘
         │
         ▼
      dotnet run
         │
         ▼
┌─────────────────────────────────┐
│ MSBuild targets:                │
│ 1. npm install (auto)          │
│ 2. Source generator runs       │
│ 3. TS generator runs           │
│ 4. Vite dev server starts      │
│ 5. Kestrel starts              │
└─────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────┐
│ Browser opens:                  │
│ - localhost:5000               │
│ - Working app                   │
│ - Edit anything → live reload  │
└─────────────────────────────────┘
```

### 9.8 Key Insight: No Manual Steps

**What users DON'T need to do:**
- ❌ Run `npm install` manually
- ❌ Start Vite in separate terminal
- ❌ Run code generators
- ❌ Configure proxy
- ❌ Set up hot reload
- ❌ Configure TypeScript paths
- ❌ Worry about hashed assets

**Everything is automated by MSBuild + runtime.**

---

## 10. Open Questions

- [ ] Virtual file routes vs physical files for TanStack?
- [ ] Embed assets in DLL vs serve from wwwroot?
- [ ] Support for SSR/streaming?
- [ ] Authentication/authorization patterns?
- [ ] OpenAPI integration?
