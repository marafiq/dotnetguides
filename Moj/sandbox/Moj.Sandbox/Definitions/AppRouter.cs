using Moj.TanStack.Dsl.Core;
using Moj.TanStack.Dsl.TanStack.Router;
using Moj.TanStack.SourceGenerator.Attributes;

namespace Moj.Sandbox.Definitions;

/// <summary>
/// DSL definition for TanStack Router configuration
/// </summary>
[TsRouter(Name = "appRouter")]
public static class AppRouter
{
    /// <summary>
    /// Router imports
    /// </summary>
    public static TsImportDeclaration Imports =>
        TanStackRouter.ImportReact("createRouter", "createRoute", "createRootRoute", "Outlet");

    /// <summary>
    /// Router hooks import
    /// </summary>
    public static TsImportDeclaration HooksImport =>
        TanStackRouter.ImportHooks();

    /// <summary>
    /// Search params interface for products page
    /// </summary>
    public static TsInterfaceDeclaration ProductsSearchParams =>
        Ts.Interface("ProductsSearch")
            .Property<string>("category", optional: true)
            .Property<int>("page", optional: true)
            .Property<string>("sort", optional: true)
            .Property<string>("q", optional: true)
            .Export()
            .Build();

    /// <summary>
    /// Product params interface
    /// </summary>
    public static TsInterfaceDeclaration ProductParams =>
        Ts.Interface("ProductParams")
            .Property<string>("productId")
            .Export()
            .Build();

    /// <summary>
    /// Root route with layout
    /// </summary>
    public static TsVariableDeclaration RootRoute =>
        TanStackRouter.CreateRootRoute()
            .Component(Ts.Var("RootLayout"))
            .NotFoundComponent(Ts.Var("NotFound"))
            .As("rootRoute")
            .Export()
            .Build();

    /// <summary>
    /// Index/home route
    /// </summary>
    public static TsVariableDeclaration IndexRoute =>
        TanStackRouter.CreateRoute<object>()
            .GetParentRoute(Ts.Var("rootRoute"))
            .Path("/")
            .Component(Ts.Var("HomePage"))
            .As("indexRoute")
            .Export()
            .Build();

    /// <summary>
    /// About page route
    /// </summary>
    public static TsVariableDeclaration AboutRoute =>
        TanStackRouter.CreateRoute<object>()
            .GetParentRoute(Ts.Var("rootRoute"))
            .Path("/about")
            .Component(Ts.Var("AboutPage"))
            .As("aboutRoute")
            .Export()
            .Build();

    /// <summary>
    /// Products listing route with search params
    /// </summary>
    public static TsVariableDeclaration ProductsRoute =>
        TanStackRouter.CreateRoute<object>()
            .GetParentRoute(Ts.Var("rootRoute"))
            .Path("/products")
            .Component(Ts.Var("ProductsPage"))
            .ValidateSearch(Ts.Arrow<object, object>("search")
                .Returns(s => Ts.Object<object>()
                    .With("category", s.Prop<string?>("category").NullishCoalesce(Ts.Undefined.Typed<string?>()))
                    .With("page", s.Prop<int?>("page").NullishCoalesce(Ts.Int(1).As<int?>()))
                    .With("sort", s.Prop<string?>("sort").NullishCoalesce(Ts.String("newest").As<string?>()))
                    .With("q", s.Prop<string?>("q"))
                    .Build()))
            .Loader(Ts.AsyncArrow<object, object>("ctx")
                .Returns(ctx =>
                    Ts.Var("fetchProducts").Invoke(ctx.Prop<object>("search"))))
            .As("productsRoute")
            .Export()
            .Build();

    /// <summary>
    /// Single product route with params
    /// </summary>
    public static TsVariableDeclaration ProductRoute =>
        TanStackRouter.CreateRoute<object>()
            .GetParentRoute(Ts.Var("productsRoute"))
            .Path("$productId")
            .Component(Ts.Var("ProductDetailPage"))
            .Loader(Ts.AsyncArrow<object, object>("ctx")
                .Returns(ctx =>
                    Ts.Var("fetchProduct").Invoke(ctx.Prop<object>("params").Prop<string>("productId"))))
            .As("productRoute")
            .Export()
            .Build();

    /// <summary>
    /// User dashboard route (authenticated)
    /// </summary>
    public static TsVariableDeclaration DashboardRoute =>
        TanStackRouter.CreateRoute<object>()
            .GetParentRoute(Ts.Var("rootRoute"))
            .Path("/dashboard")
            .Component(Ts.Var("DashboardPage"))
            .BeforeLoad(Ts.AsyncArrow<object, object>("ctx")
                .Returns(ctx => Ts.Object<object>()
                    .With("user", Ts.Await(Ts.Var("checkAuth").Invoke().Typed<Task<object>>()))
                    .Build()))
            .As("dashboardRoute")
            .Export()
            .Build();

    /// <summary>
    /// User profile route
    /// </summary>
    public static TsVariableDeclaration ProfileRoute =>
        TanStackRouter.CreateRoute<object>()
            .GetParentRoute(Ts.Var("dashboardRoute"))
            .Path("/profile")
            .Component(Ts.Var("ProfilePage"))
            .As("profileRoute")
            .Export()
            .Build();

    /// <summary>
    /// User settings route
    /// </summary>
    public static TsVariableDeclaration SettingsRoute =>
        TanStackRouter.CreateRoute<object>()
            .GetParentRoute(Ts.Var("dashboardRoute"))
            .Path("/settings")
            .Component(Ts.Var("SettingsPage"))
            .As("settingsRoute")
            .Export()
            .Build();

    /// <summary>
    /// Complete route tree
    /// </summary>
    public static TsVariableDeclaration RouteTree =>
        Ts.Const<object>("routeTree")
            .Value(Ts.Var("rootRoute").Dot("addChildren").Invoke(
                Ts.Array(
                    Ts.Var("indexRoute"),
                    Ts.Var("aboutRoute"),
                    Ts.Var("productsRoute").Dot("addChildren").Invoke(
                        Ts.Array(Ts.Var("productRoute"))
                    ),
                    Ts.Var("dashboardRoute").Dot("addChildren").Invoke(
                        Ts.Array(
                            Ts.Var("profileRoute"),
                            Ts.Var("settingsRoute")
                        )
                    )
                )
            ))
            .Export()
            .Build();

    /// <summary>
    /// Router instance
    /// </summary>
    public static TsVariableDeclaration Router =>
        TanStackRouter.CreateRouter()
            .RouteTree(Ts.Var("routeTree"))
            .DefaultPreload("intent")
            .DefaultPreloadDelay(100)
            .As("router")
            .Export()
            .Build();

    /// <summary>
    /// TypeScript module declaration for router type registration
    /// </summary>
    public static TsTypeAliasDeclaration RouterType =>
        Ts.TypeAlias("AppRouter")
            .Is(Ts.TypeRef("typeof router"))
            .Export()
            .Build();
}
