# Shalimar Razor-React Specification

## Vision
Write `.razor` files with C#-like syntax → Generate idiomatic React TSX with TanStack integration.

---

## Component Types

### @client - Interactive Client Component
```razor
@client
@using TanStack.Store

<div>
    <h1>Counter: @count</h1>
    <button @onclick="increment">+1</button>
</div>

@code {
    [Store] int count = 0;

    void increment() {
        count++;
    }
}
```

**Generates:**
```tsx
import React from 'react';
import { useStore } from '@tanstack/react-store';
import { Store } from '@tanstack/store';

const counterStore = new Store({ count: 0 });

export function Counter() {
  const count = useStore(counterStore, (s) => s.count);

  const increment = () => {
    counterStore.setState((s) => ({ ...s, count: s.count + 1 }));
  };

  return (
    <div>
      <h1>Counter: {count}</h1>
      <button onClick={increment}>+1</button>
    </div>
  );
}
```

### @server - Server Component (RSC)
```razor
@server

<div>
    <h1>@Props.Title</h1>
    <p>Rendered on server</p>
</div>

@code {
    [Parameter] public string Title { get; set; }
}
```

**Generates:** (RSC wire format + static TSX)

---

## Expressions

### Basic
| Razor | TSX |
|-------|-----|
| `@variable` | `{variable}` |
| `@Props.Name` | `{Name}` |
| `@item.Property` | `{item.Property}` |
| `@(a + b)` | `{a + b}` |
| `@($"Hello {name}")` | `{\`Hello ${name}\`}` |

### Conditionals
| Razor | TSX |
|-------|-----|
| `@if (x) { <A/> }` | `{x && <A/>}` |
| `@if (x) { <A/> } else { <B/> }` | `{x ? <A/> : <B/>}` |
| `@if (x) { } else if (y) { } else { }` | `{x ? ... : y ? ... : ...}` |
| `@(condition ? <A/> : <B/>)` | `{condition ? <A/> : <B/>}` |

### Loops
| Razor | TSX |
|-------|-----|
| `@foreach (var x in items) { <li>@x</li> }` | `{items.map((x, i) => <li key={i}>{x}</li>)}` |
| `@for (int i = 0; i < 5; i++) { }` | `{[...Array(5)].map((_, i) => ...)}` |

---

## Event Handlers

| Razor | TSX |
|-------|-----|
| `@onclick="handler"` | `onClick={handler}` |
| `@onclick="() => doThing()"` | `onClick={() => doThing()}` |
| `@onchange="handleChange"` | `onChange={handleChange}` |
| `@onsubmit="handleSubmit"` | `onSubmit={handleSubmit}` |
| `@onkeydown="handleKey"` | `onKeyDown={handleKey}` |

---

## State Management (TanStack Store)

### [Store] Attribute - Local Component State
```razor
@client

@code {
    [Store] int count = 0;
    [Store] string name = "";
    [Store] List<Item> items = new();
}
```

**Generates:**
```tsx
import { Store } from '@tanstack/store';
import { useStore } from '@tanstack/react-store';

interface ComponentState {
  count: number;
  name: string;
  items: Item[];
}

const componentStore = new Store<ComponentState>({
  count: 0,
  name: "",
  items: []
});

export function Component() {
  const count = useStore(componentStore, (s) => s.count);
  const name = useStore(componentStore, (s) => s.name);
  const items = useStore(componentStore, (s) => s.items);
  // ...
}
```

### State Mutations
```razor
@code {
    [Store] int count = 0;

    void increment() {
        count++;  // Becomes: store.setState(s => ({...s, count: s.count + 1}))
    }

    void reset() {
        count = 0;  // Becomes: store.setState(s => ({...s, count: 0}))
    }
}
```

---

## Two-Way Binding

### @bind for Inputs
```razor
<input @bind="name" />
<input @bind="email" type="email" />
<select @bind="selectedId">
    @foreach (var opt in options) {
        <option value="@opt.Id">@opt.Name</option>
    }
</select>
```

**Generates:**
```tsx
<input value={name} onChange={(e) => setName(e.target.value)} />
<input value={email} onChange={(e) => setEmail(e.target.value)} type="email" />
<select value={selectedId} onChange={(e) => setSelectedId(e.target.value)}>
  {options.map((opt, i) => (
    <option key={i} value={opt.Id}>{opt.Name}</option>
  ))}
</select>
```

---

## Imports

### @using for Components
```razor
@using "./ProductCard"
@using "./Button" as PrimaryButton
@using { formatDate, formatCurrency } from "./utils"

<ProductCard product="@item" />
<PrimaryButton @onclick="save">Save</PrimaryButton>
<span>@formatDate(date)</span>
```

**Generates:**
```tsx
import { ProductCard } from './ProductCard';
import { Button as PrimaryButton } from './Button';
import { formatDate, formatCurrency } from './utils';
```

---

## Children & Slots

### @children (Default Slot)
```razor
@* Card.razor *@
<div class="card">
    <div class="card-body">
        @children
    </div>
</div>

@code {
    [Parameter] public RenderFragment children { get; set; }
}
```

**Usage:**
```razor
<Card>
    <h1>Title</h1>
    <p>Content here</p>
</Card>
```

**Generates:**
```tsx
export function Card({ children }: { children: React.ReactNode }) {
  return (
    <div className="card">
      <div className="card-body">
        {children}
      </div>
    </div>
  );
}
```

---

## CSS Support

### Scoped Styles
```razor
<div class="container">
    <span class="label">Text</span>
</div>

<style>
.container { padding: 20px; }
.label { color: blue; }
</style>
```

**Generates:** CSS Modules or styled-jsx

---

## Example: Full Interactive Component

```razor
@client
@using "./api" as api
@using "./ProductCard"

<div class="product-list">
    <input @bind="search" placeholder="Search..." />

    @if (loading) {
        <div class="spinner">Loading...</div>
    } else if (error) {
        <div class="error">@error</div>
    } else {
        <div class="grid">
            @foreach (var product in filteredProducts) {
                <ProductCard
                    product="@product"
                    @onclick="() => selectProduct(product)"
                />
            }
        </div>
    }

    @if (selectedProduct != null) {
        <Modal @onclose="() => selectedProduct = null">
            <h2>@selectedProduct.Name</h2>
            <p>@selectedProduct.Description</p>
            <button @onclick="addToCart">Add to Cart</button>
        </Modal>
    }
</div>

@code {
    [Parameter] public List<Product> products { get; set; }

    [Store] string search = "";
    [Store] Product? selectedProduct = null;
    [Store] bool loading = false;
    [Store] string? error = null;

    List<Product> filteredProducts =>
        products.Where(p => p.Name.Contains(search)).ToList();

    void selectProduct(Product p) {
        selectedProduct = p;
    }

    async void addToCart() {
        loading = true;
        try {
            await api.addToCart(selectedProduct);
            selectedProduct = null;
        } catch (Exception e) {
            error = e.Message;
        }
        loading = false;
    }
}
```

---

## Priority Implementation Order

1. **Event handlers** (@onclick → onClick)
2. **@else / else if** (ternary expressions)
3. **TanStack Store** ([Store] attribute)
4. **@bind** (two-way binding)
5. **@using** imports
6. **@children** slots
7. **Scoped styles**
