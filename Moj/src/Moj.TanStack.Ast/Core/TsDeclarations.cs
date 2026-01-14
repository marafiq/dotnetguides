namespace Moj.TanStack.Ast.Core;

/// <summary>
/// Base class for declarations
/// </summary>
public abstract record TsDeclaration : TsNode;

/// <summary>
/// Import declaration
/// </summary>
public sealed record TsImportDeclaration(
    string ModuleSpecifier,
    TsImportClause? ImportClause = null,
    bool IsTypeOnly = false
) : TsDeclaration
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Import clause containing imported bindings
/// </summary>
public sealed record TsImportClause(
    string? DefaultImport = null,
    string? NamespaceImport = null,
    IReadOnlyList<TsImportSpecifier>? NamedImports = null
);

/// <summary>
/// Named import specifier: { name } or { name as alias }
/// </summary>
public sealed record TsImportSpecifier(string Name, string? Alias = null, bool IsTypeOnly = false);

/// <summary>
/// Export declaration
/// </summary>
public sealed record TsExportDeclaration(
    TsNode? Declaration = null,
    IReadOnlyList<TsExportSpecifier>? NamedExports = null,
    string? ModuleSpecifier = null,
    bool IsDefault = false,
    bool IsTypeOnly = false
) : TsDeclaration
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Named export specifier: { name } or { name as alias }
/// </summary>
public sealed record TsExportSpecifier(string Name, string? Alias = null, bool IsTypeOnly = false);

/// <summary>
/// Variable declaration kind
/// </summary>
public enum TsVariableKind
{
    Const,
    Let,
    Var
}

/// <summary>
/// Variable declaration: const/let/var name = value
/// </summary>
public sealed record TsVariableDeclaration(
    TsVariableKind Kind,
    IReadOnlyList<TsVariableDeclarator> Declarators,
    bool IsExported = false
) : TsDeclaration
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Variable declarator: name: Type = initializer
/// </summary>
public sealed record TsVariableDeclarator(
    TsBindingPattern Pattern,
    TsType? Type = null,
    TsExpression? Initializer = null
);

/// <summary>
/// Binding pattern (identifier or destructuring)
/// </summary>
public abstract record TsBindingPattern;

/// <summary>
/// Simple identifier binding
/// </summary>
public sealed record TsIdentifierBinding(string Name) : TsBindingPattern;

/// <summary>
/// Object destructuring pattern
/// </summary>
public sealed record TsObjectBinding(
    IReadOnlyList<TsObjectBindingElement> Elements,
    TsIdentifier? Rest = null
) : TsBindingPattern;

/// <summary>
/// Object binding element
/// </summary>
public sealed record TsObjectBindingElement(
    string Key,
    TsBindingPattern Value,
    TsExpression? DefaultValue = null
);

/// <summary>
/// Array destructuring pattern
/// </summary>
public sealed record TsArrayBinding(
    IReadOnlyList<TsBindingPattern?> Elements,
    TsBindingPattern? Rest = null
) : TsBindingPattern;

/// <summary>
/// Function declaration
/// </summary>
public sealed record TsFunctionDeclaration(
    string Name,
    IReadOnlyList<TsParameter> Parameters,
    TsBlockStatement Body,
    bool IsAsync = false,
    bool IsGenerator = false,
    IReadOnlyList<TsTypeParameter>? TypeParameters = null,
    TsType? ReturnType = null,
    bool IsExported = false,
    bool IsDefault = false
) : TsDeclaration
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Function parameter
/// </summary>
public sealed record TsParameter(
    TsBindingPattern Pattern,
    TsType? Type = null,
    TsExpression? DefaultValue = null,
    bool IsRest = false,
    bool IsOptional = false
) : TsNode
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Interface declaration
/// </summary>
public sealed record TsInterfaceDeclaration(
    string Name,
    IReadOnlyList<TsTypeMember> Members,
    IReadOnlyList<TsTypeParameter>? TypeParameters = null,
    IReadOnlyList<TsType>? Extends = null,
    bool IsExported = false
) : TsDeclaration
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Type alias declaration
/// </summary>
public sealed record TsTypeAliasDeclaration(
    string Name,
    TsType Type,
    IReadOnlyList<TsTypeParameter>? TypeParameters = null,
    bool IsExported = false
) : TsDeclaration
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}
