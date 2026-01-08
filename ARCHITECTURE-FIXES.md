# Impulse v2 - Architecture Fixes

## Fix 1: App Shell with Mount Code

**Problem:** No spec for how React app bootstraps and uses generated code.

**Solution:** Template provides shell, generated code plugs in.

### Template Provides (user can customize):

```tsx
// ClientApp/src/main.tsx
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { RouterProvider } from '@tanstack/react-router'
import { router } from '@impulse/generated/router'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>
)
```

```tsx
// ClientApp/src/App.tsx (root layout)
import { Outlet } from '@tanstack/react-router'

export function App() {
  return (
    <div className="app">
      <header>My App</header>
      <main>
        <Outlet />  {/* Routes render here */}
      </main>
    </div>
  )
}
```

### Generated Code Provides:

```tsx
// ClientApp/generated/router.ts
import { createRouter } from '@tanstack/react-router'
import { routeTree } from './routeTree'

export const router = createRouter({ routeTree })

// Type registration for type-safe navigation
declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}
```

---

## Fix 2: Mutations (not Forms) with Namespace

**Problem:** "Forms" collides with HTML `<form>`. Unclear naming.

**Solution:** Use `Mutations` namespace with clear separation.

### Generated Structure:

```
ClientApp/generated/
├── types/                    # All TypeScript types
│   ├── ResidentSummary.ts
│   ├── ResidentDetailProps.ts
│   ├── CreateResidentRequest.ts
│   ├── CreateResidentResponse.ts
│   └── index.ts
│
├── loaders/                  # GET endpoint loader data
│   ├── ResidentDetail.ts     # Loader return type
│   └── index.ts
│
├── mutations/                # POST/PUT/DELETE operations
│   ├── useCreateResident.ts
│   ├── useUpdateResident.ts
│   ├── useDeleteResident.ts
│   └── index.ts
│
├── validation/               # Zod schemas (from FluentValidation)
│   ├── CreateResidentSchema.ts
│   ├── UpdateResidentSchema.ts
│   └── index.ts
│
├── routes/                   # TanStack virtual route config
│   └── routeTree.ts          # Virtual file routes definition
│
└── router.ts                 # Router instance + type registration
```

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

## Fix 3: GET Types (Loader Data)

**Problem:** Only mutation types specified. GET endpoints need loader types too.

**Solution:** Generate `LoaderData` types that include deferred fields.

### For Simple GET:

```csharp
// C# endpoint
app.MapGet("/residents/{id}", handler)
   .Impulse<ResidentDetailProps>();
```

```typescript
// generated/loaders/ResidentDetail.ts
import type { ResidentDetailProps } from '../types'

// Simple loader - props are the data
export type ResidentDetailLoaderData = ResidentDetailProps
```

### For GET with Deferred:

```csharp
// C# endpoint with deferred data
app.MapGet("/residents/{id}", handler)
   .Impulse<ResidentDetailProps>()
   .Deferred<MedicationList>("medications", "/api/residents/{id}/meds")
   .Deferred<AppointmentList>("appointments", "/api/residents/{id}/appointments");
```

```typescript
// generated/loaders/ResidentDetail.ts
import type { Deferred } from '@impulse/react'
import type { ResidentDetailProps, MedicationList, AppointmentList } from '../types'

// Loader data includes deferred fields
export interface ResidentDetailLoaderData {
  // Sync data (awaited in loader)
  props: ResidentDetailProps

  // Deferred data (streamed after initial render)
  medications: Deferred<MedicationList>
  appointments: Deferred<AppointmentList>
}
```

### Component Usage:

```tsx
// Features/Residents/Detail.tsx
import { useLoaderData, Await } from '@tanstack/react-router'
import type { ResidentDetailLoaderData } from '@impulse/generated/loaders'

export function ResidentDetail() {
  const { props, medications, appointments } = useLoaderData<ResidentDetailLoaderData>()

  return (
    <div>
      {/* Sync data - available immediately */}
      <h1>{props.resident.name}</h1>

      {/* Deferred data - streams in */}
      <Suspense fallback={<Spinner />}>
        <Await promise={medications}>
          {(meds) => <MedicationList items={meds} />}
        </Await>
      </Suspense>

      <Suspense fallback={<Spinner />}>
        <Await promise={appointments}>
          {(appts) => <AppointmentList items={appts} />}
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
// generated/routes/routeTree.ts
import {
  createRootRoute,
  createRoute,
  createRouter,
  defer
} from '@tanstack/react-router'

import { App } from '../../src/App'
import type { ResidentDetailLoaderData } from '../loaders'

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

## Fix 5: Updated Generated Structure

```
ClientApp/
├── src/                          # User code
│   ├── main.tsx                  # Entry point (template)
│   ├── App.tsx                   # Root layout (user customizes)
│   └── Features/                 # Vertical slices
│       └── Residents/
│           ├── List.tsx
│           ├── Detail.tsx
│           └── Create.tsx
│
└── generated/                    # All generated code
    ├── types/                    # TypeScript interfaces
    │   ├── index.ts              # Re-exports all
    │   └── *.ts                  # One per type
    │
    ├── loaders/                  # GET endpoint data types
    │   ├── index.ts
    │   └── *.ts                  # Includes Deferred<T>
    │
    ├── mutations/                # POST/PUT/DELETE hooks
    │   ├── index.ts
    │   └── use*.ts               # useCreateX, useUpdateX
    │
    ├── validation/               # Zod schemas
    │   ├── index.ts
    │   └── *Schema.ts            # From FluentValidation
    │
    ├── routes/
    │   └── routeTree.ts          # Virtual route definitions
    │
    └── router.ts                 # Router instance + types
```

---

## Updated TypeScript.g.cs Structure

```csharp
// TypeScript.g.cs (generated by Roslyn)
public static partial class TypeScriptOutput
{
    // Types
    public const string Types = """
    /* IMPULSE:types/index.ts */
    export * from './ResidentSummary'
    export * from './ResidentDetailProps'
    export * from './CreateResidentRequest'
    /* END:types/index.ts */

    /* IMPULSE:types/ResidentSummary.ts */
    export interface ResidentSummary {
      id: number;
      name: string;
    }
    /* END:types/ResidentSummary.ts */
    """;

    // Loaders (GET data with Deferred)
    public const string Loaders = """
    /* IMPULSE:loaders/ResidentDetail.ts */
    import type { Deferred } from '@impulse/react'
    import type { ResidentDetailProps, MedicationList } from '../types'

    export interface ResidentDetailLoaderData {
      props: ResidentDetailProps;
      medications: Deferred<MedicationList>;
    }
    /* END:loaders/ResidentDetail.ts */
    """;

    // Mutations (POST/PUT/DELETE)
    public const string Mutations = """
    /* IMPULSE:mutations/useCreateResident.ts */
    import { useImpulseMutation } from '@impulse/react'
    import { CreateResidentSchema } from '../validation/CreateResidentSchema'
    import type { CreateResidentRequest, CreateResidentResponse } from '../types'

    export function useCreateResident() {
      return useImpulseMutation<CreateResidentRequest, CreateResidentResponse>({
        endpoint: '/api/residents',
        method: 'POST',
        schema: CreateResidentSchema,
      })
    }
    /* END:mutations/useCreateResident.ts */
    """;

    // Validation (Zod from FluentValidation)
    public const string Validation = """
    /* IMPULSE:validation/CreateResidentSchema.ts */
    import { z } from 'zod'

    export const CreateResidentSchema = z.object({
      name: z.string().min(1).max(100),
      email: z.string().min(1).email(),
    })

    export type CreateResidentInput = z.infer<typeof CreateResidentSchema>
    /* END:validation/CreateResidentSchema.ts */
    """;

    // Routes (TanStack Virtual)
    public const string Routes = """
    /* IMPULSE:routes/routeTree.ts */
    import { createRootRoute, createRoute, defer } from '@tanstack/react-router'
    import { App } from '../../src/App'
    import type { ResidentDetailLoaderData } from '../loaders/ResidentDetail'

    const rootRoute = createRootRoute({ component: App })

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

    export const routeTree = rootRoute.addChildren([residentDetailRoute])
    /* END:routes/routeTree.ts */
    """;

    // Router instance
    public const string Router = """
    /* IMPULSE:router.ts */
    import { createRouter } from '@tanstack/react-router'
    import { routeTree } from './routes/routeTree'

    export const router = createRouter({ routeTree })

    declare module '@tanstack/react-router' {
      interface Register {
        router: typeof router
      }
    }
    /* END:router.ts */
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
