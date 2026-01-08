namespace Impulse.Core;

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

    public ImpulseEndpointAttribute(string route)
    {
        Route = route ?? throw new ArgumentNullException(nameof(route));
    }
}
