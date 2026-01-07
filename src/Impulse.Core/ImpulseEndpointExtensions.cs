using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Impulse.Core;

/// <summary>
/// Extension methods for registering Impulse components and mutations on endpoints.
/// </summary>
public static class ImpulseEndpointExtensions
{
    /// <summary>
    /// Registers an endpoint as an Impulse component.
    /// Component path is derived from the Props type using convention.
    /// </summary>
    /// <typeparam name="TProps">The props type for the component. Must be in a Features namespace.</typeparam>
    /// <param name="builder">The route handler builder.</param>
    /// <returns>A component builder for further configuration.</returns>
    /// <remarks>
    /// Path derivation examples:
    /// - Features.Dashboard.DashboardProps → ./Dashboard
    /// - Features.Residents.ResidentDetailProps → ./Residents/Detail
    /// </remarks>
    public static ImpulseComponentBuilder<TProps> AsComponent<TProps>(
        this RouteHandlerBuilder builder)
    {
        var componentPath = ComponentPathConvention.GetPath<TProps>();

        var metadata = new ImpulseComponentMetadata
        {
            PropsType = typeof(TProps),
            ComponentPath = componentPath
        };

        builder.WithMetadata(metadata);
        builder.AddEndpointFilter<ImpulseComponentFilter>();

        return new ImpulseComponentBuilder<TProps>(builder, metadata);
    }

    /// <summary>
    /// Registers an endpoint as an Impulse mutation.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="builder">The route handler builder.</param>
    /// <returns>A mutation builder for further configuration.</returns>
    public static ImpulseMutationBuilder<TRequest, TResponse> AsMutation<TRequest, TResponse>(
        this RouteHandlerBuilder builder)
    {
        var metadata = new ImpulseMutationMetadata
        {
            RequestType = typeof(TRequest),
            ResponseType = typeof(TResponse)
        };

        builder.WithMetadata(metadata);

        return new ImpulseMutationBuilder<TRequest, TResponse>(builder, metadata);
    }
}

/// <summary>
/// Builder for configuring an Impulse component endpoint.
/// </summary>
public sealed class ImpulseComponentBuilder<TProps>
{
    private readonly RouteHandlerBuilder _builder;
    private readonly ImpulseComponentMetadata _metadata;

    internal ImpulseComponentBuilder(RouteHandlerBuilder builder, ImpulseComponentMetadata metadata)
    {
        _builder = builder;
        _metadata = metadata;
    }

    /// <summary>
    /// Adds a deferred component that auto-loads after hydration.
    /// </summary>
    /// <typeparam name="TDeferredProps">The props type for the deferred component.</typeparam>
    /// <param name="key">The key to identify this deferred data.</param>
    /// <param name="urlTemplate">The URL template to fetch the deferred data.</param>
    public ImpulseComponentBuilder<TProps> Deferred<TDeferredProps>(string key, string urlTemplate)
    {
        _metadata.Deferred[key] = new DeferredComponentInfo
        {
            PropsType = typeof(TDeferredProps),
            UrlTemplate = urlTemplate
        };
        return this;
    }

    /// <summary>
    /// Adds a lazy component that loads on demand.
    /// </summary>
    /// <typeparam name="TLazyProps">The props type for the lazy component.</typeparam>
    /// <param name="key">The key to identify this lazy data.</param>
    /// <param name="urlTemplate">The URL template to fetch the lazy data.</param>
    public ImpulseComponentBuilder<TProps> Lazy<TLazyProps>(string key, string urlTemplate)
    {
        _metadata.Lazy[key] = new LazyComponentInfo
        {
            PropsType = typeof(TLazyProps),
            UrlTemplate = urlTemplate
        };
        return this;
    }

    /// <summary>
    /// Provides access to the underlying route handler builder.
    /// </summary>
    public RouteHandlerBuilder Builder => _builder;

    public static implicit operator RouteHandlerBuilder(ImpulseComponentBuilder<TProps> builder) => builder._builder;
}

/// <summary>
/// Builder for configuring an Impulse mutation endpoint.
/// </summary>
public sealed class ImpulseMutationBuilder<TRequest, TResponse>
{
    private readonly RouteHandlerBuilder _builder;
    private readonly ImpulseMutationMetadata _metadata;

    internal ImpulseMutationBuilder(RouteHandlerBuilder builder, ImpulseMutationMetadata metadata)
    {
        _builder = builder;
        _metadata = metadata;
    }

    /// <summary>
    /// Declares that this mutation invalidates data of the specified props type.
    /// </summary>
    /// <typeparam name="TInvalidates">The props type to invalidate.</typeparam>
    public ImpulseMutationBuilder<TRequest, TResponse> Invalidates<TInvalidates>()
    {
        _metadata.InvalidatesTypes.Add(typeof(TInvalidates));
        return this;
    }

    /// <summary>
    /// Provides access to the underlying route handler builder.
    /// </summary>
    public RouteHandlerBuilder Builder => _builder;

    public static implicit operator RouteHandlerBuilder(ImpulseMutationBuilder<TRequest, TResponse> builder) => builder._builder;
}

/// <summary>
/// Endpoint filter that handles Impulse navigation requests.
/// For browser requests: renders full HTML shell with embedded props.
/// For X-Impulse requests: returns JSON navigation response.
/// </summary>
internal sealed class ImpulseComponentFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);
        if (result is null) return result;

        var httpContext = context.HttpContext;
        var isImpulseRequest = httpContext.Request.Headers.ContainsKey(ImpulseHeaders.Impulse);

        // Get services
        var config = httpContext.RequestServices.GetService<ImpulseConfiguration>();
        var contextProvider = httpContext.RequestServices.GetService<IImpulseContextProvider>();
        var appContext = contextProvider is not null
            ? await contextProvider.GetContextAsync(httpContext)
            : new object();

        if (isImpulseRequest)
        {
            // X-Impulse request: return JSON navigation response
            var clientVersion = httpContext.Request.Headers[ImpulseHeaders.Version].FirstOrDefault();
            var serverVersion = config?.Version;

            if (!string.IsNullOrEmpty(clientVersion) &&
                !string.IsNullOrEmpty(serverVersion) &&
                clientVersion != serverVersion)
            {
                httpContext.Response.Headers[ImpulseHeaders.Reload] = "true";
            }

            return Results.Ok(new ImpulseNavigationResponse
            {
                Props = result,
                Context = appContext
            });
        }
        else
        {
            // Browser request: render HTML shell
            var renderer = httpContext.RequestServices.GetService<ImpulseShellRenderer>();
            var metadata = httpContext.GetEndpoint()?.Metadata.GetMetadata<ImpulseComponentMetadata>();

            if (renderer is not null && config is not null)
            {
                var payload = new ImpulsePayload
                {
                    Url = httpContext.Request.Path.Value ?? "/",
                    Version = config.Version,
                    Props = result,
                    Context = appContext,
                    Deferred = metadata?.Deferred.Count > 0
                        ? metadata.Deferred.ToDictionary(
                            d => d.Key,
                            d => d.Value.UrlTemplate)
                        : null,
                    Lazy = metadata?.Lazy.Count > 0
                        ? metadata.Lazy.ToDictionary(
                            l => l.Key,
                            l => l.Value.UrlTemplate)
                        : null
                };

                var componentPath = metadata?.ComponentPath ?? "./App";
                var html = renderer.RenderShell(payload, "Impulse App");
                html = html.Replace("data-impulse='", $"data-component=\"{componentPath}\" data-impulse='");

                return Results.Content(html, "text/html");
            }
        }

        return result;
    }
}
