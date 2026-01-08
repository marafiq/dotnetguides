# Impulse v2 - Framework Developer Guide

> How we build and maintain the Impulse framework

This document is for developers working on Impulse itself.

---

## Core Principle: Test-Driven Development

**Every function starts with a failing test.**

### Tests Do Two Things

1. **Verify behavior** - confirm code does what it should
2. **Improve code quality** - writing tests FIRST forces better design

| Hard to test | Easy to test (better design) |
|--------------|------------------------------|
| Function does 5 things | Function does 1 thing |
| Hidden dependencies | Explicit dependencies (DI) |
| Tightly coupled | Loosely coupled |

**If it's hard to test, the design is wrong.** The test is telling you something.

### Red-Green-Refactor

```
1. Write test that defines the contract (RED)
2. See it fail - confirms test works
3. Write minimal code to pass (GREEN)
4. Refactor with confidence - tests guard correctness
```

Tests are the specification. If you can't write a test first, you don't understand the requirement.

---

## 1. TDD by Example: Type Emitter

We need a function that converts C# `record` to TypeScript `interface`.

### Step 1: Write the Test (RED)

```csharp
public class TypeScriptTypeEmitterTests
{
    [Fact]
    public void Emits_Interface_From_Record()
    {
        // Arrange - define the contract
        var record = new RecordInfo(
            Name: "PersonProps",
            Properties: [
                new("Name", "string"),
                new("Age", "int")
            ]);

        // Act
        var result = TypeScriptTypeEmitter.Emit(record);

        // Assert - exact expected output
        result.Should().Be("""
            export interface PersonProps {
              name: string
              age: number
            }
            """);
    }

    [Fact]
    public void Emits_Nullable_As_Union()
    {
        var record = new RecordInfo(
            Name: "OptionalProps",
            Properties: [new("Value", "int?")]);

        var result = TypeScriptTypeEmitter.Emit(record);

        result.Should().Contain("value: number | null");
    }

    [Fact]
    public void Emits_Array_From_IReadOnlyList()
    {
        var record = new RecordInfo(
            Name: "ListProps",
            Properties: [new("Items", "IReadOnlyList<string>")]);

        var result = TypeScriptTypeEmitter.Emit(record);

        result.Should().Contain("items: Array<string>");
    }
}
```

### Step 2: Run Tests - They Fail

```
FAILED: TypeScriptTypeEmitter does not exist
```

Good. The test defines what we need to build.

### Step 3: Minimal Implementation (GREEN)

```csharp
public static class TypeScriptTypeEmitter
{
    public static string Emit(RecordInfo record)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"export interface {record.Name} {{");

        foreach (var prop in record.Properties)
        {
            var tsType = MapType(prop.Type);
            var tsName = ToCamelCase(prop.Name);
            sb.AppendLine($"  {tsName}: {tsType}");
        }

        sb.AppendLine("}");
        return sb.ToString().Trim();
    }

    private static string MapType(string csharpType) => csharpType switch
    {
        "string" => "string",
        "int" or "long" => "number",
        "bool" => "boolean",
        var t when t.EndsWith("?") => $"{MapType(t[..^1])} | null",
        var t when t.StartsWith("IReadOnlyList<") =>
            $"Array<{MapType(t[14..^1])}>",
        _ => "unknown"
    };

    private static string ToCamelCase(string s) =>
        char.ToLowerInvariant(s[0]) + s[1..];
}
```

### Step 4: Run Tests - They Pass

```
PASSED: 3/3 tests
```

### Step 5: Refactor

Now we can safely refactor (extract methods, improve naming) because tests guard correctness.

---

## 2. TDD: Zod Schema Emitter

### Tests First

```csharp
public class ZodSchemaEmitterTests
{
    [Fact]
    public void Emits_NotEmpty_As_Min1()
    {
        var validator = new ValidatorInfo(
            TypeName: "CreatePersonRequest",
            Rules: [new("Name", "NotEmpty")]);

        var result = ZodSchemaEmitter.Emit(validator);

        result.Should().Contain("name: z.string().min(1)");
    }

    [Fact]
    public void Emits_MaxLength()
    {
        var validator = new ValidatorInfo(
            TypeName: "CreatePersonRequest",
            Rules: [new("Name", "MaximumLength", 100)]);

        var result = ZodSchemaEmitter.Emit(validator);

        result.Should().Contain("name: z.string().max(100)");
    }

    [Fact]
    public void Chains_Multiple_Rules()
    {
        var validator = new ValidatorInfo(
            TypeName: "CreatePersonRequest",
            Rules: [
                new("Email", "NotEmpty"),
                new("Email", "EmailAddress")
            ]);

        var result = ZodSchemaEmitter.Emit(validator);

        result.Should().Contain("email: z.string().min(1).email()");
    }

    [Fact]
    public void Exports_Named_Schema()
    {
        var validator = new ValidatorInfo(
            TypeName: "CreatePersonRequest",
            Rules: []);

        var result = ZodSchemaEmitter.Emit(validator);

        result.Should().StartWith("export const CreatePersonSchema = z.object({");
    }
}
```

### Implementation Follows Tests

```csharp
public static class ZodSchemaEmitter
{
    public static string Emit(ValidatorInfo validator)
    {
        var schemaName = validator.TypeName.Replace("Request", "Schema");
        var sb = new StringBuilder();
        sb.AppendLine($"export const {schemaName} = z.object({{");

        var rulesByProp = validator.Rules.GroupBy(r => r.Property);
        foreach (var group in rulesByProp)
        {
            var chain = string.Join("", group.Select(MapRule));
            var propName = ToCamelCase(group.Key);
            sb.AppendLine($"  {propName}: z.string(){chain},");
        }

        sb.AppendLine("})");
        return sb.ToString();
    }

    private static string MapRule(ValidationRule rule) => rule.Type switch
    {
        "NotEmpty" => ".min(1)",
        "MaximumLength" => $".max({rule.Param})",
        "EmailAddress" => ".email()",
        "GreaterThan" => $".gt({rule.Param})",
        _ => ""
    };
}
```

---

## 3. TDD: Content Negotiation Middleware

### Tests Define Behavior

```csharp
public class ContentNegotiatorTests
{
    [Fact]
    public void Returns_HTML_When_No_Impulse_Header()
    {
        var request = new MockRequest(headers: []);

        var result = ContentNegotiator.ShouldReturnJson(request);

        result.Should().BeFalse();
    }

    [Fact]
    public void Returns_JSON_When_Impulse_Header_Present()
    {
        var request = new MockRequest(headers: [("X-Impulse", "1")]);

        var result = ContentNegotiator.ShouldReturnJson(request);

        result.Should().BeTrue();
    }

    [Fact]
    public void Returns_JSON_When_Accept_Header_Is_Json()
    {
        var request = new MockRequest(headers: [("Accept", "application/json")]);

        var result = ContentNegotiator.ShouldReturnJson(request);

        result.Should().BeTrue();
    }
}
```

### Implementation

```csharp
public static class ContentNegotiator
{
    public static bool ShouldReturnJson(HttpRequest request)
    {
        if (request.Headers.ContainsKey("X-Impulse"))
            return true;

        if (request.Headers.Accept.Contains("application/json"))
            return true;

        return false;
    }
}
```

---

## 4. TDD: Route Tree Emitter

### Tests Define Output Format

```csharp
public class RouteTreeEmitterTests
{
    [Fact]
    public void Emits_RoutePaths_Constants()
    {
        var routes = new[] {
            new RouteInfo("/residents", "ResidentListProps"),
            new RouteInfo("/residents/{id:int}", "ResidentDetailProps")
        };

        var result = RouteTreeEmitter.EmitRoutePaths(routes);

        result.Should().Contain("residents: '/residents'");
        result.Should().Contain("residentDetail: '/residents/$id'");
    }

    [Fact]
    public void Emits_Route_Builder_For_Parameterized_Routes()
    {
        var routes = new[] {
            new RouteInfo("/residents/{id:int}", "ResidentDetailProps")
        };

        var result = RouteTreeEmitter.EmitRouteBuilders(routes);

        result.Should().Contain(
            "residentDetail: (id: number | string) =>");
        result.Should().Contain(
            "RoutePaths.residentDetail.replace('$id', String(id))");
    }

    [Fact]
    public void Emits_Typed_Loader()
    {
        var routes = new[] {
            new RouteInfo("/residents", "ResidentListProps")
        };

        var result = RouteTreeEmitter.EmitRoute(routes[0]);

        result.Should().Contain(
            "context.impulseFetch<ResidentListProps>(RoutePaths.residents)");
    }
}
```

---

## 5. TDD: MSBuild Extraction Task

### Tests Define File Parsing

```csharp
public class ExtractTypeScriptTaskTests
{
    [Fact]
    public void Extracts_Single_File()
    {
        var input = """
            /* IMPULSE:types.ts */
            export interface Foo { }
            """;

        var result = TypeScriptExtractor.Extract(input);

        result.Should().ContainKey("types.ts");
        result["types.ts"].Should().Be("export interface Foo { }");
    }

    [Fact]
    public void Extracts_Multiple_Files()
    {
        var input = """
            /* IMPULSE:types.ts */
            export interface Foo { }
            /* IMPULSE:validation.ts */
            export const FooSchema = z.object({})
            """;

        var result = TypeScriptExtractor.Extract(input);

        result.Should().HaveCount(2);
        result.Should().ContainKey("types.ts");
        result.Should().ContainKey("validation.ts");
    }

    [Fact]
    public void Trims_Whitespace()
    {
        var input = """
            /* IMPULSE:types.ts */

            export interface Foo { }

            """;

        var result = TypeScriptExtractor.Extract(input);

        result["types.ts"].Should().Be("export interface Foo { }");
    }
}
```

---

## 6. Integration Tests (After Unit Tests Pass)

Once unit tests define and verify individual functions, integration tests verify they work together:

```csharp
public class ImpulseIntegrationTests
{
    [Fact]
    public async Task Full_Pipeline_Generates_Correct_TypeScript()
    {
        // Arrange - full C# source
        var source = """
            public record PersonProps(string Name, int Age);

            public class CreatePersonValidator : AbstractValidator<CreatePersonRequest>
            {
                public CreatePersonValidator()
                {
                    RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
                }
            }

            app.MapGet("/people", Handler).Impulse<PersonProps>();
            """;

        // Act - run full generator
        var result = await GeneratorTestHelper.RunFullPipeline(source);

        // Assert - verify all outputs
        result["types.ts"].Should().Contain("export interface PersonProps");
        result["validation.ts"].Should().Contain("export const CreatePersonSchema");
        result["routeTree.ts"].Should().Contain("RoutePaths");
    }
}
```

---

## 7. E2E Tests - Real Browser (Critical)

**Unit tests verify functions. E2E tests verify Impulse actually works.**

These tests run against the full framework in a real browser:

```typescript
// tests/e2e/impulse-features.spec.ts
import { test, expect } from '@playwright/test'

test.describe('Impulse Core Features', () => {

  test('server-driven: initial load is HTML with embedded props', async ({ page }) => {
    const response = await page.goto('/test-route')

    // Response is HTML, not JSON
    expect(response?.headers()['content-type']).toContain('text/html')

    // Props embedded in script tag
    const props = await page.locator('script#__IMPULSE_PROPS__').textContent()
    expect(props).toBeTruthy()
    expect(JSON.parse(props!)).toHaveProperty('testData')
  })

  test('server-driven: client navigation sends X-Impulse header', async ({ page }) => {
    await page.goto('/test-route')

    // Intercept next navigation
    const requestPromise = page.waitForRequest(req =>
      req.url().includes('/other-route') &&
      req.headers()['x-impulse'] === '1'
    )

    await page.click('a[href="/other-route"]')
    const request = await requestPromise

    // X-Impulse header present
    expect(request.headers()['x-impulse']).toBe('1')

    // Response is JSON, not HTML
    const response = await request.response()
    expect(response?.headers()['content-type']).toContain('application/json')
  })

  test('generated routes: RoutePaths constants work', async ({ page }) => {
    await page.goto('/')

    // Navigate using generated route
    await page.click('[data-route="residents"]')

    // URL matches RoutePaths constant
    await expect(page).toHaveURL('/residents')
  })

  test('generated routes: parameterized Routes.xyz(id) work', async ({ page }) => {
    await page.goto('/residents')

    // Click link that uses Routes.residentDetail(123)
    await page.click('[data-resident-id="123"]')

    // URL correctly substituted
    await expect(page).toHaveURL('/residents/123')
  })

  test('mutations: POST with Zod validation', async ({ page }) => {
    await page.goto('/test-form')

    // Submit invalid - Zod catches it client-side
    await page.click('button[type="submit"]')
    await expect(page.locator('.validation-error')).toBeVisible()

    // Submit valid
    await page.fill('input[name="name"]', 'Test')
    await page.click('button[type="submit"]')

    // Request sent with correct headers
    const request = await page.waitForRequest(req =>
      req.method() === 'POST' &&
      req.headers()['content-type'] === 'application/json' &&
      req.headers()['x-impulse'] === '1'
    )
    expect(request).toBeTruthy()
  })

  test('invalidation: router.invalidate() refreshes data', async ({ page }) => {
    await page.goto('/residents')
    const initialCount = await page.locator('.resident').count()

    // Trigger mutation that calls router.invalidate()
    await page.click('[data-action="create"]')
    await page.fill('input[name="name"]', 'New Resident')
    await page.click('button[type="submit"]')

    // Wait for redirect back to list
    await page.waitForURL('/residents')

    // List refreshed - new resident appears
    const newCount = await page.locator('.resident').count()
    expect(newCount).toBe(initialCount + 1)
  })

  test('router context: impulseFetch available in loaders', async ({ page }) => {
    // This test verifies the context is properly injected
    await page.goto('/context-test')

    // Component that displays context status
    await expect(page.locator('[data-context="impulseFetch"]')).toHaveText('available')
    await expect(page.locator('[data-context="invalidate"]')).toHaveText('available')
  })

  test('type safety: loader data correctly typed', async ({ page }) => {
    await page.goto('/typed-route')

    // Component uses typed loader data
    // If types were wrong, this wouldn't render correctly
    await expect(page.locator('[data-typed-field="name"]')).toHaveText('Expected Name')
  })

})
```

### E2E Test Matrix

| Feature | What to Test |
|---------|--------------|
| Server-Driven | Initial load is HTML, navigation is JSON |
| X-Impulse Header | Sent on client navigation, not on initial load |
| RoutePaths | Constants resolve to correct URLs |
| Routes builders | Parameters substituted correctly |
| Zod validation | Client-side validation before submit |
| Mutations | Correct method, headers, body |
| Invalidation | Data refreshes after mutation |
| Router context | impulseFetch and invalidate available |
| Type safety | Loader data matches TypeScript types |

### Running E2E Tests

```bash
# Start test app
cd tests/Impulse.E2E
dotnet run &

# Run Playwright
bunx playwright test

# CI: use Impulse test harness
dotnet test --filter "Category=E2E"
```

---

## 8. Stable Build System

**No manual steps. No cat commands. No editing generated code. Ever.**

The build system must be fully automated and reproducible:

### Single Command Operations

```bash
dotnet restore    # Restores everything (NuGet + npm)
dotnet build      # Compiles + generates + extracts
dotnet test       # Runs ALL tests (unit + integration + E2E)
dotnet run        # Full dev environment
dotnet publish    # Production-ready output
```

### Build Pipeline (Automated)

```
dotnet restore
    │
    ├── NuGet restore (Impulse.* packages)
    │
    └── MSBuild target: RestoreNpm
        └── bun install (package.json dependencies)

dotnet build
    │
    ├── Roslyn compile
    │   └── Source generator runs
    │       ├── Routes.g.cs (C# constants)
    │       └── TypeScript.g.cs (embedded TS)
    │
    └── MSBuild target: ExtractTypeScript
        └── Extracts to generated/*.ts
            ├── types.ts
            ├── validation.ts
            ├── mutations.ts
            └── routeTree.ts

dotnet test
    │
    ├── xUnit tests (C#)
    │
    ├── MSBuild target: RunTypeScriptTests
    │   └── bun test
    │
    └── MSBuild target: RunE2ETests
        └── bunx playwright test

dotnet publish -c Release
    │
    ├── Release build (above steps)
    │
    ├── MSBuild target: BundleTypeScript
    │   └── bun run build
    │       └── Vite outputs dist/
    │
    └── MSBuild target: CopyAssets
        └── Copies dist/ to wwwroot/
```

### MSBuild Integration

```xml
<!-- Impulse.MSBuild provides these targets -->
<Project>
  <!-- Auto-restore npm on NuGet restore -->
  <Target Name="RestoreNpm" AfterTargets="Restore">
    <Exec Command="bun install" WorkingDirectory="$(ProjectDir)" />
  </Target>

  <!-- Extract TS after build -->
  <Target Name="ExtractTypeScript" AfterTargets="Build">
    <ImpulseExtractTypeScript
      SourceFile="$(IntermediateOutputPath)TypeScript.g.cs"
      OutputDirectory="$(ProjectDir)generated" />
  </Target>

  <!-- Run TS tests with dotnet test -->
  <Target Name="RunTypeScriptTests" AfterTargets="Test">
    <Exec Command="bun test" WorkingDirectory="$(ProjectDir)" />
  </Target>

  <!-- Run E2E tests -->
  <Target Name="RunE2ETests" AfterTargets="RunTypeScriptTests">
    <Exec Command="bunx playwright test" WorkingDirectory="$(ProjectDir)" />
  </Target>

  <!-- Bundle for publish -->
  <Target Name="BundleTypeScript" BeforeTargets="Publish">
    <Exec Command="bun run build" WorkingDirectory="$(ProjectDir)" />
  </Target>

  <!-- Copy bundled assets -->
  <Target Name="CopyAssets" AfterTargets="BundleTypeScript">
    <ItemGroup>
      <DistFiles Include="$(ProjectDir)dist\**\*" />
    </ItemGroup>
    <Copy SourceFiles="@(DistFiles)" DestinationFolder="$(PublishDir)wwwroot" />
  </Target>
</Project>
```

### What This Guarantees

| Command | Result |
|---------|--------|
| `dotnet restore` | Fresh clone → working state |
| `dotnet build` | All generated files current |
| `dotnet test` | All tests run (C# + TS + E2E) |
| `dotnet run` | Full dev environment running |
| `dotnet publish` | Deployable folder ready |

### Never Do This

```bash
# ❌ NEVER manually copy files
cp something somewhere

# ❌ NEVER cat/echo to create files
cat > file.ts << 'EOF'

# ❌ NEVER edit generated/ folder
vim generated/types.ts

# ❌ NEVER run npm/bun manually for build steps
bun install  # (dotnet restore does this)
bun run build  # (dotnet publish does this)
```

### CI/CD Example

```yaml
# GitHub Actions - single dotnet command does everything
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
      - uses: oven-sh/setup-bun@v1

      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build
      - run: dotnet publish -c Release --no-build
```

**The system is stable because it's automated. Manual steps introduce drift.**

---

## 9. NuGet Package Structure

```
Impulse                     → Meta-package
├── Impulse.Core            → Public API
├── Impulse.SourceGen       → Roslyn generator (tested via unit tests)
├── Impulse.MSBuild         → TS extraction (tested via unit tests)
├── Impulse.Runtime         → Middleware (tested via unit tests)
└── Impulse.Templates       → dotnet new template
```

---

## 10. Type Mappings (Reference)

### C# → TypeScript

| C# Type | TypeScript Type | Test Case |
|---------|-----------------|-----------|
| `string` | `string` | `Emits_String_Type` |
| `int`, `long` | `number` | `Emits_Number_Type` |
| `bool` | `boolean` | `Emits_Boolean_Type` |
| `DateTime` | `string` | `Emits_DateTime_As_String` |
| `T?` | `T \| null` | `Emits_Nullable_As_Union` |
| `IReadOnlyList<T>` | `Array<T>` | `Emits_Array_From_IReadOnlyList` |

### FluentValidation → Zod

| FluentValidation | Zod | Test Case |
|------------------|-----|-----------|
| `.NotEmpty()` | `.min(1)` | `Emits_NotEmpty_As_Min1` |
| `.MaximumLength(n)` | `.max(n)` | `Emits_MaxLength` |
| `.EmailAddress()` | `.email()` | `Emits_EmailAddress` |

---

## 11. Implementation Files

Each file has a corresponding test file:

```
src/
├── Impulse.SourceGen/
│   ├── Emitters/
│   │   ├── TypeScriptTypeEmitter.cs      ← TypeScriptTypeEmitterTests.cs
│   │   ├── ZodSchemaEmitter.cs           ← ZodSchemaEmitterTests.cs
│   │   └── RouteTreeEmitter.cs           ← RouteTreeEmitterTests.cs
│   └── Analyzers/
│       ├── RouteAnalyzer.cs              ← RouteAnalyzerTests.cs
│       └── ValidatorAnalyzer.cs          ← ValidatorAnalyzerTests.cs
│
├── Impulse.MSBuild/
│   └── TypeScriptExtractor.cs            ← TypeScriptExtractorTests.cs
│
├── Impulse.Runtime/
│   ├── ContentNegotiator.cs              ← ContentNegotiatorTests.cs
│   └── HtmlRenderer.cs                   ← HtmlRendererTests.cs
```

**Rule: No implementation file without a test file.**

---

## 12. Development Workflow

```bash
# 1. Create test file first
touch tests/Impulse.SourceGen.Tests/NewFeatureTests.cs

# 2. Write failing tests
dotnet test  # RED

# 3. Create implementation
touch src/Impulse.SourceGen/NewFeature.cs

# 4. Make tests pass
dotnet test  # GREEN

# 5. Refactor with confidence
dotnet test  # Still GREEN
```

---

## 13. Quality Gates

Before any PR:

```bash
# All tests must pass
dotnet test

# Coverage must not decrease
dotnet test --collect:"XPlat Code Coverage"

# No implementation without tests
# (enforced by PR review)
```

**Tests are not optional. Tests are the specification.**
