using System.Reflection;
using Impulse.CodeGen.Ast;

namespace Impulse.CodeGen;

/// <summary>
/// Analyzes C# types and converts them to TypeScript AST.
/// </summary>
public class TypeAnalyzer
{
    private readonly HashSet<Type> _visited = new();
    private readonly List<TsInterface> _interfaces = new();
    private readonly List<TsEnum> _enums = new();

    /// <summary>
    /// Map a C# type to its TypeScript equivalent.
    /// </summary>
    public TsType MapType(Type type)
    {
        // Handle nullable value types
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
        {
            return new TsUnion(new TsType[]
            {
                MapType(underlying),
                new TsNull()
            });
        }

        // Primitives
        if (type == typeof(string)) return new TsString();
        if (type == typeof(char)) return new TsString();
        if (type == typeof(int)) return new TsNumber();
        if (type == typeof(long)) return new TsNumber();
        if (type == typeof(short)) return new TsNumber();
        if (type == typeof(byte)) return new TsNumber();
        if (type == typeof(float)) return new TsNumber();
        if (type == typeof(double)) return new TsNumber();
        if (type == typeof(decimal)) return new TsNumber();
        if (type == typeof(bool)) return new TsBoolean();
        if (type == typeof(DateTime)) return new TsString();
        if (type == typeof(DateTimeOffset)) return new TsString();
        if (type == typeof(DateOnly)) return new TsString();
        if (type == typeof(TimeOnly)) return new TsString();
        if (type == typeof(TimeSpan)) return new TsString();
        if (type == typeof(Guid)) return new TsString();
        if (type == typeof(Uri)) return new TsString();

        // Arrays
        if (type.IsArray)
        {
            return new TsArray(MapType(type.GetElementType()!));
        }

        // Enums
        if (type.IsEnum)
        {
            return new TsRef(type.Name);
        }

        // Collections
        if (IsCollection(type, out var elementType))
        {
            return new TsArray(MapType(elementType));
        }

        // Dictionaries
        if (IsDictionary(type, out var keyType, out var valueType))
        {
            return new TsRecord(MapType(keyType), MapType(valueType));
        }

        // Complex types - reference by name
        if (type.IsClass || type.IsValueType)
        {
            return new TsRef(type.Name);
        }

        return new TsUnknown();
    }

    /// <summary>
    /// Analyze an enum type.
    /// </summary>
    public TsEnum AnalyzeEnum(Type type)
    {
        if (!type.IsEnum)
            throw new ArgumentException("Type must be an enum", nameof(type));

        var members = Enum.GetNames(type)
            .Select(name => new TsEnumMember(name, $"'{name}'"))
            .ToList();

        return new TsEnum(type.Name, members);
    }

    /// <summary>
    /// Analyze a complex type (class/record/struct).
    /// </summary>
    public TsInterface AnalyzeType(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .Select(p => new TsProperty(
                ToCamelCase(p.Name),
                MapType(p.PropertyType),
                IsOptional(p)))
            .ToList();

        return new TsInterface(type.Name, properties);
    }

    /// <summary>
    /// Analyze multiple types, returning them in topological order.
    /// Dependencies are returned before types that reference them.
    /// </summary>
    public IReadOnlyList<TsInterface> AnalyzeTypes(IEnumerable<Type> rootTypes)
    {
        _visited.Clear();
        _interfaces.Clear();
        _enums.Clear();

        foreach (var type in rootTypes)
        {
            VisitType(type);
        }

        return _interfaces;
    }

    private void VisitType(Type type)
    {
        // Skip primitives and system types
        if (!ShouldAnalyze(type)) return;

        // Skip already visited (handles circular refs)
        if (!_visited.Add(type)) return;

        // Handle enums
        if (type.IsEnum)
        {
            _enums.Add(AnalyzeEnum(type));
            return;
        }

        // Visit nested types FIRST (topological order)
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propType = GetInnerType(prop.PropertyType);
            VisitType(propType);
        }

        // Add this interface AFTER dependencies
        _interfaces.Add(AnalyzeType(type));
    }

    private Type GetInnerType(Type type)
    {
        // Unwrap nullable
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null) return GetInnerType(underlying);

        // Unwrap arrays
        if (type.IsArray) return GetInnerType(type.GetElementType()!);

        // Unwrap collections
        if (IsCollection(type, out var elementType)) return GetInnerType(elementType);

        // Unwrap dictionaries (value type)
        if (IsDictionary(type, out _, out var valueType)) return GetInnerType(valueType);

        return type;
    }

    private bool ShouldAnalyze(Type type)
    {
        if (type.IsPrimitive) return false;
        if (type == typeof(string)) return false;
        if (type == typeof(decimal)) return false;
        if (type == typeof(DateTime)) return false;
        if (type == typeof(DateTimeOffset)) return false;
        if (type == typeof(Guid)) return false;
        if (type == typeof(object)) return false;
        if (type.Namespace?.StartsWith("System") == true && !type.IsEnum) return false;

        return true;
    }

    private bool IsCollection(Type type, out Type elementType)
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

    private bool IsDictionary(Type type, out Type keyType, out Type valueType)
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

    private bool IsOptional(PropertyInfo prop)
    {
        // Check for nullable reference type
        var nullableContext = prop.DeclaringType?
            .GetCustomAttributes()
            .FirstOrDefault(a => a.GetType().Name == "NullableContextAttribute");

        if (nullableContext != null)
        {
            var nullableAttr = prop.GetCustomAttributes()
                .FirstOrDefault(a => a.GetType().Name == "NullableAttribute");

            if (nullableAttr != null)
            {
                var flagField = nullableAttr.GetType().GetField("NullableFlags");
                if (flagField?.GetValue(nullableAttr) is byte[] flags && flags.Length > 0)
                {
                    return flags[0] == 2; // 2 = nullable
                }
            }
        }

        // Check for nullable value type
        return Nullable.GetUnderlyingType(prop.PropertyType) != null;
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}
