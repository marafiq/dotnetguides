using System.Reflection;
using Impulse.CodeGen;
using Impulse.CodeGen.Ast;

if (args.Length < 2)
{
    Console.WriteLine("Usage: impulse-gen <assembly-path> <output-dir>");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  assembly-path  Path to the compiled .NET assembly to analyze");
    Console.WriteLine("  output-dir     Directory to write generated TypeScript files");
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
    // Load the assembly
    var assembly = Assembly.LoadFrom(assemblyPath);
    Console.WriteLine($"Loaded assembly: {assembly.GetName().Name}");

    // Analyze endpoints
    var endpointAnalyzer = new EndpointAnalyzer();
    var routes = endpointAnalyzer.DiscoverEndpoints(assembly);
    Console.WriteLine($"Discovered {routes.Count} endpoints");

    // Collect all types
    var referencedTypes = endpointAnalyzer.CollectTypes(assembly, routes);

    // Analyze types
    var typeAnalyzer = new TypeAnalyzer();
    var interfaces = typeAnalyzer.AnalyzeTypes(referencedTypes);
    var enums = new List<TsEnum>();

    // Find all enums in referenced types
    foreach (var type in referencedTypes)
    {
        CollectEnums(type, enums, new HashSet<Type>());
    }

    Console.WriteLine($"Analyzed {interfaces.Count} interfaces, {enums.Count} enums");

    // Ensure output directory exists
    Directory.CreateDirectory(outputDir);

    // Emit types.ts
    var tsEmitter = new TsEmitter();
    var typesContent = tsEmitter.EmitFile(enums, interfaces);
    File.WriteAllText(Path.Combine(outputDir, "types.ts"), typesContent);
    Console.WriteLine("Generated: types.ts");

    // Emit validation.ts (Zod schemas)
    var zodEmitter = new ZodEmitter();
    var zodSchemas = interfaces.Select(iface =>
        new ZodSchema(iface.Name, ConvertToZodObject(iface))).ToList();
    var zodFile = new ZodFile(zodSchemas);
    var validationContent = zodEmitter.EmitFile(zodFile);
    File.WriteAllText(Path.Combine(outputDir, "validation.ts"), validationContent);
    Console.WriteLine("Generated: validation.ts");

    // Emit routePaths.ts
    var routeEmitter = new RouteEmitter();
    var routeFile = new RouteFile(routes);
    var routePathsContent = routeEmitter.EmitFile(routeFile);
    File.WriteAllText(Path.Combine(outputDir, "routePaths.ts"), routePathsContent);
    Console.WriteLine("Generated: routePaths.ts");

    // Emit routeBuilders.ts (for parameterized routes)
    var routeBuildersContent = routeEmitter.EmitRouteBuilders(routeFile);
    if (!string.IsNullOrWhiteSpace(routeBuildersContent.Trim()))
    {
        File.WriteAllText(Path.Combine(outputDir, "routeBuilders.ts"), routeBuildersContent);
        Console.WriteLine("Generated: routeBuilders.ts");
    }

    // Emit mutations.ts
    var mutationEmitter = new MutationEmitter();
    var mutations = endpointAnalyzer.GetMutations(routes);
    if (mutations.Count > 0)
    {
        var mutationsFile = new MutationsFile(mutations);
        var mutationsContent = mutationEmitter.EmitFile(mutationsFile);
        File.WriteAllText(Path.Combine(outputDir, "mutations.ts"), mutationsContent);
        Console.WriteLine("Generated: mutations.ts");
    }

    Console.WriteLine();
    Console.WriteLine($"Code generation complete. Files written to: {outputDir}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    return 1;
}

void CollectEnums(Type type, List<TsEnum> enums, HashSet<Type> visited)
{
    if (!visited.Add(type)) return;

    foreach (var prop in type.GetProperties())
    {
        var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

        if (propType.IsEnum)
        {
            if (!enums.Any(e => e.Name == propType.Name))
            {
                enums.Add(new TypeAnalyzer().AnalyzeEnum(propType));
            }
        }
        else if (!propType.IsPrimitive && propType != typeof(string) &&
                 !propType.Namespace?.StartsWith("System") == true)
        {
            CollectEnums(propType, enums, visited);
        }
    }
}

ZodObject ConvertToZodObject(TsInterface iface)
{
    var props = iface.Properties.Select(p => new ZodProperty(
        p.Name,
        ConvertToZodType(p.Type),
        p.IsOptional)).ToList();

    return new ZodObject(props);
}

ZodType ConvertToZodType(TsType tsType)
{
    return tsType switch
    {
        TsString => new ZodString([]),
        TsNumber => new ZodNumber([]),
        TsBoolean => new ZodBoolean(),
        TsArray a => new ZodArray(ConvertToZodType(a.Element), []),
        TsRecord r => new ZodRecord(ConvertToZodType(r.KeyType), ConvertToZodType(r.ValueType)),
        TsRef r => new ZodRef(r.Name + "Schema"),
        TsUnion u when u.Types.Any(t => t is TsNull) =>
            new ZodNullable(ConvertToZodType(u.Types.First(t => t is not TsNull))),
        _ => new ZodUnknown()
    };
}

// Simple ZodUnknown type for fallback
file record ZodUnknown : ZodType;
