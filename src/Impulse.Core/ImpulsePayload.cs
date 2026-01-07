using System.Text.Json.Serialization;

namespace Impulse.Core;

/// <summary>
/// The payload structure embedded in the shell HTML and returned during navigation.
/// </summary>
public sealed class ImpulsePayload<TContext, TProps>
{
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("props")]
    public required TProps Props { get; init; }

    [JsonPropertyName("context")]
    public required TContext Context { get; init; }

    [JsonPropertyName("deferred")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Deferred { get; init; }

    [JsonPropertyName("lazy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Lazy { get; init; }
}

/// <summary>
/// Non-generic payload for internal use.
/// </summary>
public sealed class ImpulsePayload
{
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("props")]
    public required object Props { get; init; }

    [JsonPropertyName("context")]
    public required object Context { get; init; }

    [JsonPropertyName("deferred")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Deferred { get; init; }

    [JsonPropertyName("lazy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Lazy { get; init; }
}

/// <summary>
/// Navigation response structure (subset of full payload).
/// </summary>
public sealed class ImpulseNavigationResponse
{
    [JsonPropertyName("props")]
    public required object Props { get; init; }

    [JsonPropertyName("context")]
    public required object Context { get; init; }
}

/// <summary>
/// Validation error response format.
/// </summary>
public sealed class ImpulseValidationErrors
{
    [JsonPropertyName("errors")]
    public required Dictionary<string, List<string>> Errors { get; init; }
}
