# Impulse v3 Implementation Plan

## Overview

Impulse v3 is a complete rewrite focusing on:
- **Test-Driven Development**: Every module written with tests first
- **Clean Architecture**: Better separation of concerns
- **Type Safety**: End-to-end type safety from C# to TypeScript
- **Plugin System**: Extensible at every layer
- **Performance**: Efficient code generation and runtime

## Architecture

```
impulse-v3/
├── src/
│   ├── Impulse.Core/           # Core abstractions and runtime
│   ├── Impulse.CodeGen/        # TypeScript/Zod code generator
│   └── Impulse.React/          # React client runtime (npm package)
├── tests/
│   ├── Impulse.Core.Tests/     # Unit tests for core
│   ├── Impulse.CodeGen.Tests/  # Unit tests for code generation
│   └── Impulse.E2E.Tests/      # Playwright E2E tests
└── samples/
    └── CareHome/               # Full sample application
```

## Phase 1: Core Library (TDD)

### 1.1 Result Types

**Tests to write first:**
```csharp
// ImpulseResultTests.cs
[Fact] Result_Ok_HasStatusCode200()
[Fact] Result_Ok_ContainsData()
[Fact] Result_NotFound_HasStatusCode404()
[Fact] Result_NotFound_HasOptionalMessage()
[Fact] Result_ValidationProblem_HasStatusCode422()
[Fact] Result_ValidationProblem_ContainsFieldErrors()
[Fact] Result_Created_HasStatusCode201()
[Fact] Result_Created_HasLocationHeader()
[Fact] Result_Redirect_HasStatusCode302()
[Fact] Result_Redirect_HasLocationUrl()
```

**Implementation:**
- `IResult<T>` - Generic result interface
- `Result` - Static factory class
- `OkResult<T>`, `NotFoundResult`, `ValidationProblemResult`, `CreatedResult<T>`, `RedirectResult`

### 1.2 Endpoint Abstractions

**Tests to write first:**
```csharp
// EndpointTests.cs
[Fact] Endpoint_WithRequest_CanBeCreated()
[Fact] Endpoint_WithoutRequest_CanBeCreated()
[Fact] Endpoint_Handle_ReturnsResult()
[Fact] Endpoint_Handle_ReceivesCancellationToken()
[Fact] EndpointAttribute_RequiresRoute()
[Fact] EndpointAttribute_DefaultMethodIsGet()
[Fact] EndpointAttribute_SupportsAllHttpMethods()
```

**Implementation:**
- `IEndpoint` - Marker interface
- `Endpoint<TRequest, TResponse>` - Base class with request
- `Endpoint<TResponse>` - Base class without request
- `[Endpoint(route, method)]` - Attribute for routing

### 1.3 Validation

**Tests to write first:**
```csharp
// ValidationTests.cs
[Fact] ValidationProblem_SerializesToRfc7807()
[Fact] ValidationProblem_ContainsFieldErrors()
[Fact] ValidationProblem_FieldErrors_AreCamelCase()
[Fact] DataAnnotations_AreExtracted()
[Fact] FluentValidation_RulesAreExtracted()
```

**Implementation:**
- `ProblemDetails` - RFC 7807 compliant
- `IValidationExtractor` - Interface for extracting validation rules
- `DataAnnotationsExtractor` - Extract from [Required], [MaxLength], etc.
- `FluentValidationExtractor` - Extract from FluentValidation rules

### 1.4 ASP.NET Core Integration

**Tests to write first:**
```csharp
// IntegrationTests.cs
[Fact] MapEndpoints_DiscoversEndpointsFromAssembly()
[Fact] MapEndpoints_RegistersCorrectRoutes()
[Fact] MapEndpoints_HandlesGetRequests()
[Fact] MapEndpoints_HandlesPostRequests()
[Fact] MapEndpoints_ReturnsJsonForXImpulseHeader()
[Fact] MapEndpoints_ReturnsHtmlForBrowserRequests()
[Fact] MapEndpoints_HandlesRouteParameters()
[Fact] MapEndpoints_HandlesValidationErrors()
```

**Implementation:**
- `ImpulseWebApplicationExtensions.MapEndpoints()`
- Route parameter binding
- JSON/HTML content negotiation
- Validation error handling

## Phase 2: Code Generator (TDD)

### 2.1 TypeScript AST

**Tests to write first:**
```csharp
// TsAstTests.cs
[Fact] TsInterface_EmitsCorrectly()
[Fact] TsProperty_EmitsWithOptional()
[Fact] TsType_String_EmitsString()
[Fact] TsType_Number_EmitsNumber()
[Fact] TsType_Array_EmitsReadonlyArray()
[Fact] TsType_Union_EmitsUnion()
[Fact] TsEnum_EmitsStringEnum()
[Fact] TsConst_EmitsAsConst()
```

**Implementation:**
- `TsNode` hierarchy (TsFile, TsInterface, TsProperty, TsType variants)
- `TsEmitter` - Converts AST to TypeScript code

### 2.2 Zod AST

**Tests to write first:**
```csharp
// ZodAstTests.cs
[Fact] ZodObject_EmitsZodObject()
[Fact] ZodString_EmitsWithValidators()
[Fact] ZodNumber_EmitsWithIntValidator()
[Fact] ZodArray_EmitsWithMinMax()
[Fact] ZodNullable_WrapsType()
[Fact] ZodOptional_WrapsType()
[Fact] ZodEnum_EmitsNativeEnum()
[Fact] ZodRef_ReferencesOtherSchema()
```

**Implementation:**
- `ZodNode` hierarchy (ZodFile, ZodSchema, ZodType variants, ZodValidator variants)
- `ZodEmitter` - Converts AST to Zod code

### 2.3 Route AST

**Tests to write first:**
```csharp
// RouteAstTests.cs
[Fact] RouteDefinition_ExtractsParameters()
[Fact] RouteDefinition_IdentifiesQueryVsMutation()
[Fact] RoutePaths_EmitsConstObject()
[Fact] Mutations_EmitUseMutationHook()
[Fact] Loaders_EmitLoaderFunction()
[Fact] PathBuilders_EmitWithParameters()
```

**Implementation:**
- `RouteNode` hierarchy
- `RouteEmitter` - Emits routePaths.ts, mutations.ts, loaders.ts

### 2.4 Type Analyzer

**Tests to write first:**
```csharp
// TypeAnalyzerTests.cs
[Fact] Analyzer_MapsStringToTsString()
[Fact] Analyzer_MapsIntToTsNumber()
[Fact] Analyzer_MapsBoolToTsBoolean()
[Fact] Analyzer_MapsDateTimeToTsString()
[Fact] Analyzer_MapsNullableToUnion()
[Fact] Analyzer_MapsListToArray()
[Fact] Analyzer_MapsDictionaryToRecord()
[Fact] Analyzer_MapsEnumToTsEnum()
[Fact] Analyzer_MapsNestedTypesRecursively()
[Fact] Analyzer_TopologicallySortsDependencies()
```

**Implementation:**
- `TypeAnalyzer` - Converts C# types to TypeScript/Zod AST
- Handles all primitive types
- Handles collections (List, Array, IEnumerable)
- Handles dictionaries
- Handles nullable types
- Handles enums
- Handles nested/recursive types

### 2.5 CLI Tool

**Tests to write first:**
```csharp
// CliTests.cs
[Fact] Cli_RequiresAssemblyPath()
[Fact] Cli_RequiresOutputDir()
[Fact] Cli_CreatesOutputDirectory()
[Fact] Cli_GeneratesTypesTs()
[Fact] Cli_GeneratesValidationTs()
[Fact] Cli_GeneratesRoutePathsTs()
[Fact] Cli_GeneratesMutationsTs()
[Fact] Cli_GeneratesLoadersTs()
```

**Implementation:**
- `impulse-gen <assembly> <output>` CLI
- Assembly loading and endpoint discovery
- File generation

## Phase 3: React Client Runtime (TDD)

### 3.1 Impulse Context

**Tests to write first (Vitest):**
```typescript
// impulse-context.test.ts
test('impulse() fetches with X-Impulse header')
test('impulse() returns props from response')
test('impulseMutate() sends POST with JSON body')
test('impulseMutate() throws ImpulseValidationError on 422')
test('ImpulseValidationError contains field errors')
```

**Implementation:**
- `ImpulseContext` interface
- `createImpulseContext()` factory
- `ImpulseValidationError` class

### 3.2 React Provider

**Tests to write first:**
```typescript
// ImpulseProvider.test.tsx
test('ImpulseProvider provides context')
test('useImpulse() returns context')
test('useImpulse() throws outside provider')
```

**Implementation:**
- `ImpulseProvider` component
- `useImpulse()` hook

### 3.3 Form Integration

**Tests to write first:**
```typescript
// form-integration.test.tsx
test('validationErrors maps to S2 Form')
test('Zod errors are converted to field errors')
test('Server errors are displayed on fields')
```

**Implementation:**
- Error mapping utilities
- Form integration helpers

## Phase 4: Sample Application

### 4.1 CareHome Domain

**Entities:**
- Resident (id, firstName, lastName, dateOfBirth, roomNumber, status)
- Medication (id, residentId, name, dosage, frequency, startDate)
- CarePlan (id, residentId, goals, assessments)

**Endpoints:**
```
GET  /dashboard                     - Dashboard overview
GET  /residents                     - List residents
GET  /residents/{id}                - Resident detail
POST /residents                     - Create resident
PUT  /residents/{id}                - Update resident
GET  /residents/{id}/medications    - Resident medications
POST /residents/{id}/medications    - Add medication
POST /residents/{id}/medications/{medicationId}/administer - Record administration
GET  /residents/{id}/care-plan      - Care plan detail
POST /residents/{id}/care-plan/goals - Add goal
POST /residents/{id}/care-plan/assessments - Add assessment
GET  /admission/wizard              - Multi-step wizard config
POST /admission/wizard/validate/{step} - Validate wizard step
POST /admission/wizard/complete     - Complete admission
```

### 4.2 React Components

Using Adobe Spectrum 2 (S2):
- Dashboard with stats, activities, tasks
- Resident list with search/filter
- Resident detail with tabs
- Medication schedule
- Care plan with goals
- Admission wizard (multi-step form)

### 4.3 E2E Tests (Playwright)

```typescript
// residents.spec.ts
test('can list residents')
test('can view resident details')
test('can create new resident with validation')
test('server validation errors display on form')

// medications.spec.ts
test('can view medication schedule')
test('can add medication')
test('can record administration')

// wizard.spec.ts
test('wizard has 5 steps')
test('step validation prevents progression')
test('can complete full wizard flow')
```

## Implementation Order

### Day 1: Core Foundation
1. Create solution and project structure
2. Write and implement Result types with tests
3. Write and implement Endpoint abstractions with tests
4. Write and implement validation extraction with tests

### Day 2: ASP.NET Integration
1. Write and implement MapEndpoints with tests
2. Write and implement content negotiation with tests
3. Write and implement route parameter binding with tests

### Day 3: Code Generation - AST
1. Write and implement TypeScript AST with tests
2. Write and implement Zod AST with tests
3. Write and implement Route AST with tests

### Day 4: Code Generation - Analyzer & Emitters
1. Write and implement TypeAnalyzer with tests
2. Write and implement TsEmitter with tests
3. Write and implement ZodEmitter with tests
4. Write and implement RouteEmitter with tests

### Day 5: CLI & React Runtime
1. Write and implement CLI tool with tests
2. Write and implement React context with tests
3. Write and implement ImpulseProvider with tests

### Day 6: Sample Application - Backend
1. Create CareHome domain models
2. Implement all endpoints
3. Add mock data

### Day 7: Sample Application - Frontend
1. Set up React with S2
2. Create all components
3. Integrate with generated code

### Day 8: E2E Testing & Polish
1. Write Playwright tests
2. Fix any issues found
3. Documentation and cleanup

## Quality Criteria

- [ ] 100% test coverage on Core library
- [ ] 100% test coverage on CodeGen library
- [ ] All E2E tests passing
- [ ] TypeScript compiles with strict mode
- [ ] No runtime errors in sample app
- [ ] All generated code is properly formatted
- [ ] Clear error messages for validation failures

## Tech Stack

**Backend:**
- .NET 10
- ASP.NET Core Minimal APIs
- xUnit for unit tests

**Code Generation:**
- Reflection-based type analysis
- String-based AST emission (simple, fast)

**Frontend:**
- React 19
- TypeScript 5.x (strict mode)
- Vite for bundling
- TanStack Router for routing
- TanStack Query for data fetching
- Adobe Spectrum 2 for UI
- Zod for validation
- Vitest for unit tests
- Playwright for E2E tests
