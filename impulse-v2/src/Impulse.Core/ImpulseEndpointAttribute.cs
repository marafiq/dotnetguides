namespace Impulse.Core;

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
/// Marks a class as an Impulse endpoint that handles both HTML and JSON responses
/// based on the X-Impulse header.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ImpulseEndpointAttribute : Attribute
{
    /// <summary>
    /// The route pattern for this endpoint (e.g., "/residents/{id}").
    /// </summary>
    public string Route { get; }

    /// <summary>
    /// The HTTP method for this endpoint. Defaults to GET.
    /// </summary>
    public ImpulseMethod Method { get; }

    public ImpulseEndpointAttribute(string route, ImpulseMethod method = ImpulseMethod.Get)
    {
        Route = route ?? throw new ArgumentNullException(nameof(route));
        Method = method;
    }
}
