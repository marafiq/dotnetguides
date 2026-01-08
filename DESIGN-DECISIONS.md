# Impulse v2 Design Decisions

This document captures the reasoning behind key architectural decisions, based on official documentation research and core principles.

---

## 1. Server-Driven vs API-First

**Decision:** Same URL returns HTML or JSON based on `X-Impulse` header.

**Reasoning:**
- Impulse is NOT an SPA framework with separate API
- Single URL simplifies routing - no `/api/` prefix duplication
- SEO-friendly - initial load is full HTML
- Progressive enhancement - works without JavaScript

**Rejected alternative:** `/api/residents` returning JSON, `/residents` returning HTML
- Requires maintaining two URL schemes
- Client must know which URL to call when
- Violates DRY principle

---

## 2. Zero Magic Strings

**Decision:** All paths derive from generated `RoutePaths` constants.

**Reasoning:**
- TypeScript was generated specifically to eliminate magic strings
- Typos in route paths are caught at compile time
- Refactoring paths updates all references automatically
- IDE autocomplete works

**Implementation:**
```typescript
// Constants (immutable)
export const RoutePaths = {
  residents: '/residents',
  residentDetail: '/residents/$id',
} as const

// Builders (derive from constants)
export const Routes = {
  residentDetail: (id: number | string) =>
    RoutePaths.residentDetail.replace('$id', String(id)),
}
```

**Why not template literals?**
```typescript
// BAD - magic string buried in template
`/residents/${id}`

// GOOD - derives from constant
RoutePaths.residentDetail.replace('$id', String(id))
```

The `replace` approach is slightly more verbose but ensures the path comes from the single source of truth.

---

## 3. Router Context for DI

**Decision:** Use `createRootRouteWithContext<ImpulseContext>()` instead of module imports.

**Reasoning (from TanStack Router docs):**
- Context is the recommended pattern for dependency injection
- Enables testing by swapping context values
- Extensibility via TypeScript module augmentation
- No global state or singletons

**Source:** [TanStack Router Context Guide](https://tanstack.com/router/v1/docs/framework/react/guide/router-context)

**Implementation:**
```typescript
export interface ImpulseContext {
  impulseFetch: <T>(url: string) => Promise<T>
  invalidate: () => Promise<void>
}

const rootRoute = createRootRouteWithContext<ImpulseContext>()({...})
```

---

## 4. Router Type Registration

**Decision:** Register router type globally for full type safety.

**Reasoning (from TanStack Router docs):**
- Without registration, `useLoaderData()` returns `any`
- Registration enables type inference across the application
- `getRouteApi` pattern provides typed hooks for code-split components

**Source:** [TanStack Router Type Registration](https://github.com/TanStack/router/discussions/3887)

**Implementation:**
```typescript
declare module '@tanstack/react-router' {
  interface Register { router: ReturnType<typeof createImpulseRouter> }
}
```

---

## 5. Invalidation via router.invalidate()

**Decision:** Mutations call `router.invalidate()` in `onSuccess` callback.

**Reasoning (from TanStack Router docs):**
- `router.invalidate()` returns `Promise<void>` - async operation
- Forces all route loaders to re-run
- No manual cache key management needed
- Works with TanStack Router's built-in caching

**Source:** [TanStack Router Data Mutations](https://tanstack.com/router/v1/docs/framework/react/guide/data-mutations)

**Example:**
```typescript
onSuccess: () => router.invalidate()
// Or with sync option to wait for reload:
onSuccess: () => router.invalidate({ sync: true })
```

---

## 6. Typed Loaders with Generic Parameter

**Decision:** Loaders specify return type via generic: `impulseFetch<ResidentListProps>(url)`

**Reasoning:**
- TanStack Router infers loader return type from the function
- Without explicit generic, type information is lost through the context
- Explicit generic ensures `useLoaderData()` returns correct type

**Implementation:**
```typescript
loader: ({ context }) => context.impulseFetch<ResidentListProps>(RoutePaths.residents),
```

---

## 7. useImpulseMutation in Template (Not Generated)

**Decision:** `useImpulseMutation` hook lives in template static file, not generated code.

**Reasoning:**
- The hook implementation doesn't change based on user code
- Generated mutations import from template: `import { useImpulseMutation } from '../src/impulse/hooks'`
- Keeps generated code minimal and focused
- Easier to customize by editing template file

**Why not an npm package?**
- Extra dependency to manage
- Version conflicts
- Template file allows full customization

---

## 8. react-hook-form Integration

**Decision:** `useImpulseMutation` wraps react-hook-form internally.

**Reasoning (from TkDodo's blog + react-hook-form docs):**
- Form state (touched, dirty, errors) is separate from mutation state
- `zodResolver` provides validation that matches server-side (FluentValidation → Zod)
- `handleSubmit` ensures validation runs before mutation
- `isSubmitting` tracks async operation

**Source:** [TkDodo: React Query and Forms](https://tkdodo.eu/blog/react-query-and-forms)

**Implementation:**
```typescript
const form = useForm<TReq>({ resolver: zodResolver(opts.schema) })
const submit = form.handleSubmit(async (data) => {
  await fetch(...)
})
return { register: form.register, errors: form.formState.errors, submit, isSubmitting }
```

---

## 9. Vite Proxy Configuration

**Decision:** Vite dev server proxies non-asset requests to Kestrel.

**Reasoning:**
- Vite handles HMR for React components (~50ms)
- Kestrel handles data requests
- Single browser origin avoids CORS
- Pattern matches Vite's recommended setup

**Source:** [Vite Server Proxy](https://vite.dev/config/server-options.html#server-proxy)

**Implementation:**
```typescript
proxy: {
  '^(?!/src|/node_modules|/@).*': {
    target: 'http://localhost:5000',
    changeOrigin: true,
  },
}
```

This regex proxies everything except Vite's own dev assets.

---

## 10. getRouteApi for Code-Split Components

**Decision:** Use `getRouteApi` pattern for typed loader data in lazy components.

**Reasoning (from TanStack Router docs):**
- Code-split components can't import the Route object directly
- `getRouteApi('/path')` returns typed hooks for that route
- Path string is validated via router registration

**Source:** [TanStack Router useLoaderData](https://tanstack.com/router/v1/docs/framework/react/api/router/useLoaderDataHook)

**Implementation:**
```typescript
// In lazy-loaded component
const route = getRouteApi('/residents/$id')
const data = route.useLoaderData()  // Typed!
```

---

## 11. C# Routes Class Generation

**Decision:** Generate `Routes.g.cs` with nested classes mirroring route structure.

**Reasoning:**
- Zero magic strings on C# side too
- Compile-time checking of route paths
- Matches TypeScript `RoutePaths` structure
- IDE navigation and refactoring support

**Implementation:**
```csharp
public static class Routes {
  public static class Residents {
    public const string List = "/residents";
    public const string Detail = "/residents/{id:int}";
  }
}
```

---

## 12. 4 Generated Files Only

**Decision:** Generate exactly 4 TypeScript files: types.ts, validation.ts, mutations.ts, routeTree.ts

**Reasoning:**
- Minimal surface area for code generation
- Each file has clear single responsibility
- Easy to understand what's generated vs static
- Template provides the rest (hooks.ts, main.tsx, vite.config.ts)

**What's NOT generated:**
- `hooks.ts` - Static helper, lives in template
- `main.tsx` - Static entry point
- Components - User writes these
- Styles - User's choice

---

## Principles Applied

| Principle | Application |
|-----------|-------------|
| Server-Driven | Same URL, content negotiation via header |
| Zero Magic Strings | All paths from RoutePaths/Routes constants |
| Router Context | DI via createRootRouteWithContext |
| Invalidation | router.invalidate() after mutations |
| Minimal Generation | 4 files, template provides static code |
| Type Safety | Router registration, typed loaders, Zod schemas |
