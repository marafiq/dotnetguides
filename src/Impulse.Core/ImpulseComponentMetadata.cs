namespace Impulse.Core;

/// <summary>
/// Metadata for an Impulse component endpoint.
/// </summary>
public sealed class ImpulseComponentMetadata
{
    public required Type PropsType { get; init; }
    public required string ComponentPath { get; init; }
    public Dictionary<string, DeferredComponentInfo> Deferred { get; } = new();
    public Dictionary<string, LazyComponentInfo> Lazy { get; } = new();
}

/// <summary>
/// Information about a deferred component (auto-loads after hydration).
/// </summary>
public sealed class DeferredComponentInfo
{
    public required Type PropsType { get; init; }
    public required string UrlTemplate { get; init; }
}

/// <summary>
/// Information about a lazy component (loads on demand).
/// </summary>
public sealed class LazyComponentInfo
{
    public required Type PropsType { get; init; }
    public required string UrlTemplate { get; init; }
}

/// <summary>
/// Metadata for an Impulse mutation endpoint.
/// </summary>
public sealed class ImpulseMutationMetadata
{
    public required Type RequestType { get; init; }
    public required Type ResponseType { get; init; }
    public List<Type> InvalidatesTypes { get; } = new();
}
