using Moj.TanStack.Ast.Core;

namespace Moj.TanStack.Dsl.Core;

#region Object Builder

/// <summary>
/// Fluent builder for object literals
/// </summary>
public sealed class TsObjectBuilder<T>
{
    private readonly List<TsObjectElement> _properties = [];

    /// <summary>Add a property with value</summary>
    public TsObjectBuilder<T> With<TProp>(string name, TsExpr<TProp> value)
    {
        _properties.Add(new TsPropertyAssignment(
            new TsIdentifier(name),
            value.Node));
        return this;
    }

    /// <summary>Add a property with untyped value</summary>
    public TsObjectBuilder<T> With(string name, TsExprBase value)
    {
        _properties.Add(new TsPropertyAssignment(
            new TsIdentifier(name),
            value.Node));
        return this;
    }

    /// <summary>Add a shorthand property (name same as value)</summary>
    public TsObjectBuilder<T> With(string name)
    {
        _properties.Add(new TsPropertyAssignment(
            new TsIdentifier(name),
            new TsIdentifier(name),
            IsShorthand: true));
        return this;
    }

    /// <summary>Add a computed property</summary>
    public TsObjectBuilder<T> WithComputed(TsExprBase key, TsExprBase value)
    {
        _properties.Add(new TsPropertyAssignment(
            key.Node,
            value.Node,
            IsComputed: true));
        return this;
    }

    /// <summary>Spread another object</summary>
    public TsObjectBuilder<T> Spread(TsExprBase expr)
    {
        _properties.Add(new TsSpreadElement(expr.Node));
        return this;
    }

    /// <summary>Add a method</summary>
    public TsObjectBuilder<T> Method(string name, TsExprBase arrowFn)
    {
        _properties.Add(new TsPropertyAssignment(
            new TsIdentifier(name),
            arrowFn.Node));
        return this;
    }

    /// <summary>Build the object expression</summary>
    public TsExpr<T> Build() =>
        new TsTypedExpr<T>(new TsObjectLiteral(_properties));

    /// <summary>Implicit conversion to expression</summary>
    public static implicit operator TsExpr<T>(TsObjectBuilder<T> builder) => builder.Build();

    /// <summary>Implicit conversion to AST node</summary>
    public static implicit operator TsExpression(TsObjectBuilder<T> builder) => builder.Build().Node;
}

#endregion

#region Arrow Function Builders

/// <summary>Arrow function builder with no parameters</summary>
public sealed class TsArrowBuilder<TResult>
{
    private TsNode? _body;
    private TsType? _returnType;
    private readonly List<TsTypeParameter> _typeParams = [];

    public TsArrowBuilder<TResult> Returns(TsExpr<TResult> expr)
    {
        _body = expr.Node;
        return this;
    }

    public TsArrowBuilder<TResult> Body(Action<TsBlockBuilder> configure)
    {
        var builder = new TsBlockBuilder();
        configure(builder);
        _body = builder.Build();
        return this;
    }

    public TsArrowBuilder<TResult> WithReturnType(TsType type)
    {
        _returnType = type;
        return this;
    }

    public TsArrowBuilder<TResult> WithTypeParam(string name, TsType? constraint = null)
    {
        _typeParams.Add(new TsTypeParameter(name, constraint));
        return this;
    }

    public TsExpr<Func<TResult>> Build() =>
        new TsTypedExpr<Func<TResult>>(new TsArrowFunction(
            [],
            _body ?? new TsLiteral(null, TsLiteralKind.Undefined),
            TypeParameters: _typeParams.Count > 0 ? _typeParams : null,
            ReturnType: _returnType));

    public static implicit operator TsExpr<Func<TResult>>(TsArrowBuilder<TResult> builder) => builder.Build();
    public static implicit operator TsExprBase(TsArrowBuilder<TResult> builder) => builder.Build();
    public static implicit operator TsExpression(TsArrowBuilder<TResult> builder) => builder.Build().Node;
}

/// <summary>Arrow function builder with one parameter</summary>
public sealed class TsArrowBuilder<T1, TResult>
{
    private readonly string _p1;
    private TsNode? _body;
    private TsType? _p1Type;
    private TsType? _returnType;
    private bool _isAsync;

    public TsArrowBuilder(string p1) => _p1 = p1;

    public TsArrowBuilder<T1, TResult> Returns(TsExpr<TResult> expr)
    {
        _body = expr.Node;
        return this;
    }

    public TsArrowBuilder<T1, TResult> Returns(Func<TsExpr<T1>, TsExpr<TResult>> factory)
    {
        var param = Ts.Var<T1>(_p1);
        _body = factory(param).Node;
        return this;
    }

    public TsArrowBuilder<T1, TResult> Body(Action<TsExpr<T1>, TsBlockBuilder> configure)
    {
        var param = Ts.Var<T1>(_p1);
        var builder = new TsBlockBuilder();
        configure(param, builder);
        _body = builder.Build();
        return this;
    }

    public TsArrowBuilder<T1, TResult> WithParamType(TsType type)
    {
        _p1Type = type;
        return this;
    }

    public TsArrowBuilder<T1, TResult> WithReturnType(TsType type)
    {
        _returnType = type;
        return this;
    }

    public TsArrowBuilder<T1, TResult> Async()
    {
        _isAsync = true;
        return this;
    }

    public TsExpr<Func<T1, TResult>> Build() =>
        new TsTypedExpr<Func<T1, TResult>>(new TsArrowFunction(
            [new TsParameter(new TsIdentifierBinding(_p1), _p1Type ?? TsTypeMapper.MapType(typeof(T1)))],
            _body ?? new TsLiteral(null, TsLiteralKind.Undefined),
            _isAsync,
            ReturnType: _returnType));

    public static implicit operator TsExpr<Func<T1, TResult>>(TsArrowBuilder<T1, TResult> builder) => builder.Build();
    public static implicit operator TsExprBase(TsArrowBuilder<T1, TResult> builder) => builder.Build();
    public static implicit operator TsExpression(TsArrowBuilder<T1, TResult> builder) => builder.Build().Node;
}

/// <summary>Arrow function builder with two parameters</summary>
public sealed class TsArrowBuilder<T1, T2, TResult>
{
    private readonly string _p1, _p2;
    private TsNode? _body;

    public TsArrowBuilder(string p1, string p2) => (_p1, _p2) = (p1, p2);

    public TsArrowBuilder<T1, T2, TResult> Returns(TsExpr<TResult> expr)
    {
        _body = expr.Node;
        return this;
    }

    public TsArrowBuilder<T1, T2, TResult> Returns(Func<TsExpr<T1>, TsExpr<T2>, TsExpr<TResult>> factory)
    {
        _body = factory(Ts.Var<T1>(_p1), Ts.Var<T2>(_p2)).Node;
        return this;
    }

    public TsExpr<Func<T1, T2, TResult>> Build() =>
        new TsTypedExpr<Func<T1, T2, TResult>>(new TsArrowFunction(
            [
                new TsParameter(new TsIdentifierBinding(_p1), TsTypeMapper.MapType(typeof(T1))),
                new TsParameter(new TsIdentifierBinding(_p2), TsTypeMapper.MapType(typeof(T2)))
            ],
            _body ?? new TsLiteral(null, TsLiteralKind.Undefined)));

    public static implicit operator TsExpr<Func<T1, T2, TResult>>(TsArrowBuilder<T1, T2, TResult> builder) => builder.Build();
    public static implicit operator TsExprBase(TsArrowBuilder<T1, T2, TResult> builder) => builder.Build();
}

/// <summary>Arrow function builder with three parameters</summary>
public sealed class TsArrowBuilder<T1, T2, T3, TResult>
{
    private readonly string _p1, _p2, _p3;
    private TsNode? _body;

    public TsArrowBuilder(string p1, string p2, string p3) => (_p1, _p2, _p3) = (p1, p2, p3);

    public TsArrowBuilder<T1, T2, T3, TResult> Returns(Func<TsExpr<T1>, TsExpr<T2>, TsExpr<T3>, TsExpr<TResult>> factory)
    {
        _body = factory(Ts.Var<T1>(_p1), Ts.Var<T2>(_p2), Ts.Var<T3>(_p3)).Node;
        return this;
    }

    public TsExpr<Func<T1, T2, T3, TResult>> Build() =>
        new TsTypedExpr<Func<T1, T2, T3, TResult>>(new TsArrowFunction(
            [
                new TsParameter(new TsIdentifierBinding(_p1), TsTypeMapper.MapType(typeof(T1))),
                new TsParameter(new TsIdentifierBinding(_p2), TsTypeMapper.MapType(typeof(T2))),
                new TsParameter(new TsIdentifierBinding(_p3), TsTypeMapper.MapType(typeof(T3)))
            ],
            _body ?? new TsLiteral(null, TsLiteralKind.Undefined)));

    public static implicit operator TsExprBase(TsArrowBuilder<T1, T2, T3, TResult> builder) => builder.Build();
}

#endregion

#region Async Arrow Builders

/// <summary>Async arrow function builder with no parameters</summary>
public sealed class TsAsyncArrowBuilder<TResult>
{
    private TsNode? _body;

    public TsAsyncArrowBuilder<TResult> Returns(TsExprBase expr)
    {
        _body = expr.Node;
        return this;
    }

    public TsAsyncArrowBuilder<TResult> Body(Action<TsBlockBuilder> configure)
    {
        var builder = new TsBlockBuilder();
        configure(builder);
        _body = builder.Build();
        return this;
    }

    public TsExpr<Func<Task<TResult>>> Build() =>
        new TsTypedExpr<Func<Task<TResult>>>(new TsArrowFunction(
            [],
            _body ?? new TsLiteral(null, TsLiteralKind.Undefined),
            IsAsync: true));

    public static implicit operator TsExprBase(TsAsyncArrowBuilder<TResult> builder) => builder.Build();
    public static implicit operator TsExpression(TsAsyncArrowBuilder<TResult> builder) => builder.Build().Node;
}

/// <summary>Async arrow function builder with one parameter</summary>
public sealed class TsAsyncArrowBuilder<T1, TResult>
{
    private readonly string _p1;
    private TsNode? _body;

    public TsAsyncArrowBuilder(string p1) => _p1 = p1;

    public TsAsyncArrowBuilder<T1, TResult> Returns(Func<TsExpr<T1>, TsExprBase> factory)
    {
        _body = factory(Ts.Var<T1>(_p1)).Node;
        return this;
    }

    public TsAsyncArrowBuilder<T1, TResult> Body(Action<TsExpr<T1>, TsBlockBuilder> configure)
    {
        var param = Ts.Var<T1>(_p1);
        var builder = new TsBlockBuilder();
        configure(param, builder);
        _body = builder.Build();
        return this;
    }

    public TsExpr<Func<T1, Task<TResult>>> Build() =>
        new TsTypedExpr<Func<T1, Task<TResult>>>(new TsArrowFunction(
            [new TsParameter(new TsIdentifierBinding(_p1), TsTypeMapper.MapType(typeof(T1)))],
            _body ?? new TsLiteral(null, TsLiteralKind.Undefined),
            IsAsync: true));

    public static implicit operator TsExprBase(TsAsyncArrowBuilder<T1, TResult> builder) => builder.Build();
    public static implicit operator TsExpression(TsAsyncArrowBuilder<T1, TResult> builder) => builder.Build().Node;
}

#endregion

#region Block Builder

/// <summary>Builder for block statements</summary>
public sealed class TsBlockBuilder
{
    private readonly List<TsNode> _statements = [];

    public TsBlockBuilder Return(TsExprBase expr)
    {
        _statements.Add(new TsReturnStatement(expr.Node));
        return this;
    }

    public TsBlockBuilder Statement(TsExprBase expr)
    {
        _statements.Add(new TsExpressionStatement(expr.Node));
        return this;
    }

    public TsBlockBuilder Const<T>(string name, TsExpr<T> value)
    {
        _statements.Add(new TsVariableDeclaration(
            TsVariableKind.Const,
            [new TsVariableDeclarator(new TsIdentifierBinding(name), Initializer: value.Node)]));
        return this;
    }

    public TsBlockBuilder Let<T>(string name, TsExpr<T>? value = null)
    {
        _statements.Add(new TsVariableDeclaration(
            TsVariableKind.Let,
            [new TsVariableDeclarator(
                new TsIdentifierBinding(name),
                TsTypeMapper.MapType(typeof(T)),
                value?.Node)]));
        return this;
    }

    public TsBlockBuilder If(TsExpr<bool> condition, Action<TsBlockBuilder> thenBlock)
    {
        var then = new TsBlockBuilder();
        thenBlock(then);
        _statements.Add(new TsIfStatement(condition.Node, then.Build()));
        return this;
    }

    public TsBlockBuilder IfElse(TsExpr<bool> condition, Action<TsBlockBuilder> thenBlock, Action<TsBlockBuilder> elseBlock)
    {
        var then = new TsBlockBuilder();
        thenBlock(then);
        var @else = new TsBlockBuilder();
        elseBlock(@else);
        _statements.Add(new TsIfStatement(condition.Node, then.Build(), @else.Build()));
        return this;
    }

    public TsBlockStatement Build() => new(_statements);
}

#endregion

#region Import Builder

/// <summary>Builder for import declarations</summary>
public sealed class TsImportBuilder
{
    private readonly string _module;
    private string? _default;
    private string? _namespace;
    private readonly List<TsImportSpecifier> _named = [];
    private bool _typeOnly;

    public TsImportBuilder(string module) => _module = module;

    public TsImportBuilder Default(string name)
    {
        _default = name;
        return this;
    }

    public TsImportBuilder All(string asName)
    {
        _namespace = asName;
        return this;
    }

    public TsImportBuilder Named(params string[] names)
    {
        _named.AddRange(names.Select(n => new TsImportSpecifier(n)));
        return this;
    }

    public TsImportBuilder Named(string name, string alias)
    {
        _named.Add(new TsImportSpecifier(name, alias));
        return this;
    }

    public TsImportBuilder TypeOnly()
    {
        _typeOnly = true;
        return this;
    }

    public TsImportDeclaration Build() => new(
        _module,
        new TsImportClause(_default, _namespace, _named.Count > 0 ? _named : null),
        _typeOnly);

    public static implicit operator TsImportDeclaration(TsImportBuilder builder) => builder.Build();
}

#endregion

#region Declaration Builders

/// <summary>Builder for const declarations</summary>
public sealed class TsConstBuilder<T>
{
    private readonly string _name;
    private TsExpression? _value;
    private TsType? _type;
    private bool _exported;

    public TsConstBuilder(string name) => _name = name;

    public TsConstBuilder<T> Value(TsExpr<T> value)
    {
        _value = value.Node;
        return this;
    }

    public TsConstBuilder<T> Value(TsExprBase value)
    {
        _value = value.Node;
        return this;
    }

    public TsConstBuilder<T> WithType(TsType type)
    {
        _type = type;
        return this;
    }

    public TsConstBuilder<T> Export()
    {
        _exported = true;
        return this;
    }

    public TsVariableDeclaration Build() => new(
        TsVariableKind.Const,
        [new TsVariableDeclarator(
            new TsIdentifierBinding(_name),
            _type,
            _value)],
        _exported);

    public static implicit operator TsVariableDeclaration(TsConstBuilder<T> builder) => builder.Build();
}

/// <summary>Builder for let declarations</summary>
public sealed class TsLetBuilder<T>
{
    private readonly string _name;
    private TsExpression? _value;
    private TsType? _type;
    private bool _exported;

    public TsLetBuilder(string name) => _name = name;

    public TsLetBuilder<T> Value(TsExpr<T> value)
    {
        _value = value.Node;
        return this;
    }

    public TsLetBuilder<T> WithType(TsType type)
    {
        _type = type;
        return this;
    }

    public TsLetBuilder<T> Export()
    {
        _exported = true;
        return this;
    }

    public TsVariableDeclaration Build() => new(
        TsVariableKind.Let,
        [new TsVariableDeclarator(
            new TsIdentifierBinding(_name),
            _type ?? TsTypeMapper.MapType(typeof(T)),
            _value)],
        _exported);

    public static implicit operator TsVariableDeclaration(TsLetBuilder<T> builder) => builder.Build();
}

/// <summary>Builder for interface declarations</summary>
public sealed class TsInterfaceBuilder
{
    private readonly string _name;
    private readonly List<TsTypeMember> _members = [];
    private readonly List<TsTypeParameter> _typeParams = [];
    private readonly List<TsType> _extends = [];
    private bool _exported;

    public TsInterfaceBuilder(string name) => _name = name;

    public TsInterfaceBuilder Property(string name, TsType type, bool optional = false, bool @readonly = false)
    {
        _members.Add(new TsPropertySignature(name, type, optional, @readonly));
        return this;
    }

    public TsInterfaceBuilder Property<T>(string name, bool optional = false, bool @readonly = false)
    {
        _members.Add(new TsPropertySignature(name, TsTypeMapper.MapType(typeof(T)), optional, @readonly));
        return this;
    }

    public TsInterfaceBuilder Method(string name, TsType returnType, params (string name, TsType type)[] parameters)
    {
        _members.Add(new TsMethodSignature(
            name,
            parameters.Select(p => new TsParameter(new TsIdentifierBinding(p.name), p.type)).ToList(),
            returnType));
        return this;
    }

    public TsInterfaceBuilder TypeParam(string name, TsType? constraint = null, TsType? defaultType = null)
    {
        _typeParams.Add(new TsTypeParameter(name, constraint, defaultType));
        return this;
    }

    public TsInterfaceBuilder Extends(TsType type)
    {
        _extends.Add(type);
        return this;
    }

    public TsInterfaceBuilder Export()
    {
        _exported = true;
        return this;
    }

    public TsInterfaceDeclaration Build() => new(
        _name,
        _members,
        _typeParams.Count > 0 ? _typeParams : null,
        _extends.Count > 0 ? _extends : null,
        _exported);

    public static implicit operator TsInterfaceDeclaration(TsInterfaceBuilder builder) => builder.Build();
}

/// <summary>Builder for type alias declarations</summary>
public sealed class TsTypeAliasBuilder
{
    private readonly string _name;
    private TsType? _type;
    private readonly List<TsTypeParameter> _typeParams = [];
    private bool _exported;

    public TsTypeAliasBuilder(string name) => _name = name;

    public TsTypeAliasBuilder Is(TsType type)
    {
        _type = type;
        return this;
    }

    public TsTypeAliasBuilder Is<T>()
    {
        _type = TsTypeMapper.MapType(typeof(T));
        return this;
    }

    public TsTypeAliasBuilder TypeParam(string name, TsType? constraint = null)
    {
        _typeParams.Add(new TsTypeParameter(name, constraint));
        return this;
    }

    public TsTypeAliasBuilder Export()
    {
        _exported = true;
        return this;
    }

    public TsTypeAliasDeclaration Build() => new(
        _name,
        _type ?? TsPrimitiveTypes.Unknown,
        _typeParams.Count > 0 ? _typeParams : null,
        _exported);

    public static implicit operator TsTypeAliasDeclaration(TsTypeAliasBuilder builder) => builder.Build();
}

/// <summary>Builder for function declarations</summary>
public sealed class TsFunctionBuilder
{
    private readonly string _name;
    private readonly List<TsParameter> _params = [];
    private readonly List<TsTypeParameter> _typeParams = [];
    private TsType? _returnType;
    private TsBlockStatement? _body;
    private bool _async;
    private bool _exported;

    public TsFunctionBuilder(string name) => _name = name;

    public TsFunctionBuilder Param<T>(string name, bool optional = false)
    {
        _params.Add(new TsParameter(
            new TsIdentifierBinding(name),
            TsTypeMapper.MapType(typeof(T)),
            IsOptional: optional));
        return this;
    }

    public TsFunctionBuilder Param(string name, TsType type, bool optional = false)
    {
        _params.Add(new TsParameter(
            new TsIdentifierBinding(name),
            type,
            IsOptional: optional));
        return this;
    }

    public TsFunctionBuilder TypeParam(string name, TsType? constraint = null)
    {
        _typeParams.Add(new TsTypeParameter(name, constraint));
        return this;
    }

    public TsFunctionBuilder Returns(TsType type)
    {
        _returnType = type;
        return this;
    }

    public TsFunctionBuilder Returns<T>()
    {
        _returnType = TsTypeMapper.MapType(typeof(T));
        return this;
    }

    public TsFunctionBuilder Body(Action<TsBlockBuilder> configure)
    {
        var builder = new TsBlockBuilder();
        configure(builder);
        _body = builder.Build();
        return this;
    }

    public TsFunctionBuilder Async()
    {
        _async = true;
        return this;
    }

    public TsFunctionBuilder Export()
    {
        _exported = true;
        return this;
    }

    public TsFunctionDeclaration Build() => new(
        _name,
        _params,
        _body ?? new TsBlockStatement([]),
        _async,
        TypeParameters: _typeParams.Count > 0 ? _typeParams : null,
        ReturnType: _returnType,
        IsExported: _exported);

    public static implicit operator TsFunctionDeclaration(TsFunctionBuilder builder) => builder.Build();
}

#endregion
