namespace Impulse.SourceGen.Ast;

/// <summary>
/// Base class for route-related AST nodes.
/// Used to generate TanStack Router routes, loaders, and mutations.
/// </summary>
public abstract record RouteNode;

/// <summary>
/// Represents all routes in the application.
/// </summary>
public record RouteFile(IReadOnlyList<RouteDefinition> Routes) : RouteNode;

/// <summary>
/// Represents a single route definition with all its metadata.
/// </summary>
public record RouteDefinition(
    string Name,
    string Path,
    HttpMethod Method,
    string? ResponseType,
    string? RequestType,
    string? ComponentPath,
    IReadOnlyList<RouteParam> Parameters,
    IReadOnlyList<DeferredRoute> Deferred) : RouteNode
{
    public RouteDefinition(
        string name,
        string path,
        HttpMethod method,
        string? responseType = null,
        string? requestType = null,
        string? componentPath = null)
        : this(name, path, method, responseType, requestType, componentPath,
               ExtractParams(path), Array.Empty<DeferredRoute>()) { }

    public bool IsQuery => Method == HttpMethod.Get;
    public bool IsMutation => Method is HttpMethod.Post or HttpMethod.Put or HttpMethod.Patch or HttpMethod.Delete;
    public bool HasParameters => Parameters.Count > 0;

    private static IReadOnlyList<RouteParam> ExtractParams(string path)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(path, @"\{(\w+)(?::(\w+))?\}");
        return matches.Select(m => new RouteParam(
            m.Groups[1].Value,
            m.Groups[2].Success ? m.Groups[2].Value : null
        )).ToList();
    }
}

/// <summary>
/// HTTP methods supported by routes.
/// </summary>
public enum HttpMethod
{
    Get,
    Post,
    Put,
    Patch,
    Delete
}

/// <summary>
/// Represents a route parameter extracted from the path.
/// </summary>
public record RouteParam(string Name, string? Constraint);

/// <summary>
/// Represents a deferred/lazy-loaded data section.
/// </summary>
public record DeferredRoute(string Key, string Path, string ResponseType);

/// <summary>
/// Represents the route paths constant object.
/// </summary>
public record RoutePathsConst(IReadOnlyList<RoutePathEntry> Entries) : RouteNode;

/// <summary>
/// Single entry in the RoutePaths constant.
/// </summary>
public record RoutePathEntry(string Key, string Path);

/// <summary>
/// Represents a TanStack Router route tree.
/// </summary>
public record RouterTree(
    IReadOnlyList<TanStackRoute> Routes,
    IReadOnlyList<RouteDefinition> Definitions) : RouteNode;

/// <summary>
/// Represents a TanStack Router route declaration.
/// </summary>
public record TanStackRoute(
    string VariableName,
    string Path,
    string? ParentRoute,
    string? ResponseType,
    IReadOnlyList<RouteParam> Parameters,
    bool HasLoader) : RouteNode;

/// <summary>
/// Represents a type-safe path builder function.
/// </summary>
public record PathBuilder(
    string FunctionName,
    IReadOnlyList<RouteParam> Parameters,
    string PathTemplate) : RouteNode;

/// <summary>
/// Represents a loader function for fetching data.
/// </summary>
public record LoaderFunction(
    string Name,
    string Route,
    string ResponseType,
    IReadOnlyList<RouteParam> Parameters) : RouteNode;

/// <summary>
/// Represents a mutation hook for data modification.
/// </summary>
public record MutationHook(
    string Name,
    string Route,
    HttpMethod Method,
    string RequestType,
    string? ResponseType,
    string? SchemaName) : RouteNode;

/// <summary>
/// Builder for creating route definitions fluently.
/// </summary>
public static class RouteBuilder
{
    public static RouteDefinition Get(string name, string path, string responseType)
        => new(name, path, HttpMethod.Get, responseType);

    public static RouteDefinition Post(string name, string path, string requestType, string? responseType = null)
        => new(name, path, HttpMethod.Post, responseType, requestType);

    public static RouteDefinition Put(string name, string path, string requestType, string? responseType = null)
        => new(name, path, HttpMethod.Put, responseType, requestType);

    public static RouteDefinition Patch(string name, string path, string requestType, string? responseType = null)
        => new(name, path, HttpMethod.Patch, responseType, requestType);

    public static RouteDefinition Delete(string name, string path, string? responseType = null)
        => new(name, path, HttpMethod.Delete, responseType);

    public static RouteDefinition WithDeferred(this RouteDefinition route, string key, string path, string responseType)
        => route with { Deferred = route.Deferred.Append(new DeferredRoute(key, path, responseType)).ToList() };

    public static RouteDefinition WithComponent(this RouteDefinition route, string componentPath)
        => route with { ComponentPath = componentPath };
}
