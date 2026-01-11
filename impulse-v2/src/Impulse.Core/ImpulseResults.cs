namespace Impulse.Core;

/// <summary>
/// Static factory for creating Impulse results.
/// Inspired by ASP.NET Core's TypedResults.
/// </summary>
public static class ImpulseResults
{
    /// <summary>
    /// Creates a 200 OK result with the specified data.
    /// </summary>
    public static ImpulseOkResult Ok<TData>(TData data) => new(data);

    /// <summary>
    /// Creates a 204 No Content result.
    /// </summary>
    public static ImpulseNoContentResult NoContent() => new();

    /// <summary>
    /// Creates a 400 Bad Request result.
    /// </summary>
    public static ImpulseBadRequestResult BadRequest(string? message = null) => new(message);

    /// <summary>
    /// Creates a 401 Unauthorized result.
    /// </summary>
    public static ImpulseUnauthorizedResult Unauthorized(string? message = null) => new(message);

    /// <summary>
    /// Creates a 403 Forbidden result.
    /// </summary>
    public static ImpulseForbiddenResult Forbidden(string? message = null) => new(message);

    /// <summary>
    /// Creates a 404 Not Found result.
    /// </summary>
    public static ImpulseNotFoundResult NotFound(string? message = null) => new(message);

    /// <summary>
    /// Creates a 422 Unprocessable Entity result with validation errors.
    /// </summary>
    public static ImpulseValidationProblemResult ValidationProblem(IDictionary<string, string[]> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return new ImpulseValidationProblemResult(errors);
    }

    /// <summary>
    /// Creates a 201 Created result with location and data.
    /// </summary>
    public static ImpulseCreatedResult Created<TData>(string location, TData data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);
        return new ImpulseCreatedResult(location, data);
    }

    /// <summary>
    /// Creates a 302 Found redirect result.
    /// </summary>
    public static ImpulseRedirectResult Redirect(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        return new ImpulseRedirectResult(url, permanent: false);
    }

    /// <summary>
    /// Creates a 301 Moved Permanently redirect result.
    /// </summary>
    public static ImpulseRedirectResult RedirectPermanent(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        return new ImpulseRedirectResult(url, permanent: true);
    }

    /// <summary>
    /// Creates a 500 Internal Server Error result.
    /// </summary>
    public static ImpulseInternalErrorResult InternalError(string? message = null) => new(message);

    /// <summary>
    /// Creates a 503 Service Unavailable result.
    /// </summary>
    public static ImpulseServiceUnavailableResult ServiceUnavailable(string? message = null, TimeSpan? retryAfter = null)
        => new(message, retryAfter);
}

// ============================================
// Result Types (RFC 7807 Problem Details)
// ============================================

public sealed record ImpulseOkResult(object? Value) : IImpulseResult
{
    public int StatusCode => 200;
}

public sealed record ImpulseNoContentResult : IImpulseResult
{
    public int StatusCode => 204;
}

public sealed record ImpulseBadRequestResult(string? Message) : IImpulseResult
{
    public int StatusCode => 400;
}

public sealed record ImpulseUnauthorizedResult(string? Message) : IImpulseResult
{
    public int StatusCode => 401;
}

public sealed record ImpulseForbiddenResult(string? Message) : IImpulseResult
{
    public int StatusCode => 403;
}

public sealed record ImpulseNotFoundResult(string? Message) : IImpulseResult
{
    public int StatusCode => 404;
}

public sealed record ImpulseValidationProblemResult(IDictionary<string, string[]> Errors) : IImpulseResult
{
    public int StatusCode => 422;
}

public sealed record ImpulseCreatedResult(string Location, object? Value) : IImpulseResult
{
    public int StatusCode => 201;
}

public sealed record ImpulseRedirectResult(string Url, bool Permanent = false) : IImpulseResult
{
    public int StatusCode => Permanent ? 301 : 302;
}

public sealed record ImpulseInternalErrorResult(string? Message) : IImpulseResult
{
    public int StatusCode => 500;
}

public sealed record ImpulseServiceUnavailableResult(string? Message, TimeSpan? RetryAfter) : IImpulseResult
{
    public int StatusCode => 503;
}
