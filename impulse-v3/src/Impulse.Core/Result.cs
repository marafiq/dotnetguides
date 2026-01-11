namespace Impulse;

/// <summary>
/// Marker interface for all Impulse results.
/// Results carry HTTP semantics (status code) and can be pattern matched.
/// </summary>
public interface IResult
{
    int StatusCode { get; }
}

/// <summary>
/// Generic result interface for results that carry data.
/// </summary>
public interface IResult<out T> : IResult
{
}

/// <summary>
/// Static factory for creating result instances.
/// Provides a clean, discoverable API for endpoint return values.
/// </summary>
public static class Result
{
    /// <summary>
    /// Creates a 200 OK result with the specified data.
    /// </summary>
    public static OkResult<T> Ok<T>(T data) => new(data);

    /// <summary>
    /// Creates a 404 Not Found result.
    /// </summary>
    public static NotFoundResult NotFound(string? message = null) => new(message);

    /// <summary>
    /// Creates a typed 404 Not Found result (for use in typed endpoints).
    /// </summary>
    public static IResult<T> NotFound<T>(string? message = null) => new NotFoundResult<T>(message);

    /// <summary>
    /// Creates a 422 Unprocessable Entity result with validation errors.
    /// </summary>
    public static ValidationProblemResult ValidationProblem(IDictionary<string, string[]> errors)
        => new(errors);

    /// <summary>
    /// Creates a fluent builder for validation problems.
    /// </summary>
    public static ValidationProblemBuilder ValidationProblem() => new();

    /// <summary>
    /// Creates a 201 Created result with location and data.
    /// </summary>
    public static CreatedResult<T> Created<T>(string location, T data) => new(location, data);

    /// <summary>
    /// Creates a 302 Redirect result.
    /// </summary>
    public static RedirectResult Redirect(string url) => new(url);
}

/// <summary>
/// 200 OK result carrying data of type T.
/// </summary>
public sealed record OkResult<T>(T Data) : IResult<T>
{
    public int StatusCode => 200;
}

/// <summary>
/// 404 Not Found result with optional message.
/// </summary>
public sealed record NotFoundResult(string? Message) : IResult
{
    public int StatusCode => 404;
}

/// <summary>
/// 422 Unprocessable Entity result with field-level validation errors.
/// Follows RFC 7807 ProblemDetails structure.
/// </summary>
public sealed record ValidationProblemResult : IResult
{
    public int StatusCode => 422;
    public IDictionary<string, string[]> Errors { get; }

    public ValidationProblemResult(IDictionary<string, string[]> errors)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    internal ValidationProblemResult(Dictionary<string, List<string>> builder)
    {
        Errors = builder.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray());
    }
}

/// <summary>
/// Fluent builder for validation problems.
/// Makes it easy to add errors incrementally.
/// </summary>
public sealed class ValidationProblemBuilder
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public ValidationProblemBuilder WithError(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var messages))
        {
            messages = new List<string>();
            _errors[field] = messages;
        }
        messages.Add(message);
        return this;
    }

    public static implicit operator ValidationProblemResult(ValidationProblemBuilder builder)
        => new(builder._errors);
}

/// <summary>
/// 201 Created result with location header and data.
/// </summary>
public sealed record CreatedResult<T>(string Location, T Data) : IResult<T>
{
    public int StatusCode => 201;
}

/// <summary>
/// 302 Redirect result with target URL.
/// </summary>
public sealed record RedirectResult(string Url) : IResult
{
    public int StatusCode => 302;
}

/// <summary>
/// Typed 404 Not Found result (for use in typed endpoints).
/// </summary>
public sealed record NotFoundResult<T>(string? Message) : IResult<T>
{
    public int StatusCode => 404;
}

/// <summary>
/// Helper for building validation errors incrementally.
/// </summary>
public class ValidationBuilder
{
    private readonly Dictionary<string, List<string>> _errors = new();

    /// <summary>
    /// Add an error for a field.
    /// </summary>
    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var messages))
        {
            messages = new List<string>();
            _errors[field] = messages;
        }
        messages.Add(message);
    }

    /// <summary>
    /// Check if there are any errors.
    /// </summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>
    /// Convert to a ValidationProblemResult for a typed endpoint.
    /// </summary>
    public IResult<T> ToResult<T>()
    {
        return new ValidationProblemResult<T>(_errors.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray()));
    }
}

/// <summary>
/// Typed validation problem result for use in typed endpoints.
/// </summary>
public sealed record ValidationProblemResult<T> : IResult<T>
{
    public int StatusCode => 422;
    public IDictionary<string, string[]> Errors { get; }

    public ValidationProblemResult(IDictionary<string, string[]> errors)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }
}
