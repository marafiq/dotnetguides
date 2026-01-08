# Impulse v2 - Architecture Fixes

## Core Principle: Server-Driven (NOT API-First)

**Impulse is server-driven.** This is the fundamental difference from typical SPA/API architectures.

| API-First (wrong) | Server-Driven (Impulse) |
|-------------------|-------------------------|
| `/api/residents` returns JSON | `/residents` returns HTML or JSON |
| Client fetches data separately | Same URL, different responses |
| Two concerns: API + UI | One concern: server controls UI |
| React decides what to show | Server decides what to show |

**How it works:**
- `GET /residents` with browser → HTML with embedded props
- `GET /residents` with `X-Impulse: 1` header → JSON only
- No separate API layer - the route IS the data source

---

## Simplified Generated Structure

**4 files. That's it.**

```
generated/
├── types.ts       # All interfaces (props, requests, responses, loader data)
├── validation.ts  # All Zod schemas (from FluentValidation)
├── mutations.ts   # All mutation hooks (POST/PUT/DELETE)
└── routeTree.ts   # Routes + router instance
```

---

## Fix 1: App Shell with Mount Code

**Template provides:**

```tsx
// ClientApp/src/main.tsx
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { RouterProvider } from '@tanstack/react-router'
import { router } from '@impulse/generated/routeTree'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>
)
```

```tsx
// ClientApp/src/App.tsx (root layout - user customizes)
import { Outlet } from '@tanstack/react-router'

export function App() {
  return (
    <div className="app">
      <header>My App</header>
      <main>
        <Outlet />
      </main>
    </div>
  )
}
```

---

## Fix 2: Mutations (not Forms) with Namespace

**Problem:** "Forms" collides with HTML `<form>`. Unclear naming.

**Solution:** Use `Mutations` namespace with clear separation.

### Why "Mutations" not "Forms":

- `<form>` is an HTML element
- `useCreateResident()` is a mutation (data change)
- No naming collision

### Usage (no collision):

```tsx
// User code - clear namespacing
import { useCreateResident } from '@impulse/generated/mutations'
import type { CreateResidentRequest } from '@impulse/generated/types'

function CreatePage() {
  const mutation = useCreateResident()  // Not "form", it's a mutation

  return (
    <form onSubmit={mutation.submit}>  {/* HTML form element */}
      <input {...mutation.register('name')} />
      {mutation.errors.name && <span>{mutation.errors.name}</span>}
      <button disabled={mutation.isSubmitting}>Create</button>
    </form>
  )
}
```

### Mutation Hook Spec:

```typescript
// generated/mutations/useCreateResident.ts
import { useImpulseMutation } from '@impulse/react'
import { CreateResidentSchema } from '../validation/CreateResidentSchema'
import type { CreateResidentRequest, CreateResidentResponse } from '../types'

export function useCreateResident() {
  return useImpulseMutation<CreateResidentRequest, CreateResidentResponse>({
    endpoint: '/residents',  // Same URL pattern - server-driven
    method: 'POST',
    schema: CreateResidentSchema,  // Client validation
  })
}

// Return type:
interface MutationResult<TReq, TRes> {
  // Form binding
  register: (field: keyof TReq) => InputProps

  // Validation state
  errors: Partial<Record<keyof TReq, string>>
  isValid: boolean

  // Submission
  submit: (e: FormEvent) => Promise<void>
  submitAsync: (data: TReq) => Promise<TRes>

  // State
  isSubmitting: boolean
  isSuccess: boolean
  isError: boolean

  // Results
  data?: TRes
  error?: ProblemDetails

  // Reset
  reset: () => void
}
```

---

## Fix 3: GET Types (Loader Data in types.ts)

**All types in one file - including loader data types:**

```typescript
// generated/types.ts

// Props types (from .Impulse<T>())
export interface ResidentDetailProps {
  resident: ResidentSummary;
  createdAt: string;
}

// Request/Response types (from mutations)
export interface CreateResidentRequest {
  name: string;
  email: string;
}

export interface CreateResidentResponse {
  id: number;
}

// Loader data types (with Deferred for streaming)
export interface ResidentDetailLoaderData {
  props: ResidentDetailProps;
  medications: Deferred<MedicationList>;  // Streamed
  appointments: Deferred<AppointmentList>; // Streamed
}

// Deferred type (re-exported from TanStack)
export type Deferred<T> = Promise<T>
```

**Component usage:**

```tsx
import { useLoaderData, Await } from '@tanstack/react-router'
import type { ResidentDetailLoaderData } from '@impulse/generated/types'

export function ResidentDetail() {
  const { props, medications } = useLoaderData<ResidentDetailLoaderData>()

  return (
    <div>
      <h1>{props.resident.name}</h1>
      <Suspense fallback={<Spinner />}>
        <Await promise={medications}>
          {(meds) => <MedicationList items={meds} />}
        </Await>
      </Suspense>
    </div>
  )
}
```

---

## Fix 4: TanStack Virtual Routes

**Problem:** Generating physical route files causes double-processing by TanStack plugin.

**Solution:** Use TanStack's virtual file routes - generate config, not files.

### Server-Driven Architecture

**Key Insight:** Impulse is NOT API-first. The route URL IS the data source.

```
Initial Page Load:
┌─────────────────────────────────────────────────────────────┐
│ Browser requests: GET /residents/123                         │
│                                                              │
│ Server returns HTML with embedded props:                     │
│ <html>                                                       │
│   <div id="root">...SSR content...</div>                    │
│   <script id="__IMPULSE_PROPS__">                           │
│     {"resident":{"id":123,"name":"John"}}                   │
│   </script>                                                  │
│ </html>                                                      │
│                                                              │
│ React hydrates with embedded props - NO fetch needed        │
└─────────────────────────────────────────────────────────────┘

Client-Side Navigation:
┌─────────────────────────────────────────────────────────────┐
│ User clicks link to /residents/456                          │
│                                                              │
│ TanStack Router loader:                                      │
│   fetch('/residents/456', { headers: { 'X-Impulse': '1' }}) │
│                                                              │
│ Server sees X-Impulse header → returns JSON only:           │
│   {"resident":{"id":456,"name":"Jane"}}                     │
│                                                              │
│ React renders with JSON props - NO full page reload         │
└─────────────────────────────────────────────────────────────┘
```

### What We Generate:

```typescript
// generated/routeTree.ts
import {
  createRootRoute,
  createRoute,
  createRouter,
  defer
} from '@tanstack/react-router'

import { App } from '../src/App'
import type { ResidentDetailLoaderData } from './types'

// Impulse fetch - same URL, JSON response for navigation
const impulseFetch = (url: string) =>
  fetch(url, { headers: { 'X-Impulse': '1' } }).then(r => r.json())

// Root route (app shell)
const rootRoute = createRootRoute({
  component: App,
})

// GET /residents (server-driven: same URL for HTML and JSON)
const residentsIndexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/residents',
  loader: () => impulseFetch('/residents'),
  component: () => import('@features/Residents/List'),
})

// GET /residents/$id (with deferred streams from server)
const residentDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/residents/$id',
  loader: async ({ params }): Promise<ResidentDetailLoaderData> => {
    const props = await impulseFetch(`/residents/${params.id}`)

    return {
      props,
      // Deferred: Server streams these after initial response
      medications: defer(impulseFetch(`/residents/${params.id}/medications`)),
      appointments: defer(impulseFetch(`/residents/${params.id}/appointments`)),
    }
  },
  component: () => import('@features/Residents/Detail'),
})

// Route tree
export const routeTree = rootRoute.addChildren([
  residentsIndexRoute,
  residentDetailRoute,
])

// Router instance (exported for main.tsx)
export const router = createRouter({ routeTree })

// No type registration - routes generated from C#, typos impossible
```

### Server-Side (what .Impulse<T>() does):

```csharp
// In Impulse.Runtime middleware
app.Use(async (context, next) =>
{
    await next();

    // If response has Impulse props
    if (context.Items.TryGetValue("ImpulseProps", out var props))
    {
        if (context.Request.Headers.ContainsKey("X-Impulse"))
        {
            // Client navigation: return JSON only
            context.Response.ContentType = "application/json";
            await JsonSerializer.SerializeAsync(context.Response.Body, props);
        }
        else
        {
            // Initial load: embed props in HTML
            var html = await RenderWithProps(props);
            await context.Response.WriteAsync(html);
        }
    }
});
```

### No TanStack Plugin Needed:

```typescript
// vite.config.ts - SIMPLIFIED
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [
    react(),
    // NO TanStackRouterVite plugin - we generate routeTree directly
  ],
  resolve: {
    alias: {
      '@features': path.resolve(__dirname, '../Features'),
      '@impulse/generated': path.resolve(__dirname, './generated'),
    },
  },
})
```

### Why Virtual Routes Are Better:

| Physical Files | Virtual Routes |
|----------------|----------------|
| Generate routes/*.tsx | Generate routeTree.ts |
| TanStack plugin processes | No plugin needed |
| Double generation | Single generation |
| File watching complexity | Simple import |
| Route tree generated twice | Route tree is the output |

---

## Final Generated Structure (4 Files)

```
ClientApp/
├── src/                          # User code (template provides)
│   ├── main.tsx                  # Entry: imports router, renders RouterProvider
│   ├── App.tsx                   # Root layout with <Outlet />
│   └── Features/                 # Vertical slices (user writes)
│       └── Residents/
│           ├── List.tsx
│           ├── Detail.tsx
│           └── Create.tsx
│
└── generated/                    # ALL GENERATED (4 files)
    ├── types.ts                  # All interfaces (props, requests, loader data)
    ├── validation.ts             # All Zod schemas
    ├── mutations.ts              # All mutation hooks
    └── routeTree.ts              # Routes + router instance
```

**That's it. 4 files.**

---

## TypeScript.g.cs (Simplified)

```csharp
// TypeScript.g.cs (generated by Roslyn Source Generator)
public static partial class TypeScriptOutput
{
    // 1. All types in one file
    public const string Types = """
    /* IMPULSE:types.ts */
    // Props types
    export interface ResidentSummary {
      id: number;
      name: string;
    }

    export interface ResidentDetailProps {
      resident: ResidentSummary;
      createdAt: string;
    }

    // Request/Response types
    export interface CreateResidentRequest {
      name: string;
      email: string;
    }

    export interface CreateResidentResponse {
      id: number;
    }

    // Loader data types (includes Deferred)
    export type Deferred<T> = Promise<T>

    export interface ResidentDetailLoaderData {
      props: ResidentDetailProps;
      medications: Deferred<MedicationList>;
    }
    /* END:types.ts */
    """;

    // 2. All validation schemas in one file
    public const string Validation = """
    /* IMPULSE:validation.ts */
    import { z } from 'zod'

    export const CreateResidentSchema = z.object({
      name: z.string().min(1).max(100),
      email: z.string().min(1).email(),
    })

    export const UpdateResidentSchema = z.object({
      name: z.string().min(1).max(100),
    })
    /* END:validation.ts */
    """;

    // 3. All mutation hooks in one file (server-driven URLs)
    public const string Mutations = """
    /* IMPULSE:mutations.ts */
    import { useImpulseMutation } from '@impulse/react'
    import { CreateResidentSchema, UpdateResidentSchema } from './validation'
    import type { CreateResidentRequest, CreateResidentResponse } from './types'

    export function useCreateResident() {
      return useImpulseMutation<CreateResidentRequest, CreateResidentResponse>({
        endpoint: '/residents',  // Server-driven: same URL pattern
        method: 'POST',
        schema: CreateResidentSchema,
      })
    }

    export function useUpdateResident(id: number) {
      return useImpulseMutation({
        endpoint: `/residents/${id}`,  // Server-driven: same URL pattern
        method: 'PUT',
        schema: UpdateResidentSchema,
      })
    }
    /* END:mutations.ts */
    """;

    // 4. Route tree + router (server-driven, not API-first)
    public const string RouteTree = """
    /* IMPULSE:routeTree.ts */
    import { createRootRoute, createRoute, createRouter, defer } from '@tanstack/react-router'
    import { App } from '../src/App'
    import type { ResidentDetailLoaderData } from './types'

    // Server-driven: same URL for HTML (initial) and JSON (navigation)
    const impulseFetch = (url: string) =>
      fetch(url, { headers: { 'X-Impulse': '1' } }).then(r => r.json())

    const rootRoute = createRootRoute({ component: App })

    const residentsRoute = createRoute({
      getParentRoute: () => rootRoute,
      path: '/residents',
      loader: () => impulseFetch('/residents'),
      component: () => import('@features/Residents/List'),
    })

    const residentDetailRoute = createRoute({
      getParentRoute: () => rootRoute,
      path: '/residents/$id',
      loader: async ({ params }): Promise<ResidentDetailLoaderData> => {
        const props = await impulseFetch(`/residents/${params.id}`)
        return {
          props,
          medications: defer(impulseFetch(`/residents/${params.id}/medications`)),
        }
      },
      component: () => import('@features/Residents/Detail'),
    })

    export const routeTree = rootRoute.addChildren([
      residentsRoute,
      residentDetailRoute,
    ])

    export const router = createRouter({ routeTree })
    /* END:routeTree.ts */
    """;
}
```

---

## @impulse/react Runtime Package

**Must provide:**

```typescript
// @impulse/react/index.ts
export { useImpulseMutation } from './useImpulseMutation'
export type { MutationResult, ProblemDetails, Deferred } from './types'

// Deferred type for loader data
export type Deferred<T> = Promise<T>  // TanStack's defer wraps promises

// ProblemDetails RFC 7807
export interface ProblemDetails {
  type?: string
  title: string
  status: number
  detail?: string
  errors?: Record<string, string[]>
}
```
