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
    public static IImpulseResult<TData> Ok<TData>(TData data) => new OkResult<TData>(data);

    /// <summary>
    /// Creates a 404 Not Found result.
    /// </summary>
    public static IImpulseResult NotFound(string? message = null) => new NotFoundResult(message);

    /// <summary>
    /// Creates a 400 Bad Request result with validation errors.
    /// </summary>
    public static IImpulseResult ValidationProblem(IDictionary<string, string[]> errors)
        => new ValidationProblemResult(errors);

    /// <summary>
    /// Creates a 201 Created result with location and data.
    /// </summary>
    public static IImpulseResult<TData> Created<TData>(string location, TData data)
        => new CreatedResult<TData>(location, data);

    /// <summary>
    /// Creates a redirect result.
    /// </summary>
    public static IImpulseResult Redirect(string url) => new RedirectResult(url);
}

// Internal result implementations
internal sealed record OkResult<TData>(TData? Data) : IImpulseResult<TData>
{
    public int StatusCode => 200;
}

internal sealed record NotFoundResult(string? Message) : IImpulseResult
{
    public int StatusCode => 404;
}

internal sealed record ValidationProblemResult(IDictionary<string, string[]> Errors) : IImpulseResult
{
    public int StatusCode => 400;
}

internal sealed record CreatedResult<TData>(string Location, TData? Data) : IImpulseResult<TData>
{
    public int StatusCode => 201;
}

internal sealed record RedirectResult(string Url) : IImpulseResult
{
    public int StatusCode => 302;
}
