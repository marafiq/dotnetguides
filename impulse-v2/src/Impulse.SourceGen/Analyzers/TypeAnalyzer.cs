using System.Reflection;
using Impulse.SourceGen.Ast;

namespace Impulse.SourceGen.Analyzers;

/// <summary>
/// Analyzes C# types and converts them to TypeScript and Zod AST nodes.
/// This is the bridge between .NET reflection and TypeScript code generation.
/// </summary>
public class TypeAnalyzer
{
    private readonly HashSet<Type> _visitedTypes = new();
    private readonly List<TsInterface> _interfaces = new();
    private readonly List<TsEnum> _enums = new();
    private readonly List<ZodSchema> _schemas = new();

    /// <summary>
    /// Analyze a set of types and produce TypeScript AST.
    /// </summary>
    public TsFile AnalyzeToTypeScript(IEnumerable<Type> types)
    {
        _visitedTypes.Clear();
        _interfaces.Clear();
        _enums.Clear();

        foreach (var type in types)
        {
            AnalyzeType(type);
        }

        var statements = new List<TsNode>();
        statements.AddRange(_enums);
        statements.AddRange(_interfaces);

        return new TsFile(statements);
    }

    /// <summary>
    /// Analyze a set of types and produce Zod schema AST.
    /// </summary>
    public ZodFile AnalyzeToZod(IEnumerable<Type> types)
    {
        _visitedTypes.Clear();
        _schemas.Clear();

        foreach (var type in types)
        {
            AnalyzeTypeForZod(type);
        }

        return new ZodFile(_schemas);
    }

    private void AnalyzeType(Type type)
    {
        type = UnwrapNullable(type);

        if (!ShouldAnalyze(type)) return;
        if (!_visitedTypes.Add(type)) return;

        if (type.IsEnum)
        {
            AnalyzeEnum(type);
            return;
        }

        // IMPORTANT: Analyze nested types FIRST to ensure dependencies are defined
        // before they are referenced (topological order)
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propType = UnwrapNullable(prop.PropertyType);
            propType = UnwrapCollection(propType);
            AnalyzeType(propType);
        }

        // Now add this interface (after all dependencies are added)
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .Select(p => new TsProperty(
                ToCamelCase(p.Name),
                MapToTsType(p.PropertyType),
                IsNullable(p.PropertyType)))
            .ToList();

        _interfaces.Add(new TsInterface(type.Name, properties));
    }

    private void AnalyzeEnum(Type type)
    {
        var members = Enum.GetNames(type)
            .Select(name => new TsEnumMember(name, $"'{name}'"))
            .ToList();

        _enums.Add(new TsEnum(type.Name, members));
    }

    private void AnalyzeTypeForZod(Type type)
    {
        type = UnwrapNullable(type);

        if (!ShouldAnalyze(type)) return;
        if (!_visitedTypes.Add(type)) return;
        if (type.IsEnum) return; // Enums use z.nativeEnum

        // IMPORTANT: Analyze nested types FIRST to ensure dependencies are defined
        // before they are referenced (topological order)
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propType = UnwrapNullable(prop.PropertyType);
            propType = UnwrapCollection(propType);
            AnalyzeTypeForZod(propType);
        }

        // Now add this schema (after all dependencies are added)
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .Select(p => new ZodProperty(
                ToCamelCase(p.Name),
                MapToZodType(p.PropertyType),
                IsNullable(p.PropertyType)))
            .ToList();

        _schemas.Add(new ZodSchema($"{type.Name}Schema", new ZodObject(properties)));
    }

    /// <summary>
    /// Map a C# type to TypeScript type AST.
    /// </summary>
    public TsType MapToTsType(Type type)
    {
        var underlying = UnwrapNullable(type);
        var isNullable = underlying != type;

        TsType baseType = underlying switch
        {
            _ when underlying == typeof(string) => new TsString(),
            _ when underlying == typeof(char) => new TsString(),
            _ when underlying == typeof(int) => new TsNumber(),
            _ when underlying == typeof(long) => new TsNumber(),
            _ when underlying == typeof(short) => new TsNumber(),
            _ when underlying == typeof(byte) => new TsNumber(),
            _ when underlying == typeof(float) => new TsNumber(),
            _ when underlying == typeof(double) => new TsNumber(),
            _ when underlying == typeof(decimal) => new TsNumber(),
            _ when underlying == typeof(bool) => new TsBoolean(),
            _ when underlying == typeof(DateTime) => new TsString(),
            _ when underlying == typeof(DateTimeOffset) => new TsString(),
            _ when underlying == typeof(DateOnly) => new TsString(),
            _ when underlying == typeof(TimeOnly) => new TsString(),
            _ when underlying == typeof(TimeSpan) => new TsString(),
            _ when underlying == typeof(Guid) => new TsString(),
            _ when underlying == typeof(Uri) => new TsString(),
            _ when underlying.IsArray => new TsArray(MapToTsType(underlying.GetElementType()!)),
            _ when underlying.IsEnum => new TsRef(underlying.Name),
            _ when IsCollection(underlying, out var elementType) => new TsArray(MapToTsType(elementType)),
            _ when IsDictionary(underlying, out var keyType, out var valueType) =>
                new TsRecord(MapToTsType(keyType), MapToTsType(valueType)),
            _ when underlying.IsClass || underlying.IsValueType => new TsRef(underlying.Name),
            _ => new TsUnknown()
        };

        if (isNullable)
        {
            return new TsUnion(new List<TsType> { baseType, new TsNull() });
        }

        return baseType;
    }

    /// <summary>
    /// Map a C# type to Zod type AST.
    /// </summary>
    public ZodType MapToZodType(Type type)
    {
        var underlying = UnwrapNullable(type);
        var isNullable = underlying != type;

        ZodType baseType = underlying switch
        {
            _ when underlying == typeof(string) => new ZodString(),
            _ when underlying == typeof(char) => new ZodString(new ZodValidator[] { new ZodLength(1) }),
            _ when underlying == typeof(int) => new ZodNumber(new ZodValidator[] { new ZodInt() }),
            _ when underlying == typeof(long) => new ZodNumber(new ZodValidator[] { new ZodInt() }),
            _ when underlying == typeof(short) => new ZodNumber(new ZodValidator[] { new ZodInt() }),
            _ when underlying == typeof(byte) => new ZodNumber(new ZodValidator[] { new ZodInt(), new ZodMin(0), new ZodMax(255) }),
            _ when underlying == typeof(float) => new ZodNumber(),
            _ when underlying == typeof(double) => new ZodNumber(),
            _ when underlying == typeof(decimal) => new ZodNumber(),
            _ when underlying == typeof(bool) => new ZodBoolean(),
            _ when underlying == typeof(DateTime) => new ZodString(new ZodValidator[] { new ZodDatetime() }),
            _ when underlying == typeof(DateTimeOffset) => new ZodString(new ZodValidator[] { new ZodDatetime() }),
            _ when underlying == typeof(DateOnly) => new ZodString(),
            _ when underlying == typeof(TimeOnly) => new ZodString(),
            _ when underlying == typeof(TimeSpan) => new ZodString(),
            _ when underlying == typeof(Guid) => new ZodString(new ZodValidator[] { new ZodUuid() }),
            _ when underlying == typeof(Uri) => new ZodString(new ZodValidator[] { new ZodUrl() }),
            _ when underlying.IsArray => new ZodArray(MapToZodType(underlying.GetElementType()!)),
            _ when underlying.IsEnum => new ZodNativeEnum(underlying.Name),
            _ when IsCollection(underlying, out var elementType) => new ZodArray(MapToZodType(elementType)),
            _ when IsDictionary(underlying, out var keyType, out var valueType) =>
                new ZodRecord(MapToZodType(keyType), MapToZodType(valueType)),
            _ when underlying.IsClass || underlying.IsValueType => new ZodRef($"{underlying.Name}Schema"),
            _ => new ZodUnknown()
        };

        if (isNullable)
        {
            return new ZodNullable(baseType);
        }

        return baseType;
    }

    private static bool ShouldAnalyze(Type type)
    {
        if (type.IsPrimitive) return false;
        if (type == typeof(string)) return false;
        if (type == typeof(object)) return false;
        if (type == typeof(decimal)) return false;
        if (type == typeof(DateTime)) return false;
        if (type == typeof(DateTimeOffset)) return false;
        if (type == typeof(Guid)) return false;
        if (type == typeof(Uri)) return false;
        if (type == typeof(TimeSpan)) return false;
        if (type.Namespace?.StartsWith("System") == true && !type.IsEnum) return false;

        return true;
    }

    private static Type UnwrapNullable(Type type)
    {
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    private static Type UnwrapCollection(Type type)
    {
        if (type.IsArray) return type.GetElementType()!;
        if (IsCollection(type, out var elementType)) return elementType;
        return type;
    }

    private static bool IsCollection(Type type, out Type elementType)
    {
        elementType = typeof(object);

        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();

            if (genericDef == typeof(List<>) ||
                genericDef == typeof(IList<>) ||
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IEnumerable<>) ||
                genericDef == typeof(IReadOnlyList<>) ||
                genericDef == typeof(IReadOnlyCollection<>) ||
                genericDef == typeof(HashSet<>) ||
                genericDef == typeof(ISet<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }
        }

        return false;
    }

    private static bool IsDictionary(Type type, out Type keyType, out Type valueType)
    {
        keyType = typeof(string);
        valueType = typeof(object);

        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();

            if (genericDef == typeof(Dictionary<,>) ||
                genericDef == typeof(IDictionary<,>) ||
                genericDef == typeof(IReadOnlyDictionary<,>))
            {
                var args = type.GetGenericArguments();
                keyType = args[0];
                valueType = args[1];
                return true;
            }
        }

        return false;
    }

    private static bool IsNullable(Type type)
    {
        return Nullable.GetUnderlyingType(type) != null ||
               (!type.IsValueType && type != typeof(string));
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}

/// <summary>
/// Extension methods for building TypeScript AST from C# types.
/// </summary>
public static class TypeAnalyzerExtensions
{
    /// <summary>
    /// Get all types referenced by the given root types (recursive).
    /// </summary>
    public static IEnumerable<Type> GetAllReferencedTypes(this IEnumerable<Type> rootTypes)
    {
        var visited = new HashSet<Type>();
        var queue = new Queue<Type>(rootTypes);

        while (queue.Count > 0)
        {
            var type = queue.Dequeue();
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
                type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
                type == typeof(Guid) || type == typeof(object))
            {
                continue;
            }

            if (type.IsArray)
            {
                queue.Enqueue(type.GetElementType()!);
                continue;
            }

            if (type.IsGenericType)
            {
                foreach (var arg in type.GetGenericArguments())
                {
                    queue.Enqueue(arg);
                }
                continue;
            }

            if (!visited.Add(type)) continue;

            yield return type;

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                queue.Enqueue(prop.PropertyType);
            }
        }
    }
}
