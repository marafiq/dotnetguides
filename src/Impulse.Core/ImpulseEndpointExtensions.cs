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
/// </summary>
internal sealed class ImpulseComponentFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);

        // Check if this is an Impulse navigation request
        var httpContext = context.HttpContext;
        var isImpulseRequest = httpContext.Request.Headers.ContainsKey(ImpulseHeaders.Impulse);

        if (isImpulseRequest && result is not null)
        {
            var clientVersion = httpContext.Request.Headers[ImpulseHeaders.Version].FirstOrDefault();
            var serverVersion = httpContext.RequestServices.GetService<ImpulseConfiguration>()?.Version;

            // Check for version mismatch
            if (!string.IsNullOrEmpty(clientVersion) &&
                !string.IsNullOrEmpty(serverVersion) &&
                clientVersion != serverVersion)
            {
                httpContext.Response.Headers[ImpulseHeaders.Reload] = "true";
            }

            // Return navigation response format
            var contextProvider = httpContext.RequestServices.GetService<IImpulseContextProvider>();
            var appContext = contextProvider is not null
                ? await contextProvider.GetContextAsync(httpContext)
                : new object();

            return Results.Ok(new ImpulseNavigationResponse
            {
                Props = result,
                Context = appContext
            });
        }

        return result;
    }
}
