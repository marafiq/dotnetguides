using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Impulse.Core;

/// <summary>
/// Extension methods for registering Impulse services.
/// </summary>
public static class ImpulseServiceExtensions
{
    /// <summary>
    /// Adds Impulse services to the service collection.
    /// </summary>
    public static IServiceCollection AddImpulse(
        this IServiceCollection services,
        Action<ImpulseConfiguration>? configure = null)
    {
        var config = new ImpulseConfiguration();
        configure?.Invoke(config);

        services.AddSingleton(config);
        services.AddSingleton<ImpulseShellRenderer>();
        services.AddSingleton<ImpulseTypeRegistry>();

        return services;
    }

    /// <summary>
    /// Registers an Impulse context provider.
    /// </summary>
    /// <typeparam name="TContext">The context type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="contextFactory">Factory function to create the context.</param>
    public static IServiceCollection AddImpulseContext<TContext>(
        this IServiceCollection services,
        Func<HttpContext, Task<TContext>> contextFactory)
    {
        var provider = new ImpulseContextProvider<TContext>(contextFactory);
        services.AddSingleton<IImpulseContextProvider>(provider);
        services.AddSingleton<IImpulseContextProvider<TContext>>(provider);
        return services;
    }

    /// <summary>
    /// Configures the app to use Impulse context.
    /// </summary>
    public static IApplicationBuilder UseImpulseContext<TContext>(
        this IApplicationBuilder app,
        Func<HttpContext, Task<TContext>> contextFactory)
    {
        // Register context provider if not already registered
        var existingProvider = app.ApplicationServices.GetService<IImpulseContextProvider>();
        if (existingProvider is null)
        {
            throw new InvalidOperationException(
                "Impulse context provider not registered. Call AddImpulseContext<TContext>() in ConfigureServices.");
        }

        return app;
    }
}
