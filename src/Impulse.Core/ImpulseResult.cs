using Microsoft.AspNetCore.Http;

namespace Impulse.Core;

/// <summary>
/// Result builders for Impulse endpoints.
/// </summary>
public static class Impulse
{
    /// <summary>
    /// Returns 200 OK with the specified data.
    /// </summary>
    public static IResult Ok<T>(T data) => Results.Ok(data);

    /// <summary>
    /// Returns 201 Created with the specified data.
    /// </summary>
    public static IResult Created<T>(T data) => Results.Created((string?)null, data);

    /// <summary>
    /// Returns 201 Created with the specified data and location.
    /// </summary>
    public static IResult Created<T>(string location, T data) => Results.Created(location, data);

    /// <summary>
    /// Returns 204 No Content.
    /// </summary>
    public static IResult NoContent() => Results.NoContent();

    /// <summary>
    /// Returns 202 Accepted with an optional job ID for async operations.
    /// </summary>
    public static IResult Accepted(string? jobId = null) =>
        jobId is null
            ? Results.Accepted()
            : Results.Accepted(null, new { jobId });

    /// <summary>
    /// Returns 400 Bad Request with validation errors.
    /// </summary>
    public static IResult ValidationError(Dictionary<string, List<string>> errors) =>
        Results.BadRequest(new ImpulseValidationErrors { Errors = errors });

    /// <summary>
    /// Returns 400 Bad Request with a single field error.
    /// </summary>
    public static IResult ValidationError(string field, string message) =>
        Results.BadRequest(new ImpulseValidationErrors
        {
            Errors = new Dictionary<string, List<string>>
            {
                [field] = [message]
            }
        });

    /// <summary>
    /// Returns 404 Not Found.
    /// </summary>
    public static IResult NotFound() => Results.NotFound();

    /// <summary>
    /// Returns 404 Not Found with a message.
    /// </summary>
    public static IResult NotFound(string message) => Results.NotFound(new { message });

    /// <summary>
    /// Returns 403 Forbidden.
    /// </summary>
    public static IResult Forbidden() => Results.Forbid();

    /// <summary>
    /// Returns 401 Unauthorized.
    /// </summary>
    public static IResult Unauthorized() => Results.Unauthorized();
}
