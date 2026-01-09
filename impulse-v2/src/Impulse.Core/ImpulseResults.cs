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
    public static ImpulseOkResult Ok<TData>(TData data) => new ImpulseOkResult(data);

    /// <summary>
    /// Creates a 404 Not Found result.
    /// </summary>
    public static ImpulseNotFoundResult NotFound(string? message = null) => new ImpulseNotFoundResult(message);

    /// <summary>
    /// Creates a 400 Bad Request result with validation errors.
    /// </summary>
    public static ImpulseValidationProblemResult ValidationProblem(IDictionary<string, string[]> errors)
        => new ImpulseValidationProblemResult(errors);

    /// <summary>
    /// Creates a 201 Created result with location and data.
    /// </summary>
    public static ImpulseCreatedResult Created<TData>(string location, TData data)
        => new ImpulseCreatedResult(location, data);

    /// <summary>
    /// Creates a redirect result.
    /// </summary>
    public static ImpulseRedirectResult Redirect(string url) => new ImpulseRedirectResult(url);
}

// Public result types for pattern matching in Program.cs
public sealed record ImpulseOkResult(object? Value) : IImpulseResult
{
    public int StatusCode => 200;
}

public sealed record ImpulseNotFoundResult(string? Message) : IImpulseResult
{
    public int StatusCode => 404;
}

public sealed record ImpulseValidationProblemResult(IDictionary<string, string[]> Errors) : IImpulseResult
{
    public int StatusCode => 422; // Changed to 422 for validation errors
}

public sealed record ImpulseCreatedResult(string Location, object? Value) : IImpulseResult
{
    public int StatusCode => 201;
}

public sealed record ImpulseRedirectResult(string Url) : IImpulseResult
{
    public int StatusCode => 302;
}
