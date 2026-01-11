namespace Impulse.CodeGen.Ast;

/// <summary>
/// Base class for TypeScript type AST nodes.
/// </summary>
public abstract record TsType;

/// <summary>
/// TypeScript string type.
/// </summary>
public record TsString : TsType;

/// <summary>
/// TypeScript number type.
/// </summary>
public record TsNumber : TsType;

/// <summary>
/// TypeScript boolean type.
/// </summary>
public record TsBoolean : TsType;

/// <summary>
/// TypeScript null type.
/// </summary>
public record TsNull : TsType;

/// <summary>
/// TypeScript undefined type.
/// </summary>
public record TsUndefined : TsType;

/// <summary>
/// TypeScript unknown type.
/// </summary>
public record TsUnknown : TsType;

/// <summary>
/// TypeScript array type.
/// </summary>
public record TsArray(TsType Element, bool IsReadonly = true) : TsType;

/// <summary>
/// TypeScript Record type.
/// </summary>
public record TsRecord(TsType KeyType, TsType ValueType) : TsType;

/// <summary>
/// Reference to another type by name.
/// </summary>
public record TsRef(string Name) : TsType;

/// <summary>
/// Union type (A | B | C).
/// </summary>
public record TsUnion(IReadOnlyList<TsType> Types) : TsType;

/// <summary>
/// TypeScript interface declaration.
/// </summary>
public record TsInterface(string Name, IReadOnlyList<TsProperty> Properties);

/// <summary>
/// Property within an interface.
/// </summary>
public record TsProperty(string Name, TsType Type, bool IsOptional = false);

/// <summary>
/// TypeScript enum declaration.
/// </summary>
public record TsEnum(string Name, IReadOnlyList<TsEnumMember> Members);

/// <summary>
/// Enum member.
/// </summary>
public record TsEnumMember(string Name, string Value);

/// <summary>
/// TypeScript file containing multiple declarations.
/// </summary>
public record TsFile(IReadOnlyList<object> Declarations);
