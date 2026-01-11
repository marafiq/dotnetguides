using System.Collections.Concurrent;

namespace Impulse.Core;

/// <summary>
/// Marks a property or endpoint as deferred (lazy-loaded).
/// Deferred data is not loaded with the initial response but fetched on-demand.
/// </summary>
/// <example>
/// [Deferred("medications", "/api/residents/{id}/medications")]
/// public class GetResidentEndpoint : ImpulseEndpoint&lt;GetResidentRequest, GetResidentResponse&gt;
/// {
///     // ...
/// }
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public class DeferredAttribute : Attribute
{
    /// <summary>
    /// The key used to identify this deferred data section.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// The API path to fetch the deferred data.
    /// Can include route parameters like {id}.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// The response type for the deferred data.
    /// </summary>
    public Type? ResponseType { get; init; }

    public DeferredAttribute(string key, string path)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        Key = key;
        Path = path;
    }
}

/// <summary>
/// Represents deferred data configuration for an endpoint.
/// </summary>
public record DeferredConfig(string Key, string Path, Type ResponseType);

/// <summary>
/// Extension methods for configuring deferred loading.
/// Thread-safe using ConcurrentDictionary.
/// </summary>
public static class DeferredExtensions
{
    private static readonly ConcurrentDictionary<Type, List<DeferredConfig>> _deferredConfigs = new();
    private static readonly object _lock = new();

    /// <summary>
    /// Add a deferred data section to an endpoint.
    /// Thread-safe registration.
    /// </summary>
    /// <typeparam name="TDeferred">The deferred response type.</typeparam>
    /// <param name="endpoint">The endpoint type.</param>
    /// <param name="key">The key to identify this deferred section.</param>
    /// <param name="path">The API path to fetch deferred data.</param>
    public static void RegisterDeferred<TDeferred>(Type endpoint, string key, string path)
        where TDeferred : class
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var config = new DeferredConfig(key, path, typeof(TDeferred));

        _deferredConfigs.AddOrUpdate(
            endpoint,
            _ => [config],
            (_, existing) =>
            {
                lock (_lock)
                {
                    var updated = new List<DeferredConfig>(existing) { config };
                    return updated;
                }
            });
    }

    /// <summary>
    /// Get all deferred configurations for an endpoint type.
    /// </summary>
    public static IReadOnlyList<DeferredConfig> GetDeferredConfigs(Type endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        // First check attributes
        var attributeConfigs = endpoint
            .GetCustomAttributes(typeof(DeferredAttribute), true)
            .Cast<DeferredAttribute>()
            .Where(a => a.ResponseType != null)
            .Select(a => new DeferredConfig(a.Key, a.Path, a.ResponseType!))
            .ToList();

        // Then check runtime registrations
        if (_deferredConfigs.TryGetValue(endpoint, out var runtimeConfigs))
        {
            lock (_lock)
            {
                attributeConfigs.AddRange(runtimeConfigs);
            }
        }

        return attributeConfigs;
    }

    /// <summary>
    /// Clear all runtime deferred configurations (for testing).
    /// </summary>
    public static void ClearDeferredConfigs()
    {
        _deferredConfigs.Clear();
    }
}

/// <summary>
/// Builder for fluently configuring deferred data on endpoints.
/// </summary>
public class DeferredBuilder<TEndpoint>
    where TEndpoint : class
{
    private readonly Type _endpointType = typeof(TEndpoint);

    /// <summary>
    /// Add a deferred data section.
    /// </summary>
    public DeferredBuilder<TEndpoint> WithDeferred<TDeferred>(string key, string path)
        where TDeferred : class
    {
        DeferredExtensions.RegisterDeferred<TDeferred>(_endpointType, key, path);
        return this;
    }
}

/// <summary>
/// Response wrapper that includes deferred data paths.
/// </summary>
public class ImpulseResponseWithDeferred<TData>
    where TData : class
{
    /// <summary>
    /// The main response data.
    /// </summary>
    public TData Data { get; init; } = default!;

    /// <summary>
    /// Paths to deferred data sections.
    /// </summary>
    public Dictionary<string, string> Deferred { get; init; } = new();
}
