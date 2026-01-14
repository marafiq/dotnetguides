using Moj.TanStack.Ast.Core;

namespace Moj.TanStack.Dsl.Core;

/// <summary>
/// Extension methods for building binary and unary expressions
/// </summary>
public static class TsOperators
{
    #region Arithmetic Operators

    public static TsExpr<double> Add(this TsExpr<double> left, TsExpr<double> right) =>
        new TsTypedExpr<double>(new TsBinaryExpression(left.Node, TsBinaryOperator.Add, right.Node));

    public static TsExpr<int> Add(this TsExpr<int> left, TsExpr<int> right) =>
        new TsTypedExpr<int>(new TsBinaryExpression(left.Node, TsBinaryOperator.Add, right.Node));

    public static TsExpr<string> Add(this TsExpr<string> left, TsExpr<string> right) =>
        new TsTypedExpr<string>(new TsBinaryExpression(left.Node, TsBinaryOperator.Add, right.Node));

    public static TsExpr<double> Subtract(this TsExpr<double> left, TsExpr<double> right) =>
        new TsTypedExpr<double>(new TsBinaryExpression(left.Node, TsBinaryOperator.Subtract, right.Node));

    public static TsExpr<int> Subtract(this TsExpr<int> left, TsExpr<int> right) =>
        new TsTypedExpr<int>(new TsBinaryExpression(left.Node, TsBinaryOperator.Subtract, right.Node));

    public static TsExpr<double> Multiply(this TsExpr<double> left, TsExpr<double> right) =>
        new TsTypedExpr<double>(new TsBinaryExpression(left.Node, TsBinaryOperator.Multiply, right.Node));

    public static TsExpr<int> Multiply(this TsExpr<int> left, TsExpr<int> right) =>
        new TsTypedExpr<int>(new TsBinaryExpression(left.Node, TsBinaryOperator.Multiply, right.Node));

    public static TsExpr<double> Divide(this TsExpr<double> left, TsExpr<double> right) =>
        new TsTypedExpr<double>(new TsBinaryExpression(left.Node, TsBinaryOperator.Divide, right.Node));

    public static TsExpr<double> Modulo(this TsExpr<double> left, TsExpr<double> right) =>
        new TsTypedExpr<double>(new TsBinaryExpression(left.Node, TsBinaryOperator.Modulo, right.Node));

    #endregion

    #region Comparison Operators

    public static TsExpr<bool> Eq(this TsExprBase left, TsExprBase right) =>
        new TsTypedExpr<bool>(new TsBinaryExpression(left.Node, TsBinaryOperator.StrictEqual, right.Node));

    public static TsExpr<bool> NotEq(this TsExprBase left, TsExprBase right) =>
        new TsTypedExpr<bool>(new TsBinaryExpression(left.Node, TsBinaryOperator.StrictNotEqual, right.Node));

    public static TsExpr<bool> Lt(this TsExpr<double> left, TsExpr<double> right) =>
        new TsTypedExpr<bool>(new TsBinaryExpression(left.Node, TsBinaryOperator.LessThan, right.Node));

    public static TsExpr<bool> Lt(this TsExpr<int> left, TsExpr<int> right) =>
        new TsTypedExpr<bool>(new TsBinaryExpression(left.Node, TsBinaryOperator.LessThan, right.Node));

    public static TsExpr<bool> Lte(this TsExpr<double> left, TsExpr<double> right) =>
        new TsTypedExpr<bool>(new TsBinaryExpression(left.Node, TsBinaryOperator.LessThanOrEqual, right.Node));

    public static TsExpr<bool> Gt(this TsExpr<double> left, TsExpr<double> right) =>
        new TsTypedExpr<bool>(new TsBinaryExpression(left.Node, TsBinaryOperator.GreaterThan, right.Node));

    public static TsExpr<bool> Gt(this TsExpr<int> left, TsExpr<int> right) =>
        new TsTypedExpr<bool>(new TsBinaryExpression(left.Node, TsBinaryOperator.GreaterThan, right.Node));

    public static TsExpr<bool> Gte(this TsExpr<double> left, TsExpr<double> right) =>
        new TsTypedExpr<bool>(new TsBinaryExpression(left.Node, TsBinaryOperator.GreaterThanOrEqual, right.Node));

    #endregion

    #region Logical Operators

    public static TsExpr<bool> And(this TsExpr<bool> left, TsExpr<bool> right) =>
        new TsTypedExpr<bool>(new TsBinaryExpression(left.Node, TsBinaryOperator.And, right.Node));

    public static TsExpr<bool> Or(this TsExpr<bool> left, TsExpr<bool> right) =>
        new TsTypedExpr<bool>(new TsBinaryExpression(left.Node, TsBinaryOperator.Or, right.Node));

    public static TsExpr<bool> Not(this TsExpr<bool> expr) =>
        new TsTypedExpr<bool>(new TsUnaryExpression(TsUnaryOperator.Not, expr.Node));

    #endregion

    #region Nullish Operators

    /// <summary>Nullish coalescing: left ?? right</summary>
    public static TsExpr<T> NullishCoalesce<T>(this TsExpr<T> left, TsExpr<T> right) =>
        new TsTypedExpr<T>(new TsBinaryExpression(left.Node, TsBinaryOperator.NullishCoalesce, right.Node));

    /// <summary>Optional chain property access: obj?.prop</summary>
    public static TsExpr<TProp> OptionalProp<T, TProp>(this TsExpr<T> obj, string prop) =>
        new TsTypedExpr<TProp>(new TsOptionalChain(obj.Node, new TsIdentifier(prop)));

    #endregion

    #region Assignment Operators

    public static TsDynamicExpr Assign(this TsDynamicExpr target, TsExprBase value) =>
        new(new TsBinaryExpression(target.Node, TsBinaryOperator.Assign, value.Node));

    public static TsExpr<T> Assign<T>(this TsExpr<T> target, TsExpr<T> value) =>
        new TsTypedExpr<T>(new TsBinaryExpression(target.Node, TsBinaryOperator.Assign, value.Node));

    public static TsDynamicExpr AddAssign(this TsDynamicExpr target, TsExprBase value) =>
        new(new TsBinaryExpression(target.Node, TsBinaryOperator.AddAssign, value.Node));

    #endregion

    #region Member Access

    /// <summary>Property access with fluent API</summary>
    public static TsDynamicExpr Dot(this TsExprBase obj, string prop) =>
        new(new TsMemberAccess(obj.Node, prop));

    /// <summary>Bracket access with fluent API</summary>
    public static TsDynamicExpr Bracket(this TsExprBase obj, TsExprBase index) =>
        new(new TsIndexAccess(obj.Node, index.Node));

    /// <summary>Call as method</summary>
    public static TsDynamicExpr Invoke(this TsExprBase callee, params TsExprBase[] args) =>
        new(new TsCallExpression(callee.Node, args.Select(a => a.Node).ToList()));

    /// <summary>Call with type arguments</summary>
    public static TsDynamicExpr InvokeGeneric(this TsExprBase callee, TsType[] typeArgs, params TsExprBase[] args) =>
        new(new TsCallExpression(
            callee.Node,
            args.Select(a => a.Node).ToList(),
            typeArgs.ToList()));

    #endregion

    #region Type Operators

    /// <summary>TypeOf operator</summary>
    public static TsExpr<string> TypeOf(this TsExprBase expr) =>
        new TsTypedExpr<string>(new TsUnaryExpression(TsUnaryOperator.TypeOf, expr.Node));

    /// <summary>InstanceOf operator</summary>
    public static TsExpr<bool> InstanceOf(this TsExprBase expr, TsExprBase type) =>
        new TsTypedExpr<bool>(new TsBinaryExpression(expr.Node, TsBinaryOperator.InstanceOf, type.Node));

    /// <summary>In operator</summary>
    public static TsExpr<bool> In(this TsExpr<string> key, TsExprBase obj) =>
        new TsTypedExpr<bool>(new TsBinaryExpression(key.Node, TsBinaryOperator.In, obj.Node));

    #endregion

    #region Increment/Decrement

    public static TsExpr<int> Increment(this TsExpr<int> expr, bool prefix = true) =>
        new TsTypedExpr<int>(new TsUnaryExpression(TsUnaryOperator.Increment, expr.Node, prefix));

    public static TsExpr<int> Decrement(this TsExpr<int> expr, bool prefix = true) =>
        new TsTypedExpr<int>(new TsUnaryExpression(TsUnaryOperator.Decrement, expr.Node, prefix));

    #endregion
}
