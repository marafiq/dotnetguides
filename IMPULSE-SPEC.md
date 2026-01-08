# Impulse v2 - Final Specification

## Core Principles

1. **Server-Driven** - Route URL is the data source, not a separate API
2. **Zero Magic Strings** - All paths generated from C# constants
3. **Router Context** - Dependency injection via TanStack Router context
4. **Invalidation** - Mutations call `router.invalidate()` to refresh data
5. **Extensible** - Users extend context with their own dependencies
6. **4 Generated Files** - types.ts, validation.ts, mutations.ts, routeTree.ts

---

## 1. Server-Driven Architecture

**Impulse is NOT API-first.** The route URL IS the data source.

| API-First (wrong) | Server-Driven (Impulse) |
|-------------------|-------------------------|
| `/api/residents` returns JSON | `/residents` returns HTML or JSON |
| Client fetches data separately | Same URL, different responses |
| Two concerns: API + UI | One concern: server controls UI |

**Content Negotiation:**
- `GET /residents` with browser → HTML with embedded `<script id="__IMPULSE_PROPS__">`
- `GET /residents` with `X-Impulse: 1` header → JSON only

```
Initial Page Load:
┌─────────────────────────────────────────────────────────────┐
│ Browser: GET /residents/123                                  │
│                        ↓                                     │
│ Server returns HTML with embedded props:                     │
│   <script id="__IMPULSE_PROPS__">{"resident":{...}}</script>│
│                        ↓                                     │
│ React hydrates with props - NO fetch needed                  │
└─────────────────────────────────────────────────────────────┘

Client Navigation:
┌─────────────────────────────────────────────────────────────┐
│ User clicks <Link to={Routes.residentDetail(456)}>          │
│                        ↓                                     │
│ TanStack loader: fetch('/residents/456', {X-Impulse: '1'})  │
│                        ↓                                     │
│ Server returns JSON only                                     │
│ React renders - NO full page reload                          │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Generated Files (4 Total)

```
generated/
├── types.ts       # All interfaces (props, requests, responses, loader data)
├── validation.ts  # All Zod schemas (from FluentValidation)
├── mutations.ts   # Mutation hooks with invalidation
└── routeTree.ts   # RoutePaths + Routes + router context + router factory
```

---

## 3. ImpulseModel (Core IR)

**Single intermediate representation built from Minimal API code.**

```csharp
record ImpulseModel(
    IReadOnlyList<EndpointModel> Endpoints,
    IReadOnlyList<MutationModel> Mutations,
    IReadOnlyList<TypeModel> Types,
    IReadOnlyList<ValidatorModel> Validators
);

record EndpointModel(
    string Route,              // "/residents/{id:int}"
    string RouteName,          // "ResidentDetail"
    string ComponentPath,      // "@features/Residents/Detail"
    TypeModel PropsType,
    IReadOnlyList<DeferredModel> Deferred
);

record MutationModel(
    string Route,              // "/residents"
    string RouteName,          // "CreateResident"
    HttpMethod Method,         // POST, PUT, DELETE
    TypeModel RequestType,
    TypeModel ResponseType
);

record ValidatorModel(
    string TypeName,           // "CreateResidentRequest"
    IReadOnlyList<PropertyRules> Rules
);
```

---

## 4. Generated TypeScript

### 4.1 routeTree.ts (Routes + Context + Router)

```typescript
import {
  createRouter,
  createRoute,
  createRootRouteWithContext,
  defer,
} from '@tanstack/react-router'

// ============================================
// ROUTER CONTEXT - Extensible by user
// ============================================
export interface ImpulseContext {
  impulseFetch: <T>(url: string) => Promise<T>
  invalidate: () => Promise<void>
}

// User extends via module augmentation:
// declare module '@impulse/generated/routeTree' {
//   interface ImpulseContext { analytics: AnalyticsClient }
// }

// ============================================
// ROUTE PATHS - Single source of truth (from C#)
// ============================================
export const RoutePaths = {
  residents: '/residents',
  residentDetail: '/residents/$id',
  residentMedications: '/residents/$id/medications',
} as const

// PATH BUILDERS - Derive from RoutePaths, NO inline strings
export const Routes = {
  residents: () => RoutePaths.residents,
  residentDetail: (id: number | string) =>
    RoutePaths.residentDetail.replace('$id', String(id)),
  residentMedications: (id: number | string) =>
    RoutePaths.residentMedications.replace('$id', String(id)),
}

// ============================================
// ROUTES - Use context for fetch
// ============================================
const rootRoute = createRootRouteWithContext<ImpulseContext>()({
  component: () => import('../src/App'),
})

const residentsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: RoutePaths.residents,
  loader: ({ context }) => context.impulseFetch(RoutePaths.residents),
  component: () => import('@features/Residents/List'),
})

const residentDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: RoutePaths.residentDetail,
  loader: async ({ context, params }) => {
    const props = await context.impulseFetch(Routes.residentDetail(params.id))
    return {
      props,
      medications: defer(context.impulseFetch(Routes.residentMedications(params.id))),
    }
  },
  component: () => import('@features/Residents/Detail'),
})

export const routeTree = rootRoute.addChildren([
  residentsRoute,
  residentDetailRoute,
])

// ============================================
// ROUTER FACTORY - User provides extended context
// ============================================
export function createImpulseRouter(userContext?: Partial<ImpulseContext>) {
  const impulseFetch = <T>(url: string): Promise<T> =>
    fetch(url, { headers: { 'X-Impulse': '1' } }).then(r => r.json())

  const router = createRouter({
    routeTree,
    context: {
      impulseFetch,
      invalidate: () => router.invalidate(),
      ...userContext,
    },
  })

  return router
}
```

### 4.2 mutations.ts (with Invalidation)

```typescript
import { Routes } from './routeTree'
import { useRouter } from '@tanstack/react-router'
import { CreateResidentSchema } from './validation'
import type { CreateResidentRequest, CreateResidentResponse } from './types'

export function useCreateResident() {
  const router = useRouter()

  return useImpulseMutation<CreateResidentRequest, CreateResidentResponse>({
    endpoint: Routes.residents(),  // NO STRINGS
    method: 'POST',
    schema: CreateResidentSchema,
    onSuccess: () => router.invalidate(),  // Refresh all route data
  })
}

export function useUpdateResident(id: number) {
  const router = useRouter()

  return useImpulseMutation({
    endpoint: Routes.residentDetail(id),  // NO STRINGS
    method: 'PUT',
    schema: UpdateResidentSchema,
    onSuccess: () => router.invalidate(),
  })
}

export function useDeleteResident(id: number) {
  const router = useRouter()

  return useImpulseMutation({
    endpoint: Routes.residentDetail(id),
    method: 'DELETE',
    onSuccess: async () => {
      await router.invalidate()
      router.navigate({ to: Routes.residents() })
    },
  })
}
```

### 4.3 validation.ts (from FluentValidation)

```typescript
import { z } from 'zod'

export const CreateResidentSchema = z.object({
  name: z.string().min(1).max(100),
  email: z.string().min(1).email(),
})

export const UpdateResidentSchema = z.object({
  name: z.string().min(1).max(100),
})
```

### 4.4 types.ts

```typescript
export interface ResidentSummary {
  id: number
  name: string
}

export interface ResidentDetailProps {
  resident: ResidentSummary
  createdAt: string
}

export interface CreateResidentRequest {
  name: string
  email: string
}

export interface CreateResidentResponse {
  id: number
}

export type Deferred<T> = Promise<T>

export interface ResidentDetailLoaderData {
  props: ResidentDetailProps
  medications: Deferred<MedicationList>
}
```

---

## 5. Developer API (What Users Write)

### 5.1 Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddImpulse();

var app = builder.Build();
app.UseImpulse();

// Routes - NO magic strings
app.MapGet(Routes.Residents.List, ResidentsHandler.List)
   .Impulse<ResidentListProps>();

app.MapGet(Routes.Residents.Detail, ResidentsHandler.Detail)
   .Impulse<ResidentDetailProps>()
   .Deferred<MedicationList>("medications", Routes.Residents.Medications);

app.MapPost(Routes.Residents.Create, ResidentsHandler.Create);

app.Run();
```

### 5.2 Handler

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

### 5.3 Validator (FluentValidation → Zod)

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

### 5.4 React Component

```tsx
import { useLoaderData } from '@tanstack/react-router'
import type { ResidentDetailProps } from '@impulse/generated/types'

export default function ResidentDetail() {
  const { props } = useLoaderData()
  return <h1>{props.resident.name}</h1>
}
```

### 5.5 React Form with Mutation

```tsx
import { useCreateResident } from '@impulse/generated/mutations'

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

### 5.6 App Entry (main.tsx)

```tsx
import { createRoot } from 'react-dom/client'
import { RouterProvider } from '@tanstack/react-router'
import { createImpulseRouter } from '@impulse/generated/routeTree'

const router = createImpulseRouter()

createRoot(document.getElementById('root')!).render(
  <RouterProvider router={router} />
)
```

---

## 6. Extensibility

### 6.1 Extend Router Context

```typescript
import { createImpulseRouter, ImpulseContext } from '@impulse/generated/routeTree'

declare module '@impulse/generated/routeTree' {
  interface ImpulseContext {
    analytics: AnalyticsClient
  }
}

export const router = createImpulseRouter({
  analytics: new AnalyticsClient(),
})
```

### 6.2 Override Fetch (Auth, Logging)

```typescript
const router = createImpulseRouter({
  impulseFetch: async (url) => {
    const token = await getAuthToken()
    const res = await fetch(url, {
      headers: { 'X-Impulse': '1', 'Authorization': `Bearer ${token}` },
    })
    return res.json()
  },
})
```

---

## 7. Source Generator Output

**TypeScript.g.cs** - Embedded TS extracted by MSBuild:

```csharp
public static partial class TypeScriptOutput
{
    public const string Types = """
    /* IMPULSE:types.ts */
    export interface ResidentSummary { id: number; name: string; }
    // ...
    /* END:types.ts */
    """;

    public const string Validation = """
    /* IMPULSE:validation.ts */
    import { z } from 'zod'
    export const CreateResidentSchema = z.object({
      name: z.string().min(1).max(100),
      email: z.string().min(1).email(),
    })
    /* END:validation.ts */
    """;

    public const string Mutations = """
    /* IMPULSE:mutations.ts */
    // ... (imports Routes, uses router.invalidate())
    /* END:mutations.ts */
    """;

    public const string RouteTree = """
    /* IMPULSE:routeTree.ts */
    // ... (RoutePaths, Routes, context, factory)
    /* END:routeTree.ts */
    """;
}
```

**MSBuild extracts via regex** → `ClientApp/generated/*.ts`

---

## 8. Server-Side Runtime

```csharp
// Impulse middleware - content negotiation
app.Use(async (context, next) =>
{
    await next();

    if (context.Items.TryGetValue("ImpulseProps", out var props))
    {
        if (context.Request.Headers.ContainsKey("X-Impulse"))
        {
            // Client navigation → JSON
            context.Response.ContentType = "application/json";
            await JsonSerializer.SerializeAsync(context.Response.Body, props);
        }
        else
        {
            // Initial load → HTML with embedded props
            var html = RenderHtmlWithProps(props);
            await context.Response.WriteAsync(html);
        }
    }
});
```

---

## 9. Data Flow Summary

```
MUTATION FLOW:
┌─────────────────────────────────────────────────────────────────┐
│  1. User submits form with useCreateResident()                  │
│  2. Client validates with Zod schema                            │
│  3. POST to Routes.residents() - NO magic string                │
│  4. Server validates with FluentValidation                      │
│  5. Returns success or ProblemDetails RFC 7807                  │
│  6. onSuccess: router.invalidate()                              │
│  7. All route loaders re-run → UI updates                       │
└─────────────────────────────────────────────────────────────────┘
```

---

## 10. Tech Stack

| Layer | Tool | Version | Why |
|-------|------|---------|-----|
| **Backend** | .NET | 10 | Minimal API, TypedResults, source generators |
| **Frontend** | React | 19 | Latest with concurrent features |
| **Routing** | TanStack Router | 1.x | Type-safe, context-based DI, loaders |
| **Validation (Server)** | FluentValidation | 11.x | Declarative rules, source generator reads |
| **Validation (Client)** | Zod | 3.x | Generated from FluentValidation |
| **JS Runtime** | Bun | 1.x | 10x faster than Node, native TS |
| **Bundler** | Vite | 6.x | Fast HMR, optimized production builds |
| **TS Compiler** | tsgo | 1.x | 10x faster than tsc (Go-based) |
| **Package Manager** | Bun | 1.x | Binary lockfile, faster installs |

---

## 11. Development Flow (HMR)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              dotnet run                                      │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STEP 1: BUILD                                                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Developer Code                    Roslyn + Source Generator                 │
│  ┌────────────────────┐           ┌────────────────────────────────────┐   │
│  │ Program.cs         │           │                                    │   │
│  │ MapGet("/residents")│    ──►   │  Routes.g.cs      (C# constants)   │   │
│  │   .Impulse<Props>()│           │  TypeScript.g.cs  (embedded TS)    │   │
│  │                    │           │                                    │   │
│  │ Validator.cs       │           └────────────────────────────────────┘   │
│  │ RuleFor(x.Name)    │                          │                         │
│  │   .MaxLength(100)  │                          ▼                         │
│  └────────────────────┘           ┌────────────────────────────────────┐   │
│                                   │      MSBuild Extract Task          │   │
│                                   │  regex: /* IMPULSE:file.ts */      │   │
│                                   └────────────────────────────────────┘   │
│                                                  │                         │
│                          ┌───────────────────────┼───────────────────┐     │
│                          ▼                       ▼                   ▼     │
│                   ┌───────────┐          ┌───────────┐       ┌───────────┐│
│                   │ types.ts  │          │validation │       │routeTree  ││
│                   │mutations.ts│         │   .ts     │       │   .ts     ││
│                   └───────────┘          └───────────┘       └───────────┘│
│                          └───────────────────────┬───────────────────┘     │
│                                                  ▼                         │
│                                      ClientApp/generated/                  │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STEP 2: DEV SERVERS                                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────┐      ┌─────────────────────────────┐       │
│  │      KESTREL (.NET)         │      │      VITE (Bun)             │       │
│  │      localhost:5000         │      │      localhost:5173         │       │
│  │                             │      │                             │       │
│  │  • API endpoints            │      │  • React app                │       │
│  │  • Server-driven props      │      │  • HMR WebSocket            │       │
│  │  • Content negotiation      │      │  • Watches *.tsx            │       │
│  │  • X-Impulse header check   │      │  • Watches generated/       │       │
│  └─────────────────────────────┘      └─────────────────────────────┘       │
│              │                                    │                          │
│              └────────────────┬───────────────────┘                          │
│                               ▼                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                         BROWSER                                         ││
│  │                      localhost:5173                                     ││
│  │                                                                         ││
│  │   React App ◄───── HMR WebSocket ────► Vite                            ││
│  │       │                                                                 ││
│  │       │ context.impulseFetch('/residents/1')                           ││
│  │       │ (X-Impulse: 1 header)                                          ││
│  │       │                                                                 ││
│  │       └────────► Vite Proxy ────────► Kestrel ────────► JSON           ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STEP 3: HOT RELOAD SCENARIOS                                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  SCENARIO A: Edit React Component (.tsx)                                     │
│  ────────────────────────────────────────                                    │
│  Save Detail.tsx → Vite HMR → Browser updates → ~50ms                       │
│  State preserved ✓                                                           │
│                                                                              │
│  SCENARIO B: Edit C# Handler/Model                                           │
│  ─────────────────────────────────────                                       │
│  Save Handler.cs → dotnet watch → Roslyn recompile                          │
│                  → Source Generator → TypeScript.g.cs                       │
│                  → MSBuild Extract → generated/*.ts                         │
│                  → Vite HMR → Browser updates → ~2s                         │
│                                                                              │
│  SCENARIO C: Edit FluentValidation Rule                                      │
│  ──────────────────────────────────────                                      │
│  Save Validator.cs → Roslyn → Source Gen → validation.ts                    │
│  z.string().max(100) → z.string().max(200)                                  │
│  Vite HMR → Client validation matches server                                 │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 12. Production Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           dotnet publish -c Release                          │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STEP 1: BUILD                                                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Roslyn + Source Generator → Routes.g.cs, TypeScript.g.cs                   │
│                │                                                             │
│                ▼                                                             │
│  MSBuild Extract → ClientApp/generated/*.ts                                 │
│                │                                                             │
│                ▼                                                             │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                         bun run build                                  │ │
│  │                    (Vite production build)                             │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                │                                                             │
│                ▼                                                             │
│  ClientApp/dist/                                                             │
│  ├── assets/                                                                 │
│  │   ├── index-a1b2c3d4.js       (content hash, tree-shaken, minified)      │
│  │   ├── index-e5f6g7h8.css      (content hash)                             │
│  │   └── chunk-vendor-xyz123.js  (code split)                               │
│  └── .vite/                                                                  │
│      └── manifest.json            (maps source → hashed files)              │
│                │                                                             │
│                ▼                                                             │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                    MSBuild Copy to wwwroot                             │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                │                                                             │
│                ▼                                                             │
│  publish/                                                                    │
│  ├── MyApp.dll                                                               │
│  ├── appsettings.json                                                        │
│  └── wwwroot/                                                                │
│      └── assets/                                                             │
│          ├── index-a1b2c3d4.js                                               │
│          ├── index-e5f6g7h8.css                                              │
│          └── .vite/manifest.json                                             │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STEP 2: PRODUCTION RUNTIME                                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                         KESTREL (single process)                        ││
│  │                                                                         ││
│  │  Request ──► UseStaticFiles ──► UseImpulse ──► Endpoints               ││
│  │                    │                 │                                  ││
│  │                    ▼                 ▼                                  ││
│  │            /assets/*.js        / or /residents/*                        ││
│  │            (immutable cache)   (no extension)                           ││
│  │                    │                 │                                  ││
│  │                    ▼                 ▼                                  ││
│  │            wwwroot/assets/     AssetManifest reads manifest.json        ││
│  │            Cache-Control:      Returns HTML with hashed paths:          ││
│  │            max-age=31536000    <script src="/assets/index-a1b2c3d4.js"> ││
│  │            immutable                                                    ││
│  └─────────────────────────────────────────────────────────────────────────┘│
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STEP 3: BROWSER REQUEST                                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Browser: GET /residents/123                                                 │
│         │                                                                    │
│         ▼                                                                    │
│  Kestrel: No static file → UseImpulse middleware                            │
│         │                                                                    │
│         ▼                                                                    │
│  AssetManifest reads .vite/manifest.json                                    │
│  Returns HTML:                                                               │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ <!DOCTYPE html>                                                        │ │
│  │ <html>                                                                 │ │
│  │   <head>                                                               │ │
│  │     <link rel="stylesheet" href="/assets/index-e5f6g7h8.css">         │ │
│  │   </head>                                                              │ │
│  │   <body>                                                               │ │
│  │     <div id="root"></div>                                              │ │
│  │     <script id="__IMPULSE_PROPS__">{"resident":{...}}</script>        │ │
│  │     <script type="module" src="/assets/index-a1b2c3d4.js"></script>   │ │
│  │   </body>                                                              │ │
│  │ </html>                                                                │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│         │                                                                    │
│         ▼                                                                    │
│  Browser loads JS (cached forever due to content hash)                      │
│  React hydrates with embedded __IMPULSE_PROPS__                             │
│  NO additional fetch needed for initial data                                │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 13. Dev vs Prod Comparison

| Aspect | Development | Production |
|--------|-------------|------------|
| **Servers** | 2 (Kestrel + Vite) | 1 (Kestrel only) |
| **Assets** | Served by Vite (unbundled) | Served by Kestrel (bundled, hashed) |
| **JS Files** | Individual modules | Single bundle + code-split chunks |
| **CSS** | Individual files with HMR | Single hashed file |
| **Caching** | No cache (always fresh) | Immutable (max-age=31536000) |
| **Source Maps** | Full (for debugging) | Optional (smaller) |
| **Startup** | ~2s (both servers) | ~200ms (Kestrel only) |
| **TypeScript** | tsgo watch mode | tsgo production build |

---

## 14. MSBuild Integration

```xml
<!-- Impulse.Sdk.targets (ships with NuGet package) -->
<Project>
  <!-- Auto bun install -->
  <Target Name="ImpulseRestoreNpm" BeforeTargets="Build"
          Condition="!Exists('$(MSBuildProjectDirectory)/ClientApp/node_modules')">
    <Exec Command="bun install" WorkingDirectory="$(MSBuildProjectDirectory)/ClientApp" />
  </Target>

  <!-- Extract TS from TypeScript.g.cs -->
  <Target Name="ImpulseExtractTS" AfterTargets="Build">
    <ImpulseExtractTask
      SourceFile="$(IntermediateOutputPath)TypeScript.g.cs"
      OutputDir="$(MSBuildProjectDirectory)/ClientApp/generated" />
  </Target>

  <!-- Start Vite in dev -->
  <Target Name="ImpulseStartVite" BeforeTargets="Run"
          Condition="'$(ASPNETCORE_ENVIRONMENT)' == 'Development'">
    <Exec Command="bun run dev" WorkingDirectory="$(MSBuildProjectDirectory)/ClientApp"
          ContinueOnError="true" />
  </Target>

  <!-- Build client for publish -->
  <Target Name="ImpulseBuildClient" BeforeTargets="Publish">
    <Exec Command="bun run build" WorkingDirectory="$(MSBuildProjectDirectory)/ClientApp" />
  </Target>

  <!-- Copy to wwwroot -->
  <Target Name="ImpulseCopyAssets" AfterTargets="ImpulseBuildClient">
    <ItemGroup>
      <ClientAssets Include="$(MSBuildProjectDirectory)/ClientApp/dist/**/*" />
    </ItemGroup>
    <Copy SourceFiles="@(ClientAssets)"
          DestinationFolder="$(PublishDir)wwwroot/%(RecursiveDir)" />
  </Target>
</Project>
```

---

## 15. Developer Experience

```bash
dotnet new impulse -n MyApp
cd MyApp
dotnet run
# → Browser opens at localhost:5000
# → Working CRUD app with React + .NET
# → Edit C# → hot reload (~2s)
# → Edit .tsx → HMR (~50ms)
# → Everything just works
```

**What users DON'T need to do:**
- ❌ Run `bun install` manually
- ❌ Start Vite in separate terminal
- ❌ Run code generators
- ❌ Configure proxy
- ❌ Set up hot reload
- ❌ Configure TypeScript paths
- ❌ Worry about hashed assets

**Everything automated by MSBuild + runtime.**

---

## 16. NuGet Packages

```
Impulse.Core            → ImpulseAttribute, extension methods
Impulse.SourceGenerator → Roslyn generator (Routes.g.cs, TypeScript.g.cs)
Impulse.MSBuild         → TS extraction task, targets
Impulse.Runtime         → AssetManifest, middleware, dev proxy
Impulse.Templates       → dotnet new impulse

Impulse (meta-package)  → References all above
```

**Usage:**
```xml
<PackageReference Include="Impulse" Version="1.0.0" />
```

---

## 17. Key Decisions

| Decision | Rationale |
|----------|-----------|
| Server-driven, not API-first | Same URL for HTML and JSON, server controls UI |
| Router context, not imports | Dependency injection, extensibility, testability |
| `router.invalidate()` | TanStack Router's built-in cache invalidation |
| 4 generated files | Simple, no unnecessary abstraction |
| Embedded TS in .cs | Source generator limitation, MSBuild extracts |
| No type registration | Routes generated from C#, typos impossible |
| RoutePaths + Routes | Single source of truth, path builders derive |
