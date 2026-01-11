using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Impulse;

/// <summary>
/// ASP.NET Core extensions for Impulse.
/// </summary>
public static class ImpulseExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // Version for client reload detection
    private static readonly string Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";

    /// <summary>
    /// Maps all Impulse endpoints from the specified assembly.
    /// </summary>
    public static IEndpointRouteBuilder MapImpulseEndpoints(
        this IEndpointRouteBuilder endpoints,
        Assembly assembly)
    {
        var discoveredEndpoints = EndpointDiscovery.FindEndpoints(assembly);

        foreach (var endpoint in discoveredEndpoints)
        {
            RegisterEndpoint(endpoints, endpoint);
        }

        return endpoints;
    }

    private static void RegisterEndpoint(IEndpointRouteBuilder endpoints, EndpointInfo info)
    {
        var handler = CreateHandler(info);

        var routeBuilder = info.Method switch
        {
            HttpMethod.Get => endpoints.MapGet(info.Route, handler),
            HttpMethod.Post => endpoints.MapPost(info.Route, handler),
            HttpMethod.Put => endpoints.MapPut(info.Route, handler),
            HttpMethod.Patch => endpoints.MapPatch(info.Route, handler),
            HttpMethod.Delete => endpoints.MapDelete(info.Route, handler),
            _ => throw new NotSupportedException($"HTTP method {info.Method} not supported")
        };
    }

    private static Delegate CreateHandler(EndpointInfo info)
    {
        return async (HttpContext ctx) =>
        {
            // Create endpoint instance
            var endpoint = Activator.CreateInstance(info.Type)!;
            var handleMethod = info.Type.GetMethod("HandleAsync")!;

            IResult result;

            try
            {
                if (info.RequestType != null)
                {
                    // Endpoint with request - bind from body and route
                    var request = await BindRequestAsync(ctx, info);
                    result = await InvokeWithRequest(endpoint, handleMethod, request, ctx.RequestAborted);
                }
                else
                {
                    // Endpoint without request
                    result = await InvokeWithoutRequest(endpoint, handleMethod, ctx.RequestAborted);
                }
            }
            catch (OperationCanceledException)
            {
                throw; // Let cancellation propagate
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }

            return WriteResult(ctx, result, info.Route);
        };
    }

    private static async Task<object> BindRequestAsync(HttpContext ctx, EndpointInfo info)
    {
        var requestType = info.RequestType!;
        object? request = null;

        // For POST/PUT/PATCH, read from body first
        if (ctx.Request.Method is "POST" or "PUT" or "PATCH")
        {
            if (ctx.Request.ContentLength > 0 || ctx.Request.ContentType?.Contains("json") == true)
            {
                request = await ctx.Request.ReadFromJsonAsync(requestType, JsonOptions, ctx.RequestAborted);
            }
        }

        // Merge route parameters
        return MergeRouteParams(ctx, info.Route, request, requestType);
    }

    private static object MergeRouteParams(HttpContext ctx, string route, object? body, Type requestType)
    {
        var props = requestType.GetProperties();
        var constructor = requestType.GetConstructors().FirstOrDefault();

        if (constructor == null)
        {
            throw new InvalidOperationException($"Request type {requestType.Name} must have a constructor");
        }

        var args = new List<object?>();

        foreach (var param in constructor.GetParameters())
        {
            var paramName = param.Name!;

            // Check route values first
            var routeValue = ctx.Request.RouteValues
                .FirstOrDefault(kv => kv.Key.Equals(paramName, StringComparison.OrdinalIgnoreCase))
                .Value?.ToString();

            if (routeValue != null)
            {
                args.Add(ConvertValue(routeValue, param.ParameterType));
            }
            else if (body != null)
            {
                // Get from body
                var prop = props.FirstOrDefault(p =>
                    p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase));
                args.Add(prop?.GetValue(body) ?? GetDefault(param.ParameterType));
            }
            else
            {
                args.Add(GetDefault(param.ParameterType));
            }
        }

        return constructor.Invoke(args.ToArray());
    }

    private static object? ConvertValue(string value, Type targetType)
    {
        if (targetType == typeof(int)) return int.Parse(value);
        if (targetType == typeof(long)) return long.Parse(value);
        if (targetType == typeof(Guid)) return Guid.Parse(value);
        if (targetType == typeof(bool)) return bool.Parse(value);
        if (targetType == typeof(DateTime)) return DateTime.Parse(value);
        return value;
    }

    private static object? GetDefault(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private static async Task<IResult> InvokeWithRequest(
        object endpoint, MethodInfo method, object request, CancellationToken ct)
    {
        var task = (Task)method.Invoke(endpoint, new[] { request, ct })!;
        await task;

        // Get result from Task<IResult<T>>
        var resultProperty = task.GetType().GetProperty("Result");
        return (IResult)resultProperty!.GetValue(task)!;
    }

    private static async Task<IResult> InvokeWithoutRequest(
        object endpoint, MethodInfo method, CancellationToken ct)
    {
        var task = (Task)method.Invoke(endpoint, new object[] { ct })!;
        await task;

        var resultProperty = task.GetType().GetProperty("Result");
        return (IResult)resultProperty!.GetValue(task)!;
    }

    private static Microsoft.AspNetCore.Http.IResult WriteResult(HttpContext ctx, IResult result, string component)
    {
        var isImpulseRequest = ctx.Request.Headers.ContainsKey("X-Impulse");
        var clientVersion = ctx.Request.Headers["X-Impulse-Version"].FirstOrDefault();
        var isMutation = ctx.Request.Method != "GET";

        // Check version mismatch
        if (clientVersion != null && clientVersion != Version)
        {
            ctx.Response.Headers["X-Impulse-Reload"] = "true";
        }

        return result switch
        {
            OkResult<object> ok => WriteOk(ctx, ok.Data, component, isImpulseRequest, isMutation),
            NotFoundResult notFound => Results.NotFound(new { error = notFound.Message }),
            ValidationProblemResult validation => Results.UnprocessableEntity(new { errors = validation.Errors }),
            CreatedResult<object> created => Results.Created(created.Location, created.Data),
            RedirectResult redirect => Results.Redirect(redirect.Url),
            _ => WriteOkDynamic(ctx, result, component, isImpulseRequest, isMutation)
        };
    }

    private static Microsoft.AspNetCore.Http.IResult WriteOkDynamic(
        HttpContext ctx, IResult result, string component, bool isImpulseRequest, bool isMutation)
    {
        var resultType = result.GetType();

        // Handle generic OkResult<T>
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(OkResult<>))
        {
            var data = resultType.GetProperty("Data")!.GetValue(result);
            return WriteOk(ctx, data, component, isImpulseRequest, isMutation);
        }

        // Handle generic NotFoundResult<T>
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(NotFoundResult<>))
        {
            var message = resultType.GetProperty("Message")!.GetValue(result);
            return Results.NotFound(new { error = message });
        }

        // Handle generic ValidationProblemResult<T>
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(ValidationProblemResult<>))
        {
            var errors = resultType.GetProperty("Errors")!.GetValue(result);
            return Results.UnprocessableEntity(new { errors });
        }

        // Handle generic CreatedResult<T>
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(CreatedResult<>))
        {
            var location = resultType.GetProperty("Location")!.GetValue(result)?.ToString();
            var data = resultType.GetProperty("Data")!.GetValue(result);
            return Results.Created(location, data);
        }

        return Results.Problem("Unknown result type");
    }

    private static Microsoft.AspNetCore.Http.IResult WriteOk(
        HttpContext ctx, object? data, string component, bool isImpulseRequest, bool isMutation)
    {
        // For mutations, return data directly
        if (isMutation)
        {
            return Results.Json(data, JsonOptions);
        }

        // For GET with X-Impulse header, wrap response
        if (isImpulseRequest)
        {
            return Results.Json(new
            {
                props = data,
                component = component,
                version = Version
            }, JsonOptions);
        }

        // For browser requests, render HTML
        return RenderHtml(data, component);
    }

    private static Microsoft.AspNetCore.Http.IResult RenderHtml(object? data, string component)
    {
        var json = JsonSerializer.Serialize(new
        {
            props = data,
            component = component,
            version = Version
        }, JsonOptions);

        var escapedJson = System.Web.HttpUtility.HtmlAttributeEncode(json);

        var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Impulse v3</title>
    <style>
        body {{ font-family: system-ui, sans-serif; margin: 0; padding: 20px; }}
        #app {{ min-height: 100px; }}
    </style>
</head>
<body>
    <div id=""app"" data-impulse=""{escapedJson}"">
        <p>Loading...</p>
    </div>
    <script type=""module"" src=""/assets/main.js""></script>
</body>
</html>";

        return Results.Content(html, "text/html");
    }
}
