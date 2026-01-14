using Moj.TanStack.Ast.Core;
using Moj.TanStack.Dsl.Core;

namespace Moj.TanStack.Dsl.TanStack.Store;

/// <summary>
/// DSL for TanStack Store - a framework-agnostic reactive store
/// </summary>
public static class TanStackStore
{
    /// <summary>
    /// Creates a new Store instance with initial state
    /// </summary>
    public static StoreBuilder<TState> CreateStore<TState>() where TState : class =>
        new();

    /// <summary>
    /// Creates import for @tanstack/store
    /// </summary>
    public static TsImportDeclaration Import() =>
        Ts.Import("@tanstack/store")
            .Named("Store")
            .Build();

    /// <summary>
    /// Creates import for React adapter
    /// </summary>
    public static TsImportDeclaration ImportReact() =>
        Ts.Import("@tanstack/react-store")
            .Named("useStore")
            .Build();

    /// <summary>
    /// Creates import for Vue adapter
    /// </summary>
    public static TsImportDeclaration ImportVue() =>
        Ts.Import("@tanstack/vue-store")
            .Named("useStore")
            .Build();

    /// <summary>
    /// Creates import for Solid adapter
    /// </summary>
    public static TsImportDeclaration ImportSolid() =>
        Ts.Import("@tanstack/solid-store")
            .Named("useStore")
            .Build();
}

/// <summary>
/// Builder for TanStack Store
/// </summary>
public sealed class StoreBuilder<TState> where TState : class
{
    private TsExprBase? _initialState;
    private readonly List<(string name, TsExprBase updater)> _actions = [];
    private readonly List<(string name, TsExprBase selector)> _derived = [];
    private string? _variableName;
    private bool _exported;
    private TsExprBase? _onUpdate;

    /// <summary>
    /// Set the initial state
    /// </summary>
    public StoreBuilder<TState> WithState(TsExpr<TState> state)
    {
        _initialState = state;
        return this;
    }

    /// <summary>
    /// Set initial state using object builder
    /// </summary>
    public StoreBuilder<TState> WithState(TsObjectBuilder<TState> stateBuilder)
    {
        _initialState = stateBuilder.Build();
        return this;
    }

    /// <summary>
    /// Add an action that updates the store
    /// </summary>
    public StoreBuilder<TState> WithAction<TPayload>(string name, Func<TsExpr<TState>, TsExpr<TPayload>, TsExpr<TState>> updater)
    {
        var stateParam = Ts.Var<TState>("state");
        var payloadParam = Ts.Var<TPayload>("payload");
        var updateExpr = updater(stateParam, payloadParam);

        _actions.Add((name, Ts.Arrow<TPayload, TState>("payload")
            .Returns(p =>
            {
                // Create: store.setState(state => updater(state, payload))
                var storeRef = Ts.Var(_variableName ?? "store");
                return storeRef.Dot("setState").Invoke(
                    Ts.Arrow<TState, TState>("state").Returns(s => updater(s, p))
                ).Typed<TState>();
            })));
        return this;
    }

    /// <summary>
    /// Add a derived/computed value
    /// </summary>
    public StoreBuilder<TState> WithDerived<TDerived>(string name, Func<TsExpr<TState>, TsExpr<TDerived>> selector)
    {
        _derived.Add((name, Ts.Arrow<TState, TDerived>("state").Returns(selector)));
        return this;
    }

    /// <summary>
    /// Add onUpdate callback
    /// </summary>
    public StoreBuilder<TState> OnUpdate(TsExprBase handler)
    {
        _onUpdate = handler;
        return this;
    }

    /// <summary>
    /// Assign to a variable name
    /// </summary>
    public StoreBuilder<TState> As(string name)
    {
        _variableName = name;
        return this;
    }

    /// <summary>
    /// Export the store
    /// </summary>
    public StoreBuilder<TState> Export()
    {
        _exported = true;
        return this;
    }

    /// <summary>
    /// Build the store creation expression
    /// </summary>
    public TsExpr<object> BuildExpression()
    {
        var options = Ts.Object<object>()
            .With("state", _initialState ?? Ts.Object<TState>().Build());

        if (_onUpdate != null)
            options.With("onUpdate", _onUpdate);

        return Ts.New<object>("Store", options.Build());
    }

    /// <summary>
    /// Build as variable declaration
    /// </summary>
    public TsVariableDeclaration Build()
    {
        var name = _variableName ?? "store";
        var builder = Ts.Const<object>(name).Value(BuildExpression());
        if (_exported) builder.Export();
        return builder.Build();
    }

    /// <summary>
    /// Build with actions as a complete module
    /// </summary>
    public TsNode[] BuildWithActions()
    {
        var nodes = new List<TsNode> { Build() };

        foreach (var (actionName, updater) in _actions)
        {
            nodes.Add(Ts.Const<object>(actionName)
                .Value(updater)
                .Export()
                .Build());
        }

        foreach (var (derivedName, selector) in _derived)
        {
            nodes.Add(Ts.Const<object>(derivedName)
                .Value(selector)
                .Export()
                .Build());
        }

        return [.. nodes];
    }
}

/// <summary>
/// Extension methods for store usage in components
/// </summary>
public static class StoreExtensions
{
    /// <summary>
    /// Create a useStore hook call for React
    /// </summary>
    public static TsDynamicExpr UseStore(TsExprBase store, TsExprBase? selector = null)
    {
        if (selector != null)
            return Ts.Var("useStore").Invoke(store, selector);
        return Ts.Var("useStore").Invoke(store);
    }

    /// <summary>
    /// Create store.state access
    /// </summary>
    public static TsExpr<TState> State<TState>(this TsExpr<object> store) =>
        store.Prop<TState>("state");

    /// <summary>
    /// Create store.setState call
    /// </summary>
    public static TsDynamicExpr SetState<TState>(this TsExpr<object> store, TsExprBase updater) =>
        store.Prop<object>("setState").Call<object>(updater);

    /// <summary>
    /// Create store.subscribe call
    /// </summary>
    public static TsDynamicExpr Subscribe(this TsExprBase store, TsExprBase listener) =>
        store.Dot("subscribe").Invoke(listener);
}
