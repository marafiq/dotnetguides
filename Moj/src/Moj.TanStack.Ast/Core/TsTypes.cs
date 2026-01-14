namespace Moj.TanStack.Ast.Core;

/// <summary>
/// Base class for TypeScript types
/// </summary>
public abstract record TsType : TsNode;

/// <summary>
/// Type reference: TypeName or Module.TypeName
/// </summary>
public sealed record TsTypeReference(string Name, string? Qualifier = null) : TsType
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Generic type: Type<A, B>
/// </summary>
public sealed record TsGenericType(TsType BaseType, IReadOnlyList<TsType> TypeArguments) : TsType
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Union type: A | B | C
/// </summary>
public sealed record TsUnionType(IReadOnlyList<TsType> Types) : TsType
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Intersection type: A & B & C
/// </summary>
public sealed record TsIntersectionType(IReadOnlyList<TsType> Types) : TsType
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Object/interface type: { prop: Type }
/// </summary>
public sealed record TsObjectType(IReadOnlyList<TsTypeMember> Members) : TsType
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Array type: Type[]
/// </summary>
public sealed record TsArrayType(TsType ElementType) : TsType
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Tuple type: [A, B, C]
/// </summary>
public sealed record TsTupleType(IReadOnlyList<TsTupleElement> Elements) : TsType
{
    public override void Accept(ITsVisitor visitor) => throw new NotImplementedException();
    public override T Accept<T>(ITsVisitor<T> visitor) => throw new NotImplementedException();
}

/// <summary>
/// Tuple element (optionally named)
/// </summary>
public sealed record TsTupleElement(TsType Type, string? Name = null, bool IsOptional = false, bool IsRest = false);

/// <summary>
/// Function type: (params) => ReturnType
/// </summary>
public sealed record TsFunctionType(
    IReadOnlyList<TsParameter> Parameters,
    TsType ReturnType,
    IReadOnlyList<TsTypeParameter>? TypeParameters = null
) : TsType
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Literal type: 'hello' | 42 | true
/// </summary>
public sealed record TsLiteralType(TsLiteral Value) : TsType
{
    public override void Accept(ITsVisitor visitor) => throw new NotImplementedException();
    public override T Accept<T>(ITsVisitor<T> visitor) => throw new NotImplementedException();
}

/// <summary>
/// Conditional type: A extends B ? C : D
/// </summary>
public sealed record TsConditionalType(
    TsType CheckType,
    TsType ExtendsType,
    TsType TrueType,
    TsType FalseType
) : TsType
{
    public override void Accept(ITsVisitor visitor) => throw new NotImplementedException();
    public override T Accept<T>(ITsVisitor<T> visitor) => throw new NotImplementedException();
}

/// <summary>
/// Keyof type: keyof T
/// </summary>
public sealed record TsKeyofType(TsType Type) : TsType
{
    public override void Accept(ITsVisitor visitor) => throw new NotImplementedException();
    public override T Accept<T>(ITsVisitor<T> visitor) => throw new NotImplementedException();
}

/// <summary>
/// Typeof type: typeof expr
/// </summary>
public sealed record TsTypeofType(TsExpression Expression) : TsType
{
    public override void Accept(ITsVisitor visitor) => throw new NotImplementedException();
    public override T Accept<T>(ITsVisitor<T> visitor) => throw new NotImplementedException();
}

/// <summary>
/// Indexed access type: T[K]
/// </summary>
public sealed record TsIndexedAccessType(TsType ObjectType, TsType IndexType) : TsType
{
    public override void Accept(ITsVisitor visitor) => throw new NotImplementedException();
    public override T Accept<T>(ITsVisitor<T> visitor) => throw new NotImplementedException();
}

/// <summary>
/// Mapped type: { [K in Keys]: Type }
/// </summary>
public sealed record TsMappedType(
    TsTypeParameter TypeParameter,
    TsType? NameType,
    TsType Type,
    bool IsReadonly,
    bool IsOptional,
    bool RemoveReadonly,
    bool RemoveOptional
) : TsType
{
    public override void Accept(ITsVisitor visitor) => throw new NotImplementedException();
    public override T Accept<T>(ITsVisitor<T> visitor) => throw new NotImplementedException();
}

/// <summary>
/// Infer type: infer U
/// </summary>
public sealed record TsInferType(TsTypeParameter TypeParameter) : TsType
{
    public override void Accept(ITsVisitor visitor) => throw new NotImplementedException();
    public override T Accept<T>(ITsVisitor<T> visitor) => throw new NotImplementedException();
}

/// <summary>
/// Type parameter: T extends Constraint = Default
/// </summary>
public sealed record TsTypeParameter(
    string Name,
    TsType? Constraint = null,
    TsType? Default = null
) : TsNode
{
    public override void Accept(ITsVisitor visitor) => throw new NotImplementedException();
    public override T Accept<T>(ITsVisitor<T> visitor) => throw new NotImplementedException();
}

/// <summary>
/// Base for type members (property, method, index, call, construct)
/// </summary>
public abstract record TsTypeMember;

/// <summary>
/// Property signature: name: Type
/// </summary>
public sealed record TsPropertySignature(
    string Name,
    TsType Type,
    bool IsOptional = false,
    bool IsReadonly = false
) : TsTypeMember;

/// <summary>
/// Method signature: name(params): Type
/// </summary>
public sealed record TsMethodSignature(
    string Name,
    IReadOnlyList<TsParameter> Parameters,
    TsType? ReturnType = null,
    IReadOnlyList<TsTypeParameter>? TypeParameters = null,
    bool IsOptional = false
) : TsTypeMember;

/// <summary>
/// Index signature: [key: KeyType]: ValueType
/// </summary>
public sealed record TsIndexSignature(
    string ParameterName,
    TsType KeyType,
    TsType ValueType,
    bool IsReadonly = false
) : TsTypeMember;

/// <summary>
/// Call signature: (params): ReturnType
/// </summary>
public sealed record TsCallSignature(
    IReadOnlyList<TsParameter> Parameters,
    TsType? ReturnType = null,
    IReadOnlyList<TsTypeParameter>? TypeParameters = null
) : TsTypeMember;

/// <summary>
/// Construct signature: new (params): ReturnType
/// </summary>
public sealed record TsConstructSignature(
    IReadOnlyList<TsParameter> Parameters,
    TsType? ReturnType = null,
    IReadOnlyList<TsTypeParameter>? TypeParameters = null
) : TsTypeMember;

/// <summary>
/// Well-known TypeScript primitive types
/// </summary>
public static class TsPrimitiveTypes
{
    public static readonly TsTypeReference String = new("string");
    public static readonly TsTypeReference Number = new("number");
    public static readonly TsTypeReference Boolean = new("boolean");
    public static readonly TsTypeReference Null = new("null");
    public static readonly TsTypeReference Undefined = new("undefined");
    public static readonly TsTypeReference Void = new("void");
    public static readonly TsTypeReference Never = new("never");
    public static readonly TsTypeReference Unknown = new("unknown");
    public static readonly TsTypeReference Any = new("any");
    public static readonly TsTypeReference Object = new("object");
    public static readonly TsTypeReference Symbol = new("symbol");
    public static readonly TsTypeReference BigInt = new("bigint");
}
