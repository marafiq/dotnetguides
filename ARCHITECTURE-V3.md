# Impulse v2 - Complete Architecture

## Core Principles

1. **Server-Driven** - Route URL is the data source, not a separate API
2. **Zero Magic Strings** - All paths generated from C# route patterns
3. **Router Context** - Dependency injection via TanStack Router context
4. **Invalidation** - Mutations trigger `router.invalidate()` to refresh data
5. **Extensible** - Users extend context with their own dependencies

---

## TanStack Router Integration

### Router Context for Dependency Injection

```typescript
// generated/routeTree.ts
import {
  createRouter,
  createRoute,
  createRootRouteWithContext,
} from '@tanstack/react-router'

// ============================================
// ROUTER CONTEXT TYPE - Extensible by user
// ============================================
export interface ImpulseContext {
  // Core Impulse functions (generated)
  impulseFetch: <T>(url: string) => Promise<T>
  invalidate: () => Promise<void>
}

// User can extend:
// declare module '@impulse/generated/routeTree' {
//   interface ImpulseContext {
//     analytics: AnalyticsClient
//   }
// }

// ============================================
// ROUTE PATHS - Single source of truth (from C#)
// ============================================
export const RoutePaths = {
  residents: '/residents',
  residentDetail: '/residents/$id',
  residentMedications: '/residents/$id/medications',
} as const

export const Routes = {
  residents: () => RoutePaths.residents,
  residentDetail: (id: number | string) =>
    RoutePaths.residentDetail.replace('$id', String(id)),
  residentMedications: (id: number | string) =>
    RoutePaths.residentMedications.replace('$id', String(id)),
}

// ============================================
// ROOT ROUTE - Uses context, not imports
// ============================================
const rootRoute = createRootRouteWithContext<ImpulseContext>()({
  component: () => import('../src/App'),
})

// ============================================
// ROUTES - Access context in loaders
// ============================================
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
    const { impulseFetch } = context
    const props = await impulseFetch(Routes.residentDetail(params.id))
    return {
      props,
      medications: defer(impulseFetch(Routes.residentMedications(params.id))),
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

---

## Mutations with Invalidation

```typescript
// generated/mutations.ts
import { Routes } from './routeTree'
import { useRouter } from '@tanstack/react-router'
import { CreateResidentSchema, UpdateResidentSchema } from './validation'
import type { CreateResidentRequest, CreateResidentResponse } from './types'

// ============================================
// MUTATION HOOK - Uses router context for fetch + invalidate
// ============================================
export function useCreateResident() {
  const router = useRouter()
  const { impulseFetch } = router.options.context

  return useImpulseMutation<CreateResidentRequest, CreateResidentResponse>({
    endpoint: Routes.residents(),
    method: 'POST',
    schema: CreateResidentSchema,

    // After successful mutation, invalidate router to refetch data
    onSuccess: async () => {
      await router.invalidate()
    },
  })
}

export function useUpdateResident(id: number) {
  const router = useRouter()

  return useImpulseMutation({
    endpoint: Routes.residentDetail(id),
    method: 'PUT',
    schema: UpdateResidentSchema,

    onSuccess: async () => {
      await router.invalidate()
    },
  })
}

export function useDeleteResident(id: number) {
  const router = useRouter()

  return useImpulseMutation({
    endpoint: Routes.residentDetail(id),
    method: 'DELETE',

    onSuccess: async () => {
      // Invalidate and navigate away
      await router.invalidate()
      router.navigate({ to: Routes.residents() })
    },
  })
}
```

---

## App Entry Point

```tsx
// src/main.tsx (template provides)
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { RouterProvider } from '@tanstack/react-router'
import { createImpulseRouter } from '@impulse/generated/routeTree'

// Create router with optional user extensions
const router = createImpulseRouter({
  // User can add their own context
  // analytics: new AnalyticsClient(),
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>
)
```

---

## Extensibility Patterns

### 1. Extend Router Context

```typescript
// User's src/router.ts
import { createImpulseRouter, ImpulseContext } from '@impulse/generated/routeTree'
import { AnalyticsClient } from './analytics'

// Extend the context type
declare module '@impulse/generated/routeTree' {
  interface ImpulseContext {
    analytics: AnalyticsClient
  }
}

export const router = createImpulseRouter({
  analytics: new AnalyticsClient(),
})
```

### 2. Custom Mutation Behavior

```typescript
// User's component - wrap generated mutation
import { useCreateResident } from '@impulse/generated/mutations'

function CreateResidentForm() {
  const mutation = useCreateResident()
  const analytics = useRouterContext().analytics

  const handleSubmit = async (data) => {
    const result = await mutation.submitAsync(data)
    analytics.track('resident_created', { id: result.id })
    return result
  }

  return <form onSubmit={handleSubmit}>...</form>
}
```

### 3. Override Fetch Behavior

```typescript
// User can override impulseFetch for auth, logging, etc.
const router = createImpulseRouter({
  impulseFetch: async (url) => {
    const token = await getAuthToken()
    const res = await fetch(url, {
      headers: {
        'X-Impulse': '1',
        'Authorization': `Bearer ${token}`,
      },
    })
    if (!res.ok) throw new Error(res.statusText)
    return res.json()
  },
})
```

---

## State Management

### URL Search Params (Built-in)

TanStack Router treats search params as the primary state manager:

```typescript
// Route with search params
const residentsRoute = createRoute({
  path: RoutePaths.residents,
  validateSearch: (search) => ({
    page: Number(search.page) || 1,
    filter: String(search.filter) || '',
  }),
  loader: ({ context, search }) =>
    context.impulseFetch(`${RoutePaths.residents}?page=${search.page}&filter=${search.filter}`),
})

// Component uses search params
function ResidentsList() {
  const { page, filter } = useSearch({ from: RoutePaths.residents })
  const navigate = useNavigate()

  const setPage = (p: number) => navigate({ search: { page: p, filter } })
}
```

### TanStack Query Integration (Optional)

For complex caching needs, users can add TanStack Query:

```typescript
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

const queryClient = new QueryClient()

const router = createImpulseRouter({
  queryClient, // Add to context
})

// Wrap app
<QueryClientProvider client={queryClient}>
  <RouterProvider router={router} />
</QueryClientProvider>
```

---

## Generated Files (4 total)

```
generated/
├── types.ts       # All interfaces
├── validation.ts  # Zod schemas from FluentValidation
├── mutations.ts   # Mutation hooks with invalidation
└── routeTree.ts   # Routes + context + router factory
```

---

## Complete Data Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    INITIAL PAGE LOAD                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Browser: GET /residents/123                                     │
│                           │                                      │
│                           ▼                                      │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ .NET Server                                              │    │
│  │  - Route handler returns ResidentDetailProps             │    │
│  │  - Impulse middleware embeds in HTML                     │    │
│  └─────────────────────────────────────────────────────────┘    │
│                           │                                      │
│                           ▼                                      │
│  HTML with embedded <script id="__IMPULSE_PROPS__">             │
│  React hydrates with props - NO fetch                            │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                  CLIENT NAVIGATION                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  User clicks <Link to={Routes.residentDetail(456)}>             │
│                           │                                      │
│                           ▼                                      │
│  TanStack Router loader runs:                                    │
│    context.impulseFetch(Routes.residentDetail(456))             │
│                           │                                      │
│                           ▼                                      │
│  fetch('/residents/456', { headers: { 'X-Impulse': '1' } })     │
│                           │                                      │
│                           ▼                                      │
│  Server returns JSON (sees X-Impulse header)                     │
│  React renders with new props                                    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                     MUTATION                                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  User submits form with useCreateResident()                      │
│                           │                                      │
│                           ▼                                      │
│  1. Client validates with Zod schema                             │
│  2. POST to Routes.residents()                                   │
│  3. Server validates with FluentValidation                       │
│  4. Returns success or ProblemDetails                            │
│                           │                                      │
│                           ▼                                      │
│  onSuccess: router.invalidate()                                  │
│    → All current route loaders re-run                            │
│    → UI updates with fresh data                                  │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Why This Architecture

| Concern | Solution |
|---------|----------|
| No magic strings | RoutePaths/Routes generated from C# |
| Dependency injection | Router context, not imports |
| Data invalidation | `router.invalidate()` in mutation onSuccess |
| Extensibility | Users extend ImpulseContext interface |
| State management | URL search params (built-in) or TanStack Query |
| Type safety | Generated types flow through entire system |
| Server-driven | Same URL for HTML and JSON |
