# Shalimar Razor-React Compiler Architecture

## Overview

The Shalimar Razor-React Compiler transforms Blazor-style `.razor` files into React TSX components. It leverages Microsoft's battle-tested Razor parser while providing a custom TSX emitter.

```
┌─────────────┐     ┌─────────────────────┐     ┌─────────────┐
│   .razor    │ ──▶ │ RazorProjectEngine  │ ──▶ │    .tsx     │
│   (Input)   │     │  (Microsoft Parser) │     │  (Output)   │
└─────────────┘     └─────────────────────┘     └─────────────┘
                              │
                              ▼
                    ┌─────────────────────┐
                    │   RazorSyntaxTree   │
                    │   (AST Nodes)       │
                    └─────────────────────┘
                              │
                              ▼
                    ┌─────────────────────┐
                    │     TsxEmitter      │
                    │   (Our Code ~500L)  │
                    └─────────────────────┘
```

## Key Components

### 1. RazorReactCompiler (`RazorReactCompiler.cs`)

The main entry point that orchestrates the compilation pipeline.

**Responsibilities:**
- Creates and configures `RazorProjectEngine`
- Provides virtual file system for in-memory compilation
- Coordinates parsing and emission
- Reports compilation errors with source locations

```csharp
public class RazorReactCompiler
{
    public CompilationResult Compile(string razorSource, string fileName);
    public string CompileToString(string razorSource, string fileName);
    public string DumpSyntaxTree(string razorSource, string fileName);
}
```

### 2. TsxEmitter (`TsxEmitter.cs`)

A syntax tree visitor that walks the Razor AST and emits TSX.

**Handles:**
- `MarkupElementSyntax` → JSX elements
- `MarkupAttributeBlockSyntax` → JSX attributes
- `CSharpExpressionLiteralSyntax` → `{expression}`
- `CSharpCodeBlockSyntax` → Control flow patterns
- `RazorDirectiveSyntax` → Component metadata

### 3. AttributeTransformer (`AttributeTransformer.cs`)

Transforms HTML attributes to JSX-compatible names.

**Mappings:**
| HTML | JSX |
|------|-----|
| `class` | `className` |
| `for` | `htmlFor` |
| `@onclick` | `onClick` |
| `tabindex` | `tabIndex` |
| `readonly` | `readOnly` |

### 4. ExpressionTransformer (`ExpressionTransformer.cs`)

Transforms C# expressions to JavaScript equivalents.

**Mappings:**
| C# | JavaScript |
|----|------------|
| `Props.X` | `props.X` |
| `list.Select(...)` | `list.map(...)` |
| `list.Where(...)` | `list.filter(...)` |
| `list.Count` | `list.length` |
| `str.ToUpper()` | `str.toUpperCase()` |

### 5. TypeExtractor (`TypeExtractor.cs`)

Extracts type information for TypeScript interface generation.

**Analyzes:**
- `@inherits` directives for props type names
- `@Props.X` usages for property inference
- Event handler usages for handler signatures

## Compilation Pipeline

```
1. Input: Razor source string + filename

2. Parse:
   RazorProjectEngine.Process(projectItem)
   └─▶ RazorCodeDocument
       └─▶ RazorSyntaxTree

3. Validate:
   Check syntaxTree.Diagnostics for errors
   └─▶ Return CompilationResult.Failed() if errors

4. Extract Metadata (First Pass):
   - @inherits SliceComponent<TProps>
   - Child component references
   - Event handler patterns

5. Emit TSX (Second Pass):
   - React import
   - Child component imports
   - Props interface
   - Function component
   - JSX return statement

6. Output: TSX string
```

## Syntax Tree Node Types

The Razor parser produces these key node types:

```
RazorDocumentSyntax
├── RazorDirectiveSyntax         (@inherits, @using, @inject)
├── MarkupBlockSyntax            (HTML content container)
│   ├── MarkupElementSyntax      (<div>, <span>, etc.)
│   │   ├── MarkupStartTagSyntax
│   │   ├── MarkupAttributeBlockSyntax
│   │   └── MarkupEndTagSyntax
│   └── MarkupTextLiteralSyntax  (text content)
├── CSharpExpressionLiteralSyntax (@expression)
└── CSharpCodeBlockSyntax        (@if, @foreach, @code)
```

## Design Decisions

### Why RazorProjectEngine?

1. **Battle-tested**: 615M+ NuGet downloads
2. **Full syntax support**: Handles all Razor edge cases
3. **Error recovery**: Continues parsing after errors
4. **IDE support**: Same parser as VS/Rider extensions
5. **MIT Licensed**: Free for commercial use

### Why Custom Emitter?

The official Razor emitter targets:
- Blazor WebAssembly runtime
- Blazor Server render tree

Neither produces React JSX. Our emitter:
- Targets React/TSX directly
- Uses familiar React patterns (className, map, &&)
- Generates idiomatic TypeScript

### Expression Transformation Strategy

We transform C# to JavaScript at the string level rather than using Roslyn because:
1. Most expressions are simple property access
2. Razor expressions are already valid JS in many cases
3. Full Roslyn analysis would be overkill for POC

For production, Roslyn integration would enable:
- Type-aware transformations
- Better error messages
- IDE integration

## Extension Points

### Adding New Transformations

1. **New attribute mappings**: Add to `AttributeTransformer.AttributeMap`
2. **New expression patterns**: Add regex in `ExpressionTransformer`
3. **New syntax nodes**: Add case in `TsxEmitter.EmitNode()`

### Future Enhancements

1. **@code blocks**: Generate custom hooks
2. **@inject**: Generate React context consumption
3. **@typeparam**: Generate generic components
4. **@bind**: Generate controlled input patterns
5. **Source maps**: Map TSX lines to Razor lines

## Testing Strategy

### Unit Tests
- Individual transformation rules
- Expression transformer patterns
- Attribute transformer mappings

### Snapshot Tests
- Full component transformations
- Compare against expected TSX files

### Integration Tests
- TypeScript compilation of output
- React rendering validation

## CLI Commands

```bash
# Compile single file
shalimar compile Component.razor

# Compile directory
shalimar compile ./Components -o ./output

# Dump syntax tree (debugging)
shalimar tree Component.razor

# Watch mode (development)
shalimar watch ./Components -o ./dist

# Demo transformation
shalimar demo
```
