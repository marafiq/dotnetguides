# Impulse v2 Specification

## Core Principles

1. **Server-Driven** - Same URL returns HTML (browser) or JSON (`X-Impulse: 1` header)
2. **Zero Magic Strings** - All paths from generated `RoutePaths` / `Routes`
3. **Router Context** - DI via TanStack Router context, not imports
4. **Invalidation** - Mutations call `router.invalidate()` to refresh
5. **4 Generated Files** - types.ts, validation.ts, mutations.ts, routeTree.ts

---

## 1. Server-Driven Architecture

```
Initial Load:   GET /residents/123 → HTML with <script id="__IMPULSE_PROPS__">{...}</script>
Navigation:     GET /residents/123 + X-Impulse:1 → JSON only
```

| API-First (wrong) | Server-Driven (Impulse) |
|-------------------|-------------------------|
| `/api/residents` returns JSON | `/residents` returns HTML or JSON |
| Client fetches separately | Same URL, content negotiation |

---

## 2. Generated TypeScript

### routeTree.ts

```typescript
import {
  createRouter, createRoute, createRootRouteWithContext, lazyRouteComponent,
} from '@tanstack/react-router'

export interface ImpulseContext {
  impulseFetch: <T>(url: string) => Promise<T>
  invalidate: () => Promise<void>
}

export const RoutePaths = {
  residents: '/residents',
  residentDetail: '/residents/$id',
} as const

export const Routes = {
  residents: () => RoutePaths.residents,
  residentDetail: (id: number | string) =>
    RoutePaths.residentDetail.replace('$id', String(id)),
}

const rootRoute = createRootRouteWithContext<ImpulseContext>()({
  component: lazyRouteComponent(() => import('../src/App')),
})

const residentsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: RoutePaths.residents,
  loader: ({ context }) => context.impulseFetch(RoutePaths.residents),
  component: lazyRouteComponent(() => import('../src/features/Residents/List')),
})

const residentDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: RoutePaths.residentDetail,
  loader: ({ context, params }) =>
    context.impulseFetch(Routes.residentDetail(params.id)),
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

### mutations.ts

```typescript
import { useRouter } from '@tanstack/react-router'
import { useImpulseMutation } from '@impulse/react'
import { Routes } from './routeTree'
import { CreateResidentSchema } from './validation'
import type { CreateResidentRequest, CreateResidentResponse } from './types'

export function useCreateResident() {
  const router = useRouter()
  return useImpulseMutation<CreateResidentRequest, CreateResidentResponse>({
    endpoint: Routes.residents(),
    method: 'POST',
    schema: CreateResidentSchema,
    onSuccess: () => router.invalidate(),
  })
}
```

### validation.ts

```typescript
import { z } from 'zod'

export const CreateResidentSchema = z.object({
  name: z.string().min(1).max(100),
  email: z.string().min(1).email(),
})
```

### types.ts

```typescript
export interface ResidentDetailProps {
  resident: { id: number; name: string }
  createdAt: string
}

export interface CreateResidentRequest {
  name: string
  email: string
}
```

---

## 3. Developer API (C#)

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddImpulse();

var app = builder.Build();
app.UseImpulse();

app.MapGet(Routes.Residents.List, ResidentsHandler.List)
   .Impulse<ResidentListProps>();

app.MapGet(Routes.Residents.Detail, ResidentsHandler.Detail)
   .Impulse<ResidentDetailProps>();

app.MapPost(Routes.Residents.Create, ResidentsHandler.Create);

app.Run();
```

### Handler

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

### Validator (FluentValidation → Zod)

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

## 4. Developer API (React)

### Component

```tsx
// src/features/Residents/Detail.tsx
import { useLoaderData } from '@tanstack/react-router'

export default function ResidentDetail() {
  const data = useLoaderData()
  return <h1>{data.resident.name}</h1>
}
```

### Form with Mutation

```tsx
// src/features/Residents/Create.tsx
import { useCreateResident } from '../../generated/mutations'

export default function CreateResident() {
  const { register, errors, submit, isSubmitting } = useCreateResident()
  return (
    <form onSubmit={submit}>
      <input {...register('name')} />
      {errors.name && <span>{errors.name}</span>}
      <button disabled={isSubmitting}>Create</button>
    </form>
  )
}
```

### App Entry

```tsx
// src/main.tsx
import { createRoot } from 'react-dom/client'
import { RouterProvider } from '@tanstack/react-router'
import { createImpulseRouter } from './generated/routeTree'

createRoot(document.getElementById('root')!).render(
  <RouterProvider router={createImpulseRouter()} />
)
```

---

## 5. Extensibility

### Extend Context

```typescript
declare module './generated/routeTree' {
  interface ImpulseContext { analytics: AnalyticsClient }
}

export const router = createImpulseRouter({ analytics: new AnalyticsClient() })
```

### Override Fetch

```typescript
const router = createImpulseRouter({
  impulseFetch: async (url) => {
    const res = await fetch(url, {
      headers: { 'X-Impulse': '1', 'Authorization': `Bearer ${await getToken()}` },
    })
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    return res.json()
  },
})
```

---

## 6. Tech Stack

| Tool | Purpose |
|------|---------|
| .NET 10 | Minimal API, source generators |
| React 19 | Frontend |
| TanStack Router | Type-safe routing, context DI, loaders |
| FluentValidation | Server validation → generates Zod |
| Zod | Client validation |
| Bun | Runtime, package manager |
| Vite | Bundler, HMR |
| tsgo | Fast TS compiler |

---

## 7. Development Flow

```
dotnet run
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ BUILD: Roslyn → Routes.g.cs + TypeScript.g.cs                   │
│        MSBuild extracts → generated/*.ts                        │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ SERVERS: Kestrel (:5000) + Vite (:5173)                         │
│          Browser → Vite → proxy to Kestrel for data             │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ HMR: Edit .tsx → ~50ms │ Edit .cs → regenerate → ~2s            │
└─────────────────────────────────────────────────────────────────┘
```

---

## 8. Production Flow

```
dotnet publish -c Release
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ BUILD: Source gen → extract TS → bun run build                  │
│        Output: dist/assets/index-[hash].js, manifest.json       │
│        Copy to wwwroot/                                         │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│ RUNTIME: Kestrel only (single process)                          │
│   /assets/* → static files (immutable cache)                    │
│   /* → HTML with __IMPULSE_PROPS__ + hashed asset paths         │
└─────────────────────────────────────────────────────────────────┘
```

---

## 9. Developer Experience

```bash
dotnet new impulse -n MyApp && cd MyApp && dotnet run
# Browser opens, working app, HMR enabled
```

**Automated:** bun install, vite dev server, code generation, proxy config, hashed assets

---

## 10. NuGet Structure

```
Impulse                 → Meta-package (references all below)
├── Impulse.Core        → Attributes, extensions
├── Impulse.SourceGen   → Roslyn generator
├── Impulse.MSBuild     → TS extraction task
├── Impulse.Runtime     → Middleware, asset manifest
└── Impulse.Templates   → dotnet new impulse
```
