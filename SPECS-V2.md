# Impulse v2 - TDD Specifications

## Architecture: One Model, Many Generators

```
Minimal API Code
       ↓
  [ModelBuilder]  ← Roslyn Source Generator
       ↓
  ImpulseModel    ← Single source of truth
       ↓
   ┌───┴────┬─────────┬──────────┐
   ↓        ↓         ↓          ↓
TypesGen  RoutesGen  ZodGen  RegistryGen
   ↓        ↓         ↓          ↓
types.ts routes.ts  zod.ts  registry.ts
```

---

## 1. ImpulseModel (Core IR)

**The single intermediate representation built from Minimal API.**

```csharp
record ImpulseModel(
    IReadOnlyList<EndpointModel> Endpoints,
    IReadOnlyList<TypeModel> Types,
    IReadOnlyList<ValidatorModel> Validators
);

record EndpointModel(
    string Route,              // "/residents/{id:int}"
    HttpMethod Method,         // GET, POST, etc.
    string ComponentPath,      // "./Residents/Detail"
    TypeModel PropsType,       // ResidentDetailProps
    IReadOnlyList<DeferredModel> Deferred,
    IReadOnlyList<LazyModel> Lazy
);

record TypeModel(
    string Name,               // "ResidentDetailProps"
    TypeKind Kind,             // Interface, Enum
    IReadOnlyList<PropertyModel> Properties,
    IReadOnlyList<string> EnumValues  // for enums
);

record PropertyModel(
    string Name,               // "firstName"
    string TypeName,           // "string", "number", "ResidentSummary"
    bool IsOptional,
    bool IsNullable,
    bool IsArray
);

record ValidatorModel(
    string TypeName,           // "CreateResidentRequest"
    IReadOnlyList<PropertyRules> Rules
);

record PropertyRules(
    string PropertyName,
    IReadOnlyList<ValidationRule> Rules
);

record ValidationRule(
    RuleKind Kind,             // NotEmpty, MaxLength, Email, etc.
    object? Value              // 100 for MaxLength(100)
);
```

### Specs: ModelBuilder

```
ModelBuilder.FromEndpoint
  ✓ extracts route from MapGet("/path", ...)
  ✓ extracts method (GET/POST/PUT/DELETE)
  ✓ extracts component path from .Impulse("./Path")
  ✓ extracts props type from .Impulse<TProps>()
  ✓ extracts deferred from .Deferred<T>(key, url)
  ✓ extracts lazy from .Lazy<T>(key, url)

ModelBuilder.FromType
  ✓ string → TypeName: "string"
  ✓ int → TypeName: "number"
  ✓ bool → TypeName: "boolean"
  ✓ DateTime → TypeName: "string"
  ✓ T[] → IsArray: true, TypeName: T
  ✓ T? → IsNullable: true
  ✓ record Foo(int X) → Properties: [{Name: "x", TypeName: "number"}]
  ✓ enum Status { A, B } → EnumValues: ["A", "B"]

ModelBuilder.FromTypedResults
  ✓ Results<Ok<T>, NotFound> → extracts T as PropsType
  ✓ Results<Ok<T>, ValidationProblem> → extracts T

ModelBuilder.Traversal
  ✓ collects all referenced types
  ✓ deduplicates by name
  ✓ handles circular references
```

---

## 2. Generators (Model → Output)

Each generator takes `ImpulseModel` and emits a string.

### TypesGenerator

```
TypesGenerator.Emit(model)
  ✓ emits file header
  ✓ emits enums as union types first
  ✓ emits interfaces in dependency order
  ✓ property names are camelCase
  ✓ optional properties have "?"
  ✓ arrays emit "readonly T[]"
  ✓ nullable emits "T | null"
```

**Input:**
```csharp
new TypeModel("ResidentSummary", TypeKind.Interface, [
    new("Id", "number", false, false, false),
    new("Name", "string", false, false, false),
])
```

**Output:**
```typescript
export interface ResidentSummary {
  id: number;
  name: string;
}
```

### RoutesGenerator

```
RoutesGenerator.Emit(model)
  ✓ emits route constants with path and method
  ✓ emits component path
  ✓ emits lazy import function
  ✓ emits deferred/lazy metadata
  ✓ emits type-safe path builders for params
```

**Input:**
```csharp
new EndpointModel("/residents/{id:int}", GET, "./Residents/Detail", ...)
```

**Output:**
```typescript
export const routes = {
  residentDetail: {
    path: '/residents/:id',
    method: 'GET',
    component: './Residents/Detail',
  },
} as const;

export const residentDetailPath = (id: number) => `/residents/${id}`;
```

### ZodGenerator

```
ZodGenerator.Emit(model)
  ✓ emits schema for each validated type
  ✓ maps rules to Zod methods
  ✓ chains validators in order
  ✓ emits type inference export
```

**Rule mapping:**
```
NotEmpty      → .min(1)
MaxLength(n)  → .max(n)
MinLength(n)  → .min(n)
Email         → .email()
GreaterThan   → .gt(n)
LessThan      → .lt(n)
Regex(p)      → .regex(/p/)
```

**Input:**
```csharp
new ValidatorModel("CreateRequest", [
    new("Name", [new(NotEmpty), new(MaxLength, 100)]),
    new("Email", [new(NotEmpty), new(Email)])
])
```

**Output:**
```typescript
export const createRequestSchema = z.object({
  name: z.string().min(1).max(100),
  email: z.string().min(1).email(),
});
export type CreateRequest = z.infer<typeof createRequestSchema>;
```

### RegistryGenerator

```
RegistryGenerator.Emit(model)
  ✓ emits component path → import mapping
  ✓ uses lazy imports for tree-shaking
```

**Output:**
```typescript
export const loadComponent = (path: string) => {
  switch (path) {
    case './Residents/List': return import('./Residents/List');
    case './Residents/Detail': return import('./Residents/Detail');
    default: throw new Error(`Unknown: ${path}`);
  }
};
```

---

## 3. Function Signatures

```csharp
// Build model from source (Roslyn)
static ImpulseModel Build(Compilation compilation);
static ImpulseModel Build(IEnumerable<EndpointInfo> endpoints);

// Generate output (pure functions)
static string GenerateTypes(ImpulseModel model);
static string GenerateRoutes(ImpulseModel model);
static string GenerateZod(ImpulseModel model);
static string GenerateRegistry(ImpulseModel model);
```

---

## 4. Test Structure

```
tests/Impulse.CodeGen.V2.Tests/
├── ModelBuilderTests.cs     # Minimal API → ImpulseModel
├── TypesGeneratorTests.cs   # ImpulseModel → types.ts
├── RoutesGeneratorTests.cs  # ImpulseModel → routes.ts
├── ZodGeneratorTests.cs     # ImpulseModel → zod.ts
├── RegistryGeneratorTests.cs
└── IntegrationTests.cs      # Full pipeline
```

---

## 5. Design Principles

1. **One model** - ImpulseModel is the only IR
2. **Pure generators** - Input model, output string
3. **No AST nesting** - Flat records, no visitor needed
4. **Testable** - Create model manually, test generator output
5. **Extensible** - Add new generator without changing model
