# Impulse v2 - App Developer Guide

> What you see and do when building apps with Impulse

## Core Principles

1. **Server-Driven** - Same URL returns HTML (browser) or JSON (`X-Impulse: 1` header)
2. **Zero Magic Strings** - All paths from generated `RoutePaths` / `Routes` constants
3. **Type Safety** - C# types → TypeScript types, FluentValidation → Zod
4. **Test-First** - Write tests before handlers, tests define the contract
5. **Single Command** - `dotnet run` handles everything (build, generate, serve, HMR)

---

## 1. Getting Started

```bash
dotnet new impulse -n MyApp
cd MyApp
dotnet run
# Browser opens at localhost:5173, HMR enabled
```

---

## 2. Project Structure

```
MyApp/
├── Program.cs                 # YOU WRITE: Route mappings
├── Handlers/                  # YOU WRITE: Request handlers
├── Validators/                # YOU WRITE: FluentValidation rules
├── Models/                    # YOU WRITE: Props types (C# records)
│
├── Tests/                     # YOU WRITE: Tests FIRST
│   ├── Handlers/
│   │   └── ResidentsHandlerTests.cs
│   └── Validators/
│       └── CreateResidentValidatorTests.cs
│
├── src/
│   ├── features/              # YOU WRITE: React components
│   │   └── Residents/
│   │       ├── List.tsx
│   │       ├── List.test.tsx  # YOU WRITE: Test FIRST
│   │       ├── Detail.tsx
│   │       └── Detail.test.tsx
│   ├── App.tsx                # YOU WRITE: Root layout
│   ├── main.tsx               # FROM TEMPLATE: App entry
│   └── impulse/
│       └── hooks.ts           # FROM TEMPLATE: useImpulseMutation
│
├── generated/                 # AUTO-GENERATED (don't edit)
│   ├── types.ts               # C# records → TS interfaces
│   ├── validation.ts          # FluentValidation → Zod schemas
│   ├── mutations.ts           # POST/PUT/DELETE hooks
│   └── routeTree.ts           # Routes + loaders + router
│
├── vite.config.ts             # FROM TEMPLATE: Dev proxy config
└── package.json               # FROM TEMPLATE: Dependencies
```

**Legend:**
- `YOU WRITE` - Your application code (tests first!)
- `FROM TEMPLATE` - Static files, can customize
- `AUTO-GENERATED` - Regenerated on C# changes, don't edit

---

## 3. What You Write (C#)

### Program.cs - Route Mappings

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddImpulse();

var app = builder.Build();
app.UseImpulse();

// Routes.Residents.List is generated constant "/residents"
app.MapGet(Routes.Residents.List, ResidentsHandler.List)
   .Impulse<ResidentListProps>();

app.MapGet(Routes.Residents.Detail, ResidentsHandler.Detail)
   .Impulse<ResidentDetailProps>();

app.MapPost(Routes.Residents.Create, ResidentsHandler.Create);

app.Run();
```

### Handler - Business Logic

```csharp
public static class ResidentsHandler
{
    public static async Task<Results<Ok<ResidentDetailProps>, NotFound>> Detail(
        int id, IResidentService service, CancellationToken ct)
    {
        var resident = await service.GetAsync(id, ct);
        if (resident is null) return TypedResults.NotFound();
        return TypedResults.Ok(new ResidentDetailProps(resident));
    }
}
```

### Props - Data Contracts

```csharp
public record ResidentListProps(IReadOnlyList<ResidentSummary> Residents);
public record ResidentDetailProps(Resident Resident, DateTime CreatedAt);
public record CreateResidentRequest(string Name, string Email);
```

### Validator - Generates Zod

```csharp
public class CreateResidentValidator : AbstractValidator<CreateResidentRequest>
{
    public CreateResidentValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

---

## 4. What Gets Generated (TypeScript)

When you build, these files are auto-generated from your C# code:

### generated/types.ts

```typescript
// From your C# records
export interface ResidentListProps {
  residents: Array<{ id: number; name: string }>
}

export interface ResidentDetailProps {
  resident: { id: number; name: string; email: string }
  createdAt: string
}

export interface CreateResidentRequest {
  name: string
  email: string
}
```

### generated/validation.ts

```typescript
// From your FluentValidation rules
import { z } from 'zod'

export const CreateResidentSchema = z.object({
  name: z.string().min(1).max(100),
  email: z.string().min(1).email(),
})
```

### generated/mutations.ts

```typescript
// From your MapPost/MapPut/MapDelete endpoints
import { useRouter } from '@tanstack/react-router'
import { useImpulseMutation } from '../src/impulse/hooks'
import { Routes } from './routeTree'
import { CreateResidentSchema } from './validation'
import type { CreateResidentRequest } from './types'

export function useCreateResident() {
  const router = useRouter()
  return useImpulseMutation<CreateResidentRequest, { id: number }>({
    endpoint: Routes.residents(),
    method: 'POST',
    schema: CreateResidentSchema,
    onSuccess: () => router.invalidate(),  // Refreshes all route data
  })
}
```

### generated/routeTree.ts

```typescript
// From your .Impulse<T>() route mappings
import { createRouter, createRoute, createRootRouteWithContext, lazyRouteComponent } from '@tanstack/react-router'
import type { ResidentListProps, ResidentDetailProps } from './types'

// Zero magic strings - all paths are constants
export const RoutePaths = {
  residents: '/residents',
  residentDetail: '/residents/$id',
} as const

export const Routes = {
  residents: () => RoutePaths.residents,
  residentDetail: (id: number | string) =>
    RoutePaths.residentDetail.replace('$id', String(id)),
}

// Router context for DI
export interface ImpulseContext {
  impulseFetch: <T>(url: string) => Promise<T>
  invalidate: () => Promise<void>
}

// Type registration for full type safety
declare module '@tanstack/react-router' {
  interface Register { router: ReturnType<typeof createImpulseRouter> }
}

// Routes with typed loaders
const rootRoute = createRootRouteWithContext<ImpulseContext>()({
  component: lazyRouteComponent(() => import('../src/App')),
})

const residentsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: RoutePaths.residents,
  loader: ({ context }) => context.impulseFetch<ResidentListProps>(RoutePaths.residents),
  component: lazyRouteComponent(() => import('../src/features/Residents/List')),
})

const residentDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: RoutePaths.residentDetail,
  loader: ({ context, params }) =>
    context.impulseFetch<ResidentDetailProps>(Routes.residentDetail(params.id)),
  component: lazyRouteComponent(() => import('../src/features/Residents/Detail')),
})

export const routeTree = rootRoute.addChildren([residentsRoute, residentDetailRoute])

export function createImpulseRouter(userContext?: Partial<ImpulseContext>) {
  const impulseFetch = <T>(url: string): Promise<T> =>
    fetch(url, { headers: { 'X-Impulse': '1' } }).then(r => r.json())

  const router = createRouter({
    routeTree,
    context: { impulseFetch, invalidate: () => router.invalidate(), ...userContext },
  })
  return router
}
```

---

## 5. What You Write (React)

### Component - Using Loader Data

```tsx
// src/features/Residents/Detail.tsx
import { getRouteApi } from '@tanstack/react-router'

const route = getRouteApi('/residents/$id')

export default function ResidentDetail() {
  const data = route.useLoaderData()  // Typed: ResidentDetailProps
  return <h1>{data.resident.name}</h1>
}
```

### Form - Using Generated Mutation

```tsx
// src/features/Residents/Create.tsx
import { useCreateResident } from '../../generated/mutations'

export default function CreateResident() {
  const { register, errors, submit, isSubmitting } = useCreateResident()
  return (
    <form onSubmit={submit}>
      <input {...register('name')} />
      {errors.name && <span>{errors.name.message}</span>}
      <input {...register('email')} />
      {errors.email && <span>{errors.email.message}</span>}
      <button disabled={isSubmitting}>Create</button>
    </form>
  )
}
```

### Navigation - Using Generated Routes

```tsx
import { Link } from '@tanstack/react-router'
import { Routes } from '../generated/routeTree'

// Type-safe navigation, no magic strings
<Link to={Routes.residentDetail(resident.id)}>View Details</Link>
```

---

## 6. How It Works

### Server-Driven Architecture

Same URL, different response based on request:

```
Browser navigation:  GET /residents/123           → Full HTML page
Client navigation:   GET /residents/123 + X-Impulse:1 → JSON only
```

### Development Flow

```
dotnet run
    ↓
┌────────────────────────────────────────────────────┐
│ 1. BUILD: C# compiles, source generator runs       │
│    → Routes.g.cs (C# constants)                    │
│    → generated/*.ts (TypeScript files)             │
└────────────────────────────────────────────────────┘
    ↓
┌────────────────────────────────────────────────────┐
│ 2. SERVE: Two servers start automatically          │
│    Kestrel :5000 → handles data/HTML               │
│    Vite :5173 → serves React + HMR                 │
│    Browser → Vite → proxy to Kestrel               │
└────────────────────────────────────────────────────┘
    ↓
┌────────────────────────────────────────────────────┐
│ 3. EDIT:                                           │
│    .tsx change → Vite HMR → ~50ms                  │
│    .cs change → rebuild + regenerate → ~2s         │
└────────────────────────────────────────────────────┘
```

### Production Build

```bash
dotnet publish -c Release
```

```
┌────────────────────────────────────────────────────┐
│ 1. BUILD: Source gen + bun run build               │
│    → dist/assets/index-[hash].js                   │
│    → wwwroot/ (copied)                             │
└────────────────────────────────────────────────────┘
    ↓
┌────────────────────────────────────────────────────┐
│ 2. DEPLOY: Single Kestrel process                  │
│    /assets/* → static files (immutable cache)      │
│    /* → HTML with embedded props + hashed assets   │
└────────────────────────────────────────────────────┘
```

---

## 7. Customization

### Add Auth Header to All Requests

```typescript
// src/main.tsx
const router = createImpulseRouter({
  impulseFetch: async (url) => {
    const res = await fetch(url, {
      headers: { 'X-Impulse': '1', 'Authorization': `Bearer ${getToken()}` },
    })
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    return res.json()
  },
})
```

### Add Custom Context (e.g., Analytics)

```typescript
// Extend the context type
declare module './generated/routeTree' {
  interface ImpulseContext { analytics: AnalyticsClient }
}

// Provide implementation
const router = createImpulseRouter({ analytics: new AnalyticsClient() })

// Use in components
const { analytics } = useRouterContext()
```

---

## 8. Testing Your App

**Write tests first. Tests define what your handler should do.**

### Test-First: Handler

```csharp
// 1. Write the test FIRST (RED)
public class ResidentsHandlerTests
{
    [Fact]
    public async Task Detail_Returns_Resident_When_Found()
    {
        // Arrange - define expected behavior
        var service = new MockResidentService();
        service.Setup(1, new Resident { Id = 1, Name = "John" });

        // Act
        var result = await ResidentsHandler.Detail(1, service, CancellationToken.None);

        // Assert - exact contract
        var ok = result.Result.Should().BeOfType<Ok<ResidentDetailProps>>().Subject;
        ok.Value.Resident.Name.Should().Be("John");
    }

    [Fact]
    public async Task Detail_Returns_NotFound_When_Missing()
    {
        var service = new MockResidentService();  // No setup = not found

        var result = await ResidentsHandler.Detail(999, service, CancellationToken.None);

        result.Result.Should().BeOfType<NotFound>();
    }
}

// 2. Run test - it fails (method doesn't exist)
// 3. Write minimal handler to pass (GREEN)
// 4. Refactor with confidence
```

### Test-First: Validator

```csharp
public class CreateResidentValidatorTests
{
    private readonly CreateResidentValidator _validator = new();

    [Fact]
    public void Rejects_Empty_Name()
    {
        var request = new CreateResidentRequest("", "test@example.com");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Rejects_Invalid_Email()
    {
        var request = new CreateResidentRequest("John", "not-an-email");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Accepts_Valid_Request()
    {
        var request = new CreateResidentRequest("John", "john@example.com");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
```

### Integration Test: Full Request

```csharp
public class ResidentsIntegrationTests : IClassFixture<ImpulseTestFixture>
{
    private readonly HttpClient _client;

    public ResidentsIntegrationTests(ImpulseTestFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task Get_Residents_Returns_JSON_With_Impulse_Header()
    {
        _client.DefaultRequestHeaders.Add("X-Impulse", "1");

        var response = await _client.GetAsync(Routes.Residents.List);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var props = await response.Content.ReadFromJsonAsync<ResidentListProps>();
        props.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_Residents_Returns_HTML_Without_Header()
    {
        var response = await _client.GetAsync(Routes.Residents.List);

        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("__IMPULSE_PROPS__");
    }
}
```

### Component Test (Vitest)

```typescript
// src/features/Residents/Detail.test.tsx
import { render, screen } from '@testing-library/react'
import { createTestRouter } from '@impulse/testing'
import ResidentDetail from './Detail'

describe('ResidentDetail', () => {
  it('displays resident name from loader data', async () => {
    // Arrange - mock loader data
    const router = createTestRouter({
      loaderData: {
        '/residents/$id': {
          resident: { id: 1, name: 'John Doe', email: 'john@example.com' },
          createdAt: '2024-01-01',
        },
      },
    })

    // Act
    render(<ResidentDetail />, { wrapper: router })

    // Assert
    expect(await screen.findByText('John Doe')).toBeInTheDocument()
  })
})
```

### Form Test (Vitest)

```typescript
// src/features/Residents/Create.test.tsx
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { createTestRouter, mockMutation } from '@impulse/testing'
import CreateResident from './Create'

describe('CreateResident', () => {
  it('submits valid form data', async () => {
    const onSubmit = mockMutation()
    const router = createTestRouter({ mutations: { useCreateResident: onSubmit } })

    render(<CreateResident />, { wrapper: router })

    fireEvent.change(screen.getByRole('textbox', { name: /name/i }), {
      target: { value: 'John Doe' },
    })
    fireEvent.change(screen.getByRole('textbox', { name: /email/i }), {
      target: { value: 'john@example.com' },
    })
    fireEvent.click(screen.getByRole('button', { name: /create/i }))

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith({
        name: 'John Doe',
        email: 'john@example.com',
      })
    })
  })

  it('shows validation errors for invalid input', async () => {
    render(<CreateResident />, { wrapper: createTestRouter() })

    fireEvent.click(screen.getByRole('button', { name: /create/i }))

    expect(await screen.findByText(/name is required/i)).toBeInTheDocument()
  })
})
```

### Run Tests

```bash
# C# tests
dotnet test

# TypeScript tests
bun test

# All tests (before commit)
dotnet test && bun test
```

---

## 9. Tech Stack

| You Use | Purpose |
|---------|---------|
| .NET 10 Minimal API | Route handlers, business logic |
| FluentValidation | Validation rules (→ generates Zod) |
| React 19 | UI components |
| TanStack Router | Navigation, data loading |
| react-hook-form | Form state (via useImpulseMutation) |

| Runs Automatically | Purpose |
|--------------------|---------|
| Roslyn Source Gen | Generates routes + TypeScript |
| Vite | Dev server, HMR, bundling |
| Bun | Package management, TS compilation |
