namespace Impulse.Core;

/// <summary>
/// Marker interface for Impulse endpoint results.
/// Results can be rendered as HTML (full page or partial) or JSON.
/// </summary>
public interface IImpulseResult
{
    /// <summary>
    /// HTTP status code for this result.
    /// </summary>
    int StatusCode { get; }
}

/// <summary>
/// Result containing data that can be serialized to JSON or rendered in a view.
/// </summary>
public interface IImpulseResult<out TData> : IImpulseResult
{
    TData? Data { get; }
}
