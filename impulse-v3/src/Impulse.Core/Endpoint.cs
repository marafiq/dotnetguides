namespace Impulse;

/// <summary>
/// HTTP methods supported by endpoints.
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
/// Marks a class as an Impulse endpoint.
/// Defines the route pattern and HTTP method.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class EndpointAttribute : Attribute
{
    /// <summary>
    /// The route pattern (e.g., "/residents/{id}").
    /// </summary>
    public string Route { get; }

    /// <summary>
    /// The HTTP method. Defaults to GET.
    /// </summary>
    public HttpMethod Method { get; }

    public EndpointAttribute(string route, HttpMethod method = HttpMethod.Get)
    {
        Route = route ?? throw new ArgumentNullException(nameof(route));
        Method = method;
    }
}

/// <summary>
/// Base class for endpoints with a request body.
/// Typically used for POST, PUT, PATCH operations.
/// </summary>
/// <typeparam name="TRequest">The request type (from body + route params)</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public abstract class Endpoint<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    /// <summary>
    /// Handle the request and return a result.
    /// </summary>
    /// <param name="request">The validated request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A result that will be serialized as JSON or rendered as HTML</returns>
    public abstract Task<IResult<TResponse>> HandleAsync(
        TRequest request,
        CancellationToken ct = default);
}

/// <summary>
/// Base class for endpoints without a request body.
/// Typically used for GET operations.
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public abstract class Endpoint<TResponse>
    where TResponse : class
{
    /// <summary>
    /// Handle the request and return a result.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A result that will be serialized as JSON or rendered as HTML</returns>
    public abstract Task<IResult<TResponse>> HandleAsync(CancellationToken ct = default);
}
