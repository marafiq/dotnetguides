namespace Impulse;

/// <summary>
/// Attribute to mark a class as an Impulse endpoint.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ImpulseEndpointAttribute : Attribute
{
    public string Route { get; }
    public ImpulseMethod Method { get; }

    public ImpulseEndpointAttribute(string route, ImpulseMethod method = ImpulseMethod.Get)
    {
        Route = route;
        Method = method;
    }
}

/// <summary>
/// HTTP methods supported by Impulse endpoints.
/// </summary>
public enum ImpulseMethod
{
    Get,
    Post,
    Put,
    Patch,
    Delete
}

/// <summary>
/// Base class for GET endpoints that return data without request body.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
public abstract class ImpulseEndpoint<TResponse>
{
    public HttpContext Context { get; internal set; } = null!;

    public abstract Task<IImpulseResult> Handle(CancellationToken ct = default);
}

/// <summary>
/// Base class for endpoints that accept a request body and return data.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public abstract class ImpulseEndpoint<TRequest, TResponse>
{
    public HttpContext Context { get; internal set; } = null!;

    public abstract Task<IImpulseResult> Handle(TRequest request, CancellationToken ct = default);
}

/// <summary>
/// Base class for endpoints with route parameters and optional request body.
/// </summary>
/// <typeparam name="TRoute">The route parameters type.</typeparam>
/// <typeparam name="TRequest">The request body type (use Unit for no body).</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public abstract class ImpulseEndpoint<TRoute, TRequest, TResponse>
{
    public HttpContext Context { get; internal set; } = null!;

    public abstract Task<IImpulseResult> Handle(TRoute route, TRequest request, CancellationToken ct = default);
}

/// <summary>
/// Unit type for endpoints that don't need a request body.
/// </summary>
public readonly struct Unit
{
    public static readonly Unit Value = new();
}
