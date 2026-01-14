using Moj.TanStack.Ast.Core;

namespace Moj.TanStack.Dsl.Core;

/// <summary>
/// Base class for all TypeScript expression builders.
/// Provides fluent API for building TypeScript expressions with type safety.
/// </summary>
/// <typeparam name="T">The .NET type that this expression represents</typeparam>
public abstract class TsExpr<T> : TsExprBase
{
    protected TsExpr(TsExpression node) : base(node) { }

    /// <summary>
    /// Cast this expression to a different type
    /// </summary>
    public TsExpr<TNew> As<TNew>() => new TsTypedExpr<TNew>(
        new TsAsExpression(Node, TsTypeMapper.MapType(typeof(TNew))));

    /// <summary>
    /// Property access: expr.property
    /// </summary>
    public TsExpr<TProp> Prop<TProp>(string name) =>
        new TsTypedExpr<TProp>(new TsMemberAccess(Node, name));

    /// <summary>
    /// Index access: expr[index]
    /// </summary>
    public TsExpr<TElement> Index<TElement>(TsExpr<int> index) =>
        new TsTypedExpr<TElement>(new TsIndexAccess(Node, index.Node));

    /// <summary>
    /// Index access with string key: expr[key]
    /// </summary>
    public TsExpr<TValue> Index<TValue>(TsExpr<string> key) =>
        new TsTypedExpr<TValue>(new TsIndexAccess(Node, key.Node));

    /// <summary>
    /// Call this expression as a function
    /// </summary>
    public TsExpr<TResult> Call<TResult>(params TsExprBase[] args) =>
        new TsTypedExpr<TResult>(new TsCallExpression(
            Node,
            args.Select(a => a.Node).ToList()));

    /// <summary>
    /// Call with type arguments
    /// </summary>
    public TsExpr<TResult> CallGeneric<TResult>(Type[] typeArgs, params TsExprBase[] args) =>
        new TsTypedExpr<TResult>(new TsCallExpression(
            Node,
            args.Select(a => a.Node).ToList(),
            typeArgs.Select(TsTypeMapper.MapType).ToList()));
}

/// <summary>
/// Base class for all expression builders (untyped)
/// </summary>
public abstract class TsExprBase
{
    public TsExpression Node { get; }

    protected TsExprBase(TsExpression node)
    {
        Node = node;
    }

    /// <summary>
    /// Implicit conversion to AST node
    /// </summary>
    public static implicit operator TsExpression(TsExprBase expr) => expr.Node;
}

/// <summary>
/// Concrete typed expression wrapper
/// </summary>
internal sealed class TsTypedExpr<T> : TsExpr<T>
{
    public TsTypedExpr(TsExpression node) : base(node) { }
}

/// <summary>
/// Untyped/dynamic expression for advanced scenarios
/// </summary>
public sealed class TsDynamicExpr : TsExprBase
{
    public TsDynamicExpr(TsExpression node) : base(node) { }

    public TsDynamicExpr Prop(string name) =>
        new(new TsMemberAccess(Node, name));

    public TsDynamicExpr Index(TsExprBase index) =>
        new(new TsIndexAccess(Node, index.Node));

    public TsDynamicExpr Call(params TsExprBase[] args) =>
        new(new TsCallExpression(Node, args.Select(a => a.Node).ToList()));

    public TsExpr<T> Typed<T>() => new TsTypedExpr<T>(Node);
}

/// <summary>
/// Maps .NET types to TypeScript types
/// </summary>
public static class TsTypeMapper
{
    public static TsType MapType(Type type)
    {
        // Handle nullable
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
        {
            return new TsUnionType([MapType(underlying), TsPrimitiveTypes.Null]);
        }

        // Primitives
        if (type == typeof(string)) return TsPrimitiveTypes.String;
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) ||
            type == typeof(byte) || type == typeof(float) || type == typeof(double) ||
            type == typeof(decimal)) return TsPrimitiveTypes.Number;
        if (type == typeof(bool)) return TsPrimitiveTypes.Boolean;
        if (type == typeof(void)) return TsPrimitiveTypes.Void;
        if (type == typeof(object)) return TsPrimitiveTypes.Unknown;

        // Arrays and collections
        if (type.IsArray)
        {
            return new TsArrayType(MapType(type.GetElementType()!));
        }

        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            var args = type.GetGenericArguments();

            // List, IEnumerable, ICollection -> array
            if (genericDef == typeof(List<>) ||
                genericDef == typeof(IList<>) ||
                genericDef == typeof(IEnumerable<>) ||
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IReadOnlyList<>) ||
                genericDef == typeof(IReadOnlyCollection<>))
            {
                return new TsArrayType(MapType(args[0]));
            }

            // Dictionary -> Record
            if (genericDef == typeof(Dictionary<,>) ||
                genericDef == typeof(IDictionary<,>))
            {
                return new TsGenericType(
                    new TsTypeReference("Record"),
                    [MapType(args[0]), MapType(args[1])]);
            }

            // Task/ValueTask -> Promise
            if (genericDef == typeof(Task<>) || genericDef == typeof(ValueTask<>))
            {
                return new TsGenericType(
                    new TsTypeReference("Promise"),
                    [MapType(args[0])]);
            }

            // Func -> function type
            if (genericDef.Name.StartsWith("Func`"))
            {
                var returnType = MapType(args[^1]);
                var paramTypes = args.Take(args.Length - 1)
                    .Select((t, i) => new TsParameter(
                        new TsIdentifierBinding($"arg{i}"),
                        MapType(t)))
                    .ToList();
                return new TsFunctionType(paramTypes, returnType);
            }

            // Action -> void function type
            if (genericDef.Name.StartsWith("Action`"))
            {
                var paramTypes = args
                    .Select((t, i) => new TsParameter(
                        new TsIdentifierBinding($"arg{i}"),
                        MapType(t)))
                    .ToList();
                return new TsFunctionType(paramTypes, TsPrimitiveTypes.Void);
            }

            // Generic type with type parameters
            var baseName = type.Name.Split('`')[0];
            return new TsGenericType(
                new TsTypeReference(baseName),
                args.Select(MapType).ToList());
        }

        // Task without type argument -> Promise<void>
        if (type == typeof(Task) || type == typeof(ValueTask))
        {
            return new TsGenericType(
                new TsTypeReference("Promise"),
                [TsPrimitiveTypes.Void]);
        }

        // Action without type arguments -> () => void
        if (type == typeof(Action))
        {
            return new TsFunctionType([], TsPrimitiveTypes.Void);
        }

        // Use type name as-is for other types
        return new TsTypeReference(type.Name);
    }
}
