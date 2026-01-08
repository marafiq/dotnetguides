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
    └── manifest.json
    ↓
[Embed] → Assets embedded in DLL or served from wwwroot
    ↓
[Runtime] → AssetManifest reads manifest.json
    ↓
[Render] → <script src="/assets/index-a1b2c3.js">
```

### 4.3 Asset Manifest

```csharp
// Runtime reads Vite manifest
public class AssetManifest
{
    public string GetScriptPath() => _manifest["index.js"];
    public string GetStylePath() => _manifest["index.css"];
}

// In layout/middleware
app.MapGet("/", () => Results.Content($"""
    <!DOCTYPE html>
    <html>
      <head><link rel="stylesheet" href="{assets.GetStylePath()}"></head>
      <body>
        <div id="root"></div>
        <script src="{assets.GetScriptPath()}"></script>
      </body>
    </html>
    """, "text/html"));
```

### 4.4 Runtime Project

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

## 8. Open Questions

- [ ] Virtual file routes vs physical files for TanStack?
- [ ] Embed assets in DLL vs serve from wwwroot?
- [ ] Support for SSR/streaming?
- [ ] Authentication/authorization patterns?
- [ ] OpenAPI integration?
