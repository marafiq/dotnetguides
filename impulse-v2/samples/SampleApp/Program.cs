using System.Reflection;
using Impulse.Core;
using SampleApp.Residents;
using SampleApp.Medications;
using SampleApp.CarePlans;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Version for client reload detection
var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

// Map all Impulse endpoints automatically
app.MapImpulseEndpoints(Assembly.GetExecutingAssembly(), version);

app.Run();

namespace Impulse.Core
{
    public static class ImpulseWebExtensions
    {
        /// <summary>
        /// Maps all ImpulseEndpoint classes found in the assembly
        /// </summary>
        public static WebApplication MapImpulseEndpoints(this WebApplication app, Assembly assembly, string version)
        {
            var endpointTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && t.BaseType != null)
                .Where(t => IsImpulseEndpoint(t))
                .ToList();

            foreach (var type in endpointTypes)
            {
                var attr = type.GetCustomAttribute<ImpulseEndpointAttribute>();
                if (attr == null) continue;

                RegisterEndpoint(app, type, attr, version);
            }

            return app;
        }

        private static bool IsImpulseEndpoint(Type type)
        {
            var current = type.BaseType;
            while (current != null)
            {
                if (current.IsGenericType)
                {
                    var generic = current.GetGenericTypeDefinition();
                    if (generic.Name.StartsWith("ImpulseEndpoint"))
                        return true;
                }
                current = current.BaseType;
            }
            return false;
        }

        private static void RegisterEndpoint(WebApplication app, Type endpointType, ImpulseEndpointAttribute attr, string version)
        {
            // Get response type from generic base class
            var baseType = endpointType.BaseType!;
            var responseType = baseType.GenericTypeArguments.Last();
            var hasRequest = baseType.GenericTypeArguments.Length > 1;
            var requestType = hasRequest ? baseType.GenericTypeArguments[0] : null;

            var route = attr.Route;
            var method = attr.Method;

            Delegate handler = method switch
            {
                ImpulseMethod.Get => CreateGetHandler(endpointType, route, version, requestType),
                ImpulseMethod.Post => CreatePostHandler(endpointType, route, version, requestType!),
                ImpulseMethod.Put => CreatePutHandler(endpointType, route, version, requestType!),
                ImpulseMethod.Patch => CreatePatchHandler(endpointType, route, version, requestType!),
                ImpulseMethod.Delete => CreateDeleteHandler(endpointType, route, version),
                _ => throw new NotSupportedException($"Method {method} not supported")
            };

            var builder = method switch
            {
                ImpulseMethod.Get => app.MapGet(route, handler),
                ImpulseMethod.Post => app.MapPost(route, handler),
                ImpulseMethod.Put => app.MapPut(route, handler),
                ImpulseMethod.Patch => app.MapPatch(route, handler),
                ImpulseMethod.Delete => app.MapDelete(route, handler),
                _ => throw new NotSupportedException($"Method {method} not supported")
            };
        }

        private static Delegate CreateGetHandler(Type endpointType, string route, string version, Type? requestType)
        {
            return async (HttpContext ctx) =>
            {
                var endpoint = Activator.CreateInstance(endpointType)!;
                var handleMethod = endpointType.GetMethod("Handle")!;

                IImpulseResult result;

                if (requestType != null)
                {
                    // Build request from route parameters
                    var request = BuildRequestFromRoute(ctx, route, requestType);
                    result = await (Task<IImpulseResult>)handleMethod.Invoke(endpoint, [request, ctx.RequestAborted])!;
                }
                else
                {
                    result = await (Task<IImpulseResult>)handleMethod.Invoke(endpoint, [ctx.RequestAborted])!;
                }

                return WriteImpulseResult(ctx, result, route, version);
            };
        }

        private static Delegate CreatePostHandler(Type endpointType, string route, string version, Type requestType)
        {
            return async (HttpContext ctx) =>
            {
                var endpoint = Activator.CreateInstance(endpointType)!;
                var handleMethod = endpointType.GetMethod("Handle")!;

                object? request;
                try
                {
                    request = await ctx.Request.ReadFromJsonAsync(requestType, ctx.RequestAborted);
                    if (request == null)
                    {
                        return Results.BadRequest(new { error = "Invalid request body" });
                    }

                    // Merge route parameters
                    request = MergeRouteParams(ctx, route, request, requestType);
                }
                catch (Exception)
                {
                    return Results.BadRequest(new { error = "Invalid JSON" });
                }

                var result = await (Task<IImpulseResult>)handleMethod.Invoke(endpoint, [request, ctx.RequestAborted])!;
                return WriteImpulseResult(ctx, result, route, version);
            };
        }

        private static Delegate CreatePutHandler(Type endpointType, string route, string version, Type requestType)
        {
            return CreatePostHandler(endpointType, route, version, requestType);
        }

        private static Delegate CreatePatchHandler(Type endpointType, string route, string version, Type requestType)
        {
            return CreatePostHandler(endpointType, route, version, requestType);
        }

        private static Delegate CreateDeleteHandler(Type endpointType, string route, string version)
        {
            return async (HttpContext ctx) =>
            {
                var endpoint = Activator.CreateInstance(endpointType)!;
                var handleMethod = endpointType.GetMethod("Handle")!;

                var result = await (Task<IImpulseResult>)handleMethod.Invoke(endpoint, [ctx.RequestAborted])!;
                return WriteImpulseResult(ctx, result, route, version);
            };
        }

        private static object BuildRequestFromRoute(HttpContext ctx, string route, Type requestType)
        {
            var routeParams = ExtractRouteParams(route);
            var constructor = requestType.GetConstructors().First();
            var args = new List<object?>();

            foreach (var param in constructor.GetParameters())
            {
                var paramName = param.Name!.ToLowerInvariant();
                var routeValue = ctx.Request.RouteValues
                    .FirstOrDefault(kv => kv.Key.Equals(paramName, StringComparison.OrdinalIgnoreCase))
                    .Value?.ToString();

                if (routeValue != null)
                {
                    args.Add(ConvertValue(routeValue, param.ParameterType));
                }
                else
                {
                    args.Add(param.HasDefaultValue ? param.DefaultValue : GetDefaultValue(param.ParameterType));
                }
            }

            return constructor.Invoke(args.ToArray());
        }

        private static object MergeRouteParams(HttpContext ctx, string route, object request, Type requestType)
        {
            // For records, we need to create new instance with merged values
            var props = requestType.GetProperties();
            var constructor = requestType.GetConstructors().First();
            var args = new List<object?>();

            foreach (var param in constructor.GetParameters())
            {
                var prop = props.FirstOrDefault(p => p.Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase));
                var paramName = param.Name!.ToLowerInvariant();

                // Check if this is a route parameter
                var routeValue = ctx.Request.RouteValues
                    .FirstOrDefault(kv => kv.Key.Equals(paramName, StringComparison.OrdinalIgnoreCase))
                    .Value?.ToString();

                if (routeValue != null)
                {
                    args.Add(ConvertValue(routeValue, param.ParameterType));
                }
                else if (prop != null)
                {
                    args.Add(prop.GetValue(request));
                }
                else
                {
                    args.Add(param.HasDefaultValue ? param.DefaultValue : GetDefaultValue(param.ParameterType));
                }
            }

            return constructor.Invoke(args.ToArray());
        }

        private static List<string> ExtractRouteParams(string route)
        {
            var result = new List<string>();
            var regex = new System.Text.RegularExpressions.Regex(@"\{(\w+)\}");
            foreach (System.Text.RegularExpressions.Match match in regex.Matches(route))
            {
                result.Add(match.Groups[1].Value.ToLowerInvariant());
            }
            return result;
        }

        private static object? ConvertValue(string value, Type targetType)
        {
            if (targetType == typeof(int)) return int.Parse(value);
            if (targetType == typeof(long)) return long.Parse(value);
            if (targetType == typeof(bool)) return bool.Parse(value);
            if (targetType == typeof(Guid)) return Guid.Parse(value);
            return value;
        }

        private static object? GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private static IResult WriteImpulseResult(HttpContext ctx, IImpulseResult result, string component, string version)
        {
            var isImpulseRequest = ctx.Request.Headers.ContainsKey("X-Impulse");
            var clientVersion = ctx.Request.Headers["X-Impulse-Version"].FirstOrDefault();

            // Check version mismatch
            if (clientVersion != null && clientVersion != version)
            {
                ctx.Response.Headers["X-Impulse-Reload"] = "true";
            }

            if (result is ImpulseOkResult ok)
            {
                if (isImpulseRequest)
                {
                    return Results.Json(new
                    {
                        props = ok.Value,
                        component = component,
                        version = version
                    });
                }
                return RenderHtml(ok.Value, component, version);
            }

            if (result is ImpulseCreatedResult created)
            {
                return Results.Created(created.Location, created.Value);
            }

            if (result is ImpulseNotFoundResult notFound)
            {
                return Results.NotFound(new { error = notFound.Message });
            }

            if (result is ImpulseValidationProblemResult validation)
            {
                return Results.UnprocessableEntity(new { errors = validation.Errors });
            }

            return Results.Problem("Unknown result type");
        }

        private static IResult RenderHtml(object? props, string component, string version)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                props,
                component,
                version
            });

            var escapedJson = System.Web.HttpUtility.HtmlAttributeEncode(json);

            var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Impulse v2</title>
</head>
<body>
    <div id=""app"" data-impulse=""{escapedJson}""></div>
    <script type=""module"" src=""/assets/main.js""></script>
</body>
</html>";

            return Results.Content(html, "text/html");
        }
    }
}
