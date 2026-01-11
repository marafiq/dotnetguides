namespace Impulse;

/// <summary>
/// Base class for organizing endpoints into domain modules.
/// </summary>
public abstract class ImpulseModule
{
    /// <summary>
    /// The base path for all endpoints in this module.
    /// </summary>
    public abstract string BasePath { get; }

    /// <summary>
    /// Configure the module's endpoints on the route builder.
    /// </summary>
    public abstract void Configure(IEndpointRouteBuilder app);

    /// <summary>
    /// Pre-processors that run before endpoint handlers.
    /// </summary>
    public virtual IEnumerable<IPreProcessor> PreProcessors => [];

    /// <summary>
    /// Post-processors that run after endpoint handlers.
    /// </summary>
    public virtual IEnumerable<IPostProcessor> PostProcessors => [];
}

/// <summary>
/// Pre-processor that runs before endpoint execution.
/// </summary>
public interface IPreProcessor
{
    /// <summary>
    /// Process the request. Return false to short-circuit the pipeline.
    /// </summary>
    Task<bool> ProcessAsync(HttpContext context, CancellationToken ct = default);
}

/// <summary>
/// Post-processor that runs after endpoint execution.
/// </summary>
public interface IPostProcessor
{
    /// <summary>
    /// Process the response after endpoint execution.
    /// </summary>
    Task ProcessAsync(HttpContext context, object? response, CancellationToken ct = default);
}

/// <summary>
/// Built-in logging pre-processor.
/// </summary>
public class LoggingPreProcessor : IPreProcessor
{
    private readonly ILogger<LoggingPreProcessor> _logger;

    public LoggingPreProcessor(ILogger<LoggingPreProcessor> logger)
    {
        _logger = logger;
    }

    public Task<bool> ProcessAsync(HttpContext context, CancellationToken ct = default)
    {
        _logger.LogInformation("Impulse: {Method} {Path}", context.Request.Method, context.Request.Path);
        return Task.FromResult(true);
    }
}

/// <summary>
/// Built-in cache header post-processor.
/// </summary>
public class CacheHeaderPostProcessor : IPostProcessor
{
    private readonly int _maxAgeSeconds;

    public CacheHeaderPostProcessor(int maxAgeSeconds = 0)
    {
        _maxAgeSeconds = maxAgeSeconds;
    }

    public Task ProcessAsync(HttpContext context, object? response, CancellationToken ct = default)
    {
        if (_maxAgeSeconds > 0)
        {
            context.Response.Headers.CacheControl = $"public, max-age={_maxAgeSeconds}";
        }
        else
        {
            context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        }
        return Task.CompletedTask;
    }
}
