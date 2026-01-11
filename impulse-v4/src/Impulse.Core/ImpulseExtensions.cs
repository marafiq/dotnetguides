using System.Reflection;
using System.Text.Json;

namespace Impulse;

/// <summary>
/// Extension methods for registering Impulse endpoints with ASP.NET Core.
/// </summary>
public static class ImpulseExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Maps all Impulse endpoints found in the given assembly.
    /// </summary>
    public static IEndpointRouteBuilder MapImpulseEndpoints(
        this IEndpointRouteBuilder app,
        Assembly assembly,
        string? basePath = null)
    {
        var endpointTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<ImpulseEndpointAttribute>() != null && !t.IsAbstract);

        foreach (var type in endpointTypes)
        {
            var attr = type.GetCustomAttribute<ImpulseEndpointAttribute>()!;
            var route = string.IsNullOrEmpty(basePath) ? attr.Route : $"{basePath.TrimEnd('/')}{attr.Route}";

            MapEndpoint(app, type, route, attr.Method);
        }

        return app;
    }

    /// <summary>
    /// Maps all modules found in the given assembly.
    /// </summary>
    public static IEndpointRouteBuilder MapImpulseModules(
        this IEndpointRouteBuilder app,
        Assembly assembly)
    {
        var moduleTypes = assembly.GetTypes()
            .Where(t => typeof(ImpulseModule).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var moduleType in moduleTypes)
        {
            var module = (ImpulseModule)Activator.CreateInstance(moduleType)!;
            var group = app.MapGroup(module.BasePath);
            module.Configure(group);
        }

        return app;
    }

    private static void MapEndpoint(IEndpointRouteBuilder app, Type endpointType, string route, ImpulseMethod method)
    {
        var handler = CreateHandler(endpointType);

        var routeBuilder = method switch
        {
            ImpulseMethod.Get => app.MapGet(route, handler),
            ImpulseMethod.Post => app.MapPost(route, handler),
            ImpulseMethod.Put => app.MapPut(route, handler),
            ImpulseMethod.Patch => app.MapPatch(route, handler),
            ImpulseMethod.Delete => app.MapDelete(route, handler),
            _ => throw new ArgumentException($"Unsupported method: {method}")
        };

        routeBuilder.WithName(endpointType.Name);
    }

    private static Delegate CreateHandler(Type endpointType)
    {
        // Check endpoint base types to determine handler signature
        var baseType = endpointType.BaseType;

        if (baseType?.IsGenericType == true)
        {
            var genericDef = baseType.GetGenericTypeDefinition();
            var typeArgs = baseType.GetGenericArguments();

            // ImpulseEndpoint<TResponse> - GET without request body
            if (genericDef == typeof(ImpulseEndpoint<>))
            {
                return async (HttpContext context, IServiceProvider services, CancellationToken ct) =>
                {
                    var endpoint = ActivatorUtilities.CreateInstance(services, endpointType);
                    SetContext(endpoint, context);
                    var handleMethod = endpointType.GetMethod("Handle", [typeof(CancellationToken)])!;
                    var result = await (Task<IImpulseResult>)handleMethod.Invoke(endpoint, [ct])!;
                    await result.ExecuteAsync(context);
                };
            }

            // ImpulseEndpoint<TRequest, TResponse> - POST/PUT with request body
            if (genericDef == typeof(ImpulseEndpoint<,>))
            {
                var requestType = typeArgs[0];

                return async (HttpContext context, IServiceProvider services, CancellationToken ct) =>
                {
                    var endpoint = ActivatorUtilities.CreateInstance(services, endpointType);
                    SetContext(endpoint, context);

                    object? request;
                    try
                    {
                        request = await context.Request.ReadFromJsonAsync(requestType, JsonOptions, ct);
                        if (request == null)
                        {
                            var errorResult = ImpulseResults.BadRequest("Request body is required");
                            await errorResult.ExecuteAsync(context);
                            return;
                        }
                    }
                    catch (JsonException ex)
                    {
                        var errorResult = ImpulseResults.BadRequest($"Invalid JSON: {ex.Message}");
                        await errorResult.ExecuteAsync(context);
                        return;
                    }

                    var handleMethod = endpointType.GetMethod("Handle", [requestType, typeof(CancellationToken)])!;
                    var result = await (Task<IImpulseResult>)handleMethod.Invoke(endpoint, [request, ct])!;
                    await result.ExecuteAsync(context);
                };
            }

            // ImpulseEndpoint<TRoute, TRequest, TResponse> - with route parameters
            if (genericDef == typeof(ImpulseEndpoint<,,>))
            {
                var routeType = typeArgs[0];
                var requestType = typeArgs[1];

                return async (HttpContext context, IServiceProvider services, CancellationToken ct) =>
                {
                    var endpoint = ActivatorUtilities.CreateInstance(services, endpointType);
                    SetContext(endpoint, context);

                    // Parse route parameters
                    var routeParams = ParseRouteParameters(context, routeType);

                    // Parse request body (Unit for no body)
                    object? request;
                    if (requestType == typeof(Unit))
                    {
                        request = Unit.Value;
                    }
                    else
                    {
                        try
                        {
                            request = await context.Request.ReadFromJsonAsync(requestType, JsonOptions, ct);
                            if (request == null)
                            {
                                var errorResult = ImpulseResults.BadRequest("Request body is required");
                                await errorResult.ExecuteAsync(context);
                                return;
                            }
                        }
                        catch (JsonException ex)
                        {
                            var errorResult = ImpulseResults.BadRequest($"Invalid JSON: {ex.Message}");
                            await errorResult.ExecuteAsync(context);
                            return;
                        }
                    }

                    var handleMethod = endpointType.GetMethod("Handle", [routeType, requestType, typeof(CancellationToken)])!;
                    var result = await (Task<IImpulseResult>)handleMethod.Invoke(endpoint, [routeParams, request, ct])!;
                    await result.ExecuteAsync(context);
                };
            }
        }

        throw new InvalidOperationException($"Unknown endpoint type: {endpointType.Name}");
    }

    private static void SetContext(object endpoint, HttpContext context)
    {
        var contextProp = endpoint.GetType().GetProperty("Context");
        contextProp?.SetValue(endpoint, context);
    }

    private static object ParseRouteParameters(HttpContext context, Type routeType)
    {
        var instance = Activator.CreateInstance(routeType)!;
        var routeValues = context.Request.RouteValues;

        foreach (var prop in routeType.GetProperties())
        {
            if (routeValues.TryGetValue(prop.Name.ToLowerInvariant(), out var value) ||
                routeValues.TryGetValue(prop.Name, out value))
            {
                if (value != null)
                {
                    var converted = Convert.ChangeType(value.ToString(), prop.PropertyType);
                    prop.SetValue(instance, converted);
                }
            }
        }

        return instance;
    }
}
