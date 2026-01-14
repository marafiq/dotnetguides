using Moj.TanStack.Ast.Core;

namespace Moj.TanStack.Dsl.Core;

/// <summary>
/// Static factory for creating TypeScript expressions with fluent API
/// </summary>
public static class Ts
{
    #region Literals

    /// <summary>String literal</summary>
    public static TsExpr<string> String(string value) =>
        new TsTypedExpr<string>(new TsLiteral(value, TsLiteralKind.String));

    /// <summary>Number literal</summary>
    public static TsExpr<double> Number(double value) =>
        new TsTypedExpr<double>(new TsLiteral(value, TsLiteralKind.Number));

    /// <summary>Integer literal</summary>
    public static TsExpr<int> Int(int value) =>
        new TsTypedExpr<int>(new TsLiteral(value, TsLiteralKind.Number));

    /// <summary>Boolean literal</summary>
    public static TsExpr<bool> Bool(bool value) =>
        new TsTypedExpr<bool>(new TsLiteral(value, TsLiteralKind.Boolean));

    /// <summary>True literal</summary>
    public static TsExpr<bool> True => Bool(true);

    /// <summary>False literal</summary>
    public static TsExpr<bool> False => Bool(false);

    /// <summary>Null literal</summary>
    public static TsDynamicExpr Null =>
        new(new TsLiteral(null, TsLiteralKind.Null));

    /// <summary>Undefined literal</summary>
    public static TsDynamicExpr Undefined =>
        new(new TsLiteral(null, TsLiteralKind.Undefined));

    #endregion

    #region Identifiers & Variables

    /// <summary>Reference a variable by name</summary>
    public static TsExpr<T> Var<T>(string name) =>
        new TsTypedExpr<T>(new TsIdentifier(name));

    /// <summary>Reference a variable (untyped)</summary>
    public static TsDynamicExpr Var(string name) =>
        new(new TsIdentifier(name));

    /// <summary>Reference 'this'</summary>
    public static TsDynamicExpr This =>
        new(new TsIdentifier("this"));

    #endregion

    #region Objects & Arrays

    /// <summary>Empty object literal {}</summary>
    public static TsObjectBuilder<T> Object<T>() => new();

    /// <summary>Empty object literal (untyped)</summary>
    public static TsObjectBuilder<object> Object() => new();

    /// <summary>Array literal</summary>
    public static TsExpr<T[]> Array<T>(params TsExpr<T>[] elements) =>
        new TsTypedExpr<T[]>(new TsArrayLiteral(
            elements.Select(e => (TsExpression?)e.Node).ToList()));

    /// <summary>Array literal (untyped elements)</summary>
    public static TsDynamicExpr Array(params TsExprBase[] elements) =>
        new(new TsArrayLiteral(elements.Select(e => (TsExpression?)e.Node).ToList()));

    /// <summary>Empty array</summary>
    public static TsExpr<T[]> EmptyArray<T>() =>
        new TsTypedExpr<T[]>(new TsArrayLiteral([]));

    #endregion

    #region Functions & Lambdas

    /// <summary>Arrow function with no parameters</summary>
    public static TsArrowBuilder<TResult> Arrow<TResult>() =>
        new();

    /// <summary>Arrow function with one parameter</summary>
    public static TsArrowBuilder<T1, TResult> Arrow<T1, TResult>(string p1) =>
        new(p1);

    /// <summary>Arrow function with two parameters</summary>
    public static TsArrowBuilder<T1, T2, TResult> Arrow<T1, T2, TResult>(string p1, string p2) =>
        new(p1, p2);

    /// <summary>Arrow function with three parameters</summary>
    public static TsArrowBuilder<T1, T2, T3, TResult> Arrow<T1, T2, T3, TResult>(string p1, string p2, string p3) =>
        new(p1, p2, p3);

    /// <summary>Async arrow function</summary>
    public static TsAsyncArrowBuilder<TResult> AsyncArrow<TResult>() =>
        new();

    /// <summary>Async arrow function with one parameter</summary>
    public static TsAsyncArrowBuilder<T1, TResult> AsyncArrow<T1, TResult>(string p1) =>
        new(p1);

    #endregion

    #region Operators

    /// <summary>Await expression</summary>
    public static TsExpr<T> Await<T>(TsExpr<Task<T>> promise) =>
        new TsTypedExpr<T>(new TsAwaitExpression(promise.Node));

    /// <summary>Await expression (untyped)</summary>
    public static TsDynamicExpr Await(TsExprBase promise) =>
        new(new TsAwaitExpression(promise.Node));

    /// <summary>Negation: !expr</summary>
    public static TsExpr<bool> Not(TsExpr<bool> expr) =>
        new TsTypedExpr<bool>(new TsUnaryExpression(TsUnaryOperator.Not, expr.Node));

    /// <summary>Typeof: typeof expr</summary>
    public static TsExpr<string> TypeOf(TsExprBase expr) =>
        new TsTypedExpr<string>(new TsUnaryExpression(TsUnaryOperator.TypeOf, expr.Node));

    /// <summary>Spread: ...expr</summary>
    public static TsSpreadExpr Spread(TsExprBase expr) =>
        new(expr.Node);

    /// <summary>Conditional: cond ? then : else</summary>
    public static TsExpr<T> Ternary<T>(TsExpr<bool> condition, TsExpr<T> whenTrue, TsExpr<T> whenFalse) =>
        new TsTypedExpr<T>(new TsConditionalExpression(condition.Node, whenTrue.Node, whenFalse.Node));

    /// <summary>New expression: new Class(...args)</summary>
    public static TsExpr<T> New<T>(string className, params TsExprBase[] args) =>
        new TsTypedExpr<T>(new TsNewExpression(
            new TsIdentifier(className),
            args.Select(a => a.Node).ToList()));

    /// <summary>New with type arguments</summary>
    public static TsExpr<T> NewGeneric<T>(string className, Type[] typeArgs, params TsExprBase[] args) =>
        new TsTypedExpr<T>(new TsNewExpression(
            new TsIdentifier(className),
            args.Select(a => a.Node).ToList(),
            typeArgs.Select(TsTypeMapper.MapType).ToList()));

    #endregion

    #region Template Literals

    /// <summary>Template literal</summary>
    public static TsExpr<string> Template(params object[] parts)
    {
        var quasis = new List<string>();
        var expressions = new List<TsExpression>();
        var currentQuasi = new System.Text.StringBuilder();

        foreach (var part in parts)
        {
            if (part is string s)
            {
                currentQuasi.Append(s);
            }
            else if (part is TsExprBase expr)
            {
                quasis.Add(currentQuasi.ToString());
                currentQuasi.Clear();
                expressions.Add(expr.Node);
            }
            else
            {
                currentQuasi.Append(part?.ToString() ?? "");
            }
        }

        quasis.Add(currentQuasi.ToString());

        return new TsTypedExpr<string>(new TsTemplateLiteral(quasis, expressions));
    }

    #endregion

    #region Imports & Exports

    /// <summary>Import builder</summary>
    public static TsImportBuilder Import(string module) => new(module);

    /// <summary>Import everything as namespace</summary>
    public static TsImportBuilder ImportAll(string module, string asName) =>
        new TsImportBuilder(module).All(asName);

    #endregion

    #region Types

    /// <summary>Create a type reference</summary>
    public static TsType Type<T>() => TsTypeMapper.MapType(typeof(T));

    /// <summary>Create a type reference by name</summary>
    public static TsType TypeRef(string name) => new TsTypeReference(name);

    /// <summary>Create a generic type reference</summary>
    public static TsType GenericType(string name, params TsType[] typeArgs) =>
        new TsGenericType(new TsTypeReference(name), typeArgs.ToList());

    /// <summary>Create a union type</summary>
    public static TsType Union(params TsType[] types) => new TsUnionType(types.ToList());

    /// <summary>Create an intersection type</summary>
    public static TsType Intersection(params TsType[] types) => new TsIntersectionType(types.ToList());

    #endregion

    #region Declarations

    /// <summary>Const declaration</summary>
    public static TsConstBuilder<T> Const<T>(string name) => new(name);

    /// <summary>Let declaration</summary>
    public static TsLetBuilder<T> Let<T>(string name) => new(name);

    /// <summary>Interface declaration</summary>
    public static TsInterfaceBuilder Interface(string name) => new(name);

    /// <summary>Type alias declaration</summary>
    public static TsTypeAliasBuilder TypeAlias(string name) => new(name);

    /// <summary>Function declaration</summary>
    public static TsFunctionBuilder Function(string name) => new(name);

    #endregion
}

/// <summary>
/// Spread expression wrapper
/// </summary>
public sealed class TsSpreadExpr : TsExprBase
{
    public TsSpreadExpr(TsExpression inner) : base(inner) { }

    public TsSpreadElement ToSpreadElement() => new(Node);
}
