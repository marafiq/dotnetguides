using System.Linq.Expressions;
using Moj.TanStack.Ast.Core;
using Moj.TanStack.Ast.CodeGen;
using Moj.TanStack.Dsl.Core.TypeInference;

namespace Moj.TanStack.Dsl.TanStack.Query;

/// <summary>
/// Type-safe TanStack Query DSL that infers everything from C# types.
/// No manual TypeScript construction needed!
/// </summary>
public static class TanStackTypedQuery
{
    /// <summary>
    /// Create a query definition for fetching data of type TData.
    /// </summary>
    public static TypedQueryBuilder<TData> Query<TData>() where TData : class
        => new();

    /// <summary>
    /// Create a mutation definition.
    /// </summary>
    public static TypedMutationBuilder<TData, TVariables> Mutation<TData, TVariables>()
        where TData : class
        where TVariables : class
        => new();

    /// <summary>
    /// Create a query definitions file with multiple queries.
    /// </summary>
    public static QueryDefinitionsBuilder Queries() => new();
}

/// <summary>
/// Fluent builder for TanStack Query using C# type inference.
/// </summary>
public sealed class TypedQueryBuilder<TData> where TData : class
{
    private string _name = "query";
    private readonly List<string> _queryKey = [];
    private string? _endpoint;
    private int? _staleTime;
    private int? _gcTime;
    private int? _refetchInterval;
    private bool _enabled = true;
    private int? _retry;
    private bool _exported = true;

    /// <summary>
    /// Set the query name.
    /// </summary>
    public TypedQueryBuilder<TData> Named(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Set the query key parts.
    /// </summary>
    public TypedQueryBuilder<TData> Key(params string[] parts)
    {
        _queryKey.AddRange(parts);
        return this;
    }

    /// <summary>
    /// Set the API endpoint to fetch from.
    /// </summary>
    public TypedQueryBuilder<TData> Endpoint(string url)
    {
        _endpoint = url;
        return this;
    }

    /// <summary>
    /// Set stale time in milliseconds.
    /// </summary>
    public TypedQueryBuilder<TData> StaleTime(int ms)
    {
        _staleTime = ms;
        return this;
    }

    /// <summary>
    /// Set garbage collection time in milliseconds.
    /// </summary>
    public TypedQueryBuilder<TData> GcTime(int ms)
    {
        _gcTime = ms;
        return this;
    }

    /// <summary>
    /// Set refetch interval in milliseconds.
    /// </summary>
    public TypedQueryBuilder<TData> RefetchInterval(int ms)
    {
        _refetchInterval = ms;
        return this;
    }

    /// <summary>
    /// Set enabled condition (defaults to true).
    /// </summary>
    public TypedQueryBuilder<TData> Enabled(bool enabled)
    {
        _enabled = enabled;
        return this;
    }

    /// <summary>
    /// Set retry count.
    /// </summary>
    public TypedQueryBuilder<TData> Retry(int count)
    {
        _retry = count;
        return this;
    }

    /// <summary>
    /// Set whether this query is exported (default true).
    /// </summary>
    public TypedQueryBuilder<TData> Export(bool export = true)
    {
        _exported = export;
        return this;
    }

    /// <summary>
    /// Build the query definition.
    /// </summary>
    public QueryDefinition<TData> Build()
    {
        return new QueryDefinition<TData>(
            _name,
            typeof(TData),
            _queryKey,
            _endpoint,
            _staleTime,
            _gcTime,
            _refetchInterval,
            _enabled,
            _retry,
            _exported);
    }
}

/// <summary>
/// Fluent builder for TanStack Mutation using C# type inference.
/// </summary>
public sealed class TypedMutationBuilder<TData, TVariables>
    where TData : class
    where TVariables : class
{
    private string _name = "mutation";
    private readonly List<string> _mutationKey = [];
    private string? _endpoint;
    private string _method = "POST";
    private int? _retry;
    private bool _exported = true;
    private Expression<Action<TData>>? _onSuccess;
    private Expression<Action<Exception>>? _onError;

    /// <summary>
    /// Set the mutation name.
    /// </summary>
    public TypedMutationBuilder<TData, TVariables> Named(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Set the mutation key parts.
    /// </summary>
    public TypedMutationBuilder<TData, TVariables> Key(params string[] parts)
    {
        _mutationKey.AddRange(parts);
        return this;
    }

    /// <summary>
    /// Set the API endpoint to mutate.
    /// </summary>
    public TypedMutationBuilder<TData, TVariables> Endpoint(string url)
    {
        _endpoint = url;
        return this;
    }

    /// <summary>
    /// Set the HTTP method (default POST).
    /// </summary>
    public TypedMutationBuilder<TData, TVariables> Method(string method)
    {
        _method = method;
        return this;
    }

    /// <summary>
    /// Set retry count.
    /// </summary>
    public TypedMutationBuilder<TData, TVariables> Retry(int count)
    {
        _retry = count;
        return this;
    }

    /// <summary>
    /// Set whether this mutation is exported (default true).
    /// </summary>
    public TypedMutationBuilder<TData, TVariables> Export(bool export = true)
    {
        _exported = export;
        return this;
    }

    /// <summary>
    /// Build the mutation definition.
    /// </summary>
    public MutationDefinition<TData, TVariables> Build()
    {
        return new MutationDefinition<TData, TVariables>(
            _name,
            typeof(TData),
            typeof(TVariables),
            _mutationKey,
            _endpoint,
            _method,
            _retry,
            _exported);
    }
}

/// <summary>
/// Builder for a file containing multiple query definitions.
/// </summary>
public sealed class QueryDefinitionsBuilder
{
    private readonly List<IQueryDefinition> _queries = [];
    private readonly List<IMutationDefinition> _mutations = [];

    /// <summary>
    /// Add a query definition.
    /// </summary>
    public QueryDefinitionsBuilder Add<TData>(QueryDefinition<TData> query) where TData : class
    {
        _queries.Add(query);
        return this;
    }

    /// <summary>
    /// Add a mutation definition.
    /// </summary>
    public QueryDefinitionsBuilder Add<TData, TVariables>(MutationDefinition<TData, TVariables> mutation)
        where TData : class
        where TVariables : class
    {
        _mutations.Add(mutation);
        return this;
    }

    /// <summary>
    /// Build all definitions.
    /// </summary>
    public QueryDefinitions Build()
    {
        return new QueryDefinitions(_queries, _mutations);
    }
}

// ========== Definition Records ==========

public interface IQueryDefinition
{
    string Name { get; }
    Type DataType { get; }
    IEnumerable<TsNode> ToAst();
    string ToTypeScript();
}

public interface IMutationDefinition
{
    string Name { get; }
    Type DataType { get; }
    Type VariablesType { get; }
    IEnumerable<TsNode> ToAst();
    string ToTypeScript();
}

/// <summary>
/// Represents a complete query definition ready for TypeScript generation.
/// </summary>
public sealed class QueryDefinition<TData> : IQueryDefinition where TData : class
{
    public string Name { get; }
    public Type DataType { get; }
    public IReadOnlyList<string> QueryKey { get; }
    public string? Endpoint { get; }
    public int? StaleTime { get; }
    public int? GcTime { get; }
    public int? RefetchInterval { get; }
    public bool Enabled { get; }
    public int? Retry { get; }
    public bool IsExported { get; }

    internal QueryDefinition(
        string name,
        Type dataType,
        List<string> queryKey,
        string? endpoint,
        int? staleTime,
        int? gcTime,
        int? refetchInterval,
        bool enabled,
        int? retry,
        bool exported)
    {
        Name = name;
        DataType = dataType;
        QueryKey = queryKey;
        Endpoint = endpoint;
        StaleTime = staleTime;
        GcTime = gcTime;
        RefetchInterval = refetchInterval;
        Enabled = enabled;
        Retry = retry;
        IsExported = exported;
    }

    public string ToTypeScript()
    {
        var emitter = new TypeScriptEmitter();
        var program = new TsProgram(ToAst().ToList());
        return emitter.Emit(program);
    }

    public IEnumerable<TsNode> ToAst()
    {
        var nodes = new List<TsNode>();

        // Import
        nodes.Add(new TsImportDeclaration(
            "@tanstack/react-query",
            new TsImportClause(NamedImports: [
                new TsImportSpecifier("useQuery"),
                new TsImportSpecifier("queryOptions")
            ])));

        // Generate interface for data type
        nodes.Add(TypeToTsConverter.ToInterface(DataType, export: IsExported));

        // Generate query options
        var optionProps = new List<TsObjectElement>();

        // Query key
        var keyElements = QueryKey.Select(k => (TsExpression?)new TsLiteral(k, TsLiteralKind.String)).ToList();
        optionProps.Add(new TsPropertyAssignment(
            new TsIdentifier("queryKey"),
            new TsArrayLiteral(keyElements)));

        // Query function
        if (Endpoint != null)
        {
            var fetchCall = new TsCallExpression(
                new TsIdentifier("fetch"),
                [new TsLiteral(Endpoint, TsLiteralKind.String)]);

            var jsonCall = new TsCallExpression(
                new TsMemberAccess(new TsIdentifier("response"), "json"),
                []);

            var queryFn = new TsArrowFunction(
                [],
                new TsCallExpression(
                    new TsMemberAccess(fetchCall, "then"),
                    [new TsArrowFunction(
                        [new TsParameter(new TsIdentifierBinding("response"))],
                        jsonCall)]));

            optionProps.Add(new TsPropertyAssignment(
                new TsIdentifier("queryFn"),
                queryFn));
        }

        if (StaleTime.HasValue)
        {
            optionProps.Add(new TsPropertyAssignment(
                new TsIdentifier("staleTime"),
                new TsLiteral(StaleTime.Value, TsLiteralKind.Number)));
        }

        if (GcTime.HasValue)
        {
            optionProps.Add(new TsPropertyAssignment(
                new TsIdentifier("gcTime"),
                new TsLiteral(GcTime.Value, TsLiteralKind.Number)));
        }

        if (RefetchInterval.HasValue)
        {
            optionProps.Add(new TsPropertyAssignment(
                new TsIdentifier("refetchInterval"),
                new TsLiteral(RefetchInterval.Value, TsLiteralKind.Number)));
        }

        if (!Enabled)
        {
            optionProps.Add(new TsPropertyAssignment(
                new TsIdentifier("enabled"),
                new TsLiteral(false, TsLiteralKind.Boolean)));
        }

        if (Retry.HasValue)
        {
            optionProps.Add(new TsPropertyAssignment(
                new TsIdentifier("retry"),
                new TsLiteral(Retry.Value, TsLiteralKind.Number)));
        }

        var optionsObject = new TsObjectLiteral(optionProps);
        var queryOptionsCall = new TsCallExpression(
            new TsIdentifier("queryOptions"),
            [optionsObject]);

        nodes.Add(new TsVariableDeclaration(
            TsVariableKind.Const,
            [new TsVariableDeclarator(
                new TsIdentifierBinding($"{Name}Options"),
                Initializer: queryOptionsCall)],
            IsExported));

        // Generate hook
        var hookBody = new TsCallExpression(
            new TsIdentifier("useQuery"),
            [new TsIdentifier($"{Name}Options")]);

        var hook = new TsArrowFunction([], hookBody);

        nodes.Add(new TsVariableDeclaration(
            TsVariableKind.Const,
            [new TsVariableDeclarator(
                new TsIdentifierBinding($"use{ToPascalCase(Name)}"),
                Initializer: hook)],
            IsExported));

        return nodes;
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToUpperInvariant(name[0]) + name[1..];
    }
}

/// <summary>
/// Represents a complete mutation definition ready for TypeScript generation.
/// </summary>
public sealed class MutationDefinition<TData, TVariables> : IMutationDefinition
    where TData : class
    where TVariables : class
{
    public string Name { get; }
    public Type DataType { get; }
    public Type VariablesType { get; }
    public IReadOnlyList<string> MutationKey { get; }
    public string? Endpoint { get; }
    public string Method { get; }
    public int? Retry { get; }
    public bool IsExported { get; }

    internal MutationDefinition(
        string name,
        Type dataType,
        Type variablesType,
        List<string> mutationKey,
        string? endpoint,
        string method,
        int? retry,
        bool exported)
    {
        Name = name;
        DataType = dataType;
        VariablesType = variablesType;
        MutationKey = mutationKey;
        Endpoint = endpoint;
        Method = method;
        Retry = retry;
        IsExported = exported;
    }

    public string ToTypeScript()
    {
        var emitter = new TypeScriptEmitter();
        var program = new TsProgram(ToAst().ToList());
        return emitter.Emit(program);
    }

    public IEnumerable<TsNode> ToAst()
    {
        var nodes = new List<TsNode>();

        // Import
        nodes.Add(new TsImportDeclaration(
            "@tanstack/react-query",
            new TsImportClause(NamedImports: [new TsImportSpecifier("useMutation")])));

        // Generate interfaces
        nodes.Add(TypeToTsConverter.ToInterface(DataType, export: IsExported));
        if (DataType != VariablesType)
        {
            nodes.Add(TypeToTsConverter.ToInterface(VariablesType, export: IsExported));
        }

        // Generate mutation options
        var optionProps = new List<TsObjectElement>();

        // Mutation key
        if (MutationKey.Count > 0)
        {
            var keyElements = MutationKey.Select(k => (TsExpression?)new TsLiteral(k, TsLiteralKind.String)).ToList();
            optionProps.Add(new TsPropertyAssignment(
                new TsIdentifier("mutationKey"),
                new TsArrayLiteral(keyElements)));
        }

        // Mutation function
        if (Endpoint != null)
        {
            var fetchOptions = new TsObjectLiteral([
                new TsPropertyAssignment(new TsIdentifier("method"), new TsLiteral(Method, TsLiteralKind.String)),
                new TsPropertyAssignment(new TsIdentifier("headers"),
                    new TsObjectLiteral([
                        new TsPropertyAssignment(
                            new TsLiteral("Content-Type", TsLiteralKind.String),
                            new TsLiteral("application/json", TsLiteralKind.String))
                    ])),
                new TsPropertyAssignment(new TsIdentifier("body"),
                    new TsCallExpression(
                        new TsMemberAccess(new TsIdentifier("JSON"), "stringify"),
                        [new TsIdentifier("variables")]))
            ]);

            var fetchCall = new TsCallExpression(
                new TsIdentifier("fetch"),
                [new TsLiteral(Endpoint, TsLiteralKind.String), fetchOptions]);

            var jsonCall = new TsCallExpression(
                new TsMemberAccess(new TsIdentifier("response"), "json"),
                []);

            var mutationFn = new TsArrowFunction(
                [new TsParameter(new TsIdentifierBinding("variables"), TypeToTsConverter.ToTsType(VariablesType))],
                new TsCallExpression(
                    new TsMemberAccess(fetchCall, "then"),
                    [new TsArrowFunction(
                        [new TsParameter(new TsIdentifierBinding("response"))],
                        jsonCall)]));

            optionProps.Add(new TsPropertyAssignment(
                new TsIdentifier("mutationFn"),
                mutationFn));
        }

        if (Retry.HasValue)
        {
            optionProps.Add(new TsPropertyAssignment(
                new TsIdentifier("retry"),
                new TsLiteral(Retry.Value, TsLiteralKind.Number)));
        }

        var optionsObject = new TsObjectLiteral(optionProps);

        nodes.Add(new TsVariableDeclaration(
            TsVariableKind.Const,
            [new TsVariableDeclarator(
                new TsIdentifierBinding($"{Name}Options"),
                Initializer: optionsObject)],
            IsExported));

        // Generate hook
        var hookBody = new TsCallExpression(
            new TsIdentifier("useMutation"),
            [new TsIdentifier($"{Name}Options")]);

        var hook = new TsArrowFunction([], hookBody);

        nodes.Add(new TsVariableDeclaration(
            TsVariableKind.Const,
            [new TsVariableDeclarator(
                new TsIdentifierBinding($"use{ToPascalCase(Name)}"),
                Initializer: hook)],
            IsExported));

        return nodes;
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToUpperInvariant(name[0]) + name[1..];
    }
}

/// <summary>
/// Container for multiple query and mutation definitions.
/// </summary>
public sealed class QueryDefinitions
{
    public IReadOnlyList<IQueryDefinition> Queries { get; }
    public IReadOnlyList<IMutationDefinition> Mutations { get; }

    internal QueryDefinitions(List<IQueryDefinition> queries, List<IMutationDefinition> mutations)
    {
        Queries = queries;
        Mutations = mutations;
    }

    public string ToTypeScript()
    {
        var emitter = new TypeScriptEmitter();
        var program = new TsProgram(ToAst().ToList());
        return emitter.Emit(program);
    }

    public IEnumerable<TsNode> ToAst()
    {
        var nodes = new List<TsNode>();
        var generatedTypes = new HashSet<string>();

        // Collect all imports
        var imports = new HashSet<string> { "useQuery", "queryOptions" };
        if (Mutations.Count > 0)
        {
            imports.Add("useMutation");
        }

        nodes.Add(new TsImportDeclaration(
            "@tanstack/react-query",
            new TsImportClause(NamedImports: imports.Select(i => new TsImportSpecifier(i)).ToList())));

        // Generate all dependent types first
        var allTypes = new HashSet<Type>();
        foreach (var query in Queries)
        {
            CollectDependentTypes(query.DataType, allTypes);
        }
        foreach (var mutation in Mutations)
        {
            CollectDependentTypes(mutation.DataType, allTypes);
            CollectDependentTypes(mutation.VariablesType, allTypes);
        }

        foreach (var type in allTypes.Where(t => !IsPrimitive(t)))
        {
            if (!generatedTypes.Contains(type.Name))
            {
                generatedTypes.Add(type.Name);

                if (type.IsEnum)
                {
                    nodes.Add(EnumToTsConverter.ToTsEnum(type, export: true));
                }
                else
                {
                    nodes.Add(TypeToTsConverter.ToInterface(type, export: true));
                }
            }
        }

        // Generate query options and hooks
        foreach (var query in Queries)
        {
            nodes.AddRange(GenerateQueryNodes(query, generatedTypes));
        }

        // Generate mutation options and hooks
        foreach (var mutation in Mutations)
        {
            nodes.AddRange(GenerateMutationNodes(mutation, generatedTypes));
        }

        return nodes;
    }

    private IEnumerable<TsNode> GenerateQueryNodes(IQueryDefinition query, HashSet<string> generatedTypes)
    {
        // Use reflection to get the generic version
        var queryType = query.GetType();
        if (queryType.IsGenericType)
        {
            var toAstMethod = queryType.GetMethod("ToAst");
            if (toAstMethod != null)
            {
                var nodes = (IEnumerable<TsNode>)toAstMethod.Invoke(query, null)!;
                // Skip imports and already generated interfaces
                return nodes.Where(n => n is not TsImportDeclaration &&
                                        !(n is TsInterfaceDeclaration iface && generatedTypes.Contains(iface.Name)));
            }
        }
        return [];
    }

    private IEnumerable<TsNode> GenerateMutationNodes(IMutationDefinition mutation, HashSet<string> generatedTypes)
    {
        var mutationType = mutation.GetType();
        if (mutationType.IsGenericType)
        {
            var toAstMethod = mutationType.GetMethod("ToAst");
            if (toAstMethod != null)
            {
                var nodes = (IEnumerable<TsNode>)toAstMethod.Invoke(mutation, null)!;
                return nodes.Where(n => n is not TsImportDeclaration &&
                                        !(n is TsInterfaceDeclaration iface && generatedTypes.Contains(iface.Name)));
            }
        }
        return [];
    }

    private void CollectDependentTypes(Type type, HashSet<Type> collected)
    {
        if (collected.Contains(type) || IsPrimitive(type)) return;

        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
        {
            CollectDependentTypes(underlying, collected);
            return;
        }

        if (type.IsArray)
        {
            CollectDependentTypes(type.GetElementType()!, collected);
            return;
        }

        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments())
                CollectDependentTypes(arg, collected);
            return;
        }

        collected.Add(type);

        if (!type.IsEnum)
        {
            foreach (var prop in type.GetProperties())
            {
                CollectDependentTypes(prop.PropertyType, collected);
            }
        }
    }

    private bool IsPrimitive(Type type)
    {
        return type == typeof(string) || type == typeof(bool) ||
               type == typeof(int) || type == typeof(long) || type == typeof(double) ||
               type == typeof(decimal) || type == typeof(DateTime) || type == typeof(Guid);
    }
}
