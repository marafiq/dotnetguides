# Shalimar Vision: Three Approaches to React Component Generation

## The Goal
Express React components with TanStack Store using C#, generating clean TypeScript.

---

## Approach 1: Razor Files (Current)
**Files:** `Counter.razor` → `Counter.tsx`

```razor
@client
<div>
    <h1>Count: @count</h1>
    <button @onclick="increment">+1</button>
</div>

@code {
    [Store] int count = 0;
    void increment() { count++; }
}
```

**Pros:**
- Familiar to Blazor developers
- Template and code in one file
- Visual Studio tooling exists

**Cons:**
- Template is Razor markup, not TSX
- Need Razor parser (Microsoft.AspNetCore.Razor.Language)
- Mixing paradigms (C# + Razor + React concepts)

---

## Approach 2: Fluent C# DSL (Just Built)
**Files:** `CounterComponent.cs` → `Counter.tsx`

```csharp
public static class CounterComponent
{
    public static string Generate() => ComponentBuilder
        .Create("Counter")
        .Client()
        .Store("count", 0)
        .Action("increment", "count++")
        .ToTsx(@"
            <div>
                <h1>Count: {count}</h1>
                <button onClick={increment}>+1</button>
            </div>");
}
```

**Pros:**
- Pure C# - no Razor parser needed
- Type-safe props and state via generics
- Actions transform C# mutations to TanStack setState

**Cons:**
- Template is a string (no compile-time validation)
- Action body is a string (no C# compile-time checking)

---

## Approach 3: Hybrid - C# DSL + TSX Templates (The Vision)
**Files:** `Counter.component.cs` + `Counter.template.tsx` → `Counter.tsx`

### Component Definition (Pure C#)
```csharp
// Counter.component.cs
[Component("Counter")]
[Client]
public partial class CounterDef
{
    // State - generates TanStack Store
    [Store] public int Count { get; set; } = 0;
    [Store] public string Label { get; set; } = "Count";

    // Props - generates interface
    [Prop] public string Title { get; set; } = default!;
    [Prop] public int Step { get; set; } = 1;

    // Actions - generates functions
    [Action]
    public void Increment() => Count += Step;

    [Action]
    public void Decrement() => Count -= Step;

    [Action]
    public void Reset() => Count = 0;

    // Computed - generates derived values
    [Computed]
    public string DisplayText => $"{Label}: {Count}";
}
```

### Template (Separate TSX - Uses Generated Hooks)
```tsx
// Counter.template.tsx
// Template only - uses hooks generated from CounterDef

export const CounterTemplate = ({ count, title, step, increment, decrement, reset, displayText }) => (
  <div>
    <h1>{title}</h1>
    <span>{displayText}</span>
    <button onClick={increment}>+{step}</button>
    <button onClick={decrement}>-{step}</button>
    <button onClick={reset}>Reset</button>
  </div>
);
```

### Generated Output
```tsx
// Counter.tsx (generated)
import React from 'react';
import { Store } from '@tanstack/store';
import { useStore } from '@tanstack/react-store';

interface CounterState {
  count: number;
  label: string;
}

interface CounterProps {
  title: string;
  step?: number;
}

const counterStore = new Store<CounterState>({
  count: 0,
  label: "Count"
});

export function Counter({ title, step = 1 }: CounterProps) {
  const count = useStore(counterStore, (s) => s.count);
  const label = useStore(counterStore, (s) => s.label);

  const displayText = `${label}: ${count}`;

  const increment = () => counterStore.setState((s) => ({ ...s, count: s.count + step }));
  const decrement = () => counterStore.setState((s) => ({ ...s, count: s.count - step }));
  const reset = () => counterStore.setState((s) => ({ ...s, count: 0 }));

  return (
    <div>
      <h1>{title}</h1>
      <span>{displayText}</span>
      <button onClick={increment}>+{step}</button>
      <button onClick={decrement}>-{step}</button>
      <button onClick={reset}>Reset</button>
    </div>
  );
}
```

**Pros:**
- Full C# compile-time checking for state/props/actions
- Template is actual TSX (IDE support, type checking)
- Clear separation of concerns
- Source Generator friendly

**Cons:**
- Two files per component
- More complex tooling

---

## Approach 4: C# Expression Trees (Advanced Vision)
**Files:** `Counter.cs` → `Counter.tsx`

```csharp
public class Counter : ShalimarComponent
{
    // State with full type inference
    [Store] int count = 0;

    // Actions using expression trees
    [Action] void Increment() => count++;

    // Template using JSX-like syntax in C#
    protected override IElement Render() =>
        Div(
            H1(Props.Title),
            Span($"Count: {count}"),
            Button(onclick: Increment, "+1"),
            Button(onclick: Decrement, "-1")
        );
}
```

**Pros:**
- Everything in C# with full IDE support
- Template validated at compile time
- Expression trees capture action semantics

**Cons:**
- Custom JSX-like DSL learning curve
- Complex implementation (Roslyn analyzers, expression tree visitors)

---

## Recommendation: Start with Approach 3

**Why:**
1. C# handles what C# is good at (types, logic, state)
2. TSX handles what TSX is good at (templates, IDE support)
3. Can leverage Source Generators for build-time generation
4. Path to Approach 4 if desired

**Implementation Path:**
1. Define attributes: `[Component]`, `[Client]`, `[Store]`, `[Prop]`, `[Action]`, `[Computed]`
2. Create Source Generator that reads these attributes
3. Generate TypeScript interfaces and store boilerplate
4. Template either inline or in separate file
5. Final composition into single .tsx

---

## Quick Win: Enhanced Fluent DSL

Before full Source Generator, enhance current fluent DSL:

```csharp
var counter = Component.Define("Counter")
    .Client()
    .State(s => {
        s.Field("count", 0);
        s.Field("label", "Count");
    })
    .Props(p => {
        p.Required<string>("title");
        p.Optional<int>("step", 1);
    })
    .Actions(a => {
        a.Define("increment", (state, props) => state.count += props.step);
        a.Define("decrement", (state, props) => state.count -= props.step);
        a.Define("reset", state => state.count = 0);
    })
    .Computed(c => {
        c.Define("displayText", state => $"{state.label}: {state.count}");
    })
    .Render(ctx => $@"
        <div>
            <h1>{ctx.title}</h1>
            <span>{ctx.displayText}</span>
            <button onClick={ctx.increment}>+{ctx.step}</button>
            <button onClick={ctx.decrement}>-{ctx.step}</button>
        </div>");
```

This keeps everything in one place while maintaining type safety through the builder pattern.
