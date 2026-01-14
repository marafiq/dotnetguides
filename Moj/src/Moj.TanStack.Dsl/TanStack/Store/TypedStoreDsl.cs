using System.Linq.Expressions;
using Moj.TanStack.Ast.Core;
using Moj.TanStack.Ast.CodeGen;
using Moj.TanStack.Dsl.Core.TypeInference;

namespace Moj.TanStack.Dsl.TanStack.Store;

/// <summary>
/// Type-safe TanStack Store DSL that infers everything from C# types.
/// No manual TypeScript construction needed!
/// </summary>
public static class TanStack
{
    /// <summary>
    /// Create a TanStack Store for a C# type.
    /// TypeScript interfaces and types are automatically generated.
    /// </summary>
    public static TypedStoreBuilder<TState> Store<TState>() where TState : class, new()
        => new();

    /// <summary>
    /// Create a TanStack Store with a specific initial state instance.
    /// </summary>
    public static TypedStoreBuilder<TState> Store<TState>(TState initialState) where TState : class
        => new TypedStoreBuilder<TState>().WithInitialState(initialState);
}

/// <summary>
/// Fluent builder for TanStack Store using C# type inference.
/// </summary>
public sealed class TypedStoreBuilder<TState> where TState : class
{
    private TState? _initialState;
    private readonly List<StoreAction<TState>> _actions = [];
    private readonly List<StoreSelector<TState>> _selectors = [];
    private readonly List<StoreDerivedState<TState>> _derived = [];
    private string _storeName = "store";
    private bool _exported = true;

    /// <summary>
    /// Set the initial state from a C# object instance.
    /// All values are automatically converted to TypeScript literals.
    /// </summary>
    public TypedStoreBuilder<TState> WithInitialState(TState state)
    {
        _initialState = state;
        return this;
    }

    /// <summary>
    /// Define a store action using C# lambda expressions.
    /// The lambda is analyzed to generate TypeScript code.
    /// </summary>
    public TypedStoreBuilder<TState> Action<TPayload>(
        string name,
        Expression<Func<TState, TPayload, TState>> reducer)
    {
        _actions.Add(new StoreAction<TState>(
            name,
            typeof(TPayload),
            reducer.Body.ToString(), // Will be properly analyzed by source generator
            reducer));
        return this;
    }

    /// <summary>
    /// Define a store action with no payload.
    /// </summary>
    public TypedStoreBuilder<TState> Action(
        string name,
        Expression<Func<TState, TState>> reducer)
    {
        _actions.Add(new StoreAction<TState>(
            name,
            null,
            reducer.Body.ToString(),
            reducer));
        return this;
    }

    /// <summary>
    /// Define a selector that derives a value from state.
    /// </summary>
    public TypedStoreBuilder<TState> Selector<TResult>(
        string name,
        Expression<Func<TState, TResult>> selector)
    {
        _selectors.Add(new StoreSelector<TState>(
            name,
            typeof(TResult),
            selector.Body.ToString(),
            selector));
        return this;
    }

    /// <summary>
    /// Define derived state that computes from other state.
    /// </summary>
    public TypedStoreBuilder<TState> Derived<TResult>(
        string name,
        Expression<Func<TState, TResult>> computation)
    {
        _derived.Add(new StoreDerivedState<TState>(
            name,
            typeof(TResult),
            computation));
        return this;
    }

    /// <summary>
    /// Set the store variable name.
    /// </summary>
    public TypedStoreBuilder<TState> As(string name)
    {
        _storeName = name;
        return this;
    }

    /// <summary>
    /// Mark as exported (default true).
    /// </summary>
    public TypedStoreBuilder<TState> Export(bool export = true)
    {
        _exported = export;
        return this;
    }

    /// <summary>
    /// Build the store definition.
    /// </summary>
    public StoreDefinition<TState> Build()
    {
        return new StoreDefinition<TState>(
            _storeName,
            typeof(TState),
            _initialState,
            _actions,
            _selectors,
            _derived,
            _exported);
    }
}

/// <summary>
/// Represents a complete store definition ready for TypeScript generation.
/// </summary>
public sealed class StoreDefinition<TState> where TState : class
{
    public string Name { get; }
    public Type StateType { get; }
    public TState? InitialState { get; }
    public IReadOnlyList<StoreAction<TState>> Actions { get; }
    public IReadOnlyList<StoreSelector<TState>> Selectors { get; }
    public IReadOnlyList<StoreDerivedState<TState>> DerivedState { get; }
    public bool IsExported { get; }

    internal StoreDefinition(
        string name,
        Type stateType,
        TState? initialState,
        List<StoreAction<TState>> actions,
        List<StoreSelector<TState>> selectors,
        List<StoreDerivedState<TState>> derived,
        bool exported)
    {
        Name = name;
        StateType = stateType;
        InitialState = initialState;
        Actions = actions;
        Selectors = selectors;
        DerivedState = derived;
        IsExported = exported;
    }

    /// <summary>
    /// Generate complete TypeScript code for this store.
    /// </summary>
    public string ToTypeScript()
    {
        var generator = new StoreTypeScriptGenerator<TState>(this);
        return generator.Generate();
    }

    /// <summary>
    /// Generate TypeScript AST nodes.
    /// </summary>
    public IEnumerable<TsNode> ToAst()
    {
        var generator = new StoreTypeScriptGenerator<TState>(this);
        return generator.GenerateAst();
    }
}

public sealed record StoreAction<TState>(
    string Name,
    Type? PayloadType,
    string ExpressionBody,
    LambdaExpression Lambda) where TState : class;

public sealed record StoreSelector<TState>(
    string Name,
    Type ResultType,
    string ExpressionBody,
    LambdaExpression Lambda) where TState : class;

public sealed record StoreDerivedState<TState>(
    string Name,
    Type ResultType,
    LambdaExpression Computation) where TState : class;

/// <summary>
/// Generates TypeScript code from a StoreDefinition.
/// </summary>
internal sealed class StoreTypeScriptGenerator<TState> where TState : class
{
    private readonly StoreDefinition<TState> _definition;

    public StoreTypeScriptGenerator(StoreDefinition<TState> definition)
    {
        _definition = definition;
    }

    public string Generate()
    {
        var emitter = new TypeScriptEmitter();
        var program = new TsProgram(GenerateAst().ToList());
        return emitter.Emit(program);
    }

    public IEnumerable<TsNode> GenerateAst()
    {
        var nodes = new List<TsNode>();

        // Import statement
        nodes.Add(new TsImportDeclaration(
            "@tanstack/store",
            new TsImportClause(NamedImports: [new TsImportSpecifier("Store")])));

        // Generate interfaces for state type and all dependent types
        var dependentTypes = TypeToTsConverter.GetDependentTypes(_definition.StateType).ToList();
        foreach (var depType in dependentTypes)
        {
            nodes.Add(TypeToTsConverter.ToInterface(depType, export: true));
        }

        // Generate enums as type aliases
        var enumTypes = GetEnumTypes(_definition.StateType);
        foreach (var enumType in enumTypes)
        {
            nodes.Add(EnumToTsConverter.ToTsEnum(enumType, export: true));
        }

        // Main state interface
        nodes.Add(TypeToTsConverter.ToInterface(_definition.StateType, export: true));

        // Initial state
        TsExpression initialStateExpr;
        if (_definition.InitialState != null)
        {
            initialStateExpr = TypeToTsConverter.ToObjectLiteral(
                _definition.InitialState,
                _definition.StateType);
        }
        else
        {
            initialStateExpr = new TsObjectLiteral([]);
        }

        // Store declaration
        var storeDecl = new TsVariableDeclaration(
            TsVariableKind.Const,
            [new TsVariableDeclarator(
                new TsIdentifierBinding(_definition.Name),
                new TsGenericType(new TsTypeReference("Store"), [new TsTypeReference(_definition.StateType.Name)]),
                new TsNewExpression(
                    new TsIdentifier("Store"),
                    [initialStateExpr]))],
            _definition.IsExported);
        nodes.Add(storeDecl);

        // Generate actions
        foreach (var action in _definition.Actions)
        {
            nodes.Add(GenerateAction(action));
        }

        // Generate selectors
        foreach (var selector in _definition.Selectors)
        {
            nodes.Add(GenerateSelector(selector));
        }

        return nodes;
    }

    private TsVariableDeclaration GenerateAction(StoreAction<TState> action)
    {
        var stateParam = new TsParameter(
            new TsIdentifierBinding("state"),
            new TsTypeReference(_definition.StateType.Name));

        TsArrowFunction actionFn;
        if (action.PayloadType != null)
        {
            var payloadParam = new TsParameter(
                new TsIdentifierBinding("payload"),
                TypeToTsConverter.ToTsType(action.PayloadType));

            // Generate the inner setState arrow function
            var setStateFn = new TsArrowFunction(
                [stateParam],
                ConvertExpressionToTs(action.Lambda));

            // Generate the outer action function
            actionFn = new TsArrowFunction(
                [payloadParam],
                new TsCallExpression(
                    new TsMemberAccess(new TsIdentifier(_definition.Name), "setState"),
                    [setStateFn]));
        }
        else
        {
            var setStateFn = new TsArrowFunction(
                [stateParam],
                ConvertExpressionToTs(action.Lambda));

            actionFn = new TsArrowFunction(
                [],
                new TsCallExpression(
                    new TsMemberAccess(new TsIdentifier(_definition.Name), "setState"),
                    [setStateFn]));
        }

        return new TsVariableDeclaration(
            TsVariableKind.Const,
            [new TsVariableDeclarator(
                new TsIdentifierBinding(action.Name),
                Initializer: actionFn)],
            _definition.IsExported);
    }

    private TsVariableDeclaration GenerateSelector(StoreSelector<TState> selector)
    {
        var stateParam = new TsParameter(
            new TsIdentifierBinding("state"),
            new TsTypeReference(_definition.StateType.Name));

        var selectorFn = new TsArrowFunction(
            [stateParam],
            ConvertExpressionToTs(selector.Lambda),
            ReturnType: TypeToTsConverter.ToTsType(selector.ResultType));

        return new TsVariableDeclaration(
            TsVariableKind.Const,
            [new TsVariableDeclarator(
                new TsIdentifierBinding(selector.Name),
                Initializer: selectorFn)],
            _definition.IsExported);
    }

    private TsExpression ConvertExpressionToTs(LambdaExpression lambda)
    {
        // This is a simplified conversion - the real source generator would do full expression tree analysis
        return new ExpressionTreeConverter().Convert(lambda.Body);
    }

    private IEnumerable<Type> GetEnumTypes(Type type)
    {
        var enums = new HashSet<Type>();
        CollectEnums(type, enums, new HashSet<Type>());
        return enums;
    }

    private void CollectEnums(Type type, HashSet<Type> enums, HashSet<Type> visited)
    {
        if (visited.Contains(type)) return;
        visited.Add(type);

        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
        {
            CollectEnums(underlying, enums, visited);
            return;
        }

        if (type.IsEnum)
        {
            enums.Add(type);
            return;
        }

        if (type.IsArray)
        {
            CollectEnums(type.GetElementType()!, enums, visited);
            return;
        }

        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
                CollectEnums(arg, enums, visited);
            return;
        }

        foreach (var prop in type.GetProperties())
        {
            CollectEnums(prop.PropertyType, enums, visited);
        }
    }
}

/// <summary>
/// Converts C# expression trees to TypeScript AST.
/// </summary>
internal sealed class ExpressionTreeConverter
{
    public TsExpression Convert(Expression expression)
    {
        return expression switch
        {
            ConstantExpression constant => ConvertConstant(constant),
            MemberExpression member => ConvertMember(member),
            BinaryExpression binary => ConvertBinary(binary),
            UnaryExpression unary => ConvertUnary(unary),
            ConditionalExpression conditional => ConvertConditional(conditional),
            MethodCallExpression call => ConvertMethodCall(call),
            NewExpression @new => ConvertNew(@new),
            MemberInitExpression init => ConvertMemberInit(init),
            ParameterExpression param => new TsIdentifier(param.Name ?? "param"),
            LambdaExpression lambda => ConvertLambda(lambda),
            _ => new TsIdentifier($"/* TODO: {expression.NodeType} */")
        };
    }

    private TsExpression ConvertConstant(ConstantExpression expr)
    {
        return expr.Value switch
        {
            null => new TsLiteral(null, TsLiteralKind.Null),
            string s => new TsLiteral(s, TsLiteralKind.String),
            bool b => new TsLiteral(b, TsLiteralKind.Boolean),
            int or long or double or float or decimal => new TsLiteral(expr.Value, TsLiteralKind.Number),
            _ => TypeToTsConverter.ToObjectLiteral(expr.Value, expr.Type)
        };
    }

    private TsExpression ConvertMember(MemberExpression expr)
    {
        if (expr.Expression == null)
        {
            // Static member
            return new TsIdentifier(expr.Member.Name);
        }

        var obj = Convert(expr.Expression);
        return new TsMemberAccess(obj, ToCamelCase(expr.Member.Name));
    }

    private TsExpression ConvertBinary(BinaryExpression expr)
    {
        var left = Convert(expr.Left);
        var right = Convert(expr.Right);

        var op = expr.NodeType switch
        {
            ExpressionType.Equal => TsBinaryOperator.StrictEqual,
            ExpressionType.NotEqual => TsBinaryOperator.StrictNotEqual,
            ExpressionType.GreaterThan => TsBinaryOperator.GreaterThan,
            ExpressionType.GreaterThanOrEqual => TsBinaryOperator.GreaterThanOrEqual,
            ExpressionType.LessThan => TsBinaryOperator.LessThan,
            ExpressionType.LessThanOrEqual => TsBinaryOperator.LessThanOrEqual,
            ExpressionType.Add or ExpressionType.AddChecked => TsBinaryOperator.Add,
            ExpressionType.Subtract or ExpressionType.SubtractChecked => TsBinaryOperator.Subtract,
            ExpressionType.Multiply or ExpressionType.MultiplyChecked => TsBinaryOperator.Multiply,
            ExpressionType.Divide => TsBinaryOperator.Divide,
            ExpressionType.Modulo => TsBinaryOperator.Modulo,
            ExpressionType.And or ExpressionType.AndAlso => TsBinaryOperator.And,
            ExpressionType.Or or ExpressionType.OrElse => TsBinaryOperator.Or,
            ExpressionType.Coalesce => TsBinaryOperator.NullishCoalesce,
            _ => TsBinaryOperator.StrictEqual
        };

        return new TsBinaryExpression(left, op, right);
    }

    private TsExpression ConvertUnary(UnaryExpression expr)
    {
        var operand = Convert(expr.Operand);

        return expr.NodeType switch
        {
            ExpressionType.Not => new TsUnaryExpression(TsUnaryOperator.Not, operand),
            ExpressionType.Negate => new TsUnaryExpression(TsUnaryOperator.Negate, operand),
            ExpressionType.Convert or ExpressionType.ConvertChecked => operand, // Type coercion handled implicitly
            _ => operand
        };
    }

    private TsExpression ConvertConditional(ConditionalExpression expr)
    {
        return new TsConditionalExpression(
            Convert(expr.Test),
            Convert(expr.IfTrue),
            Convert(expr.IfFalse));
    }

    private TsExpression ConvertMethodCall(MethodCallExpression expr)
    {
        // Handle special cases like .ToString(), LINQ methods, etc.
        var methodName = ToCamelCase(expr.Method.Name);

        if (expr.Object != null)
        {
            var obj = Convert(expr.Object);
            var args = expr.Arguments.Select(Convert).ToList();
            return new TsCallExpression(new TsMemberAccess(obj, methodName), args);
        }
        else
        {
            // Static method
            var args = expr.Arguments.Select(Convert).ToList();
            return new TsCallExpression(new TsIdentifier(methodName), args);
        }
    }

    private TsExpression ConvertNew(NewExpression expr)
    {
        if (expr.Members != null && expr.Members.Count > 0)
        {
            // Anonymous type or object initializer
            var properties = new List<TsObjectElement>();
            for (int i = 0; i < expr.Members.Count; i++)
            {
                properties.Add(new TsPropertyAssignment(
                    new TsIdentifier(ToCamelCase(expr.Members[i].Name)),
                    Convert(expr.Arguments[i])));
            }
            return new TsObjectLiteral(properties);
        }

        // Regular constructor call
        var args = expr.Arguments.Select(Convert).ToList();
        return new TsNewExpression(new TsIdentifier(expr.Type.Name), args);
    }

    private TsExpression ConvertMemberInit(MemberInitExpression expr)
    {
        var properties = new List<TsObjectElement>();

        // Start with constructor args if any
        if (expr.NewExpression.Arguments.Count > 0 && expr.NewExpression.Members != null)
        {
            for (int i = 0; i < expr.NewExpression.Members.Count; i++)
            {
                properties.Add(new TsPropertyAssignment(
                    new TsIdentifier(ToCamelCase(expr.NewExpression.Members[i].Name)),
                    Convert(expr.NewExpression.Arguments[i])));
            }
        }

        // Add member bindings
        foreach (var binding in expr.Bindings)
        {
            if (binding is MemberAssignment assignment)
            {
                properties.Add(new TsPropertyAssignment(
                    new TsIdentifier(ToCamelCase(binding.Member.Name)),
                    Convert(assignment.Expression)));
            }
        }

        return new TsObjectLiteral(properties);
    }

    private TsExpression ConvertLambda(LambdaExpression expr)
    {
        var @params = expr.Parameters
            .Select(p => new TsParameter(new TsIdentifierBinding(p.Name ?? "p")))
            .ToList();

        return new TsArrowFunction(@params, Convert(expr.Body));
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (name.Length == 1) return name.ToLowerInvariant();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
