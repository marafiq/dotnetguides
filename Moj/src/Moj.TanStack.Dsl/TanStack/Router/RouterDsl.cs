using Moj.TanStack.Ast.Core;
using Moj.TanStack.Dsl.Core;

namespace Moj.TanStack.Dsl.TanStack.Router;

/// <summary>
/// DSL for TanStack Router - Type-safe routing for React
/// </summary>
public static class TanStackRouter
{
    /// <summary>
    /// Creates a root route
    /// </summary>
    public static RootRouteBuilder CreateRootRoute() => new();

    /// <summary>
    /// Creates a route builder
    /// </summary>
    public static RouteBuilder<TParent> CreateRoute<TParent>() => new();

    /// <summary>
    /// Creates a router builder
    /// </summary>
    public static RouterBuilder CreateRouter() => new();

    /// <summary>
    /// Import from @tanstack/react-router
    /// </summary>
    public static TsImportDeclaration ImportReact(params string[] imports) =>
        Ts.Import("@tanstack/react-router")
            .Named(imports.Length > 0 ? imports : ["createRouter", "createRoute", "createRootRoute"])
            .Build();

    /// <summary>
    /// Import Link component
    /// </summary>
    public static TsImportDeclaration ImportLink() =>
        Ts.Import("@tanstack/react-router").Named("Link").Build();

    /// <summary>
    /// Import Outlet component
    /// </summary>
    public static TsImportDeclaration ImportOutlet() =>
        Ts.Import("@tanstack/react-router").Named("Outlet").Build();

    /// <summary>
    /// Import router hooks
    /// </summary>
    public static TsImportDeclaration ImportHooks() =>
        Ts.Import("@tanstack/react-router")
            .Named("useRouter", "useParams", "useSearch", "useNavigate", "useMatch")
            .Build();
}

/// <summary>
/// Builder for root route
/// </summary>
public sealed class RootRouteBuilder
{
    private TsExprBase? _component;
    private TsExprBase? _errorComponent;
    private TsExprBase? _pendingComponent;
    private TsExprBase? _notFoundComponent;
    private TsExprBase? _beforeLoad;
    private string _variableName = "rootRoute";
    private bool _exported;

    public RootRouteBuilder Component(TsExprBase component)
    {
        _component = component;
        return this;
    }

    public RootRouteBuilder ErrorComponent(TsExprBase component)
    {
        _errorComponent = component;
        return this;
    }

    public RootRouteBuilder PendingComponent(TsExprBase component)
    {
        _pendingComponent = component;
        return this;
    }

    public RootRouteBuilder NotFoundComponent(TsExprBase component)
    {
        _notFoundComponent = component;
        return this;
    }

    public RootRouteBuilder BeforeLoad(TsExprBase handler)
    {
        _beforeLoad = handler;
        return this;
    }

    public RootRouteBuilder As(string name)
    {
        _variableName = name;
        return this;
    }

    public RootRouteBuilder Export()
    {
        _exported = true;
        return this;
    }

    public TsExprBase BuildExpression()
    {
        var args = Ts.Object<object>();

        if (_component != null)
            args.With("component", _component);
        if (_errorComponent != null)
            args.With("errorComponent", _errorComponent);
        if (_pendingComponent != null)
            args.With("pendingComponent", _pendingComponent);
        if (_notFoundComponent != null)
            args.With("notFoundComponent", _notFoundComponent);
        if (_beforeLoad != null)
            args.With("beforeLoad", _beforeLoad);

        return Ts.Var("createRootRoute").Invoke(args.Build());
    }

    public TsVariableDeclaration Build()
    {
        var builder = Ts.Const<object>(_variableName).Value(BuildExpression());
        if (_exported) builder.Export();
        return builder.Build();
    }
}

/// <summary>
/// Builder for routes
/// </summary>
public sealed class RouteBuilder<TParent>
{
    private string? _path;
    private TsExprBase? _parent;
    private TsExprBase? _component;
    private TsExprBase? _errorComponent;
    private TsExprBase? _pendingComponent;
    private TsExprBase? _loader;
    private TsExprBase? _beforeLoad;
    private TsExprBase? _validateSearch;
    private TsExprBase? _loaderDeps;
    private readonly Dictionary<string, TsType> _params = [];
    private readonly Dictionary<string, TsType> _searchParams = [];
    private string _variableName = "route";
    private bool _exported;

    public RouteBuilder<TParent> Path(string path)
    {
        _path = path;
        return this;
    }

    public RouteBuilder<TParent> GetParentRoute(TsExprBase parentRoute)
    {
        _parent = parentRoute;
        return this;
    }

    public RouteBuilder<TParent> Component(TsExprBase component)
    {
        _component = component;
        return this;
    }

    public RouteBuilder<TParent> ErrorComponent(TsExprBase component)
    {
        _errorComponent = component;
        return this;
    }

    public RouteBuilder<TParent> PendingComponent(TsExprBase component)
    {
        _pendingComponent = component;
        return this;
    }

    /// <summary>
    /// Add a route param definition
    /// </summary>
    public RouteBuilder<TParent> Param<T>(string name)
    {
        _params[name] = TsTypeMapper.MapType(typeof(T));
        return this;
    }

    /// <summary>
    /// Add search param validation
    /// </summary>
    public RouteBuilder<TParent> SearchParam<T>(string name, bool required = false)
    {
        var type = TsTypeMapper.MapType(typeof(T));
        _searchParams[name] = required ? type : new TsUnionType([type, TsPrimitiveTypes.Undefined]);
        return this;
    }

    /// <summary>
    /// Add search validation schema
    /// </summary>
    public RouteBuilder<TParent> ValidateSearch(TsExprBase validator)
    {
        _validateSearch = validator;
        return this;
    }

    /// <summary>
    /// Add a data loader
    /// </summary>
    public RouteBuilder<TParent> Loader(TsExprBase loader)
    {
        _loader = loader;
        return this;
    }

    /// <summary>
    /// Add loader dependencies
    /// </summary>
    public RouteBuilder<TParent> LoaderDeps(TsExprBase deps)
    {
        _loaderDeps = deps;
        return this;
    }

    /// <summary>
    /// Add beforeLoad hook
    /// </summary>
    public RouteBuilder<TParent> BeforeLoad(TsExprBase handler)
    {
        _beforeLoad = handler;
        return this;
    }

    public RouteBuilder<TParent> As(string name)
    {
        _variableName = name;
        return this;
    }

    public RouteBuilder<TParent> Export()
    {
        _exported = true;
        return this;
    }

    public TsExprBase BuildExpression()
    {
        var args = Ts.Object<object>();

        if (_parent != null)
            args.With("getParentRoute", Ts.Arrow<object>().Returns(_parent));
        if (_path != null)
            args.With("path", Ts.String(_path));
        if (_component != null)
            args.With("component", _component);
        if (_errorComponent != null)
            args.With("errorComponent", _errorComponent);
        if (_pendingComponent != null)
            args.With("pendingComponent", _pendingComponent);
        if (_loader != null)
            args.With("loader", _loader);
        if (_loaderDeps != null)
            args.With("loaderDeps", _loaderDeps);
        if (_beforeLoad != null)
            args.With("beforeLoad", _beforeLoad);
        if (_validateSearch != null)
            args.With("validateSearch", _validateSearch);

        return Ts.Var("createRoute").Invoke(args.Build());
    }

    public TsVariableDeclaration Build()
    {
        var builder = Ts.Const<object>(_variableName).Value(BuildExpression());
        if (_exported) builder.Export();
        return builder.Build();
    }
}

/// <summary>
/// Builder for router configuration
/// </summary>
public sealed class RouterBuilder
{
    private TsExprBase? _routeTree;
    private TsExprBase? _defaultPreload;
    private TsExprBase? _defaultPreloadDelay;
    private TsExprBase? _defaultErrorComponent;
    private TsExprBase? _defaultPendingComponent;
    private TsExprBase? _defaultNotFoundComponent;
    private TsExprBase? _context;
    private string _variableName = "router";
    private bool _exported;

    public RouterBuilder RouteTree(TsExprBase tree)
    {
        _routeTree = tree;
        return this;
    }

    public RouterBuilder DefaultPreload(string preload)
    {
        _defaultPreload = Ts.String(preload);
        return this;
    }

    public RouterBuilder DefaultPreloadDelay(int ms)
    {
        _defaultPreloadDelay = Ts.Int(ms);
        return this;
    }

    public RouterBuilder DefaultErrorComponent(TsExprBase component)
    {
        _defaultErrorComponent = component;
        return this;
    }

    public RouterBuilder DefaultPendingComponent(TsExprBase component)
    {
        _defaultPendingComponent = component;
        return this;
    }

    public RouterBuilder DefaultNotFoundComponent(TsExprBase component)
    {
        _defaultNotFoundComponent = component;
        return this;
    }

    public RouterBuilder Context(TsExprBase context)
    {
        _context = context;
        return this;
    }

    public RouterBuilder As(string name)
    {
        _variableName = name;
        return this;
    }

    public RouterBuilder Export()
    {
        _exported = true;
        return this;
    }

    public TsExprBase BuildExpression()
    {
        var args = Ts.Object<object>();

        if (_routeTree != null)
            args.With("routeTree", _routeTree);
        if (_defaultPreload != null)
            args.With("defaultPreload", _defaultPreload);
        if (_defaultPreloadDelay != null)
            args.With("defaultPreloadDelay", _defaultPreloadDelay);
        if (_defaultErrorComponent != null)
            args.With("defaultErrorComponent", _defaultErrorComponent);
        if (_defaultPendingComponent != null)
            args.With("defaultPendingComponent", _defaultPendingComponent);
        if (_defaultNotFoundComponent != null)
            args.With("defaultNotFoundComponent", _defaultNotFoundComponent);
        if (_context != null)
            args.With("context", _context);

        return Ts.Var("createRouter").Invoke(args.Build());
    }

    public TsVariableDeclaration Build()
    {
        var builder = Ts.Const<object>(_variableName).Value(BuildExpression());
        if (_exported) builder.Export();
        return builder.Build();
    }
}

/// <summary>
/// Route tree builder for combining routes
/// </summary>
public sealed class RouteTreeBuilder
{
    private TsExprBase? _rootRoute;
    private readonly List<TsExprBase> _children = [];

    public RouteTreeBuilder Root(TsExprBase root)
    {
        _rootRoute = root;
        return this;
    }

    public RouteTreeBuilder AddChild(TsExprBase route)
    {
        _children.Add(route);
        return this;
    }

    public TsExprBase Build()
    {
        if (_rootRoute == null)
            throw new InvalidOperationException("Root route is required");

        if (_children.Count == 0)
            return _rootRoute;

        // rootRoute.addChildren([child1, child2, ...])
        var childrenArray = Ts.Array([.. _children.Cast<TsExprBase>()]);
        return _rootRoute.Dot("addChildren").Invoke(childrenArray);
    }
}

/// <summary>
/// Extension methods for router hooks
/// </summary>
public static class RouterExtensions
{
    /// <summary>useParams hook</summary>
    public static TsDynamicExpr UseParams() =>
        Ts.Var("useParams").Invoke();

    /// <summary>useSearch hook</summary>
    public static TsDynamicExpr UseSearch() =>
        Ts.Var("useSearch").Invoke();

    /// <summary>useNavigate hook</summary>
    public static TsDynamicExpr UseNavigate() =>
        Ts.Var("useNavigate").Invoke();

    /// <summary>useRouter hook</summary>
    public static TsDynamicExpr UseRouter() =>
        Ts.Var("useRouter").Invoke();

    /// <summary>useMatch hook</summary>
    public static TsDynamicExpr UseMatch(TsExprBase options) =>
        Ts.Var("useMatch").Invoke(options);

    /// <summary>useLoaderData hook</summary>
    public static TsDynamicExpr UseLoaderData() =>
        Ts.Var("useLoaderData").Invoke();

    /// <summary>Link component builder</summary>
    public static TsExprBase Link(string to, TsExprBase? children = null, TsExprBase? @params = null, TsExprBase? search = null)
    {
        var props = Ts.Object<object>().With("to", Ts.String(to));
        if (@params != null) props.With("params", @params);
        if (search != null) props.With("search", search);
        if (children != null) props.With("children", children);

        return Ts.Var("Link").Invoke(props.Build());
    }
}
