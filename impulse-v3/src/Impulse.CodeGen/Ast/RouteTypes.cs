namespace Impulse.CodeGen.Ast;

/// <summary>
/// Represents an endpoint route for code generation.
/// </summary>
public record EndpointRoute(
    string Name,
    string Route,
    string Method,
    string? RequestType,
    string? ResponseType);

/// <summary>
/// Route file containing all endpoint routes.
/// </summary>
public record RouteFile(IReadOnlyList<EndpointRoute> Routes);

/// <summary>
/// Mutation info for generating React Query hooks.
/// </summary>
public record MutationInfo(
    string Name,
    string Route,
    string Method,
    string RequestType,
    string ResponseType,
    bool InvalidatesQueries = true);

/// <summary>
/// Mutations file for generating React Query hooks.
/// </summary>
public record MutationsFile(IReadOnlyList<MutationInfo> Mutations);
