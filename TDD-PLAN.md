# TDD Implementation Plan

## Constraint: Each sub-agent < 10K tokens

## Phase 1: Core Generators (No Roslyn)

These generators are pure functions: `ImpulseModel → string`. No external dependencies.

| # | Task | Input | Output | Tests |
|---|------|-------|--------|-------|
| 1 | TypesGenerator | TypeModel[] | types.ts | 6 tests |
| 2 | ZodGenerator | ValidatorModel[] | validation/*.ts | 5 tests |
| 3 | DotNetRoutesGenerator | Endpoints + Mutations | Routes.g.cs | 5 tests |
| 4 | FormGenerator | Mutations + Validators | forms/*.ts | 4 tests |
| 5 | RoutesGenerator | Endpoints | TanStack routes | 5 tests |

## Phase 2: ModelBuilder (Requires Roslyn)

Extracts ImpulseModel from C# source code.

| # | Task | Input | Output | Tests |
|---|------|-------|--------|-------|
| 6 | FromType | C# type symbol | TypeModel | 7 tests |
| 7 | FromEndpoint | MapGet invocation | EndpointModel | 6 tests |
| 8 | FromMutation | MapPost/Put/Delete | MutationModel | 4 tests |
| 9 | FromValidator | AbstractValidator<T> | ValidatorModel | 8 tests |

## Execution Order

```
Phase 1 (parallel - no dependencies):
  [1] TypesGenerator
  [2] ZodGenerator
  [3] DotNetRoutesGenerator

Phase 1 (after Zod):
  [4] FormGenerator (needs Zod schema names)

Phase 1 (after Types):
  [5] RoutesGenerator (needs type imports)

Phase 2 (sequential - Roslyn setup):
  [6] FromType
  [7] FromEndpoint
  [8] FromMutation
  [9] FromValidator
```

## Sub-Agent Prompt Template

```
Task: TDD for {GeneratorName}
Location: /home/user/dotnetguides
Budget: < 10K tokens

1. Read: src/Impulse.CodeGen.V2/Models.cs
2. Create test: tests/.../Generators/{Name}Tests.cs
3. Write N failing tests (list specific test names)
4. Create impl: src/.../Generators/{Name}.cs
5. Run: dotnet test
6. Return: pass/fail count
```

## Approval Required

Before starting, confirm:
- [ ] Phase 1 first (no Roslyn complexity)
- [ ] 3 parallel agents for generators 1-3
- [ ] Sequential for 4-5 (dependencies)
- [ ] Phase 2 after Phase 1 complete
