# Impulse v2 - Cleaner Architecture

## Problems with Current Design

### 1. Filter Magic
```csharp
// Current: Hidden behavior in filter
builder.AddEndpointFilter<ImpulseComponentFilter>();
```
- Version checking buried in filter
- JSON vs HTML decision hidden
- Hard to test, hard to extend

### 2. Global Registry
```csharp
// Current: Mutable singleton
ImpulseTypeRegistry._components.Add(...)
```
- All types collected (not tree-shakeable)
- Runtime discovery (should be compile-time)
- Testing requires global state reset

### 3. Convention Magic
```csharp
// Current: Implicit path derivation
ComponentPathConvention.GetPath<ResidentsListProps>()
// Returns "./Residents/List" - magic!
```
- Fragile plural/singular detection
- Refactoring breaks paths silently
- Can't override without hacks

### 4. String-Based Code Gen
```csharp
// Current: StringBuilder hell
sb.AppendLine($"export interface {name} {{");
```
- Not extensible
- No validation
- Hard to transform

---

## v2 Design: All Truth in Extension Methods

### Principle 1: Explicit > Implicit

```csharp
// OLD: Magic path derivation
app.MapGet("/residents", handler)
   .AsComponent<ResidentsListProps>();

// NEW: Explicit path, typed result
app.MapGet("/residents", () =>
    Impulse.View("./Residents/List", ResidentsHandler.GetList()));
```

### Principle 2: No Global State

```csharp
// OLD: Global registry
services.AddImpulse();  // Creates singleton registry

// NEW: Build-time analysis
[ImpulseEndpoints]
public static class ResidentsEndpoints
{
    [View("./Residents/List")]
    public static ResidentsListProps List() => ...;

    [View("./Residents/Detail")]
    public static ResidentDetailProps Detail(int id) => ...;
}
// Source generator extracts at compile time
```

### Principle 3: AST-First Code Generation

```csharp
// AST Types
public abstract record TsNode;
public record TsFile(IReadOnlyList<TsNode> Statements) : TsNode;
public record TsInterface(string Name, IReadOnlyList<TsProperty> Props) : TsNode;
public record TsProperty(string Name, TsType Type, bool Optional = false);
public abstract record TsType;
public record TsString() : TsType;
public record TsNumber() : TsType;
public record TsBoolean() : TsType;
public record TsArray(TsType Element) : TsType;
public record TsRecord(TsType Key, TsType Value) : TsType;
public record TsRef(string Name) : TsType;
public record TsUnion(IReadOnlyList<TsType> Types) : TsType;
public record TsLiteral(string Value) : TsType;

// Build AST
var ast = new TsFile([
    new TsInterface("ResidentsListProps", [
        new TsProperty("residents", new TsArray(new TsRef("ResidentSummary"))),
        new TsProperty("totalCount", new TsNumber()),
    ])
]);

// Transform (plugin hook)
ast = plugins.Aggregate(ast, (a, p) => p.Transform(a));

// Emit
var code = new TsEmitter().Emit(ast);
```

### Principle 4: Tree-Shakeable Output

```typescript
// OLD: One big file with everything
export const IMPULSE_COMPONENTS = new Map([...everything...]);

// NEW: Per-route modules
// routes/residents.ts
export const residentsRoute = {
  path: '/residents' as const,
  load: () => import('../Features/Residents/List'),
} as const;

// routes/index.ts (barrel with tree-shaking)
export { residentsRoute } from './residents';
export { dashboardRoute } from './dashboard';
// Only imported routes bundled
```

---

## v2 API Design

### Endpoints

```csharp
public static class ResidentsEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/residents", List)
           .Impulse("./Residents/List");

        app.MapGet("/residents/{id:int}", Detail)
           .Impulse("./Residents/Detail")
           .Deferred<MedicationsProps>("medications", "/api/residents/{id}/meds");

        app.MapPost("/residents", Create)
           .ImpulseMutation<CreateRequest, CreateResponse>()
           .Validates<CreateRequestValidator>();
    }

    static ResidentsListProps List() => new(...);
    static ResidentDetailProps? Detail(int id) => ...;
    static CreateResponse Create(CreateRequest req) => ...;
}
```

### No Filter - Direct Results

```csharp
// Impulse extension returns typed result
public static RouteHandlerBuilder Impulse<TProps>(
    this RouteHandlerBuilder builder,
    string componentPath)
{
    return builder.AddEndpointFilter(async (ctx, next) =>
    {
        var props = await next(ctx);
        if (props is null) return props;

        var http = ctx.HttpContext;

        // Explicit: check header
        if (http.Request.Headers.ContainsKey("X-Impulse"))
        {
            return Results.Json(new { props, component = componentPath });
        }

        // Explicit: render shell
        var shell = http.RequestServices.GetRequiredService<IShellRenderer>();
        return Results.Content(shell.Render(props, componentPath), "text/html");
    });
}
```

### AST Generator

```csharp
public interface ITsGenerator
{
    TsFile Generate(IEnumerable<Type> types);
}

public interface ITsPlugin
{
    TsFile Transform(TsFile ast);
}

public class TypeScriptGenerator : ITsGenerator
{
    private readonly IEnumerable<ITsPlugin> _plugins;

    public TsFile Generate(IEnumerable<Type> types)
    {
        var statements = types.Select(TypeToInterface).ToList();
        var ast = new TsFile(statements);

        // Apply plugins
        foreach (var plugin in _plugins)
        {
            ast = plugin.Transform(ast);
        }

        return ast;
    }
}

// Emit to string
public class TsEmitter
{
    public string Emit(TsFile file)
    {
        var sb = new StringBuilder();
        foreach (var stmt in file.Statements)
        {
            EmitNode(sb, stmt, 0);
        }
        return sb.ToString();
    }

    void EmitNode(StringBuilder sb, TsNode node, int indent)
    {
        switch (node)
        {
            case TsInterface iface:
                sb.AppendLine($"export interface {iface.Name} {{");
                foreach (var prop in iface.Props)
                {
                    var opt = prop.Optional ? "?" : "";
                    sb.AppendLine($"  {prop.Name}{opt}: {EmitType(prop.Type)};");
                }
                sb.AppendLine("}");
                break;
            // ...
        }
    }

    string EmitType(TsType type) => type switch
    {
        TsString => "string",
        TsNumber => "number",
        TsBoolean => "boolean",
        TsArray arr => $"readonly {EmitType(arr.Element)}[]",
        TsRef r => r.Name,
        TsUnion u => string.Join(" | ", u.Types.Select(EmitType)),
        _ => "unknown"
    };
}
```

### Validation AST

```csharp
// Zod AST
public abstract record ZodNode;
public record ZodSchema(string Name, ZodObject Shape) : ZodNode;
public record ZodObject(IReadOnlyList<ZodProperty> Props) : ZodNode;
public record ZodProperty(string Name, ZodType Type);
public abstract record ZodType;
public record ZodString(IReadOnlyList<ZodValidator> Validators) : ZodType;
public record ZodNumber(IReadOnlyList<ZodValidator> Validators) : ZodType;
public abstract record ZodValidator;
public record ZodMin(int Value) : ZodValidator;
public record ZodMax(int Value) : ZodValidator;
public record ZodEmail() : ZodValidator;

// Build from FluentValidation
var schema = new ZodSchema("personSchema", new ZodObject([
    new ZodProperty("email", new ZodString([new ZodEmail()])),
    new ZodProperty("age", new ZodNumber([new ZodMin(0), new ZodMax(150)])),
]));

// Emit
// export const personSchema = z.object({
//   email: z.string().email(),
//   age: z.number().min(0).max(150),
// });
```

---

## File Structure

```
src/
├── Impulse.Core/
│   ├── ImpulseExtensions.cs      # .Impulse() extension only
│   ├── IShellRenderer.cs         # Interface
│   └── DefaultShellRenderer.cs   # Implementation
│
├── Impulse.CodeGen/
│   ├── Ast/
│   │   ├── TsNode.cs             # TypeScript AST
│   │   ├── ZodNode.cs            # Zod AST
│   │   └── RouteNode.cs          # Route AST
│   ├── Analyzers/
│   │   ├── TypeAnalyzer.cs       # C# type → AST
│   │   └── ValidatorAnalyzer.cs  # FluentValidation → AST
│   ├── Emitters/
│   │   ├── TsEmitter.cs          # AST → TypeScript
│   │   └── ZodEmitter.cs         # AST → Zod
│   ├── Plugins/
│   │   └── ITsPlugin.cs          # Extension point
│   └── Generator.cs              # Orchestrates pipeline
│
└── Impulse.Validation/
    └── FluentValidationAdapter.cs # FluentValidation → Zod AST
```

---

## Source of Truth: C# AST from Minimal API

The source generator analyzes actual code - no runtime reflection:

```csharp
// Your code (source of truth)
app.MapGet("/residents", ResidentsHandler.List)
   .Impulse<ResidentsListProps>("./Residents/List");

app.MapPost("/residents", ResidentsHandler.Create)
   .ImpulseMutation<CreateRequest, CreateResponse>();
```

### Source Generator (Roslyn)

```csharp
[Generator]
public class ImpulseGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all .Impulse<T>() and .ImpulseMutation<T,R>() calls
        var impulseEndpoints = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsImpulseCall,
                transform: ExtractEndpointInfo)
            .Where(x => x is not null);

        context.RegisterSourceOutput(impulseEndpoints.Collect(), GenerateCode);
    }

    static bool IsImpulseCall(SyntaxNode node, CancellationToken _)
    {
        // Match: .Impulse<T>(...) or .ImpulseMutation<T,R>()
        return node is InvocationExpressionSyntax invocation &&
               invocation.Expression is MemberAccessExpressionSyntax member &&
               member.Name.Identifier.Text is "Impulse" or "ImpulseMutation";
    }

    static EndpointInfo? ExtractEndpointInfo(GeneratorSyntaxContext ctx, CancellationToken _)
    {
        var invocation = (InvocationExpressionSyntax)ctx.Node;

        // Walk up to find MapGet/MapPost
        var mapCall = FindMapCall(invocation);
        if (mapCall is null) return null;

        // Extract route pattern: "/residents/{id}"
        var route = ExtractRoute(mapCall);

        // Extract HTTP method: GET, POST, etc.
        var method = ExtractMethod(mapCall);

        // Extract type arguments from .Impulse<TProps>()
        var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
        var genericName = memberAccess.Name as GenericNameSyntax;
        var typeArgs = genericName?.TypeArgumentList.Arguments;

        // Get component path from argument: "./Residents/List"
        var componentPath = ExtractComponentPath(invocation);

        // Resolve type symbols
        var propsType = ctx.SemanticModel.GetTypeInfo(typeArgs[0]).Type;

        return new EndpointInfo(route, method, propsType, componentPath);
    }

    static void GenerateCode(SourceProductionContext ctx, ImmutableArray<EndpointInfo> endpoints)
    {
        // Build C# AST for type information
        var types = endpoints.SelectMany(e => GetAllTypes(e.PropsType)).Distinct();

        // Build TypeScript AST
        var tsAst = new TsFile(types.Select(TypeToTsInterface).ToList());

        // Build Routes AST
        var routesAst = endpoints.Select(e => new RouteDecl(e.Route, e.ComponentPath, e.Method));

        // Emit
        var typesCode = new TsEmitter().Emit(tsAst);
        var routesCode = new RoutesEmitter().Emit(routesAst);

        // Output as embedded resource or compile-time file
        ctx.AddSource("impulse.types.g.ts", typesCode);
        ctx.AddSource("impulse.routes.g.ts", routesCode);
    }
}
```

### What Gets Extracted

```csharp
// This code:
app.MapGet("/residents/{id:int}", ResidentsHandler.Detail)
   .Impulse<ResidentDetailProps>("./Residents/Detail")
   .Deferred<MedicationsProps>("meds", "/api/residents/{id}/meds");

// Produces this AST:
new EndpointInfo(
    Route: "/residents/{id:int}",
    Method: HttpMethod.Get,
    PropsType: typeof(ResidentDetailProps),
    ComponentPath: "./Residents/Detail",
    Deferred: [
        new DeferredInfo("meds", typeof(MedicationsProps), "/api/residents/{id}/meds")
    ]
)
```

### Type Traversal (from C# AST)

```csharp
static IEnumerable<ITypeSymbol> GetAllTypes(ITypeSymbol root)
{
    var visited = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
    var queue = new Queue<ITypeSymbol>();
    queue.Enqueue(root);

    while (queue.Count > 0)
    {
        var type = queue.Dequeue();
        if (!visited.Add(type)) continue;

        yield return type;

        // Traverse properties
        foreach (var member in type.GetMembers().OfType<IPropertySymbol>())
        {
            var propType = UnwrapNullable(member.Type);

            // Handle collections
            if (IsCollection(propType, out var elementType))
            {
                queue.Enqueue(elementType);
            }
            // Handle nested types
            else if (propType.TypeKind == TypeKind.Class || propType.TypeKind == TypeKind.Struct)
            {
                queue.Enqueue(propType);
            }
        }
    }
}
```

### Generated Output (Tree-Shakeable)

```typescript
// impulse.routes.g.ts
export const routes = {
  residents: {
    path: '/residents' as const,
    method: 'GET' as const,
    component: './Residents/List',
    load: () => import('../Features/Residents/List'),
  },
  residentDetail: {
    path: '/residents/:id' as const,
    method: 'GET' as const,
    component: './Residents/Detail',
    load: () => import('../Features/Residents/Detail'),
    deferred: {
      meds: '/api/residents/:id/meds',
    },
  },
} as const;

// Type-safe route builder
export function residentDetailPath(id: number) {
  return `/residents/${id}` as const;
}
```

---

## Benefits

| Current | v2 |
|---------|-----|
| Filter magic | Explicit extension method |
| Global registry | Compile-time source gen |
| String building | AST + plugins |
| All types bundled | Tree-shakeable per-route |
| Convention paths | Explicit paths |
| Hard to test | Pure functions |
