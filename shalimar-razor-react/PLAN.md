# Shalimar Razor-React Compiler

## Vision

**Write React in C# using Razor syntax. Zero TypeScript authoring.**

```
.razor → RazorProjectEngine → SyntaxTree → TSX Emitter → React Components
```

-----

## The Proof

Microsoft's `Microsoft.AspNetCore.Razor.Language` package (615M+ downloads) provides:

- Battle-tested Razor parser
- Full syntax tree access via `RazorProjectEngine`
- Extensible code generation pipeline
- MIT Licensed

We write **only the emitter** (~500 lines). Microsoft maintains everything else.

-----

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         INPUT                                    │
│                                                                  │
│  @inherits SliceComponent<ResidentCardProps>                    │
│                                                                  │
│  <div class="card">                                             │
│      <h1>@Props.Name</h1>                                       │
│      @foreach (var med in Props.Medications)                    │
│      {                                                          │
│          <MedRow medication="@med" />                           │
│      }                                                          │
│  </div>                                                         │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                  RazorProjectEngine.Process()                    │
│                                                                  │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐         │
│  │   Lexer     │ →  │   Parser    │ →  │ SyntaxTree  │         │
│  └─────────────┘    └─────────────┘    └─────────────┘         │
│                                                                  │
│  Microsoft.AspNetCore.Razor.Language (battle-tested)            │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    OUR CODE: TsxEmitter                          │
│                                                                  │
│  SyntaxWalker that visits:                                      │
│  • MarkupElement → JSX tags                                     │
│  • MarkupAttribute → JSX attributes (class→className)           │
│  • CSharpExpression → {expression}                              │
│  • CSharpCodeBlock → map/conditional rendering                  │
│  • RazorDirective → component metadata                          │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                         OUTPUT                                   │
│                                                                  │
│  import React from 'react';                                     │
│                                                                  │
│  export interface ResidentCardProps {                           │
│    Name: string;                                                │
│    Medications: Medication[];                                   │
│  }                                                              │
│                                                                  │
│  export function ResidentCard(props: ResidentCardProps) {       │
│    return (                                                     │
│      <div className="card">                                     │
│        <h1>{props.Name}</h1>                                    │
│        {props.Medications.map((med) => (                        │
│          <MedRow medication={med} />                            │
│        ))}                                                      │
│      </div>                                                     │
│    );                                                           │
│  }                                                              │
└─────────────────────────────────────────────────────────────────┘
```

-----

## POC Implementation Status

### Phase 1: Parser Integration ✅

- [x] Reference `Microsoft.AspNetCore.Razor.Language`
- [x] Create `RazorProjectEngine` instance
- [x] Parse `.razor` file to `RazorSyntaxTree`
- [x] Dump syntax tree structure for inspection

### Phase 2: Basic TSX Emission ✅

- [x] Implement `TsxEmitter : SyntaxWalker`
- [x] Handle `MarkupElement` → JSX tags
- [x] Handle `MarkupTextLiteral` → text content
- [x] Handle `MarkupAttribute` → JSX attributes
- [x] Transform `class` → `className`

### Phase 3: C# Expressions ✅

- [x] Handle `@Props.X` → `{props.X}`
- [x] Handle `@expression` → `{expression}`
- [x] Handle method calls

### Phase 4: Control Flow ✅

- [x] Handle `@if (cond) { }` → `{cond && ( )}`
- [x] Handle `@if/else` → ternary
- [x] Handle `@foreach` → `.map()`

### Phase 5: Component Integration ✅

- [x] Parse `@inherits SliceComponent<TProps>`
- [x] Extract props type name
- [x] Generate TypeScript interface stub
- [x] Handle child components with props

### Phase 6: Event Handlers ✅

- [x] Handle `@onclick` → `onClick`
- [x] Handle `@onchange` → `onChange`

-----

## File Structure

```
shalimar-razor-react/
├── PLAN.md                          # This file
├── src/
│   ├── Shalimar.Razor.sln
│   ├── Shalimar.Razor/
│   │   ├── Shalimar.Razor.csproj
│   │   ├── RazorReactCompiler.cs    # Main compiler entry
│   │   ├── TsxEmitter.cs            # SyntaxWalker → TSX
│   │   ├── ExpressionTransformer.cs # C# expr → JS expr
│   │   ├── AttributeTransformer.cs  # HTML attr → JSX attr
│   │   └── TypeExtractor.cs         # Props type extraction
│   │
│   ├── Shalimar.Razor.Tests/
│   │   ├── Shalimar.Razor.Tests.csproj
│   │   └── EmitterTests.cs
│   │
│   └── Shalimar.Razor.Cli/
│       ├── Shalimar.Razor.Cli.csproj
│       └── Program.cs               # CLI tool
│
├── samples/
│   └── Components/
│       ├── ResidentCard.razor
│       ├── MedPassGrid.razor
│       └── SimpleButton.razor
│
└── docs/
    └── screenshots/
        ├── 01-razor-input.txt
        ├── 02-syntax-tree.txt
        └── 03-tsx-output.txt
```

-----

## Razor → TSX Transformation Rules

|Razor                       |TSX                           |Notes                      |
|----------------------------|------------------------------|---------------------------|
|`<div class="x">`           |`<div className="x">`         |Attribute rename           |
|`<label for="x">`           |`<label htmlFor="x">`         |Attribute rename           |
|`@Props.Name`               |`{props.Name}`                |Expression, lowercase props|
|`@(expr)`                   |`{expr}`                      |Explicit expression        |
|`@if (c) { <X/> }`          |`{c && <X/>}`                 |Conditional                |
|`@if (c) { A } else { B }`  |`{c ? A : B}`                 |Ternary                    |
|`@foreach (var x in xs) { }`|`{xs.map(x => ( ))}`          |Iteration                  |
|`@onclick="@Handler"`       |`onClick={Handler}`           |Event binding              |

-----

## The Kicker

```
Lines of code Microsoft wrote: ~50,000+ (parser alone)
Lines of code we write: ~500 (emitter)

We get: Full Razor syntax, IDE support, error recovery, 10+ years of hardening
We build: Just the output target
```

**That's leverage.**
