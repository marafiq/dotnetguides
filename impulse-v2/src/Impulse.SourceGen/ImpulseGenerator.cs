using System.Reflection;
using Impulse.Core;
using Impulse.SourceGen.Analyzers;
using Impulse.SourceGen.Ast;
using Impulse.SourceGen.Emitters;
using Impulse.SourceGen.Plugins;

namespace Impulse.SourceGen;

/// <summary>
/// CLI tool that extracts TypeScript types, Zod schemas, and route definitions
/// from compiled Impulse assemblies using AST-based code generation.
///
/// Usage: impulse-gen [assembly-path] [output-dir]
/// </summary>
public static class ImpulseGenerator
{
    private static readonly List<ITsPlugin> _tsPlugins = new();
    private static readonly List<IZodPlugin> _zodPlugins = new();
    private static readonly List<IRoutePlugin> _routePlugins = new();

    public static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: impulse-gen <assembly-path> <output-dir>");
            return 1;
        }

        var assemblyPath = args[0];
        var outputDir = args[1];

        if (!File.Exists(assemblyPath))
        {
            Console.Error.WriteLine($"Assembly not found: {assemblyPath}");
            return 1;
        }

        try
        {
            Directory.CreateDirectory(outputDir);
            var assembly = Assembly.LoadFrom(assemblyPath);

            // Discover endpoints
            var endpoints = DiscoverEndpoints(assembly);

            if (endpoints.Count == 0)
            {
                Console.WriteLine("No Impulse endpoints found.");
                return 0;
            }

            Console.WriteLine($"Found {endpoints.Count} endpoint(s):");
            foreach (var ep in endpoints)
            {
                Console.WriteLine($"  - [{ep.Method}] {ep.Name}: {ep.Route}");
            }

            // Build route AST
            var routeFile = BuildRouteAst(endpoints);

            // Apply route plugins
            foreach (var plugin in _routePlugins.OrderBy(p => p.Priority))
            {
                routeFile = plugin.Transform(routeFile);
            }

            // Generate route files
            GenerateRouteFiles(routeFile, outputDir);

            // Build and emit TypeScript types
            var types = endpoints
                .SelectMany(e => new[] { e.RequestType, e.ResponseType })
                .Where(t => t != null)
                .Cast<Type>()
                .GetAllReferencedTypes()
                .ToList();

            var typeAnalyzer = new TypeAnalyzer();
            var tsAst = typeAnalyzer.AnalyzeToTypeScript(types);

            // Apply TypeScript plugins
            foreach (var plugin in _tsPlugins.OrderBy(p => p.Priority))
            {
                tsAst = plugin.Transform(tsAst);
            }

            var tsEmitter = new TsEmitter();
            var typesCode = EmitTypesFile(tsAst, tsEmitter);
            File.WriteAllText(Path.Combine(outputDir, "types.ts"), typesCode);

            // Build and emit Zod schemas
            var zodAst = typeAnalyzer.AnalyzeToZod(
                endpoints.Where(e => e.RequestType != null).Select(e => e.RequestType!));

            // Apply Zod plugins
            foreach (var plugin in _zodPlugins.OrderBy(p => p.Priority))
            {
                zodAst = plugin.Transform(zodAst);
            }

            var zodEmitter = new ZodEmitter();
            var validationCode = zodEmitter.Emit(zodAst);
            File.WriteAllText(Path.Combine(outputDir, "validation.ts"), validationCode);

            Console.WriteLine($"\nGenerated files in {outputDir}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Register a TypeScript AST plugin.
    /// </summary>
    public static void RegisterPlugin(ITsPlugin plugin) => _tsPlugins.Add(plugin);

    /// <summary>
    /// Register a Zod AST plugin.
    /// </summary>
    public static void RegisterPlugin(IZodPlugin plugin) => _zodPlugins.Add(plugin);

    /// <summary>
    /// Register a Route AST plugin.
    /// </summary>
    public static void RegisterPlugin(IRoutePlugin plugin) => _routePlugins.Add(plugin);

    private static List<EndpointInfo> DiscoverEndpoints(Assembly assembly)
    {
        var endpoints = new List<EndpointInfo>();

        foreach (var type in assembly.GetExportedTypes())
        {
            var attr = type.GetCustomAttribute<ImpulseEndpointAttribute>();
            if (attr is null) continue;

            // Find request/response types from base class
            var baseType = type.BaseType;
            Type? requestType = null;
            Type? responseType = null;

            while (baseType != null)
            {
                if (baseType.IsGenericType)
                {
                    var genericDef = baseType.GetGenericTypeDefinition();
                    if (genericDef.Name.StartsWith("ImpulseEndpoint"))
                    {
                        var args = baseType.GetGenericArguments();
                        if (args.Length == 2)
                        {
                            requestType = args[0];
                            responseType = args[1];
                        }
                        else if (args.Length == 1)
                        {
                            responseType = args[0];
                        }
                        break;
                    }
                }
                baseType = baseType.BaseType;
            }

            // Check for deferred configurations
            var deferred = DeferredExtensions.GetDeferredConfigs(type)
                .Select(d => new DeferredRoute(d.Key, d.Path, d.ResponseType.Name))
                .ToList();

            endpoints.Add(new EndpointInfo(
                Name: type.Name,
                FullName: type.FullName ?? type.Name,
                Route: attr.Route,
                Method: MapMethod(attr.Method),
                RequestType: requestType,
                ResponseType: responseType,
                Deferred: deferred));
        }

        return endpoints;
    }

    private static Ast.HttpMethod MapMethod(ImpulseMethod method) => method switch
    {
        ImpulseMethod.Get => Ast.HttpMethod.Get,
        ImpulseMethod.Post => Ast.HttpMethod.Post,
        ImpulseMethod.Put => Ast.HttpMethod.Put,
        ImpulseMethod.Patch => Ast.HttpMethod.Patch,
        ImpulseMethod.Delete => Ast.HttpMethod.Delete,
        _ => Ast.HttpMethod.Get
    };

    private static RouteFile BuildRouteAst(List<EndpointInfo> endpoints)
    {
        var routes = endpoints.Select(ep =>
        {
            var name = ep.Name;
            var route = new RouteDefinition(
                name,
                ep.Route,
                ep.Method,
                ep.ResponseType?.Name,
                ep.RequestType?.Name);

            // Add deferred routes
            foreach (var deferred in ep.Deferred)
            {
                route = route.WithDeferred(deferred.Key, deferred.Path, deferred.ResponseType);
            }

            return route;
        }).ToList();

        return new RouteFile(routes);
    }

    private static void GenerateRouteFiles(RouteFile routeFile, string outputDir)
    {
        var emitter = new RouteEmitter();

        // Generate routePaths.ts
        var routePathsCode = emitter.EmitRoutePaths(routeFile);
        File.WriteAllText(Path.Combine(outputDir, "routePaths.ts"), routePathsCode);

        // Generate routes.ts (virtual TanStack Router routes)
        var routesCode = emitter.EmitVirtualRoutes(routeFile);
        File.WriteAllText(Path.Combine(outputDir, "routes.ts"), routesCode);

        // Generate loaders.ts
        var loadersCode = emitter.EmitLoaders(routeFile);
        File.WriteAllText(Path.Combine(outputDir, "loaders.ts"), loadersCode);

        // Generate mutations.ts
        var mutationsCode = emitter.EmitMutations(routeFile);
        File.WriteAllText(Path.Combine(outputDir, "mutations.ts"), mutationsCode);

        // Clean up old file-based routes directory if it exists
        var oldRoutesDir = Path.Combine(outputDir, "routes");
        if (Directory.Exists(oldRoutesDir))
        {
            Directory.Delete(oldRoutesDir, recursive: true);
        }
    }

    private static string EmitTypesFile(TsFile ast, TsEmitter emitter)
    {
        var header = "// Auto-generated by impulse-gen - DO NOT EDIT\n\n";
        return header + emitter.Emit(ast);
    }
}

/// <summary>
/// Internal endpoint information record.
/// </summary>
internal record EndpointInfo(
    string Name,
    string FullName,
    string Route,
    Ast.HttpMethod Method,
    Type? RequestType,
    Type? ResponseType,
    IReadOnlyList<DeferredRoute> Deferred)
{
    public EndpointInfo(
        string name,
        string fullName,
        string route,
        Ast.HttpMethod method,
        Type? requestType,
        Type? responseType)
        : this(name, fullName, route, method, requestType, responseType,
               Array.Empty<DeferredRoute>()) { }

    public bool IsQuery => Method == Ast.HttpMethod.Get;
    public bool IsMutation => Method is Ast.HttpMethod.Post or Ast.HttpMethod.Put or Ast.HttpMethod.Patch or Ast.HttpMethod.Delete;
}
