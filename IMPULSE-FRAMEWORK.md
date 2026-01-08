# Impulse v2 - Framework Developer Guide

> How we build and maintain the Impulse framework

This document is for developers working on Impulse itself, not app developers using Impulse.

---

## 1. NuGet Package Structure

```
Impulse                     → Meta-package (references all below)
├── Impulse.Core            → Public API for app developers
├── Impulse.SourceGen       → Roslyn incremental generator
├── Impulse.MSBuild         → TypeScript extraction task
├── Impulse.Runtime         → Request handling middleware
└── Impulse.Templates       → dotnet new impulse template
```

### Impulse.Core

**What it provides:**
- `[Impulse]` attribute
- `.Impulse<TProps>()` extension method
- `AddImpulse()` / `UseImpulse()` extensions

```csharp
// Public API
[Impulse("/residents")]
public static class ResidentsRoutes { }

app.MapGet("/residents", Handler).Impulse<ResidentListProps>();
```

### Impulse.SourceGen

**Roslyn incremental source generator that produces:**

1. `Routes.g.cs` - C# route constants
2. `TypeScript.g.cs` - Embedded TS with markers

```csharp
// Generated: Routes.g.cs
public static partial class Routes
{
    public static class Residents
    {
        public const string List = "/residents";
        public const string Detail = "/residents/{id:int}";
    }
}
```

```csharp
// Generated: TypeScript.g.cs (embedded TS)
internal static class GeneratedTypeScript
{
    public const string Content = """
        /* IMPULSE:types.ts */
        export interface ResidentListProps { ... }
        /* IMPULSE:routeTree.ts */
        export const RoutePaths = { ... }
        """;
}
```

### Impulse.MSBuild

**MSBuild task that:**
1. Reads `TypeScript.g.cs` after build
2. Extracts content between `/* IMPULSE:filename */` markers
3. Writes to `generated/filename.ts`

```xml
<Target Name="ExtractImpulseTypeScript" AfterTargets="Build">
  <ImpulseExtractTypeScript
    SourceFile="$(IntermediateOutputPath)TypeScript.g.cs"
    OutputDirectory="$(ProjectDir)generated" />
</Target>
```

### Impulse.Runtime

**Middleware that handles:**
1. Content negotiation (`X-Impulse` header detection)
2. HTML rendering with `__IMPULSE_PROPS__` injection
3. Asset manifest reading for hashed URLs
4. Static file serving for production

### Impulse.Templates

**Template content for `dotnet new impulse`:**

```
content/
├── MyApp.csproj.template
├── Program.cs.template
├── src/
│   ├── main.tsx
│   ├── App.tsx
│   └── impulse/
│       └── hooks.ts          # useImpulseMutation implementation
├── vite.config.ts
├── package.json
└── tsconfig.json
```

---

## 2. Source Generator Implementation

### Input Analysis

```csharp
[Generator]
public class ImpulseGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all .Impulse<T>() calls
        var impulseRoutes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsImpulseCall,
                transform: ExtractRouteInfo)
            .Where(x => x is not null);

        // Find all FluentValidation validators
        var validators = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsValidatorClass,
                transform: ExtractValidationRules)
            .Where(x => x is not null);

        // Combine and generate
        context.RegisterSourceOutput(
            impulseRoutes.Collect().Combine(validators.Collect()),
            GenerateOutput);
    }
}
```

### Output Generation

```csharp
private void GenerateOutput(SourceProductionContext ctx, (Routes, Validators) input)
{
    var (routes, validators) = input;

    // C# routes
    ctx.AddSource("Routes.g.cs", GenerateCSharpRoutes(routes));

    // Embedded TypeScript
    var ts = new StringBuilder();
    ts.AppendLine(GenerateTypes(routes));
    ts.AppendLine(GenerateValidation(validators));
    ts.AppendLine(GenerateMutations(routes, validators));
    ts.AppendLine(GenerateRouteTree(routes));

    ctx.AddSource("TypeScript.g.cs", WrapAsEmbeddedTs(ts));
}
```

---

## 3. TypeScript Generation Patterns

### Type Mapping (C# → TypeScript)

| C# Type | TypeScript Type |
|---------|-----------------|
| `string` | `string` |
| `int`, `long` | `number` |
| `bool` | `boolean` |
| `DateTime` | `string` (ISO format) |
| `Guid` | `string` |
| `T?` | `T \| null` |
| `IReadOnlyList<T>` | `Array<T>` |
| `record` | `interface` |

### FluentValidation → Zod Mapping

| FluentValidation | Zod |
|------------------|-----|
| `.NotEmpty()` | `.min(1)` |
| `.MaximumLength(n)` | `.max(n)` |
| `.EmailAddress()` | `.email()` |
| `.GreaterThan(n)` | `.gt(n)` |
| `.Must(...)` | `.refine(...)` |

---

## 4. Dev Server Orchestration

`dotnet run` triggers:

```
1. MSBuild compiles C#
2. Source generator runs
3. MSBuild extracts TypeScript
4. Kestrel starts (:5000)
5. Impulse.Runtime spawns Vite child process (:5173)
6. Browser opens localhost:5173
```

**File watcher flow:**
```
.cs file change
    → dotnet watch rebuild
    → Source generator re-runs
    → MSBuild extracts new TS
    → Vite detects generated/*.ts change
    → HMR updates browser
```

---

## 5. Production Build Pipeline

`dotnet publish -c Release` triggers:

```
1. MSBuild compiles C# (Release)
2. Source generator runs
3. MSBuild extracts TypeScript
4. MSBuild runs: bun run build
5. Vite produces dist/assets/index-[hash].js
6. MSBuild copies dist/ to wwwroot/
7. Generates manifest.json (asset hash map)
8. Publishes single deployable folder
```

---

## 6. Testing Strategy

### Unit Tests (Impulse.SourceGen.Tests)

```csharp
[Fact]
public void Generates_TypeScript_Interface_From_Record()
{
    var source = """
        public record PersonProps(string Name, int Age);
        """;

    var result = GeneratorTestHelper.Run(source);

    result.GeneratedSources.Should().Contain(s =>
        s.Contains("export interface PersonProps") &&
        s.Contains("name: string") &&
        s.Contains("age: number"));
}
```

### Integration Tests (Impulse.Tests)

```csharp
[Fact]
public async Task Returns_HTML_Without_Impulse_Header()
{
    await using var app = new ImpulseTestApp();
    var response = await app.Client.GetAsync("/residents");

    response.ContentType.Should().Be("text/html");
    var html = await response.Content.ReadAsStringAsync();
    html.Should().Contain("__IMPULSE_PROPS__");
}

[Fact]
public async Task Returns_JSON_With_Impulse_Header()
{
    await using var app = new ImpulseTestApp();
    app.Client.DefaultRequestHeaders.Add("X-Impulse", "1");

    var response = await app.Client.GetAsync("/residents");

    response.ContentType.Should().Be("application/json");
}
```

### E2E Tests (Playwright)

```typescript
test('creates resident and sees in list', async ({ page }) => {
  await page.goto('/residents/new')
  await page.fill('[name="name"]', 'John Doe')
  await page.fill('[name="email"]', 'john@example.com')
  await page.click('button[type="submit"]')

  // After mutation, router.invalidate() refreshes list
  await expect(page.locator('text=John Doe')).toBeVisible()
})
```

---

## 7. Key Implementation Files

```
src/
├── Impulse.Core/
│   ├── ImpulseAttribute.cs
│   ├── ImpulseEndpointExtensions.cs
│   └── ImpulseServiceExtensions.cs
│
├── Impulse.SourceGen/
│   ├── ImpulseGenerator.cs
│   ├── Analyzers/
│   │   ├── RouteAnalyzer.cs
│   │   └── ValidatorAnalyzer.cs
│   └── Emitters/
│       ├── CSharpRouteEmitter.cs
│       ├── TypeScriptTypeEmitter.cs
│       ├── ZodSchemaEmitter.cs
│       └── RouteTreeEmitter.cs
│
├── Impulse.MSBuild/
│   └── ExtractTypeScriptTask.cs
│
├── Impulse.Runtime/
│   ├── ImpulseMiddleware.cs
│   ├── ContentNegotiator.cs
│   ├── HtmlRenderer.cs
│   └── AssetManifest.cs
│
└── Impulse.Templates/
    └── content/
        └── (template files)
```

---

## 8. Release Checklist

1. [ ] All tests pass
2. [ ] Version bumped in all .csproj files
3. [ ] CHANGELOG.md updated
4. [ ] Template content matches spec
5. [ ] NuGet packages build locally
6. [ ] Test `dotnet new impulse` with local packages
7. [ ] Tag release in git
8. [ ] Push to NuGet.org
