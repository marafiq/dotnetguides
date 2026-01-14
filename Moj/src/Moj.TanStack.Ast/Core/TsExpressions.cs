namespace Moj.TanStack.Ast.Core;

/// <summary>
/// Base class for expressions
/// </summary>
public abstract record TsExpression : TsNode;

/// <summary>
/// Identifier (variable name, function name, etc.)
/// </summary>
public sealed record TsIdentifier(string Name) : TsExpression
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Literal values (string, number, boolean, null, undefined)
/// </summary>
public sealed record TsLiteral(object? Value, TsLiteralKind Kind) : TsExpression
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

public enum TsLiteralKind
{
    String,
    Number,
    Boolean,
    Null,
    Undefined,
    BigInt,
    Regex
}

/// <summary>
/// Template literal `hello ${name}`
/// </summary>
public sealed record TsTemplateLiteral(
    IReadOnlyList<string> Quasis,
    IReadOnlyList<TsExpression> Expressions
) : TsExpression
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Object literal { key: value }
/// </summary>
public sealed record TsObjectLiteral(IReadOnlyList<TsObjectElement> Properties) : TsExpression
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Base for object elements (properties, spread, methods)
/// </summary>
public abstract record TsObjectElement : TsNode;

/// <summary>
/// Property assignment in object: key: value
/// </summary>
public sealed record TsPropertyAssignment(
    TsExpression Key,
    TsExpression Value,
    bool IsComputed = false,
    bool IsShorthand = false
) : TsObjectElement
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Spread element in object or array: ...expr
/// </summary>
public sealed record TsSpreadElement(TsExpression Expression) : TsObjectElement
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Array literal [a, b, c]
/// </summary>
public sealed record TsArrayLiteral(IReadOnlyList<TsExpression?> Elements) : TsExpression
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Member access expr.property
/// </summary>
public sealed record TsMemberAccess(TsExpression Object, string Property) : TsExpression
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Index/bracket access expr[index]
/// </summary>
public sealed record TsIndexAccess(TsExpression Object, TsExpression Index) : TsExpression
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Function call expr(args)
/// </summary>
public sealed record TsCallExpression(
    TsExpression Callee,
    IReadOnlyList<TsExpression> Arguments,
    IReadOnlyList<TsType>? TypeArguments = null
) : TsExpression
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// New expression: new Class(args)
/// </summary>
public sealed record TsNewExpression(
    TsExpression Callee,
    IReadOnlyList<TsExpression> Arguments,
    IReadOnlyList<TsType>? TypeArguments = null
) : TsExpression
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Arrow function: (params) => body
/// </summary>
public sealed record TsArrowFunction(
    IReadOnlyList<TsParameter> Parameters,
    TsNode Body, // TsExpression or TsBlockStatement
    bool IsAsync = false,
    IReadOnlyList<TsTypeParameter>? TypeParameters = null,
    TsType? ReturnType = null
) : TsExpression
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Binary expression: left op right
/// </summary>
public sealed record TsBinaryExpression(
    TsExpression Left,
    TsBinaryOperator Operator,
    TsExpression Right
) : TsExpression
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

public enum TsBinaryOperator
{
    // Arithmetic
    Add,           // +
    Subtract,      // -
    Multiply,      // *
    Divide,        // /
    Modulo,        // %
    Power,         // **

    // Comparison
    Equal,         // ==
    StrictEqual,   // ===
    NotEqual,      // !=
    StrictNotEqual,// !==
    LessThan,      // <
    LessThanOrEqual, // <=
    GreaterThan,   // >
    GreaterThanOrEqual, // >=

    // Logical
    And,           // &&
    Or,            // ||
    NullishCoalesce, // ??

    // Bitwise
    BitwiseAnd,    // &
    BitwiseOr,     // |
    BitwiseXor,    // ^
    LeftShift,     // <<
    RightShift,    // >>
    UnsignedRightShift, // >>>

    // Other
    In,            // in
    InstanceOf,    // instanceof

    // Assignment
    Assign,        // =
    AddAssign,     // +=
    SubtractAssign,// -=
    MultiplyAssign,// *=
    DivideAssign,  // /=
    ModuloAssign,  // %=
    AndAssign,     // &&=
    OrAssign,      // ||=
    NullishAssign  // ??=
}

/// <summary>
/// Unary expression: op expr or expr op
/// </summary>
public sealed record TsUnaryExpression(
    TsUnaryOperator Operator,
    TsExpression Operand,
    bool IsPrefix = true
) : TsExpression
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

public enum TsUnaryOperator
{
    Negate,        // -
    Plus,          // +
    Not,           // !
    BitwiseNot,    // ~
    TypeOf,        // typeof
    Void,          // void
    Delete,        // delete
    Increment,     // ++
    Decrement,     // --
    Await,         // await (also has dedicated node)
    Spread         // ...
}

/// <summary>
/// Conditional/ternary expression: cond ? then : else
/// </summary>
public sealed record TsConditionalExpression(
    TsExpression Condition,
    TsExpression WhenTrue,
    TsExpression WhenFalse
) : TsExpression
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Await expression: await expr
/// </summary>
public sealed record TsAwaitExpression(TsExpression Expression) : TsExpression
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Type assertion: expr as Type
/// </summary>
public sealed record TsAsExpression(TsExpression Expression, TsType Type) : TsExpression
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Type assertion (angle bracket): <Type>expr
/// </summary>
public sealed record TsTypeAssertion(TsType Type, TsExpression Expression) : TsExpression
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Optional chaining: expr?.prop or expr?.method()
/// </summary>
public sealed record TsOptionalChain(TsExpression Object, TsExpression Property, bool IsCall = false) : TsExpression
{
    public override void Accept(ITsVisitor visitor) => throw new NotImplementedException();
    public override T Accept<T>(ITsVisitor<T> visitor) => throw new NotImplementedException();
}
