# Critical Analysis: Gaps Between Specs and Implementation

## Diagram: Actual Data Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                         BUILD TIME                                   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Developer Code                                                      │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │ app.MapGet("/residents/{id}", handler)                        │   │
│  │    .Impulse<ResidentDetailProps>("./Detail");                 │   │
│  │                                                               │   │
│  │ public class CreateValidator : AbstractValidator<CreateReq>   │   │
│  │ { RuleFor(x => x.Name).NotEmpty().MaxLength(100); }          │   │
│  └──────────────────────────────────────────────────────────────┘   │
│         │                                                            │
│         ▼                                                            │
│  ┌─────────────────────┐                                            │
│  │  ROSLYN COMPILER    │                                            │
│  │  + Source Generator │                                            │
│  └─────────────────────┘                                            │
│         │                                                            │
│         ├──────────────────────┬────────────────────┐               │
│         ▼                      ▼                    ▼               │
│  ┌────────────┐    ┌─────────────────┐    ┌──────────────┐         │
│  │ Routes.g.cs│    │ Handlers.g.cs   │    │ model.json   │         │
│  │ (C# only)  │    │ (C# only)       │    │ (serialized) │         │
│  └────────────┘    └─────────────────┘    └──────────────┘         │
│                                                   │                  │
│                                                   ▼                  │
│                              ┌─────────────────────────────┐        │
│                              │    MSBUILD TASK             │        │
│                              │    (AfterBuild)             │        │
│                              │    Reads model.json         │        │
│                              │    Writes TypeScript        │        │
│                              └─────────────────────────────┘        │
│                                           │                          │
│         ┌────────────┬───────────┬───────┴───────┐                  │
│         ▼            ▼           ▼               ▼                  │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐           │
│  │ types/   │ │ routes/  │ │validation│ │ forms/       │           │
│  │ *.ts     │ │ *.tsx    │ │ *.ts     │ │ use*.ts      │           │
│  └──────────┘ └──────────┘ └──────────┘ └──────────────┘           │
│                    │                                                 │
│                    ▼                                                 │
│         ┌─────────────────────┐                                     │
│         │ TanStack Router     │                                     │
│         │ Vite Plugin         │                                     │
│         │ (reads route files) │                                     │
│         └─────────────────────┘                                     │
│                    │                                                 │
│                    ▼                                                 │
│         ┌─────────────────────┐                                     │
│         │ routeTree.gen.ts    │  ← SECOND generation (redundant?)   │
│         └─────────────────────┘                                     │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Gap 1: "No Magic Strings" Claim is Misleading

**The Spec Claims:**
> "No magic strings in endpoint registration"
> `app.MapGet(Routes.Residents.Detail, DetailHandler);`

**The Reality:**
The generator READS the route pattern from MapGet. It can't generate `Routes.Residents.Detail` unless the pattern exists in source code FIRST.

**Actual Flow:**
```csharp
// Step 1: Developer writes (HAS magic string)
app.MapGet("/residents/{id:int}", handler).Impulse<Props>();

// Step 2: Generator reads "/residents/{id:int}" and emits
public const string Detail = "/residents/{id:int}";

// Step 3: OTHER code uses generated constant
var url = Routes.Residents.DetailPath(123);  // No magic string here
```

**Resolution:**
Either:
a) Accept that registration has the string (it's the source of truth)
b) Define routes separately with attributes:
```csharp
[ImpulseRoute("/residents/{id:int}")]
public static Task<Props> Detail(int id) { ... }
```

---

## Gap 2: Source Generator Cannot Emit TypeScript

**The SPECS-V2.md Diagram Shows:**
```
ModelBuilder → ImpulseModel → TypesGen → .ts
                            → RoutesGen → .tsx
```

**The Reality:**
Roslyn Source Generators can ONLY emit C# code. They run inside the compiler process.

**ROADMAP.md is Correct:**
- Source Generator → Routes.g.cs, Handlers.g.cs, model.json
- MSBuild Task → reads model.json → emits TypeScript

**Fix:** Update SPECS-V2.md diagram to show two-phase generation.

---

## Gap 3: Model Serialization Undefined

**Question:** How does ImpulseModel travel from Source Generator to MSBuild Task?

**Options:**
```
Option A: File-based
┌──────────────┐    model.json    ┌─────────────┐
│ Source Gen   │ ───────────────▶ │ MSBuild Task│
└──────────────┘   ($(IntDir))    └─────────────┘

Option B: Assembly-embedded
┌──────────────┐    embedded      ┌─────────────┐
│ Source Gen   │ ───resource────▶ │ MSBuild Task│
└──────────────┘    in DLL        └─────────────┘

Option C: Generated C# writes JSON
┌──────────────┐ ImpulseModel.g.cs ┌─────────────┐
│ Source Gen   │ ─────────────────▶│ static ctor │
└──────────────┘                   │ writes JSON │
                                   └─────────────┘
```

**Missing Spec:** Which option? When does model.json get written?

---

## Gap 4: TanStack Router Double Generation

**The Flow:**
1. Impulse generates: `routes/residents/$id.tsx`
2. TanStack plugin reads routes/ → generates `routeTree.gen.ts`

**Problem:** Two code generators processing the same thing.

**Better Options:**
```
Option A: Skip file-based routing
┌─────────────┐
│ Impulse Gen │──▶ routeTree.gen.ts directly
└─────────────┘    (no intermediate files)

Option B: Virtual file routes
┌─────────────┐
│ Impulse Gen │──▶ virtual route config
└─────────────┘    (TanStack reads from config, not files)
```

---

## Gap 5: useImpulseForm Not Defined

**Generated Code References:**
```typescript
export function useCreateResident() {
  return useImpulseForm({  // ← WHERE IS THIS?
    schema: createResidentSchema,
    action: routes.createResident.submit,
    onError: (problem: ProblemDetails) => mapFieldErrors(problem),
  })
}
```

**Missing Runtime Library:**
```
Impulse.Runtime (C#)     →  asset serving, middleware
@impulse/react (npm)     →  useImpulseForm, ProblemDetails, etc.  ← MISSING
```

**Need to Specify:**
- `useImpulseForm(config)` - form state + validation + submission
- `ProblemDetails` - RFC 7807 type
- `mapFieldErrors(problem)` - converts server errors to form errors
- `defer(promise)` - TanStack defer wrapper

---

## Gap 6: Deferred Data Typing

**Loader Returns:**
```typescript
return {
  props: await props,           // ResidentDetailProps
  meds: defer(fetchMeds()),     // Promise<MedicationList>
}
```

**Component Receives:**
```typescript
const data = useLoaderData()
// data.props is ResidentDetailProps
// data.meds is Deferred<MedicationList>  ← HOW TYPED?
```

**Props Type Doesn't Capture This:**
```typescript
interface ResidentDetailProps {
  id: number;
  name: string;
  // meds not here - it's deferred
}
```

**Need New Type:**
```typescript
// Generated loader data type
interface ResidentDetailLoaderData {
  props: ResidentDetailProps;
  meds: Deferred<MedicationList>;  // From .Deferred<T>()
}
```

---

## Gap 7: MSBuild Exec Blocks Forever

**The Target:**
```xml
<Target Name="ImpulseStartVite" BeforeTargets="Run">
  <Exec Command="bun run dev" ContinueOnError="true" />
</Target>
```

**Problem:** `bun run dev` is a dev server - it runs forever.
`Exec` blocks until command completes.
`ContinueOnError` only helps if command FAILS, not if it runs.

**Solutions:**
```xml
<!-- Option A: Background process (Windows) -->
<Exec Command="start /B bun run dev" />

<!-- Option B: Background process (Unix) -->
<Exec Command="bun run dev &amp;" />

<!-- Option C: Use separate task that spawns background -->
<StartProcess Executable="bun" Arguments="run dev"
              WorkingDirectory="..." />

<!-- Option D: Don't use MSBuild, use .NET middleware -->
// In Program.cs
if (env.IsDevelopment())
    app.UseViteDevServer();  // Spawns process on first request
```

---

## Gap 8: First Build Race Condition

**Timeline:**
```
T0: dotnet run
T1: Restore packages
T2: Compile (source generator runs)
T3: Build complete (model.json written)
T4: AfterBuild (TypeScript generator runs)
T5: Generated/*.ts written
T6: Vite dev server starts
T7: Vite reads generated files
T8: Kestrel starts
T9: Browser requests /
```

**Potential Race:**
- If Vite starts watching BEFORE T5, it sees missing files
- Vite plugin might fail or have stale cache

**Solution:**
- MSBuild must FINISH TypeScript generation BEFORE Vite starts
- Or: Vite waits for generated files (file watcher)

---

## Gap 9: Convention vs Explicit Path

**ROADMAP 8.4 (Convention):**
```csharp
.Impulse<ResidentListProps>();  // No path - discovers from handler location
```

**ROADMAP 1.1 (Explicit):**
```csharp
.Impulse<ResidentListProps>("./Residents/List");  // Path provided
```

**Both Are Valid, But Spec Unclear:**

```
Convention Discovery Algorithm:
1. Handler: Features/Residents/ResidentsHandler.cs
2. Method: List
3. Component: Features/Residents/Components/List.tsx
   (Replace *Handler.cs with Components/*.tsx)

Explicit Override:
1. Path: "./Detail"
2. Relative to handler: Features/Residents/Components/Detail.tsx
```

**Need Clear Rule:**
```csharp
// No path = convention
.Impulse<Props>()  // → {HandlerDir}/Components/{MethodName}.tsx

// Relative path = relative to handler
.Impulse<Props>("./Detail")  // → {HandlerDir}/Components/Detail.tsx

// Absolute path = from project root
.Impulse<Props>("/Shared/Detail")  // → Shared/Detail.tsx
```

---

## Gap 10: Routes.g.cs Two Different Uses

**Use 1: Registration (spec shows both)**
```csharp
// Developer writes route string
app.MapGet("/residents", handler);

// OR developer uses generated const (circular?)
app.MapGet(Routes.Residents.List, handler);
```

**Use 2: Path Building (clear)**
```csharp
var url = Routes.Residents.DetailPath(123);  // "/residents/123"
```

**Resolution:**
The route string in MapGet IS the source of truth. Generator reads it and emits Routes.g.cs for:
1. Path builders (with params)
2. TypeScript route constants
3. Other C# code that needs the pattern

The registration itself keeps the string - that's okay.

---

## Summary: What Needs Fixing

| Gap | Severity | Fix |
|-----|----------|-----|
| 1. "No magic strings" claim | Medium | Clarify: registration has string, other code uses const |
| 2. TS gen in source gen diagram | Low | Fix SPECS-V2.md diagram |
| 3. Model serialization | High | Specify: file path, format, timing |
| 4. Double route generation | Medium | Decide: generate routeTree directly? |
| 5. useImpulseForm undefined | High | Add @impulse/react package spec |
| 6. Deferred typing | High | Add LoaderData type generation |
| 7. Exec blocks | High | Use background process or middleware |
| 8. Race condition | Medium | Ensure ordering in MSBuild |
| 9. Convention unclear | Medium | Document algorithm clearly |
| 10. Route const usage | Low | Clarify in docs |
