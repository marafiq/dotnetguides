namespace Impulse.SourceGen.Ast;

/// <summary>
/// Base class for all TypeScript AST nodes.
/// Immutable records enable functional transformations via plugins.
/// </summary>
public abstract record TsNode;

/// <summary>
/// Represents a TypeScript file containing multiple statements.
/// </summary>
public record TsFile(IReadOnlyList<TsNode> Statements) : TsNode;

/// <summary>
/// Represents a TypeScript interface declaration.
/// </summary>
public record TsInterface(string Name, IReadOnlyList<TsProperty> Properties) : TsNode;

/// <summary>
/// Represents a property within an interface.
/// </summary>
public record TsProperty(string Name, TsType Type, bool Optional = false);

/// <summary>
/// Represents a TypeScript import statement.
/// </summary>
public record TsImport(IReadOnlyList<string> Names, string From, bool IsType = false) : TsNode;

/// <summary>
/// Represents a TypeScript const declaration.
/// </summary>
public record TsConst(string Name, TsExpression Value, bool AsConst = false) : TsNode;

/// <summary>
/// Represents a TypeScript function declaration.
/// </summary>
public record TsFunction(
    string Name,
    IReadOnlyList<TsParameter> Parameters,
    TsType ReturnType,
    TsExpression Body) : TsNode;

/// <summary>
/// Represents a function parameter.
/// </summary>
public record TsParameter(string Name, TsType Type);

/// <summary>
/// Represents a TypeScript type alias.
/// </summary>
public record TsTypeAlias(string Name, TsType Type) : TsNode;

/// <summary>
/// Represents a TypeScript enum declaration.
/// </summary>
public record TsEnum(string Name, IReadOnlyList<TsEnumMember> Members) : TsNode;

/// <summary>
/// Represents an enum member.
/// </summary>
public record TsEnumMember(string Name, string? Value = null);

// ============================================
// Type System
// ============================================

/// <summary>
/// Base class for TypeScript types.
/// </summary>
public abstract record TsType;

/// <summary>
/// Primitive string type.
/// </summary>
public record TsString() : TsType;

/// <summary>
/// Primitive number type.
/// </summary>
public record TsNumber() : TsType;

/// <summary>
/// Primitive boolean type.
/// </summary>
public record TsBoolean() : TsType;

/// <summary>
/// Null type.
/// </summary>
public record TsNull() : TsType;

/// <summary>
/// Undefined type.
/// </summary>
public record TsUndefined() : TsType;

/// <summary>
/// Unknown type (safer than any).
/// </summary>
public record TsUnknown() : TsType;

/// <summary>
/// Void type (for functions).
/// </summary>
public record TsVoid() : TsType;

/// <summary>
/// Array type with element type.
/// </summary>
public record TsArray(TsType Element) : TsType;

/// <summary>
/// Record/dictionary type with key and value types.
/// </summary>
public record TsRecord(TsType Key, TsType Value) : TsType;

/// <summary>
/// Reference to another type by name.
/// </summary>
public record TsRef(string Name) : TsType;

/// <summary>
/// Generic type reference with type arguments.
/// </summary>
public record TsGeneric(string Name, IReadOnlyList<TsType> TypeArgs) : TsType;

/// <summary>
/// Union type (A | B | C).
/// </summary>
public record TsUnion(IReadOnlyList<TsType> Types) : TsType;

/// <summary>
/// Intersection type (A & B).
/// </summary>
public record TsIntersection(IReadOnlyList<TsType> Types) : TsType;

/// <summary>
/// Literal type (specific value as type).
/// </summary>
public record TsLiteral(string Value) : TsType;

/// <summary>
/// Inline object type.
/// </summary>
public record TsObjectType(IReadOnlyList<TsProperty> Properties) : TsType;

/// <summary>
/// Function type signature.
/// </summary>
public record TsFunctionType(IReadOnlyList<TsParameter> Parameters, TsType ReturnType) : TsType;

/// <summary>
/// Tuple type.
/// </summary>
public record TsTuple(IReadOnlyList<TsType> Elements) : TsType;

// ============================================
// Expressions
// ============================================

/// <summary>
/// Base class for TypeScript expressions.
/// </summary>
public abstract record TsExpression;

/// <summary>
/// String literal expression.
/// </summary>
public record TsStringLiteral(string Value) : TsExpression;

/// <summary>
/// Number literal expression.
/// </summary>
public record TsNumberLiteral(double Value) : TsExpression;

/// <summary>
/// Boolean literal expression.
/// </summary>
public record TsBoolLiteral(bool Value) : TsExpression;

/// <summary>
/// Identifier/variable reference.
/// </summary>
public record TsIdentifier(string Name) : TsExpression;

/// <summary>
/// Object literal expression.
/// </summary>
public record TsObjectLiteral(IReadOnlyList<TsObjectProperty> Properties) : TsExpression;

/// <summary>
/// Property in an object literal.
/// </summary>
public record TsObjectProperty(string Key, TsExpression Value, bool Shorthand = false);

/// <summary>
/// Array literal expression.
/// </summary>
public record TsArrayLiteral(IReadOnlyList<TsExpression> Elements) : TsExpression;

/// <summary>
/// Function call expression.
/// </summary>
public record TsCall(TsExpression Callee, IReadOnlyList<TsExpression> Arguments) : TsExpression;

/// <summary>
/// Member access expression (a.b).
/// </summary>
public record TsMemberAccess(TsExpression Object, string Property) : TsExpression;

/// <summary>
/// Arrow function expression.
/// </summary>
public record TsArrowFunction(
    IReadOnlyList<TsParameter> Parameters,
    TsExpression Body,
    bool IsAsync = false) : TsExpression;

/// <summary>
/// Template literal expression.
/// </summary>
public record TsTemplateLiteral(IReadOnlyList<TsTemplateSpan> Spans) : TsExpression;

/// <summary>
/// Span in a template literal (either string or expression).
/// </summary>
public record TsTemplateSpan(string? Text, TsExpression? Expression);

/// <summary>
/// Await expression.
/// </summary>
public record TsAwait(TsExpression Expression) : TsExpression;

/// <summary>
/// Return statement as expression.
/// </summary>
public record TsReturn(TsExpression? Expression) : TsExpression;

/// <summary>
/// Type assertion (value as Type).
/// </summary>
public record TsAssertion(TsExpression Expression, TsType Type) : TsExpression;
