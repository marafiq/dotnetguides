# Impulse v2 - Architecture Fixes

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
    endpoint: '/api/residents',
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

// Root route (app shell)
const rootRoute = createRootRoute({
  component: App,
})

// GET /residents
const residentsIndexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/residents',
  loader: async () => {
    const res = await fetch('/api/residents')
    return res.json()
  },
  component: () => import('@features/Residents/List'),
})

// GET /residents/$id (with deferred)
const residentDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/residents/$id',
  loader: async ({ params }): Promise<ResidentDetailLoaderData> => {
    const props = await fetch(`/api/residents/${params.id}`).then(r => r.json())

    return {
      props,
      medications: defer(
        fetch(`/api/residents/${params.id}/meds`).then(r => r.json())
      ),
      appointments: defer(
        fetch(`/api/residents/${params.id}/appointments`).then(r => r.json())
      ),
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

// No type registration needed - routes are generated from C#,
// so typos in route paths are impossible by construction
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

    // 3. All mutation hooks in one file
    public const string Mutations = """
    /* IMPULSE:mutations.ts */
    import { useImpulseMutation } from '@impulse/react'
    import { CreateResidentSchema, UpdateResidentSchema } from './validation'
    import type { CreateResidentRequest, CreateResidentResponse } from './types'

    export function useCreateResident() {
      return useImpulseMutation<CreateResidentRequest, CreateResidentResponse>({
        endpoint: '/api/residents',
        method: 'POST',
        schema: CreateResidentSchema,
      })
    }

    export function useUpdateResident(id: number) {
      return useImpulseMutation({
        endpoint: `/api/residents/${id}`,
        method: 'PUT',
        schema: UpdateResidentSchema,
      })
    }
    /* END:mutations.ts */
    """;

    // 4. Route tree + router (all in one file)
    public const string RouteTree = """
    /* IMPULSE:routeTree.ts */
    import { createRootRoute, createRoute, createRouter, defer } from '@tanstack/react-router'
    import { App } from '../src/App'
    import type { ResidentDetailLoaderData } from './types'

    const rootRoute = createRootRoute({ component: App })

    const residentsRoute = createRoute({
      getParentRoute: () => rootRoute,
      path: '/residents',
      loader: async () => {
        return fetch('/api/residents').then(r => r.json())
      },
      component: () => import('@features/Residents/List'),
    })

    const residentDetailRoute = createRoute({
      getParentRoute: () => rootRoute,
      path: '/residents/$id',
      loader: async ({ params }): Promise<ResidentDetailLoaderData> => {
        const props = await fetch(`/api/residents/${params.id}`).then(r => r.json())
        return {
          props,
          medications: defer(fetch(`/api/residents/${params.id}/meds`).then(r => r.json())),
        }
      },
      component: () => import('@features/Residents/Detail'),
    })

    export const routeTree = rootRoute.addChildren([
      residentsRoute,
      residentDetailRoute,
    ])

    // Router instance
    export const router = createRouter({ routeTree })

    // No type registration - routes generated from C#, typos impossible
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
