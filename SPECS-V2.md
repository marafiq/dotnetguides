# Impulse v2 - TDD Specifications

## Architecture: One Model, Many Generators

```
Minimal API Code + FluentValidation
              ↓
       [ModelBuilder]  ← Roslyn Source Generator
              ↓
        ImpulseModel   ← Single source of truth
              ↓
    ┌─────┬──┴──┬─────┬──────┐
    ↓     ↓     ↓     ↓      ↓
 Types  Routes  Zod  Form  DotNetRoutes
    ↓     ↓     ↓     ↓      ↓
  .ts    .tsx   .ts   .ts    .cs
```

## Key Principles

- **No magic strings** - All routes type-safe in both .NET and React
- **Tree-shakeable** - One file per route, dead code elimination
- **ProblemDetails RFC 7807** - Server validation returns standard format
- **Form abstraction** - React hooks for validation + submission

---

## 1. ImpulseModel (Core IR)

**The single intermediate representation built from Minimal API.**

```csharp
record ImpulseModel(
    IReadOnlyList<EndpointModel> Endpoints,     // GET → Component
    IReadOnlyList<MutationModel> Mutations,     // POST/PUT/DELETE → Handler
    IReadOnlyList<TypeModel> Types,
    IReadOnlyList<ValidatorModel> Validators
);

record EndpointModel(
    string Route,              // "/residents/{id:int}"
    string RouteName,          // "ResidentDetail" (for code gen)
    string ComponentPath,      // "./Residents/Detail"
    TypeModel PropsType,       // ResidentDetailProps
    IReadOnlyList<DeferredModel> Deferred,
    IReadOnlyList<LazyModel> Lazy
);

record MutationModel(
    string Route,              // "/residents"
    string RouteName,          // "CreateResident"
    HttpMethod Method,         // POST, PUT, DELETE
    TypeModel RequestType,     // CreateResidentRequest
    TypeModel ResponseType,    // CreateResidentResponse
    string? HandlerType        // "CreateResidentHandler" (for DI)
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
ModelBuilder.FromEndpoint (GET → Component)
  ✓ extracts route from MapGet("/path", ...)
  ✓ derives RouteName from route ("ResidentDetail")
  ✓ extracts component path from .Impulse("./Path")
  ✓ extracts props type from .Impulse<TProps>()
  ✓ extracts deferred from .Deferred<T>(key, url)
  ✓ extracts lazy from .Lazy<T>(key, url)

ModelBuilder.FromMutation (POST/PUT/DELETE → Handler)
  ✓ extracts route from MapPost/Put/Delete
  ✓ extracts request type from handler parameter
  ✓ extracts response type from TypedResults<Ok<T>>
  ✓ finds [ImpulseHandler] class for DI registration

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

ModelBuilder.FromFluentValidator
  ✓ finds AbstractValidator<T> implementations
  ✓ extracts RuleFor(x => x.Property) → PropertyName
  ✓ extracts .NotEmpty() → RuleKind.NotEmpty
  ✓ extracts .MaximumLength(n) → RuleKind.MaxLength, Value: n
  ✓ extracts .EmailAddress() → RuleKind.Email
  ✓ extracts .GreaterThan(n) → RuleKind.GreaterThan, Value: n
  ✓ extracts .Matches(pattern) → RuleKind.Regex, Value: pattern
  ✓ chains multiple rules on same property
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

### RoutesGenerator (TanStack Router)

```
RoutesGenerator.Emit(model)
  ✓ emits createFileRoute for each endpoint
  ✓ converts {id:int} to $id param format
  ✓ emits loader with typed props fetch
  ✓ emits deferred/lazy in loader
  ✓ generates routeTree with parent/child hierarchy
```

**Input:**
```csharp
new EndpointModel("/residents/{id:int}", GET, "./Residents/Detail", propsType, ...)
```

**Output:**
```typescript
// routes/residents/$id.tsx
import { createFileRoute } from '@tanstack/react-router'
import type { ResidentDetailProps } from '../types.g'

export const Route = createFileRoute('/residents/$id')({
  loader: async ({ params }) => {
    const res = await fetch(`/api/residents/${params.id}`)
    return res.json() as Promise<ResidentDetailProps>
  },
  component: () => import('../Features/Residents/Detail'),
})
```

**With Deferred:**
```typescript
export const Route = createFileRoute('/residents/$id')({
  loader: async ({ params }) => {
    const props = fetch(`/api/residents/${params.id}`).then(r => r.json())
    return {
      props: await props,
      meds: defer(fetch(`/api/residents/${params.id}/meds`).then(r => r.json())),
    }
  },
})
```

### ZodGenerator (from FluentValidation only)

```
ZodGenerator.Emit(model)
  ✓ emits schema for types with FluentValidation
  ✓ maps: NotEmpty→min(1), MaxLength(n)→max(n), Email→email()
  ✓ chains validators in order
```

**Output:**
```typescript
// validation/createResident.ts
export const createResidentSchema = z.object({
  name: z.string().min(1).max(100),
  email: z.string().min(1).email(),
});
```

### FormGenerator (React hooks)

```
FormGenerator.Emit(model)
  ✓ emits useForm hook per mutation endpoint
  ✓ integrates Zod schema for client validation
  ✓ handles ProblemDetails from server 400
  ✓ returns { register, errors, submit, isSubmitting }
```

**Output:**
```typescript
// forms/useCreateResident.ts
import { createResidentSchema } from '../validation/createResident'

export function useCreateResident() {
  return useImpulseForm({
    schema: createResidentSchema,
    action: routes.createResident.submit,
    onError: (problem: ProblemDetails) => mapFieldErrors(problem),
  })
}
```

**Usage (no magic strings):**
```tsx
function CreateForm() {
  const { register, errors, submit } = useCreateResident()
  return (
    <form onSubmit={submit}>
      <input {...register('name')} />
      {errors.name && <span>{errors.name}</span>}
    </form>
  )
}
```

### DotNetRoutesGenerator (C# static routes)

```
DotNetRoutesGenerator.Emit(model)
  ✓ emits static class with route constants
  ✓ emits type-safe path builders with params
  ✓ no magic strings in endpoint registration
```

**Output:**
```csharp
// Routes.g.cs
public static partial class Routes
{
    public static class Residents
    {
        public const string List = "/residents";
        public const string Detail = "/residents/{id:int}";
        public static string DetailPath(int id) => $"/residents/{id}";
        public const string Create = "/residents";
    }
}

// Usage in Program.cs
app.MapGet(Routes.Residents.List, ListHandler);
app.MapGet(Routes.Residents.Detail, DetailHandler);
app.MapPost(Routes.Residents.Create, CreateHandler);
```

---

## 3. Function Signatures

```csharp
// Build model from source (Roslyn)
static ImpulseModel Build(Compilation compilation);

// Generate TypeScript (pure functions)
static string GenerateTypes(ImpulseModel model);
static string GenerateRoutes(ImpulseModel model);  // TanStack Router
static string GenerateZod(ImpulseModel model);     // FluentValidation only
static string GenerateForms(ImpulseModel model);   // useForm hooks

// Generate C# (pure functions)
static string GenerateDotNetRoutes(ImpulseModel model);
```

---

## 4. Test Structure

```
tests/Impulse.CodeGen.V2.Tests/
├── Model/
│   ├── FromEndpointTests.cs      # MapGet → EndpointModel
│   ├── FromTypeTests.cs          # C# type → TypeModel
│   ├── FromValidatorTests.cs     # FluentValidation → ValidatorModel
│   └── TraversalTests.cs         # Type graph walking
├── Generators/
│   ├── TypesGeneratorTests.cs    # → types.ts
│   ├── RoutesGeneratorTests.cs   # → TanStack routes
│   ├── ZodGeneratorTests.cs      # → validation schemas
│   ├── FormGeneratorTests.cs     # → useForm hooks
│   └── DotNetRoutesTests.cs      # → Routes.g.cs
└── Integration/
    └── FullPipelineTests.cs
```

---

## 5. Server Validation (ProblemDetails RFC 7807)

```
Server returns 400 with ProblemDetails:
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation failed",
  "status": 400,
  "errors": {
    "name": ["Name is required"],
    "email": ["Invalid email format"]
  }
}

FormGenerator maps to field errors:
  ✓ extracts errors object from ProblemDetails
  ✓ maps server field names to form fields
  ✓ merges with client Zod errors
```

---

## 6. Handler Registration with DI

```csharp
// Source generator creates handler registration
[ImpulseHandler]
public class CreateResidentHandler(IDb db) : IHandler<CreateRequest, CreateResponse>
{
    public async Task<Results<Ok<CreateResponse>, ValidationProblem>> Handle(
        CreateRequest request, CancellationToken ct)
    {
        // DI-injected db available
        var resident = await db.Residents.AddAsync(...);
        return TypedResults.Ok(new CreateResponse(resident.Id));
    }
}

// Generated registration (no magic strings)
public static void MapImpulseHandlers(this IEndpointRouteBuilder app)
{
    app.MapPost(Routes.Residents.Create,
        (CreateRequest req, CreateResidentHandler handler, CancellationToken ct)
            => handler.Handle(req, ct));
}
```

---

## 7. Output Structure (Tree-Shakeable)

```
generated/
├── types/
│   ├── ResidentSummary.ts
│   ├── ResidentDetailProps.ts
│   └── index.ts              # re-exports all
├── routes/
│   ├── residents/
│   │   ├── index.tsx         # GET /residents
│   │   └── $id.tsx           # GET /residents/:id
│   └── routeTree.gen.ts      # TanStack tree
├── validation/
│   ├── createResident.ts     # Zod schema
│   └── index.ts
├── forms/
│   ├── useCreateResident.ts  # Form hook
│   └── index.ts
└── Routes.g.cs               # .NET routes
```

---

## 8. Design Principles

1. **One model** - ImpulseModel is the only IR
2. **Pure generators** - Input model, output string
3. **No magic strings** - Type-safe routes in .NET and React
4. **Tree-shakeable** - One file per route/form/validation
5. **ProblemDetails** - Standard validation error format
6. **DI support** - Handlers are injectable classes
