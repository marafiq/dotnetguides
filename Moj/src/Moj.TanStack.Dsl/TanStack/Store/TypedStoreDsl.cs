using Moj.TanStack.Ast.Core;
using Moj.TanStack.Ast.CodeGen;
using Moj.TanStack.Dsl.Core.TypeInference;

namespace Moj.TanStack.Dsl.TanStack.Store;

/// <summary>
/// Type-safe TanStack Store DSL that infers TypeScript types from C# types.
/// Actions and selectors are generated as type-safe stubs.
/// </summary>
public static class TanStackTypedStore
{
    /// <summary>
    /// Create a TanStack Store for a C# type.
    /// TypeScript interfaces and types are automatically generated.
    /// </summary>
    public static TypedStoreBuilder<TState> Store<TState>() where TState : class, new()
        => new();

    /// <summary>
    /// Create a TanStack Store with a specific initial state instance.
    /// </summary>
    public static TypedStoreBuilder<TState> Store<TState>(TState initialState) where TState : class
        => new TypedStoreBuilder<TState>().WithInitialState(initialState);
}

/// <summary>
/// Fluent builder for TanStack Store using C# type inference.
/// </summary>
public sealed class TypedStoreBuilder<TState> where TState : class
{
    private TState? _initialState;
    private readonly List<StoreActionDef> _actions = [];
    private readonly List<StoreSelectorDef> _selectors = [];
    private readonly List<StoreDerivedDef> _derived = [];
    private string _storeName = "store";
    private bool _exported = true;

    /// <summary>
    /// Set the initial state from a C# object instance.
    /// All values are automatically converted to TypeScript literals.
    /// </summary>
    public TypedStoreBuilder<TState> WithInitialState(TState state)
    {
        _initialState = state;
        return this;
    }

    /// <summary>
    /// Define a store action with a payload type.
    /// Generates a type-safe action function stub.
    /// </summary>
    public TypedStoreBuilder<TState> Action<TPayload>(string name, Func<TState, TPayload, TState> reducer)
    {
        _actions.Add(new StoreActionDef(name, typeof(TPayload)));
        return this;
    }

    /// <summary>
    /// Define a store action with no payload.
    /// </summary>
    public TypedStoreBuilder<TState> Action(string name, Func<TState, TState> reducer)
    {
        _actions.Add(new StoreActionDef(name, null));
        return this;
    }

    /// <summary>
    /// Define a selector that derives a value from state.
    /// </summary>
    public TypedStoreBuilder<TState> Selector<TResult>(string name, Func<TState, TResult> selector)
    {
        _selectors.Add(new StoreSelectorDef(name, typeof(TResult)));
        return this;
    }

    /// <summary>
    /// Define derived (computed) state that reactively updates when dependencies change.
    /// Uses TanStack Store's derived() function.
    /// </summary>
    public TypedStoreBuilder<TState> Derived<TResult>(string name, Func<TState, TResult> computation)
    {
        _derived.Add(new StoreDerivedDef(name, typeof(TResult)));
        return this;
    }

    /// <summary>
    /// Set the store variable name.
    /// </summary>
    public TypedStoreBuilder<TState> As(string name)
    {
        _storeName = name;
        return this;
    }

    /// <summary>
    /// Mark as exported (default true).
    /// </summary>
    public TypedStoreBuilder<TState> Export(bool export = true)
    {
        _exported = export;
        return this;
    }

    /// <summary>
    /// Build the store definition.
    /// </summary>
    public StoreDefinition<TState> Build()
    {
        return new StoreDefinition<TState>(
            _storeName,
            typeof(TState),
            _initialState,
            _actions,
            _selectors,
            _derived,
            _exported);
    }
}

/// <summary>
/// Represents an action definition (name and payload type).
/// </summary>
public sealed record StoreActionDef(string Name, Type? PayloadType);

/// <summary>
/// Represents a selector definition (name and result type).
/// </summary>
public sealed record StoreSelectorDef(string Name, Type ResultType);

/// <summary>
/// Represents a derived (computed) state definition.
/// </summary>
public sealed record StoreDerivedDef(string Name, Type ResultType);

/// <summary>
/// Represents a complete store definition ready for TypeScript generation.
/// </summary>
public sealed class StoreDefinition<TState> where TState : class
{
    public string Name { get; }
    public Type StateType { get; }
    public TState? InitialState { get; }
    public IReadOnlyList<StoreActionDef> Actions { get; }
    public IReadOnlyList<StoreSelectorDef> Selectors { get; }
    public IReadOnlyList<StoreDerivedDef> DerivedState { get; }
    public bool IsExported { get; }

    internal StoreDefinition(
        string name,
        Type stateType,
        TState? initialState,
        List<StoreActionDef> actions,
        List<StoreSelectorDef> selectors,
        List<StoreDerivedDef> derived,
        bool exported)
    {
        Name = name;
        StateType = stateType;
        InitialState = initialState;
        Actions = actions;
        Selectors = selectors;
        DerivedState = derived;
        IsExported = exported;
    }

    /// <summary>
    /// Generate complete TypeScript code for this store.
    /// </summary>
    public string ToTypeScript()
    {
        var generator = new StoreTypeScriptGenerator<TState>(this);
        return generator.Generate();
    }

    /// <summary>
    /// Generate TypeScript AST nodes.
    /// </summary>
    public IEnumerable<TsNode> ToAst()
    {
        var generator = new StoreTypeScriptGenerator<TState>(this);
        return generator.GenerateAst();
    }
}

/// <summary>
/// Generates TypeScript code from a StoreDefinition.
/// </summary>
internal sealed class StoreTypeScriptGenerator<TState> where TState : class
{
    private readonly StoreDefinition<TState> _definition;

    public StoreTypeScriptGenerator(StoreDefinition<TState> definition)
    {
        _definition = definition;
    }

    public string Generate()
    {
        var emitter = new TypeScriptEmitter();
        var program = new TsProgram(GenerateAst().ToList());
        return emitter.Emit(program);
    }

    public IEnumerable<TsNode> GenerateAst()
    {
        var nodes = new List<TsNode>();

        // Import statement (include 'derived' if we have derived state)
        var imports = new List<TsImportSpecifier> { new("Store") };
        if (_definition.DerivedState.Count > 0)
        {
            imports.Add(new TsImportSpecifier("derived"));
        }
        nodes.Add(new TsImportDeclaration(
            "@tanstack/store",
            new TsImportClause(NamedImports: imports)));

        // Generate interfaces for state type and all dependent types
        var dependentTypes = TypeToTsConverter.GetDependentTypes(_definition.StateType).ToList();
        foreach (var depType in dependentTypes)
        {
            nodes.Add(TypeToTsConverter.ToInterface(depType, export: true));
        }

        // Generate enums as type aliases
        var enumTypes = GetEnumTypes(_definition.StateType);
        foreach (var enumType in enumTypes)
        {
            nodes.Add(EnumToTsConverter.ToTsEnum(enumType, export: true));
        }

        // Main state interface
        nodes.Add(TypeToTsConverter.ToInterface(_definition.StateType, export: true));

        // Initial state
        TsExpression initialStateExpr;
        if (_definition.InitialState != null)
        {
            initialStateExpr = TypeToTsConverter.ToObjectLiteral(
                _definition.InitialState,
                _definition.StateType);
        }
        else
        {
            initialStateExpr = new TsObjectLiteral([]);
        }

        // Store declaration: export const store = new Store<StateType>({ ... })
        var storeDecl = new TsVariableDeclaration(
            TsVariableKind.Const,
            [new TsVariableDeclarator(
                new TsIdentifierBinding(_definition.Name),
                new TsGenericType(new TsTypeReference("Store"), [new TsTypeReference(_definition.StateType.Name)]),
                new TsNewExpression(
                    new TsIdentifier("Store"),
                    [initialStateExpr]))],
            _definition.IsExported);
        nodes.Add(storeDecl);

        // Generate actions
        foreach (var action in _definition.Actions)
        {
            nodes.Add(GenerateAction(action));
        }

        // Generate selectors
        foreach (var selector in _definition.Selectors)
        {
            nodes.Add(GenerateSelector(selector));
        }

        // Generate derived (computed) state
        foreach (var derived in _definition.DerivedState)
        {
            nodes.Add(GenerateDerived(derived));
        }

        return nodes;
    }

    private TsVariableDeclaration GenerateAction(StoreActionDef action)
    {
        var stateParam = new TsParameter(
            new TsIdentifierBinding("state"),
            new TsTypeReference(_definition.StateType.Name));

        TsArrowFunction actionFn;
        if (action.PayloadType != null)
        {
            var payloadParam = new TsParameter(
                new TsIdentifierBinding("payload"),
                TypeToTsConverter.ToTsType(action.PayloadType));

            // Generate: (payload) => store.setState((state) => ({ ...state }))
            var spreadState = new TsSpreadElement(new TsIdentifier("state"));

            var setStateFn = new TsArrowFunction(
                [stateParam],
                new TsObjectLiteral([spreadState]));

            actionFn = new TsArrowFunction(
                [payloadParam],
                new TsCallExpression(
                    new TsMemberAccess(new TsIdentifier(_definition.Name), "setState"),
                    [setStateFn]));
        }
        else
        {
            // No payload version
            var spreadState = new TsSpreadElement(new TsIdentifier("state"));

            var setStateFn = new TsArrowFunction(
                [stateParam],
                new TsObjectLiteral([spreadState]));

            actionFn = new TsArrowFunction(
                [],
                new TsCallExpression(
                    new TsMemberAccess(new TsIdentifier(_definition.Name), "setState"),
                    [setStateFn]));
        }

        return new TsVariableDeclaration(
            TsVariableKind.Const,
            [new TsVariableDeclarator(
                new TsIdentifierBinding(action.Name),
                Initializer: actionFn)],
            _definition.IsExported);
    }

    private TsVariableDeclaration GenerateSelector(StoreSelectorDef selector)
    {
        var stateParam = new TsParameter(
            new TsIdentifierBinding("state"),
            new TsTypeReference(_definition.StateType.Name));

        // Generate: (state: StateType): ResultType => { /* TODO */ return undefined as any; }
        var returnStmt = new TsReturnStatement(
            new TsAsExpression(
                new TsIdentifier("undefined"),
                TsPrimitiveTypes.Any));

        var selectorFn = new TsArrowFunction(
            [stateParam],
            new TsBlockStatement([returnStmt]),
            ReturnType: TypeToTsConverter.ToTsType(selector.ResultType));

        return new TsVariableDeclaration(
            TsVariableKind.Const,
            [new TsVariableDeclarator(
                new TsIdentifierBinding(selector.Name),
                Initializer: selectorFn)],
            _definition.IsExported);
    }

    private TsVariableDeclaration GenerateDerived(StoreDerivedDef derived)
    {
        // Generate: export const derivedName = derived({ store, fn: (state) => { /* TODO */ return undefined as any; } })
        var stateParam = new TsParameter(
            new TsIdentifierBinding("state"),
            new TsTypeReference(_definition.StateType.Name));

        var returnStmt = new TsReturnStatement(
            new TsAsExpression(
                new TsIdentifier("undefined"),
                TsPrimitiveTypes.Any));

        var fnArrow = new TsArrowFunction(
            [stateParam],
            new TsBlockStatement([returnStmt]),
            ReturnType: TypeToTsConverter.ToTsType(derived.ResultType));

        var derivedCall = new TsCallExpression(
            new TsIdentifier("derived"),
            [new TsObjectLiteral([
                new TsPropertyAssignment(new TsIdentifier("store"), new TsIdentifier(_definition.Name)),
                new TsPropertyAssignment(new TsIdentifier("fn"), fnArrow)
            ])]);

        return new TsVariableDeclaration(
            TsVariableKind.Const,
            [new TsVariableDeclarator(
                new TsIdentifierBinding(derived.Name),
                Initializer: derivedCall)],
            _definition.IsExported);
    }

    private IEnumerable<Type> GetEnumTypes(Type type)
    {
        var enums = new HashSet<Type>();
        CollectEnums(type, enums, new HashSet<Type>());
        return enums;
    }

    private void CollectEnums(Type type, HashSet<Type> enums, HashSet<Type> visited)
    {
        if (visited.Contains(type)) return;
        visited.Add(type);

        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
        {
            CollectEnums(underlying, enums, visited);
            return;
        }

        if (type.IsEnum)
        {
            enums.Add(type);
            return;
        }

        if (type.IsArray)
        {
            CollectEnums(type.GetElementType()!, enums, visited);
            return;
        }

        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
                CollectEnums(arg, enums, visited);
            return;
        }

        foreach (var prop in type.GetProperties())
        {
            CollectEnums(prop.PropertyType, enums, visited);
        }
    }
}
