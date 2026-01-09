using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Impulse.Core;

/// <summary>
/// Base class for Impulse modules (Nancy-style endpoint registration).
/// Group related endpoints together with a common base path.
/// </summary>
/// <example>
/// public class ResidentsModule : ImpulseModule
/// {
///     public override string BasePath => "/residents";
///
///     public override void Configure(IEndpointRouteBuilder app)
///     {
///         app.MapGet("/", List).Impulse("./Residents/List");
///         app.MapGet("/{id:int}", Detail).Impulse("./Residents/Detail");
///         app.MapPost("/", Create).ImpulseMutation();
///     }
/// }
/// </example>
public abstract class ImpulseModule
{
    /// <summary>
    /// The base path for all endpoints in this module.
    /// </summary>
    public abstract string BasePath { get; }

    /// <summary>
    /// Configure endpoints for this module.
    /// </summary>
    public abstract void Configure(IEndpointRouteBuilder app);

    /// <summary>
    /// Optional: Processors to apply to all endpoints in this module.
    /// </summary>
    public virtual IEnumerable<IPreProcessor> PreProcessors => Enumerable.Empty<IPreProcessor>();

    /// <summary>
    /// Optional: Post-processors to apply to all endpoints in this module.
    /// </summary>
    public virtual IEnumerable<IPostProcessor> PostProcessors => Enumerable.Empty<IPostProcessor>();
}

/// <summary>
/// Extension methods for registering Impulse modules.
/// </summary>
public static class ImpulseModuleExtensions
{
    /// <summary>
    /// Register all ImpulseModules from the current assembly.
    /// </summary>
    public static IEndpointRouteBuilder MapImpulseModules(this IEndpointRouteBuilder app)
    {
        return app.MapImpulseModules(typeof(ImpulseModuleExtensions).Assembly);
    }

    /// <summary>
    /// Register all ImpulseModules from the specified assembly.
    /// </summary>
    public static IEndpointRouteBuilder MapImpulseModules(
        this IEndpointRouteBuilder app,
        System.Reflection.Assembly assembly)
    {
        var moduleTypes = assembly.GetExportedTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(ImpulseModule)));

        foreach (var moduleType in moduleTypes)
        {
            var module = (ImpulseModule)Activator.CreateInstance(moduleType)!;

            var group = app.MapGroup(module.BasePath);
            module.Configure(group);
        }

        return app;
    }

    /// <summary>
    /// Register a specific ImpulseModule.
    /// </summary>
    public static IEndpointRouteBuilder MapImpulseModule<TModule>(this IEndpointRouteBuilder app)
        where TModule : ImpulseModule, new()
    {
        var module = new TModule();
        var group = app.MapGroup(module.BasePath);
        module.Configure(group);
        return app;
    }
}

/// <summary>
/// Pre-processor interface for cross-cutting concerns before endpoint execution.
/// </summary>
public interface IPreProcessor
{
    /// <summary>
    /// Process the request before the endpoint handler.
    /// Return false to short-circuit and prevent execution.
    /// </summary>
    Task<bool> ProcessAsync(HttpContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic pre-processor with access to the request type.
/// </summary>
public interface IPreProcessor<TRequest> : IPreProcessor
    where TRequest : class
{
    /// <summary>
    /// Process the request before the endpoint handler.
    /// </summary>
    Task<bool> ProcessAsync(TRequest request, HttpContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Post-processor interface for cross-cutting concerns after endpoint execution.
/// </summary>
public interface IPostProcessor
{
    /// <summary>
    /// Process after the endpoint handler completes.
    /// </summary>
    Task ProcessAsync(HttpContext context, object? response, CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic post-processor with access to request and response types.
/// </summary>
public interface IPostProcessor<TRequest, TResponse> : IPostProcessor
    where TRequest : class
    where TResponse : class
{
    /// <summary>
    /// Process after the endpoint handler completes.
    /// </summary>
    Task ProcessAsync(TRequest request, TResponse response, HttpContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Common pre-processor for logging requests.
/// </summary>
public class LoggingPreProcessor : IPreProcessor
{
    private readonly Action<HttpContext> _logAction;

    public LoggingPreProcessor(Action<HttpContext>? logAction = null)
    {
        _logAction = logAction ?? DefaultLog;
    }

    public Task<bool> ProcessAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        _logAction(context);
        return Task.FromResult(true);
    }

    private static void DefaultLog(HttpContext context)
    {
        Console.WriteLine($"[Impulse] {context.Request.Method} {context.Request.Path}");
    }
}

/// <summary>
/// Common post-processor for setting cache headers.
/// </summary>
public class CacheHeaderPostProcessor : IPostProcessor
{
    private readonly int _maxAgeSeconds;

    public CacheHeaderPostProcessor(int maxAgeSeconds = 60)
    {
        _maxAgeSeconds = maxAgeSeconds;
    }

    public Task ProcessAsync(HttpContext context, object? response, CancellationToken cancellationToken = default)
    {
        if (context.Request.Method == "GET" && response != null)
        {
            context.Response.Headers.CacheControl = $"max-age={_maxAgeSeconds}";
        }
        return Task.CompletedTask;
    }
}
