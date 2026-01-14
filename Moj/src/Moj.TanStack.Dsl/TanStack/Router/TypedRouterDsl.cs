using System.Linq.Expressions;
using Moj.TanStack.Ast.Core;
using Moj.TanStack.Ast.CodeGen;
using Moj.TanStack.Dsl.Core.TypeInference;

namespace Moj.TanStack.Dsl.TanStack.Router;

/// <summary>
/// Type-safe TanStack Router DSL that infers everything from C# types.
/// No manual TypeScript construction needed!
/// </summary>
public static class TanStackTypedRouter
{
    /// <summary>
    /// Create a router definition.
    /// </summary>
    public static TypedRouterBuilder Router() => new();

    /// <summary>
    /// Create a route definition with typed parameters.
    /// </summary>
    public static TypedRouteBuilder<TParams> Route<TParams>() where TParams : class, new()
        => new();

    /// <summary>
    /// Create a route definition with no params.
    /// </summary>
    public static TypedRouteBuilder Route() => new();
}

/// <summary>
/// Builder for a route with no typed params.
/// </summary>
public sealed class TypedRouteBuilder
{
    private string _path = "/";
    private string _name = "route";
    private string? _component;
    private string? _parentRoute;
    private bool _exported = true;

    public TypedRouteBuilder Path(string path)
    {
        _path = path;
        return this;
    }

    public TypedRouteBuilder Named(string name)
    {
        _name = name;
        return this;
    }

    public TypedRouteBuilder Component(string componentName)
    {
        _component = componentName;
        return this;
    }

    public TypedRouteBuilder Parent(string parentRouteName)
    {
        _parentRoute = parentRouteName;
        return this;
    }

    public TypedRouteBuilder Export(bool export = true)
    {
        _exported = export;
        return this;
    }

    public RouteDefinition Build()
    {
        return new RouteDefinition(_name, _path, _component, _parentRoute, null, _exported);
    }
}

/// <summary>
/// Builder for a route with typed params.
/// </summary>
public sealed class TypedRouteBuilder<TParams> where TParams : class, new()
{
    private string _path = "/";
    private string _name = "route";
    private string? _component;
    private string? _parentRoute;
    private bool _exported = true;

    public TypedRouteBuilder<TParams> Path(string path)
    {
        _path = path;
        return this;
    }

    public TypedRouteBuilder<TParams> Named(string name)
    {
        _name = name;
        return this;
    }

    public TypedRouteBuilder<TParams> Component(string componentName)
    {
        _component = componentName;
        return this;
    }

    public TypedRouteBuilder<TParams> Parent(string parentRouteName)
    {
        _parentRoute = parentRouteName;
        return this;
    }

    public TypedRouteBuilder<TParams> Export(bool export = true)
    {
        _exported = export;
        return this;
    }

    public RouteDefinition Build()
    {
        return new RouteDefinition(_name, _path, _component, _parentRoute, typeof(TParams), _exported);
    }
}

/// <summary>
/// Builder for the complete router.
/// </summary>
public sealed class TypedRouterBuilder
{
    private readonly List<RouteDefinition> _routes = [];
    private string _rootName = "rootRoute";
    private string _routerName = "router";
    private string? _rootComponent;
    private string _defaultPreload = "intent";
    private int _defaultPreloadDelay = 100;
    private bool _exported = true;

    public TypedRouterBuilder RootRoute(string name)
    {
        _rootName = name;
        return this;
    }

    public TypedRouterBuilder RootComponent(string componentName)
    {
        _rootComponent = componentName;
        return this;
    }

    public TypedRouterBuilder RouterName(string name)
    {
        _routerName = name;
        return this;
    }

    public TypedRouterBuilder AddRoute(RouteDefinition route)
    {
        _routes.Add(route);
        return this;
    }

    public TypedRouterBuilder DefaultPreload(string preload)
    {
        _defaultPreload = preload;
        return this;
    }

    public TypedRouterBuilder DefaultPreloadDelay(int ms)
    {
        _defaultPreloadDelay = ms;
        return this;
    }

    public TypedRouterBuilder Export(bool export = true)
    {
        _exported = export;
        return this;
    }

    public RouterDefinition Build()
    {
        return new RouterDefinition(
            _routerName,
            _rootName,
            _rootComponent,
            _routes,
            _defaultPreload,
            _defaultPreloadDelay,
            _exported);
    }
}

// ========== Definition Records ==========

/// <summary>
/// Represents a route definition.
/// </summary>
public sealed class RouteDefinition
{
    public string Name { get; }
    public string Path { get; }
    public string? Component { get; }
    public string? ParentRoute { get; }
    public Type? ParamsType { get; }
    public bool IsExported { get; }

    internal RouteDefinition(
        string name,
        string path,
        string? component,
        string? parentRoute,
        Type? paramsType,
        bool exported)
    {
        Name = name;
        Path = path;
        Component = component;
        ParentRoute = parentRoute;
        ParamsType = paramsType;
        IsExported = exported;
    }
}

/// <summary>
/// Represents a complete router definition ready for TypeScript generation.
/// </summary>
public sealed class RouterDefinition
{
    public string RouterName { get; }
    public string RootRouteName { get; }
    public string? RootComponent { get; }
    public IReadOnlyList<RouteDefinition> Routes { get; }
    public string DefaultPreload { get; }
    public int DefaultPreloadDelay { get; }
    public bool IsExported { get; }

    internal RouterDefinition(
        string routerName,
        string rootRouteName,
        string? rootComponent,
        List<RouteDefinition> routes,
        string defaultPreload,
        int defaultPreloadDelay,
        bool exported)
    {
        RouterName = routerName;
        RootRouteName = rootRouteName;
        RootComponent = rootComponent;
        Routes = routes;
        DefaultPreload = defaultPreload;
        DefaultPreloadDelay = defaultPreloadDelay;
        IsExported = exported;
    }

    public string ToTypeScript()
    {
        var emitter = new TypeScriptEmitter();
        var program = new TsProgram(ToAst().ToList());
        return emitter.Emit(program);
    }

    public IEnumerable<TsNode> ToAst()
    {
        var nodes = new List<TsNode>();

        // Import from @tanstack/react-router
        nodes.Add(new TsImportDeclaration(
            "@tanstack/react-router",
            new TsImportClause(NamedImports: [
                new TsImportSpecifier("createRouter"),
                new TsImportSpecifier("createRoute"),
                new TsImportSpecifier("createRootRoute"),
                new TsImportSpecifier("Outlet")
            ])));

        // Generate param types if needed
        var generatedTypes = new HashSet<string>();
        foreach (var route in Routes.Where(r => r.ParamsType != null))
        {
            if (!generatedTypes.Contains(route.ParamsType!.Name))
            {
                generatedTypes.Add(route.ParamsType.Name);
                nodes.Add(TypeToTsConverter.ToInterface(route.ParamsType, export: IsExported));
            }
        }

        // Create root route
        var rootRouteProps = new List<TsObjectElement>();
        if (RootComponent != null)
        {
            rootRouteProps.Add(new TsPropertyAssignment(
                new TsIdentifier("component"),
                new TsIdentifier(RootComponent)));
        }

        var rootRouteCall = new TsCallExpression(
            new TsIdentifier("createRootRoute"),
            rootRouteProps.Count > 0 ? [new TsObjectLiteral(rootRouteProps)] : []);

        nodes.Add(new TsVariableDeclaration(
            TsVariableKind.Const,
            [new TsVariableDeclarator(
                new TsIdentifierBinding(RootRouteName),
                Initializer: rootRouteCall)],
            IsExported));

        // Create child routes
        foreach (var route in Routes)
        {
            var routeProps = new List<TsObjectElement>();

            // getParentRoute
            var parentName = route.ParentRoute ?? RootRouteName;
            routeProps.Add(new TsPropertyAssignment(
                new TsIdentifier("getParentRoute"),
                new TsArrowFunction([], new TsIdentifier(parentName))));

            // path
            routeProps.Add(new TsPropertyAssignment(
                new TsIdentifier("path"),
                new TsLiteral(route.Path, TsLiteralKind.String)));

            // component
            if (route.Component != null)
            {
                routeProps.Add(new TsPropertyAssignment(
                    new TsIdentifier("component"),
                    new TsIdentifier(route.Component)));
            }

            var routeCall = new TsCallExpression(
                new TsIdentifier("createRoute"),
                [new TsObjectLiteral(routeProps)]);

            nodes.Add(new TsVariableDeclaration(
                TsVariableKind.Const,
                [new TsVariableDeclarator(
                    new TsIdentifierBinding(route.Name),
                    Initializer: routeCall)],
                IsExported));
        }

        // Create route tree
        var childRoutes = Routes.Where(r => r.ParentRoute == null || r.ParentRoute == RootRouteName).ToList();
        if (childRoutes.Count > 0)
        {
            var childrenArray = new TsArrayLiteral(
                childRoutes.Select(r => (TsExpression?)new TsIdentifier(r.Name)).ToList());

            var routeTree = new TsCallExpression(
                new TsMemberAccess(new TsIdentifier(RootRouteName), "addChildren"),
                [childrenArray]);

            nodes.Add(new TsVariableDeclaration(
                TsVariableKind.Const,
                [new TsVariableDeclarator(
                    new TsIdentifierBinding("routeTree"),
                    Initializer: routeTree)],
                IsExported));
        }

        // Create router
        var routerProps = new List<TsObjectElement>
        {
            new TsPropertyAssignment(
                new TsIdentifier("routeTree"),
                new TsIdentifier("routeTree")),
            new TsPropertyAssignment(
                new TsIdentifier("defaultPreload"),
                new TsLiteral(DefaultPreload, TsLiteralKind.String)),
            new TsPropertyAssignment(
                new TsIdentifier("defaultPreloadDelay"),
                new TsLiteral(DefaultPreloadDelay, TsLiteralKind.Number))
        };

        var routerCall = new TsCallExpression(
            new TsIdentifier("createRouter"),
            [new TsObjectLiteral(routerProps)]);

        nodes.Add(new TsVariableDeclaration(
            TsVariableKind.Const,
            [new TsVariableDeclarator(
                new TsIdentifierBinding(RouterName),
                Initializer: routerCall)],
            IsExported));

        // Type declaration for router: type RouterType = typeof router
        nodes.Add(new TsTypeAliasDeclaration(
            "RouterType",
            new TsTypeofType(new TsIdentifier(RouterName)),
            IsExported: IsExported));

        return nodes;
    }
}
