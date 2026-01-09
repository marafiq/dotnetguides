namespace Impulse.Core;

/// <summary>
/// Context for Impulse requests, used for TanStack Router context DI.
/// Determines whether request is from Impulse client (JSON) or browser (HTML).
/// </summary>
public class ImpulseContext
{
    /// <summary>
    /// Whether the current request is from an Impulse client (has X-Impulse header).
    /// When true, return JSON. When false, return full HTML page.
    /// </summary>
    public bool IsImpulseRequest { get; init; }

    /// <summary>
    /// The current authenticated user ID, if any.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Additional context data that can be passed to loaders.
    /// </summary>
    public IDictionary<string, object> Items { get; } = new Dictionary<string, object>();
}
