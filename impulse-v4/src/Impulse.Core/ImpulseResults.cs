namespace Impulse;

/// <summary>
/// Factory for creating Impulse result types following RFC 7807 Problem Details.
/// </summary>
public static class ImpulseResults
{
    public static ImpulseOkResult Ok<TData>(TData data) => new(data);

    public static ImpulseNotFoundResult NotFound(string? message = null) => new(message);

    public static ImpulseValidationProblemResult ValidationProblem(IDictionary<string, string[]> errors) => new(errors);

    public static ImpulseCreatedResult Created<TData>(string location, TData data) => new(location, data);

    public static ImpulseRedirectResult Redirect(string url) => new(url);

    public static ImpulseBadRequestResult BadRequest(string message) => new(message);
}

/// <summary>
/// Marker interface for all Impulse results.
/// </summary>
public interface IImpulseResult
{
    int StatusCode { get; }
    Task ExecuteAsync(HttpContext context);
}

/// <summary>
/// 200 OK with data payload.
/// </summary>
public class ImpulseOkResult : IImpulseResult
{
    public object? Data { get; }
    public int StatusCode => 200;

    public ImpulseOkResult(object? data)
    {
        Data = data;
    }

    public async Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(Data);
    }
}

/// <summary>
/// 404 Not Found with optional message.
/// </summary>
public class ImpulseNotFoundResult : IImpulseResult
{
    public string? Message { get; }
    public int StatusCode => 404;

    public ImpulseNotFoundResult(string? message = null)
    {
        Message = message;
    }

    public async Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title = "Not Found",
            status = StatusCode,
            detail = Message ?? "The requested resource was not found."
        });
    }
}

/// <summary>
/// 400 Validation Problem with field errors (RFC 7807).
/// </summary>
public class ImpulseValidationProblemResult : IImpulseResult
{
    public IDictionary<string, string[]> Errors { get; }
    public int StatusCode => 400;

    public ImpulseValidationProblemResult(IDictionary<string, string[]> errors)
    {
        Errors = errors;
    }

    public async Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title = "One or more validation errors occurred.",
            status = StatusCode,
            errors = Errors
        });
    }
}

/// <summary>
/// 201 Created with location header.
/// </summary>
public class ImpulseCreatedResult : IImpulseResult
{
    public string Location { get; }
    public object? Data { get; }
    public int StatusCode => 201;

    public ImpulseCreatedResult(string location, object? data)
    {
        Location = location;
        Data = data;
    }

    public async Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCode;
        context.Response.Headers.Location = Location;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(Data);
    }
}

/// <summary>
/// 302 Redirect.
/// </summary>
public class ImpulseRedirectResult : IImpulseResult
{
    public string Url { get; }
    public int StatusCode => 302;

    public ImpulseRedirectResult(string url)
    {
        Url = url;
    }

    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.Redirect(Url);
        return Task.CompletedTask;
    }
}

/// <summary>
/// 400 Bad Request with message.
/// </summary>
public class ImpulseBadRequestResult : IImpulseResult
{
    public string Message { get; }
    public int StatusCode => 400;

    public ImpulseBadRequestResult(string message)
    {
        Message = message;
    }

    public async Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title = "Bad Request",
            status = StatusCode,
            detail = Message
        });
    }
}
