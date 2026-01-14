using Moj.TanStack.Dsl.Core;
using Moj.TanStack.Dsl.TanStack.Query;
using Moj.TanStack.SourceGenerator.Attributes;

namespace Moj.Sandbox.Definitions;

/// <summary>
/// Product data types
/// </summary>
public interface IProduct
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    decimal Price { get; }
    string Category { get; }
    string[] Tags { get; }
    bool InStock { get; }
}

/// <summary>
/// DSL definition for TanStack Query - Product queries and mutations
/// </summary>
[TsQuery(Name = "productQueries")]
public static class ProductQueries
{
    /// <summary>
    /// Query imports
    /// </summary>
    public static TsImportDeclaration Imports =>
        TanStackQuery.ImportReact(
            "useQuery",
            "useMutation",
            "useQueryClient",
            "QueryClient",
            "QueryClientProvider");

    /// <summary>
    /// Query options helper import
    /// </summary>
    public static TsImportDeclaration QueryOptionsImport =>
        TanStackQuery.ImportQueryOptions();

    /// <summary>
    /// Product type definition
    /// </summary>
    public static TsInterfaceDeclaration ProductInterface =>
        Ts.Interface("Product")
            .Property<string>("id")
            .Property<string>("name")
            .Property<string>("description")
            .Property<double>("price")
            .Property<string>("category")
            .Property("tags", new Moj.TanStack.Ast.Core.TsArrayType(Moj.TanStack.Ast.Core.TsPrimitiveTypes.String))
            .Property<bool>("inStock")
            .Export()
            .Build();

    /// <summary>
    /// Products response type
    /// </summary>
    public static TsInterfaceDeclaration ProductsResponseInterface =>
        Ts.Interface("ProductsResponse")
            .Property("products", new Moj.TanStack.Ast.Core.TsArrayType(Ts.TypeRef("Product")))
            .Property<int>("total")
            .Property<int>("page")
            .Property<int>("pageSize")
            .Property<bool>("hasNextPage")
            .Export()
            .Build();

    /// <summary>
    /// Create product input type
    /// </summary>
    public static TsInterfaceDeclaration CreateProductInput =>
        Ts.Interface("CreateProductInput")
            .Property<string>("name")
            .Property<string>("description")
            .Property<double>("price")
            .Property<string>("category")
            .Property("tags", new Moj.TanStack.Ast.Core.TsArrayType(Moj.TanStack.Ast.Core.TsPrimitiveTypes.String), optional: true)
            .Export()
            .Build();

    /// <summary>
    /// Update product input type
    /// </summary>
    public static TsInterfaceDeclaration UpdateProductInput =>
        Ts.Interface("UpdateProductInput")
            .Property<string>("name", optional: true)
            .Property<string>("description", optional: true)
            .Property<double>("price", optional: true)
            .Property<string>("category", optional: true)
            .Property("tags", new Moj.TanStack.Ast.Core.TsArrayType(Moj.TanStack.Ast.Core.TsPrimitiveTypes.String), optional: true)
            .Property<bool>("inStock", optional: true)
            .Export()
            .Build();

    /// <summary>
    /// Query client configuration
    /// </summary>
    public static TsVariableDeclaration QueryClient =>
        TanStackQuery.QueryClient()
            .DefaultStaleTime(5 * 60 * 1000) // 5 minutes
            .DefaultGcTime(10 * 60 * 1000)   // 10 minutes
            .DefaultRetry(3)
            .RefetchOnWindowFocus(false)
            .As("queryClient")
            .Export()
            .Build();

    /// <summary>
    /// Products list query options factory
    /// </summary>
    public static TsVariableDeclaration ProductsQueryOptions =>
        Ts.Const<object>("productsQueryOptions")
            .Value(Ts.Arrow<object, object>("filters")
                .Returns(f =>
                    TanStackQuery.QueryOptions<object>()
                        .QueryKey("products", f)
                        .QueryFn(fn => fn.Returns(
                            Ts.Var("api").Dot("products").Dot("list").Invoke(f)))
                        .StaleTime(5 * 60 * 1000)
                        .UseHelper()
                        .BuildExpression()))
            .Export()
            .Build();

    /// <summary>
    /// Single product query options factory
    /// </summary>
    public static TsVariableDeclaration ProductQueryOptions =>
        Ts.Const<object>("productQueryOptions")
            .Value(Ts.Arrow<string, object>("productId")
                .Returns(id =>
                    TanStackQuery.QueryOptions<object>()
                        .QueryKey("product", id)
                        .QueryFn(fn => fn.Returns(
                            Ts.Var("api").Dot("products").Dot("get").Invoke(id)))
                        .StaleTime(10 * 60 * 1000)
                        .UseHelper()
                        .BuildExpression()))
            .Export()
            .Build();

    /// <summary>
    /// Products by category query options
    /// </summary>
    public static TsVariableDeclaration ProductsByCategoryOptions =>
        Ts.Const<object>("productsByCategoryOptions")
            .Value(Ts.Arrow<string, object>("category")
                .Returns(cat =>
                    TanStackQuery.QueryOptions<object>()
                        .QueryKey("products", "category", cat)
                        .QueryFn(fn => fn.Returns(
                            Ts.Var("api").Dot("products").Dot("byCategory").Invoke(cat)))
                        .StaleTimeInfinity()
                        .UseHelper()
                        .BuildExpression()))
            .Export()
            .Build();

    /// <summary>
    /// Create product mutation options
    /// </summary>
    public static TsVariableDeclaration CreateProductMutation =>
        TanStackQuery.MutationOptions<object, object>()
            .MutationKey("createProduct")
            .MutationFn(fn => fn.Returns(input =>
                Ts.Var("api").Dot("products").Dot("create").Invoke(input)))
            .OnSuccess(Ts.AsyncArrow<object, object>("data")
                .Body((data, block) => block
                    .Const("queryClient", QueryExtensions.UseQueryClient())
                    .Statement(Ts.Var("queryClient").InvalidateQueries(
                        Ts.Object<object>().With("queryKey", Ts.Array(Ts.String("products"))).Build()))
                    .Statement(Ts.Var("toast").Dot("success").Invoke(Ts.String("Product created!")))))
            .OnError(Ts.Arrow<object, object>("error")
                .Returns(e => Ts.Var("toast").Dot("error").Invoke(
                    e.Prop<string>("message")).Typed<object>()))
            .As("createProductMutation")
            .Export()
            .Build();

    /// <summary>
    /// Update product mutation options
    /// </summary>
    public static TsVariableDeclaration UpdateProductMutation =>
        TanStackQuery.MutationOptions<object, object>()
            .MutationKey("updateProduct")
            .MutationFn(fn => fn.Returns(input =>
                Ts.Var("api").Dot("products").Dot("update").Invoke(
                    input.Prop<string>("id"),
                    input.Prop<object>("data"))))
            .OnMutate(Ts.AsyncArrow<object, object>("variables")
                .Body((vars, block) => block
                    // Optimistic update
                    .Const("queryClient", QueryExtensions.UseQueryClient())
                    .Const("previousProduct",
                        Ts.Var("queryClient").GetQueryData(
                            Ts.Array(Ts.String("product"), vars.Prop<string>("id"))))
                    .Statement(Ts.Var("queryClient").SetQueryData(
                        Ts.Array(Ts.String("product"), vars.Prop<string>("id")),
                        Ts.Object<object>()
                            .Spread(Ts.Var("previousProduct"))
                            .Spread(vars.Prop<object>("data"))
                            .Build()))
                    .Return(Ts.Object<object>()
                        .With("previousProduct", Ts.Var("previousProduct"))
                        .Build())))
            .OnError(Ts.Arrow<object, object, object>("error", "variables", "context")
                .Returns((e, v, c) =>
                    Ts.Var("queryClient").SetQueryData(
                        Ts.Array(Ts.String("product"), v.Prop<string>("id")),
                        c.Prop<object>("previousProduct")).Typed<object>()))
            .OnSettled(Ts.AsyncArrow<object, object>("data")
                .Returns(_ =>
                    Ts.Var("queryClient").InvalidateQueries(
                        Ts.Object<object>().With("queryKey", Ts.Array(Ts.String("products"))).Build())))
            .As("updateProductMutation")
            .Export()
            .Build();

    /// <summary>
    /// Delete product mutation options
    /// </summary>
    public static TsVariableDeclaration DeleteProductMutation =>
        TanStackQuery.MutationOptions<object, string>()
            .MutationKey("deleteProduct")
            .MutationFn(fn => fn.Returns(id =>
                Ts.Var("api").Dot("products").Dot("delete").Invoke(id)))
            .OnSuccess(Ts.AsyncArrow<object, object>("_")
                .Returns(_ =>
                    Ts.Var("queryClient").InvalidateQueries(
                        Ts.Object<object>().With("queryKey", Ts.Array(Ts.String("products"))).Build())))
            .As("deleteProductMutation")
            .Export()
            .Build();

    /// <summary>
    /// Custom hook for products
    /// </summary>
    public static TsVariableDeclaration UseProducts =>
        Ts.Const<object>("useProducts")
            .Value(Ts.Arrow<object, object>("filters")
                .Returns(f => QueryExtensions.UseQuery(
                    Ts.Var("productsQueryOptions").Invoke(f))))
            .Export()
            .Build();

    /// <summary>
    /// Custom hook for single product
    /// </summary>
    public static TsVariableDeclaration UseProduct =>
        Ts.Const<object>("useProduct")
            .Value(Ts.Arrow<string, object>("productId")
                .Returns(id => QueryExtensions.UseQuery(
                    Ts.Var("productQueryOptions").Invoke(id))))
            .Export()
            .Build();

    /// <summary>
    /// Custom hook for creating product
    /// </summary>
    public static TsVariableDeclaration UseCreateProduct =>
        Ts.Const<object>("useCreateProduct")
            .Value(Ts.Arrow<object>()
                .Returns(QueryExtensions.UseMutation(Ts.Var("createProductMutation"))))
            .Export()
            .Build();
}
