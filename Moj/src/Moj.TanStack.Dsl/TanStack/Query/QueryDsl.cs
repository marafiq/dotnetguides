using Moj.TanStack.Ast.Core;
using Moj.TanStack.Dsl.Core;

namespace Moj.TanStack.Dsl.TanStack.Query;

/// <summary>
/// DSL for TanStack Query - Powerful async state management
/// </summary>
public static class TanStackQuery
{
    /// <summary>
    /// Creates a query options builder
    /// </summary>
    public static QueryOptionsBuilder<TData> QueryOptions<TData>() => new();

    /// <summary>
    /// Creates a mutation options builder
    /// </summary>
    public static MutationOptionsBuilder<TData, TVariables> MutationOptions<TData, TVariables>() => new();

    /// <summary>
    /// Creates a query client builder
    /// </summary>
    public static QueryClientBuilder QueryClient() => new();

    /// <summary>
    /// Creates an infinite query options builder
    /// </summary>
    public static InfiniteQueryOptionsBuilder<TData> InfiniteQueryOptions<TData>() => new();

    /// <summary>
    /// Import core from @tanstack/react-query
    /// </summary>
    public static TsImportDeclaration ImportReact(params string[] imports) =>
        Ts.Import("@tanstack/react-query")
            .Named(imports.Length > 0 ? imports :
                ["useQuery", "useMutation", "useQueryClient", "QueryClient", "QueryClientProvider"])
            .Build();

    /// <summary>
    /// Import query options helper
    /// </summary>
    public static TsImportDeclaration ImportQueryOptions() =>
        Ts.Import("@tanstack/react-query").Named("queryOptions").Build();

    /// <summary>
    /// Import infinite query hooks
    /// </summary>
    public static TsImportDeclaration ImportInfiniteQuery() =>
        Ts.Import("@tanstack/react-query").Named("useInfiniteQuery", "infiniteQueryOptions").Build();

    /// <summary>
    /// Import suspense hooks
    /// </summary>
    public static TsImportDeclaration ImportSuspense() =>
        Ts.Import("@tanstack/react-query").Named("useSuspenseQuery", "useSuspenseInfiniteQuery").Build();
}

/// <summary>
/// Builder for query options (for type-safe query definitions)
/// </summary>
public sealed class QueryOptionsBuilder<TData>
{
    private TsExprBase? _queryKey;
    private TsExprBase? _queryFn;
    private TsExprBase? _staleTime;
    private TsExprBase? _gcTime;
    private TsExprBase? _refetchInterval;
    private TsExprBase? _enabled;
    private TsExprBase? _select;
    private TsExprBase? _placeholderData;
    private TsExprBase? _initialData;
    private TsExprBase? _retry;
    private TsExprBase? _retryDelay;
    private string? _variableName;
    private bool _exported;
    private bool _useQueryOptionsHelper;

    /// <summary>
    /// Set the query key
    /// </summary>
    public QueryOptionsBuilder<TData> QueryKey(params TsExprBase[] keyParts)
    {
        _queryKey = Ts.Array([.. keyParts]);
        return this;
    }

    /// <summary>
    /// Set the query key with a string array
    /// </summary>
    public QueryOptionsBuilder<TData> QueryKey(params string[] keys)
    {
        _queryKey = Ts.Array([.. keys.Select(Ts.String)]);
        return this;
    }

    /// <summary>
    /// Set the query function
    /// </summary>
    public QueryOptionsBuilder<TData> QueryFn(TsExprBase fn)
    {
        _queryFn = fn;
        return this;
    }

    /// <summary>
    /// Set query function with async arrow
    /// </summary>
    public QueryOptionsBuilder<TData> QueryFn(Func<TsAsyncArrowBuilder<TData>, TsAsyncArrowBuilder<TData>> configure)
    {
        var arrow = configure(Ts.AsyncArrow<TData>());
        _queryFn = arrow.Build();
        return this;
    }

    /// <summary>
    /// Set stale time in milliseconds
    /// </summary>
    public QueryOptionsBuilder<TData> StaleTime(int ms)
    {
        _staleTime = Ts.Int(ms);
        return this;
    }

    /// <summary>
    /// Set stale time to Infinity
    /// </summary>
    public QueryOptionsBuilder<TData> StaleTimeInfinity()
    {
        _staleTime = Ts.Var("Infinity");
        return this;
    }

    /// <summary>
    /// Set garbage collection time in milliseconds
    /// </summary>
    public QueryOptionsBuilder<TData> GcTime(int ms)
    {
        _gcTime = Ts.Int(ms);
        return this;
    }

    /// <summary>
    /// Set refetch interval
    /// </summary>
    public QueryOptionsBuilder<TData> RefetchInterval(int ms)
    {
        _refetchInterval = Ts.Int(ms);
        return this;
    }

    /// <summary>
    /// Set enabled condition
    /// </summary>
    public QueryOptionsBuilder<TData> Enabled(TsExpr<bool> condition)
    {
        _enabled = condition;
        return this;
    }

    /// <summary>
    /// Set select transformer
    /// </summary>
    public QueryOptionsBuilder<TData> Select<TSelected>(TsExprBase selector)
    {
        _select = selector;
        return this;
    }

    /// <summary>
    /// Set placeholder data
    /// </summary>
    public QueryOptionsBuilder<TData> PlaceholderData(TsExprBase data)
    {
        _placeholderData = data;
        return this;
    }

    /// <summary>
    /// Set initial data
    /// </summary>
    public QueryOptionsBuilder<TData> InitialData(TsExprBase data)
    {
        _initialData = data;
        return this;
    }

    /// <summary>
    /// Set retry count or boolean
    /// </summary>
    public QueryOptionsBuilder<TData> Retry(int count)
    {
        _retry = Ts.Int(count);
        return this;
    }

    /// <summary>
    /// Disable retries
    /// </summary>
    public QueryOptionsBuilder<TData> NoRetry()
    {
        _retry = Ts.False;
        return this;
    }

    /// <summary>
    /// Set retry delay
    /// </summary>
    public QueryOptionsBuilder<TData> RetryDelay(int ms)
    {
        _retryDelay = Ts.Int(ms);
        return this;
    }

    /// <summary>
    /// Use queryOptions() helper for type inference
    /// </summary>
    public QueryOptionsBuilder<TData> UseHelper()
    {
        _useQueryOptionsHelper = true;
        return this;
    }

    public QueryOptionsBuilder<TData> As(string name)
    {
        _variableName = name;
        return this;
    }

    public QueryOptionsBuilder<TData> Export()
    {
        _exported = true;
        return this;
    }

    public TsExprBase BuildExpression()
    {
        var options = Ts.Object<object>();

        if (_queryKey != null)
            options.With("queryKey", _queryKey);
        if (_queryFn != null)
            options.With("queryFn", _queryFn);
        if (_staleTime != null)
            options.With("staleTime", _staleTime);
        if (_gcTime != null)
            options.With("gcTime", _gcTime);
        if (_refetchInterval != null)
            options.With("refetchInterval", _refetchInterval);
        if (_enabled != null)
            options.With("enabled", _enabled);
        if (_select != null)
            options.With("select", _select);
        if (_placeholderData != null)
            options.With("placeholderData", _placeholderData);
        if (_initialData != null)
            options.With("initialData", _initialData);
        if (_retry != null)
            options.With("retry", _retry);
        if (_retryDelay != null)
            options.With("retryDelay", _retryDelay);

        if (_useQueryOptionsHelper)
            return Ts.Var("queryOptions").Invoke(options.Build());

        return options.Build();
    }

    public TsVariableDeclaration Build()
    {
        var name = _variableName ?? "queryOptions";
        var builder = Ts.Const<object>(name).Value(BuildExpression());
        if (_exported) builder.Export();
        return builder.Build();
    }
}

/// <summary>
/// Builder for mutation options
/// </summary>
public sealed class MutationOptionsBuilder<TData, TVariables>
{
    private TsExprBase? _mutationKey;
    private TsExprBase? _mutationFn;
    private TsExprBase? _onSuccess;
    private TsExprBase? _onError;
    private TsExprBase? _onSettled;
    private TsExprBase? _onMutate;
    private TsExprBase? _retry;
    private string? _variableName;
    private bool _exported;

    /// <summary>
    /// Set the mutation key
    /// </summary>
    public MutationOptionsBuilder<TData, TVariables> MutationKey(params string[] keys)
    {
        _mutationKey = Ts.Array([.. keys.Select(Ts.String)]);
        return this;
    }

    /// <summary>
    /// Set the mutation function
    /// </summary>
    public MutationOptionsBuilder<TData, TVariables> MutationFn(TsExprBase fn)
    {
        _mutationFn = fn;
        return this;
    }

    /// <summary>
    /// Set mutation function with async arrow
    /// </summary>
    public MutationOptionsBuilder<TData, TVariables> MutationFn(
        Func<TsAsyncArrowBuilder<TVariables, TData>, TsAsyncArrowBuilder<TVariables, TData>> configure)
    {
        var arrow = configure(Ts.AsyncArrow<TVariables, TData>("variables"));
        _mutationFn = arrow.Build();
        return this;
    }

    /// <summary>
    /// Set onSuccess callback
    /// </summary>
    public MutationOptionsBuilder<TData, TVariables> OnSuccess(TsExprBase handler)
    {
        _onSuccess = handler;
        return this;
    }

    /// <summary>
    /// Set onError callback
    /// </summary>
    public MutationOptionsBuilder<TData, TVariables> OnError(TsExprBase handler)
    {
        _onError = handler;
        return this;
    }

    /// <summary>
    /// Set onSettled callback
    /// </summary>
    public MutationOptionsBuilder<TData, TVariables> OnSettled(TsExprBase handler)
    {
        _onSettled = handler;
        return this;
    }

    /// <summary>
    /// Set onMutate callback (optimistic updates)
    /// </summary>
    public MutationOptionsBuilder<TData, TVariables> OnMutate(TsExprBase handler)
    {
        _onMutate = handler;
        return this;
    }

    /// <summary>
    /// Set retry count
    /// </summary>
    public MutationOptionsBuilder<TData, TVariables> Retry(int count)
    {
        _retry = Ts.Int(count);
        return this;
    }

    public MutationOptionsBuilder<TData, TVariables> As(string name)
    {
        _variableName = name;
        return this;
    }

    public MutationOptionsBuilder<TData, TVariables> Export()
    {
        _exported = true;
        return this;
    }

    public TsExprBase BuildExpression()
    {
        var options = Ts.Object<object>();

        if (_mutationKey != null)
            options.With("mutationKey", _mutationKey);
        if (_mutationFn != null)
            options.With("mutationFn", _mutationFn);
        if (_onSuccess != null)
            options.With("onSuccess", _onSuccess);
        if (_onError != null)
            options.With("onError", _onError);
        if (_onSettled != null)
            options.With("onSettled", _onSettled);
        if (_onMutate != null)
            options.With("onMutate", _onMutate);
        if (_retry != null)
            options.With("retry", _retry);

        return options.Build();
    }

    public TsVariableDeclaration Build()
    {
        var name = _variableName ?? "mutationOptions";
        var builder = Ts.Const<object>(name).Value(BuildExpression());
        if (_exported) builder.Export();
        return builder.Build();
    }
}

/// <summary>
/// Builder for infinite query options
/// </summary>
public sealed class InfiniteQueryOptionsBuilder<TData>
{
    private TsExprBase? _queryKey;
    private TsExprBase? _queryFn;
    private TsExprBase? _getNextPageParam;
    private TsExprBase? _getPreviousPageParam;
    private TsExprBase? _initialPageParam;
    private TsExprBase? _maxPages;
    private TsExprBase? _staleTime;
    private string? _variableName;
    private bool _exported;
    private bool _useHelper;

    public InfiniteQueryOptionsBuilder<TData> QueryKey(params string[] keys)
    {
        _queryKey = Ts.Array([.. keys.Select(Ts.String)]);
        return this;
    }

    public InfiniteQueryOptionsBuilder<TData> QueryFn(TsExprBase fn)
    {
        _queryFn = fn;
        return this;
    }

    public InfiniteQueryOptionsBuilder<TData> GetNextPageParam(TsExprBase fn)
    {
        _getNextPageParam = fn;
        return this;
    }

    public InfiniteQueryOptionsBuilder<TData> GetPreviousPageParam(TsExprBase fn)
    {
        _getPreviousPageParam = fn;
        return this;
    }

    public InfiniteQueryOptionsBuilder<TData> InitialPageParam(TsExprBase param)
    {
        _initialPageParam = param;
        return this;
    }

    public InfiniteQueryOptionsBuilder<TData> MaxPages(int count)
    {
        _maxPages = Ts.Int(count);
        return this;
    }

    public InfiniteQueryOptionsBuilder<TData> StaleTime(int ms)
    {
        _staleTime = Ts.Int(ms);
        return this;
    }

    public InfiniteQueryOptionsBuilder<TData> UseHelper()
    {
        _useHelper = true;
        return this;
    }

    public InfiniteQueryOptionsBuilder<TData> As(string name)
    {
        _variableName = name;
        return this;
    }

    public InfiniteQueryOptionsBuilder<TData> Export()
    {
        _exported = true;
        return this;
    }

    public TsExprBase BuildExpression()
    {
        var options = Ts.Object<object>();

        if (_queryKey != null)
            options.With("queryKey", _queryKey);
        if (_queryFn != null)
            options.With("queryFn", _queryFn);
        if (_getNextPageParam != null)
            options.With("getNextPageParam", _getNextPageParam);
        if (_getPreviousPageParam != null)
            options.With("getPreviousPageParam", _getPreviousPageParam);
        if (_initialPageParam != null)
            options.With("initialPageParam", _initialPageParam);
        if (_maxPages != null)
            options.With("maxPages", _maxPages);
        if (_staleTime != null)
            options.With("staleTime", _staleTime);

        if (_useHelper)
            return Ts.Var("infiniteQueryOptions").Invoke(options.Build());

        return options.Build();
    }

    public TsVariableDeclaration Build()
    {
        var name = _variableName ?? "infiniteQueryOptions";
        var builder = Ts.Const<object>(name).Value(BuildExpression());
        if (_exported) builder.Export();
        return builder.Build();
    }
}

/// <summary>
/// Builder for QueryClient configuration
/// </summary>
public sealed class QueryClientBuilder
{
    private int? _defaultStaleTime;
    private int? _defaultGcTime;
    private int? _defaultRetry;
    private bool? _defaultRefetchOnWindowFocus;
    private bool? _defaultRefetchOnReconnect;
    private bool? _defaultRefetchOnMount;
    private string _variableName = "queryClient";
    private bool _exported;

    public QueryClientBuilder DefaultStaleTime(int ms)
    {
        _defaultStaleTime = ms;
        return this;
    }

    public QueryClientBuilder DefaultGcTime(int ms)
    {
        _defaultGcTime = ms;
        return this;
    }

    public QueryClientBuilder DefaultRetry(int count)
    {
        _defaultRetry = count;
        return this;
    }

    public QueryClientBuilder RefetchOnWindowFocus(bool value)
    {
        _defaultRefetchOnWindowFocus = value;
        return this;
    }

    public QueryClientBuilder RefetchOnReconnect(bool value)
    {
        _defaultRefetchOnReconnect = value;
        return this;
    }

    public QueryClientBuilder RefetchOnMount(bool value)
    {
        _defaultRefetchOnMount = value;
        return this;
    }

    public QueryClientBuilder As(string name)
    {
        _variableName = name;
        return this;
    }

    public QueryClientBuilder Export()
    {
        _exported = true;
        return this;
    }

    public TsExprBase BuildExpression()
    {
        var queryDefaults = Ts.Object<object>();
        var hasQueryDefaults = false;

        if (_defaultStaleTime.HasValue)
        {
            queryDefaults.With("staleTime", Ts.Int(_defaultStaleTime.Value));
            hasQueryDefaults = true;
        }
        if (_defaultGcTime.HasValue)
        {
            queryDefaults.With("gcTime", Ts.Int(_defaultGcTime.Value));
            hasQueryDefaults = true;
        }
        if (_defaultRetry.HasValue)
        {
            queryDefaults.With("retry", Ts.Int(_defaultRetry.Value));
            hasQueryDefaults = true;
        }
        if (_defaultRefetchOnWindowFocus.HasValue)
        {
            queryDefaults.With("refetchOnWindowFocus", Ts.Bool(_defaultRefetchOnWindowFocus.Value));
            hasQueryDefaults = true;
        }
        if (_defaultRefetchOnReconnect.HasValue)
        {
            queryDefaults.With("refetchOnReconnect", Ts.Bool(_defaultRefetchOnReconnect.Value));
            hasQueryDefaults = true;
        }
        if (_defaultRefetchOnMount.HasValue)
        {
            queryDefaults.With("refetchOnMount", Ts.Bool(_defaultRefetchOnMount.Value));
            hasQueryDefaults = true;
        }

        if (hasQueryDefaults)
        {
            var defaultOptions = Ts.Object<object>()
                .With("queries", queryDefaults.Build());

            return Ts.New<object>("QueryClient", Ts.Object<object>()
                .With("defaultOptions", defaultOptions.Build())
                .Build());
        }

        return Ts.New<object>("QueryClient");
    }

    public TsVariableDeclaration Build()
    {
        var builder = Ts.Const<object>(_variableName).Value(BuildExpression());
        if (_exported) builder.Export();
        return builder.Build();
    }
}

/// <summary>
/// Extension methods for query hooks
/// </summary>
public static class QueryExtensions
{
    /// <summary>useQuery hook</summary>
    public static TsDynamicExpr UseQuery(TsExprBase options) =>
        Ts.Var("useQuery").Invoke(options);

    /// <summary>useMutation hook</summary>
    public static TsDynamicExpr UseMutation(TsExprBase options) =>
        Ts.Var("useMutation").Invoke(options);

    /// <summary>useInfiniteQuery hook</summary>
    public static TsDynamicExpr UseInfiniteQuery(TsExprBase options) =>
        Ts.Var("useInfiniteQuery").Invoke(options);

    /// <summary>useSuspenseQuery hook</summary>
    public static TsDynamicExpr UseSuspenseQuery(TsExprBase options) =>
        Ts.Var("useSuspenseQuery").Invoke(options);

    /// <summary>useQueryClient hook</summary>
    public static TsDynamicExpr UseQueryClient() =>
        Ts.Var("useQueryClient").Invoke();

    /// <summary>useIsFetching hook</summary>
    public static TsDynamicExpr UseIsFetching(TsExprBase? filters = null) =>
        filters != null
            ? Ts.Var("useIsFetching").Invoke(filters)
            : Ts.Var("useIsFetching").Invoke();

    /// <summary>useIsMutating hook</summary>
    public static TsDynamicExpr UseIsMutating(TsExprBase? filters = null) =>
        filters != null
            ? Ts.Var("useIsMutating").Invoke(filters)
            : Ts.Var("useIsMutating").Invoke();

    /// <summary>queryClient.invalidateQueries</summary>
    public static TsDynamicExpr InvalidateQueries(this TsExprBase client, TsExprBase filters) =>
        client.Dot("invalidateQueries").Invoke(filters);

    /// <summary>queryClient.prefetchQuery</summary>
    public static TsDynamicExpr PrefetchQuery(this TsExprBase client, TsExprBase options) =>
        client.Dot("prefetchQuery").Invoke(options);

    /// <summary>queryClient.setQueryData</summary>
    public static TsDynamicExpr SetQueryData(this TsExprBase client, TsExprBase key, TsExprBase data) =>
        client.Dot("setQueryData").Invoke(key, data);

    /// <summary>queryClient.getQueryData</summary>
    public static TsDynamicExpr GetQueryData(this TsExprBase client, TsExprBase key) =>
        client.Dot("getQueryData").Invoke(key);
}
