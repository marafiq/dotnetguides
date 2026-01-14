using System.Collections;
using System.Reflection;
using System.Text.Json;
using Moj.TanStack.Ast.Core;

namespace Moj.TanStack.Dsl.Core.TypeInference;

/// <summary>
/// Converts C# types to TypeScript AST nodes with full type inference.
/// No manual Ts.String() needed - everything inferred from C# types.
/// </summary>
public static class TypeToTsConverter
{
    private static readonly HashSet<Type> _processedTypes = new();

    /// <summary>
    /// Generate TypeScript interface from a C# type
    /// </summary>
    public static TsInterfaceDeclaration ToInterface(Type type, bool export = true)
    {
        var members = new List<TsTypeMember>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var tsType = ToTsType(prop.PropertyType);
            var isOptional = IsNullable(prop.PropertyType) ||
                             prop.GetCustomAttribute<System.ComponentModel.DataAnnotations.RequiredAttribute>() == null &&
                             !prop.PropertyType.IsValueType;

            members.Add(new TsPropertySignature(
                ToCamelCase(prop.Name),
                tsType,
                isOptional,
                prop.CanWrite == false // readonly if no setter
            ));
        }

        return new TsInterfaceDeclaration(type.Name, members, IsExported: export);
    }

    /// <summary>
    /// Generate TypeScript type from C# type
    /// </summary>
    public static TsType ToTsType(Type type)
    {
        // Handle nullable
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
        {
            return new TsUnionType([ToTsType(underlying), TsPrimitiveTypes.Null]);
        }

        // Primitives
        if (type == typeof(string)) return TsPrimitiveTypes.String;
        if (type == typeof(bool)) return TsPrimitiveTypes.Boolean;
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) ||
            type == typeof(byte) || type == typeof(float) || type == typeof(double) ||
            type == typeof(decimal)) return TsPrimitiveTypes.Number;
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly))
            return TsPrimitiveTypes.String; // ISO string
        if (type == typeof(Guid)) return TsPrimitiveTypes.String;
        if (type == typeof(object)) return TsPrimitiveTypes.Unknown;

        // Enums -> union of literal strings
        if (type.IsEnum)
        {
            var values = Enum.GetNames(type)
                .Select(name => new TsLiteralType(new TsLiteral(ToCamelCase(name), TsLiteralKind.String)))
                .Cast<TsType>()
                .ToList();
            return new TsUnionType(values);
        }

        // Arrays and collections
        if (type.IsArray)
        {
            return new TsArrayType(ToTsType(type.GetElementType()!));
        }

        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            var args = type.GetGenericArguments();

            // List, IEnumerable, etc -> array
            if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {
                return new TsArrayType(ToTsType(args[0]));
            }

            // Dictionary -> Record<K, V>
            if (genericDef == typeof(Dictionary<,>) || genericDef == typeof(IDictionary<,>))
            {
                return new TsGenericType(
                    new TsTypeReference("Record"),
                    [ToTsType(args[0]), ToTsType(args[1])]);
            }

            // Task/ValueTask -> Promise
            if (genericDef == typeof(Task<>) || genericDef == typeof(ValueTask<>))
            {
                return new TsGenericType(
                    new TsTypeReference("Promise"),
                    [ToTsType(args[0])]);
            }
        }

        // Task without type -> Promise<void>
        if (type == typeof(Task) || type == typeof(ValueTask))
        {
            return new TsGenericType(new TsTypeReference("Promise"), [TsPrimitiveTypes.Void]);
        }

        // Complex type -> reference to interface (will be generated separately)
        return new TsTypeReference(type.Name);
    }

    /// <summary>
    /// Convert a C# object instance to TypeScript object literal expression
    /// </summary>
    public static TsExpression ToObjectLiteral(object? value, Type type)
    {
        if (value == null)
            return new TsLiteral(null, TsLiteralKind.Null);

        // Primitives
        if (type == typeof(string))
            return new TsLiteral(value, TsLiteralKind.String);
        if (type == typeof(bool))
            return new TsLiteral(value, TsLiteralKind.Boolean);
        if (type == typeof(int) || type == typeof(long) || type == typeof(double) ||
            type == typeof(float) || type == typeof(decimal))
            return new TsLiteral(value, TsLiteralKind.Number);
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            return new TsLiteral(((IFormattable)value).ToString("O", null), TsLiteralKind.String);
        if (type == typeof(Guid))
            return new TsLiteral(value.ToString(), TsLiteralKind.String);

        // Enum -> string literal
        if (type.IsEnum)
            return new TsLiteral(ToCamelCase(value.ToString()!), TsLiteralKind.String);

        // Array/List -> array literal
        if (value is IEnumerable enumerable && type != typeof(string))
        {
            var elementType = type.IsArray
                ? type.GetElementType()!
                : type.GetGenericArguments().FirstOrDefault() ?? typeof(object);

            var elements = new List<TsExpression?>();
            foreach (var item in enumerable)
            {
                elements.Add(ToObjectLiteral(item, elementType));
            }
            return new TsArrayLiteral(elements);
        }

        // Complex object -> object literal
        var properties = new List<TsObjectElement>();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead) continue;

            var propValue = prop.GetValue(value);
            var propExpr = ToObjectLiteral(propValue, prop.PropertyType);

            properties.Add(new TsPropertyAssignment(
                new TsIdentifier(ToCamelCase(prop.Name)),
                propExpr));
        }

        return new TsObjectLiteral(properties);
    }

    /// <summary>
    /// Get all types that need interfaces generated (walks the type graph)
    /// </summary>
    public static IEnumerable<Type> GetDependentTypes(Type rootType)
    {
        var types = new HashSet<Type>();
        CollectTypes(rootType, types);
        return types.Where(t => !IsPrimitive(t) && !t.IsEnum && t != rootType);
    }

    private static void CollectTypes(Type type, HashSet<Type> collected)
    {
        // Skip primitives and already processed
        if (IsPrimitive(type) || collected.Contains(type))
            return;

        // Handle nullable
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
        {
            CollectTypes(underlying, collected);
            return;
        }

        // Handle arrays
        if (type.IsArray)
        {
            CollectTypes(type.GetElementType()!, collected);
            return;
        }

        // Handle generics (List<T>, Dictionary<K,V>, etc)
        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
            {
                CollectTypes(arg, collected);
            }
            return;
        }

        // Skip enums (they become union types, not interfaces)
        if (type.IsEnum)
            return;

        // Add this type
        if (type.IsClass || type.IsValueType && !type.IsPrimitive)
        {
            collected.Add(type);

            // Recursively process properties
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                CollectTypes(prop.PropertyType, collected);
            }
        }
    }

    private static bool IsPrimitive(Type type)
    {
        return type == typeof(string) ||
               type == typeof(bool) ||
               type == typeof(int) || type == typeof(long) || type == typeof(short) ||
               type == typeof(byte) || type == typeof(float) || type == typeof(double) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
               type == typeof(Guid) ||
               type == typeof(object);
    }

    private static bool IsNullable(Type type)
    {
        return Nullable.GetUnderlyingType(type) != null ||
               (!type.IsValueType && type != typeof(string));
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (name.Length == 1) return name.ToLowerInvariant();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}

/// <summary>
/// Extension to generate TypeScript enum from C# enum
/// </summary>
public static class EnumToTsConverter
{
    public static TsTypeAliasDeclaration ToTsEnum(Type enumType, bool export = true)
    {
        if (!enumType.IsEnum)
            throw new ArgumentException($"{enumType.Name} is not an enum");

        var values = Enum.GetNames(enumType)
            .Select(name => new TsLiteralType(new TsLiteral(ToCamelCase(name), TsLiteralKind.String)))
            .Cast<TsType>()
            .ToList();

        return new TsTypeAliasDeclaration(
            enumType.Name,
            new TsUnionType(values),
            IsExported: export);
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (name.Length == 1) return name.ToLowerInvariant();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
