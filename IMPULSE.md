# Impulse Framework

**.NET 10 + React 19 Server-Driven UI**

## Core Principle

Server sends props, client renders. No magic - everything explicit.

## Features

### 1. Type-Safe Props
```csharp
// Server defines what client receives
public record ResidentsListProps(
    IReadOnlyList<ResidentSummary> Residents,
    int TotalCount
);

// Register endpoint
app.MapGet("/residents", () => new ResidentsListProps(...))
   .AsComponent<ResidentsListProps>();
```

```typescript
// Auto-generated types.g.ts
export interface ResidentsListProps {
  residents: readonly ResidentSummary[];
  totalCount: number;
}
```

### 2. Colocated Features
```
Features/
├── Residents/
│   ├── Handler.cs      // Server logic + Props
│   ├── Component.tsx   // React component
│   └── Validator.cs    // FluentValidation (optional)
├── Wizard/
│   ├── Handler.cs
│   ├── Component.tsx
│   └── Validator.cs
└── Shared/
    ├── types.g.ts      // Generated
    ├── validation.g.ts // Generated
    └── index.ts        // Entry point
```

### 3. Validation (Server → Client)
```csharp
// Server: FluentValidation
public class PersonValidator : AbstractValidator<PersonData>
{
    public PersonValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Age).GreaterThan(0).LessThan(150);
    }
}
```

```typescript
// Client: Auto-generated Zod schema
export const personDataSchema = z.object({
  firstName: z.string().min(1).max(50),
  email: z.string().min(1).email(),
  age: z.number().gt(0).lt(150),
});

// Usage
const result = personDataSchema.safeParse(formData);
if (!result.success) {
  showErrors(result.error.issues);
}
```

### 4. Dynamic Forms
```csharp
// Server controls field visibility
new FormField(
    Name: "employer",
    Label: "Employer",
    Required: true,
    VisibleWhen: new VisibilityCondition(
        Field: "employmentStatus",
        Operator: ConditionOperator.Equals,
        Value: "employed"
    )
)
```

### 5. Deferred & Lazy Loading
```csharp
// Deferred: Auto-fetch after hydration
app.MapGet("/residents/{id}", handler)
   .AsComponent<ResidentDetailProps>()
   .Deferred<MedicationsProps>("medications", "/residents/{id}/medications");

// Lazy: Fetch on demand
   .Lazy<DocumentsProps>("documents", "/residents/{id}/documents");
```

```typescript
// Client usage
const meds = useDeferred<MedicationsProps>('medications', urls);
const [docs, loadDocs] = useLazy<DocumentsProps>('documents', urls);
```

### 6. Mutations with Validation
```csharp
app.MapPost("/residents", handler)
   .AsMutation<CreateResidentRequest, CreateResidentResponse>();
```

```typescript
const { mutate, state } = useMutation<CreateReq, CreateRes>('/residents');

// Validate client-side first
const validation = createResidentRequestSchema.safeParse(data);
if (validation.success) {
  mutate(data); // Server validates again
}
```

## How It Works

```
┌─────────────────┐     ┌─────────────────┐
│  .NET Backend   │────▶│  React Client   │
│                 │     │                 │
│ • Props records │     │ • types.g.ts    │
│ • Handlers      │     │ • Components    │
│ • Validators    │     │ • validation.g  │
└─────────────────┘     └─────────────────┘
         │                       │
         └───── Same Types ──────┘
```

**Build:**
```bash
dotnet build  # Generates types.g.ts, validation.g.ts
npm run build # Bundles React app
```

**Dev:**
```bash
dotnet watch  # Hot reload backend
npm run dev   # Vite dev server
```

## Key Files

| File | Purpose |
|------|---------|
| `types.g.ts` | TypeScript interfaces from C# records |
| `validation.g.ts` | Zod schemas from FluentValidation |
| `registry.g.ts` | Component path → React component map |
| `routes.g.ts` | Type-safe route helpers |

## Conventions

- Props: `<Feature>Props` → Component at `./Feature`
- Request: `<Action>Request` → Mutation input
- Response: `<Action>Response` → Mutation output
- Validator: `<Type>Validator` → Zod schema generated

## No Magic

1. **Explicit registration** - `AsComponent<T>()` declares intent
2. **Explicit paths** - Component paths derived from type names
3. **Explicit validation** - FluentValidation rules → Zod (you see both)
4. **Explicit loading** - Deferred vs Lazy explicitly chosen
5. **Explicit types** - Generated `.g.ts` files are readable

## Tests

```bash
# Unit tests
dotnet run --project tests/Impulse.Core.Tests
dotnet run --project tests/Impulse.CodeGen.Tests
dotnet run --project tests/Impulse.Validation.Tests

# E2E tests
cd tests/Impulse.Playwright.Tests
npx playwright test
```
