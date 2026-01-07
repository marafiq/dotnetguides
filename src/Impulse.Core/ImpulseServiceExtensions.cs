using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        services.AddSingleton(sp =>
        {
            var config = new ImpulseConfiguration();
            configure?.Invoke(config);

            // Auto-detect development mode from environment
            var env = sp.GetService<IWebHostEnvironment>();
            if (env?.IsDevelopment() == true)
            {
                config.UseDevelopmentServer = true;
            }

            return config;
        });

        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<ImpulseConfiguration>();
            var env = sp.GetService<IWebHostEnvironment>();
            var manifest = new ViteManifest(config.AssetBasePath);

            // Load manifest in production mode
            if (!config.UseDevelopmentServer && env is not null)
            {
                var manifestPath = Path.Combine(env.WebRootPath, config.ManifestPath);
                manifest.Load(manifestPath);
            }

            return manifest;
        });

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
    /// Configures the app to use Impulse middleware.
    /// Handles version mismatch detection and reload headers.
    /// </summary>
    public static IApplicationBuilder UseImpulse(this IApplicationBuilder app)
    {
        var config = app.ApplicationServices.GetRequiredService<ImpulseConfiguration>();

        app.Use(async (context, next) =>
        {
            // Check for version mismatch on Impulse requests
            if (context.Request.Headers.TryGetValue(ImpulseHeaders.Impulse, out var impulseHeader)
                && impulseHeader == "true"
                && context.Request.Headers.TryGetValue(ImpulseHeaders.Version, out var clientVersion)
                && !string.IsNullOrEmpty(config.Version)
                && clientVersion != config.Version)
            {
                // Signal client to reload
                context.Response.Headers.Append(ImpulseHeaders.Reload, "true");
            }

            await next();
        });

        return app;
    }

    /// <summary>
    /// Configures the app to use Impulse context.
    /// </summary>
    [Obsolete("Use UseImpulse() instead")]
    public static IApplicationBuilder UseImpulseContext<TContext>(
        this IApplicationBuilder app,
        Func<HttpContext, Task<TContext>> contextFactory)
    {
        var existingProvider = app.ApplicationServices.GetService<IImpulseContextProvider>();
        if (existingProvider is null)
        {
            throw new InvalidOperationException(
                "Impulse context provider not registered. Call AddImpulseContext<TContext>() in ConfigureServices.");
        }

        return UseImpulse(app);
    }
}
