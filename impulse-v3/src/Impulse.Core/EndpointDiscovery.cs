using System.Reflection;

namespace Impulse;

/// <summary>
/// Discovers Impulse endpoints from assemblies.
/// </summary>
public static class EndpointDiscovery
{
    /// <summary>
    /// Find all Impulse endpoints in the specified assembly.
    /// </summary>
    public static IReadOnlyList<EndpointInfo> FindEndpoints(Assembly assembly)
    {
        var endpoints = new List<EndpointInfo>();

        foreach (var type in assembly.GetExportedTypes())
        {
            var attr = type.GetCustomAttribute<EndpointAttribute>();
            if (attr is null) continue;

            var (requestType, responseType) = ExtractGenericTypes(type);

            endpoints.Add(new EndpointInfo(
                Type: type,
                Route: attr.Route,
                Method: attr.Method,
                RequestType: requestType,
                ResponseType: responseType));
        }

        return endpoints;
    }

    private static (Type? RequestType, Type? ResponseType) ExtractGenericTypes(Type endpointType)
    {
        var baseType = endpointType.BaseType;

        while (baseType != null)
        {
            if (baseType.IsGenericType)
            {
                var genericDef = baseType.GetGenericTypeDefinition();

                // Endpoint<TRequest, TResponse>
                if (genericDef == typeof(Endpoint<,>))
                {
                    var args = baseType.GetGenericArguments();
                    return (args[0], args[1]);
                }

                // Endpoint<TResponse>
                if (genericDef == typeof(Endpoint<>))
                {
                    var args = baseType.GetGenericArguments();
                    return (null, args[0]);
                }
            }

            baseType = baseType.BaseType;
        }

        return (null, null);
    }
}

/// <summary>
/// Information about a discovered endpoint.
/// </summary>
public record EndpointInfo(
    Type Type,
    string Route,
    HttpMethod Method,
    Type? RequestType,
    Type? ResponseType);
