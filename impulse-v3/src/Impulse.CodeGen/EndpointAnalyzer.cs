using System.Reflection;
using System.Text.RegularExpressions;
using Impulse.CodeGen.Ast;

namespace Impulse.CodeGen;

/// <summary>
/// Analyzes assemblies to discover Impulse endpoints.
/// </summary>
public class EndpointAnalyzer
{
    /// <summary>
    /// Discover all endpoints in an assembly.
    /// </summary>
    public IReadOnlyList<EndpointRoute> DiscoverEndpoints(Assembly assembly)
    {
        var routes = new List<EndpointRoute>();

        foreach (var type in assembly.GetExportedTypes())
        {
            var attr = type.GetCustomAttributes()
                .FirstOrDefault(a => a.GetType().Name == "EndpointAttribute");

            if (attr == null) continue;

            var routeProp = attr.GetType().GetProperty("Route");
            var methodProp = attr.GetType().GetProperty("Method");

            if (routeProp == null || methodProp == null) continue;

            var route = routeProp.GetValue(attr)?.ToString() ?? "/";
            var method = methodProp.GetValue(attr)?.ToString() ?? "GET";

            // Find request and response types from base class
            var (requestType, responseType) = GetEndpointTypes(type);

            var name = type.Name.Replace("Endpoint", "");
            routes.Add(new EndpointRoute(name, route, method, requestType, responseType));
        }

        return routes;
    }

    /// <summary>
    /// Get all mutation endpoints (POST, PUT, PATCH, DELETE).
    /// </summary>
    public IReadOnlyList<MutationInfo> GetMutations(IEnumerable<EndpointRoute> routes)
    {
        return routes
            .Where(r => r.Method is "POST" or "PUT" or "PATCH" or "DELETE")
            .Where(r => r.RequestType != null && r.ResponseType != null)
            .Select(r => new MutationInfo(
                r.Name,
                r.Route,
                r.Method,
                r.RequestType!,
                r.ResponseType!,
                InvalidatesQueries: ShouldInvalidateQueries(r)))
            .ToList();
    }

    /// <summary>
    /// Get all query endpoints (GET).
    /// </summary>
    public IReadOnlyList<EndpointRoute> GetQueries(IEnumerable<EndpointRoute> routes)
    {
        return routes.Where(r => r.Method == "GET").ToList();
    }

    /// <summary>
    /// Collect all types referenced by endpoints.
    /// </summary>
    public IReadOnlyList<Type> CollectTypes(Assembly assembly, IEnumerable<EndpointRoute> routes)
    {
        var types = new HashSet<Type>();

        foreach (var route in routes)
        {
            if (route.RequestType != null)
            {
                var reqType = assembly.GetType(route.RequestType) ??
                    assembly.GetExportedTypes().FirstOrDefault(t => t.Name == route.RequestType);
                if (reqType != null) types.Add(reqType);
            }

            if (route.ResponseType != null)
            {
                var respType = assembly.GetType(route.ResponseType) ??
                    assembly.GetExportedTypes().FirstOrDefault(t => t.Name == route.ResponseType);
                if (respType != null) types.Add(respType);
            }
        }

        return types.ToList();
    }

    private (string? RequestType, string? ResponseType) GetEndpointTypes(Type endpointType)
    {
        var baseType = endpointType.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType)
            {
                var genericDef = baseType.GetGenericTypeDefinition();
                var args = baseType.GetGenericArguments();

                // Endpoint<TRequest, TResponse>
                if (args.Length == 2)
                {
                    return (args[0].Name, args[1].Name);
                }

                // Endpoint<TResponse>
                if (args.Length == 1)
                {
                    return (null, args[0].Name);
                }
            }

            baseType = baseType.BaseType;
        }

        return (null, null);
    }

    private bool ShouldInvalidateQueries(EndpointRoute route)
    {
        // Validation-only endpoints don't invalidate
        if (route.Route.Contains("/validate/")) return false;
        if (route.Name.StartsWith("Validate")) return false;

        // State-changing operations should invalidate
        return route.Method is "POST" or "PUT" or "PATCH" or "DELETE";
    }
}
