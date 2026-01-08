# Impulse v2 - TDD Specifications

## Core Modules

### 1. TypeScript AST (`TsAst`)

**Responsibility:** Immutable representation of TypeScript code

**Specs:**
```
TsAst.Interface
  ✓ creates interface with name and properties
  ✓ properties preserve order
  ✓ supports optional properties (?)
  ✓ is immutable (With returns new instance)

TsAst.Property
  ✓ has name and type
  ✓ optional flag defaults to false
  ✓ readonly flag defaults to true

TsAst.Types
  ✓ TsString emits "string"
  ✓ TsNumber emits "number"
  ✓ TsBoolean emits "boolean"
  ✓ TsArray(T) emits "readonly T[]"
  ✓ TsRecord(K,V) emits "Record<K, V>"
  ✓ TsUnion([A,B]) emits "A | B"
  ✓ TsLiteral("foo") emits "'foo'"
  ✓ TsRef("Name") emits "Name"
  ✓ TsNullable(T) emits "T | null"

TsAst.Enum
  ✓ creates union of string literals
  ✓ emits: type Status = 'Active' | 'Inactive'

TsAst.File
  ✓ contains ordered list of declarations
  ✓ can have imports
  ✓ can have exports
```

---

### 2. TypeScript Emitter (`TsEmit`)

**Responsibility:** Convert TsAst to string

**Specs:**
```
TsEmit.Interface
  ✓ emits "export interface Name {"
  ✓ emits properties with correct indentation
  ✓ emits "}" on final line
  ✓ optional properties have "?"
  ✓ empty interface emits "{}"

TsEmit.Property
  ✓ emits "name: type;"
  ✓ emits "name?: type;" when optional
  ✓ camelCase conversion from PascalCase

TsEmit.File
  ✓ emits header comment
  ✓ emits imports first
  ✓ emits declarations in order
  ✓ blank line between declarations
  ✓ no trailing whitespace
```

---

### 3. Type Analyzer (`TypeAnalyzer`)

**Responsibility:** C# Type → TsAst

**Specs:**
```
TypeAnalyzer.Primitives
  ✓ string → TsString
  ✓ int/long/decimal/double → TsNumber
  ✓ bool → TsBoolean
  ✓ DateTime/DateOnly → TsString
  ✓ Guid → TsString

TypeAnalyzer.Nullables
  ✓ int? → TsNullable(TsNumber)
  ✓ string? → TsNullable(TsString)

TypeAnalyzer.Collections
  ✓ T[] → TsArray(T)
  ✓ List<T> → TsArray(T)
  ✓ IReadOnlyList<T> → TsArray(T)
  ✓ IEnumerable<T> → TsArray(T)

TypeAnalyzer.Dictionaries
  ✓ Dictionary<string, T> → TsRecord(TsString, T)
  ✓ IReadOnlyDictionary<K, V> → TsRecord(K, V)

TypeAnalyzer.Enums
  ✓ enum Status { A, B } → TsUnion([TsLiteral("A"), TsLiteral("B")])

TypeAnalyzer.Records
  ✓ record Foo(int X) → TsInterface("Foo", [TsProperty("x", TsNumber)])
  ✓ nested records traversed
  ✓ circular references handled (TsRef)

TypeAnalyzer.Traversal
  ✓ discovers all dependent types
  ✓ deduplicates types
  ✓ orders: enums first, then interfaces
```

---

### 4. Zod AST (`ZodAst`)

**Responsibility:** Immutable representation of Zod schemas

**Specs:**
```
ZodAst.Schema
  ✓ has name and shape (ZodObject)
  ✓ emits type inference export

ZodAst.Object
  ✓ contains list of properties
  ✓ properties preserve order

ZodAst.Types
  ✓ ZodString emits "z.string()"
  ✓ ZodNumber emits "z.number()"
  ✓ ZodBoolean emits "z.boolean()"
  ✓ ZodArray(T) emits "z.array(T)"
  ✓ ZodOptional(T) emits "T.optional()"
  ✓ ZodNullable(T) emits "T.nullable()"

ZodAst.Validators
  ✓ Min(n) emits ".min(n)"
  ✓ Max(n) emits ".max(n)"
  ✓ Email emits ".email()"
  ✓ Url emits ".url()"
  ✓ Regex(p) emits ".regex(/p/)"
  ✓ Gt(n) emits ".gt(n)"
  ✓ Lt(n) emits ".lt(n)"
  ✓ Length(n) emits ".length(n)"

ZodAst.Chaining
  ✓ validators chain: z.string().min(1).max(100).email()
  ✓ order preserved
```

---

### 5. Zod Emitter (`ZodEmit`)

**Responsibility:** Convert ZodAst to string

**Specs:**
```
ZodEmit.Schema
  ✓ emits "export const nameSchema = z.object({"
  ✓ emits properties
  ✓ emits "});"
  ✓ emits type inference: "export type Name = z.infer<typeof nameSchema>;"

ZodEmit.Property
  ✓ emits "name: zodType,"
  ✓ validators chained in order
  ✓ camelCase property names
```

---

### 6. Validator Analyzer (`ValidatorAnalyzer`)

**Responsibility:** FluentValidation → ZodAst

**Specs:**
```
ValidatorAnalyzer.Rules
  ✓ NotEmpty → ZodString + Min(1)
  ✓ MaximumLength(n) → Max(n)
  ✓ MinimumLength(n) → Min(n)
  ✓ EmailAddress → Email
  ✓ GreaterThan(n) → Gt(n)
  ✓ LessThan(n) → Lt(n)
  ✓ Matches(regex) → Regex(pattern)

ValidatorAnalyzer.Discovery
  ✓ finds validators in assembly
  ✓ extracts validated type
  ✓ extracts all rules per property

ValidatorAnalyzer.Unsupported
  ✓ custom validators skipped (server-only)
  ✓ When() conditions noted but not emitted
```

---

### 7. Route Analyzer (`RouteAnalyzer`)

**Responsibility:** Minimal API → Route metadata

**Specs:**
```
RouteAnalyzer.MapMethods
  ✓ MapGet → GET
  ✓ MapPost → POST
  ✓ MapPut → PUT
  ✓ MapDelete → DELETE

RouteAnalyzer.Routes
  ✓ extracts route pattern: "/residents/{id}"
  ✓ extracts route parameters: [{name: "id", type: int}]
  ✓ handles constraints: {id:int}, {slug:regex(^[a-z]+$)}

RouteAnalyzer.ImpulseExtension
  ✓ .Impulse("./Path") extracts component path
  ✓ .Impulse<TProps>() extracts props type
  ✓ .Deferred<T>(key, url) extracts deferred info
  ✓ .Lazy<T>(key, url) extracts lazy info

RouteAnalyzer.TypedResults
  ✓ Results<Ok<T>, NotFound> → extracts T
  ✓ Results<Ok<T>, ValidationProblem> → extracts T + error type
```

---

### 8. Route Emitter (`RouteEmit`)

**Responsibility:** Route metadata → TypeScript

**Specs:**
```
RouteEmit.Routes
  ✓ emits route object with path, method, component
  ✓ emits deferred/lazy metadata
  ✓ emits lazy import: () => import('./path')

RouteEmit.PathBuilders
  ✓ /residents → residents: () => '/residents'
  ✓ /residents/{id} → residentDetail: (id: number) => `/residents/${id}`
  ✓ type-safe parameters
```

---

## Design Constraints

1. **Immutable AST** - All nodes are `record` types
2. **No exceptions** - Return `Result<T, Error>` for fallible ops
3. **Pure functions** - Emitters have no side effects
4. **Composable** - Each module testable in isolation
5. **Extensible** - Visitor pattern for AST transforms

## Function Signatures

```csharp
// Core types
record TsFile(IReadOnlyList<TsDeclaration> Declarations);
record TsInterface(string Name, IReadOnlyList<TsProperty> Properties);
record TsProperty(string Name, TsType Type, bool Optional = false);
abstract record TsType;

// Emitter (pure function)
static string Emit(TsFile file);
static string Emit(TsInterface iface);
static string Emit(TsType type);

// Analyzer (pure function)
static TsFile Analyze(IEnumerable<Type> types);
static TsInterface Analyze(Type type);
static TsType Analyze(PropertyInfo property);

// Validator analyzer
static ZodFile Analyze(IEnumerable<IValidator> validators);
static ZodSchema Analyze<T>(IValidator<T> validator);
```

## Test Structure

```
tests/Impulse.CodeGen.V2.Tests/
├── Ast/
│   ├── TsAstTests.cs      # AST construction
│   └── ZodAstTests.cs
├── Emitters/
│   ├── TsEmitTests.cs     # AST → string
│   └── ZodEmitTests.cs
├── Analyzers/
│   ├── TypeAnalyzerTests.cs    # Type → TsAst
│   ├── ValidatorAnalyzerTests.cs
│   └── RouteAnalyzerTests.cs
└── Integration/
    └── EndToEndTests.cs   # Full pipeline
```
