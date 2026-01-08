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

## 7. NuGet Package Structure

```
Impulse                     → Meta-package
├── Impulse.Core            → Public API
├── Impulse.SourceGen       → Roslyn generator (tested via unit tests)
├── Impulse.MSBuild         → TS extraction (tested via unit tests)
├── Impulse.Runtime         → Middleware (tested via unit tests)
└── Impulse.Templates       → dotnet new template
```

---

## 8. Type Mappings (Reference)

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

## 9. Implementation Files

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

## 10. Development Workflow

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

## 11. Quality Gates

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
