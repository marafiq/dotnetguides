using Moj.TanStack.Ast.CodeGen;
using Moj.TanStack.Ast.Core;
using Moj.TanStack.Dsl.Core;
using Moj.Sandbox.Definitions;

Console.WriteLine("=== Moj TanStack DSL - TypeScript Code Generator ===\n");

// Create the TypeScript emitter
var emitter = new TypeScriptEmitter();

// Generate User Store TypeScript
Console.WriteLine("--- Generating userStore.ts ---\n");
var userStoreFile = TsFile.Create("userStore.ts")
    .Import(UserStore.Imports)
    .Import(UserStore.ReactImports)
    .Add(new TsComment(" User State Types "))
    .Interface(UserStore.PreferencesInterface)
    .Interface(UserStore.UserStateInterface)
    .Add(new TsComment(" Store Instance "))
    .Variable(UserStore.Store)
    .Add(new TsComment(" Actions "))
    .Variable(UserStore.SetUserAction)
    .Variable(UserStore.LogoutAction)
    .Variable(UserStore.UpdatePreferencesAction)
    .Add(new TsComment(" Selectors "))
    .Variable(UserStore.IsAdminSelector)
    .Add(new TsComment(" React Hook "))
    .Variable(UserStore.UseUserStore);

var userStoreTs = userStoreFile.Emit();
Console.WriteLine(userStoreTs);
Console.WriteLine("\n");

// Generate App Router TypeScript
Console.WriteLine("--- Generating appRouter.ts ---\n");
var appRouterFile = TsFile.Create("appRouter.ts")
    .Import(AppRouter.Imports)
    .Import(AppRouter.HooksImport)
    .Add(new TsComment(" Type Definitions "))
    .Interface(AppRouter.ProductsSearchParams)
    .Interface(AppRouter.ProductParams)
    .Add(new TsComment(" Route Definitions "))
    .Variable(AppRouter.RootRoute)
    .Variable(AppRouter.IndexRoute)
    .Variable(AppRouter.AboutRoute)
    .Variable(AppRouter.ProductsRoute)
    .Variable(AppRouter.ProductRoute)
    .Variable(AppRouter.DashboardRoute)
    .Variable(AppRouter.ProfileRoute)
    .Variable(AppRouter.SettingsRoute)
    .Add(new TsComment(" Route Tree "))
    .Variable(AppRouter.RouteTree)
    .Add(new TsComment(" Router Instance "))
    .Variable(AppRouter.Router)
    .TypeAlias(AppRouter.RouterType);

var appRouterTs = appRouterFile.Emit();
Console.WriteLine(appRouterTs);
Console.WriteLine("\n");

// Generate Product Queries TypeScript
Console.WriteLine("--- Generating productQueries.ts ---\n");
var productQueriesFile = TsFile.Create("productQueries.ts")
    .Import(ProductQueries.Imports)
    .Import(ProductQueries.QueryOptionsImport)
    .Add(new TsComment(" Type Definitions "))
    .Interface(ProductQueries.ProductInterface)
    .Interface(ProductQueries.ProductsResponseInterface)
    .Interface(ProductQueries.CreateProductInput)
    .Interface(ProductQueries.UpdateProductInput)
    .Add(new TsComment(" Query Client "))
    .Variable(ProductQueries.QueryClient)
    .Add(new TsComment(" Query Options Factories "))
    .Variable(ProductQueries.ProductsQueryOptions)
    .Variable(ProductQueries.ProductQueryOptions)
    .Variable(ProductQueries.ProductsByCategoryOptions)
    .Add(new TsComment(" Mutations "))
    .Variable(ProductQueries.CreateProductMutation)
    .Variable(ProductQueries.UpdateProductMutation)
    .Variable(ProductQueries.DeleteProductMutation)
    .Add(new TsComment(" Custom Hooks "))
    .Variable(ProductQueries.UseProducts)
    .Variable(ProductQueries.UseProduct)
    .Variable(ProductQueries.UseCreateProduct);

var productQueriesTs = productQueriesFile.Emit();
Console.WriteLine(productQueriesTs);
Console.WriteLine("\n");

// Write files to disk
var outputDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Generated", "ts");
Directory.CreateDirectory(outputDir);

File.WriteAllText(Path.Combine(outputDir, "userStore.ts"), userStoreTs);
File.WriteAllText(Path.Combine(outputDir, "appRouter.ts"), appRouterTs);
File.WriteAllText(Path.Combine(outputDir, "productQueries.ts"), productQueriesTs);

Console.WriteLine($"=== TypeScript files generated in: {outputDir} ===");
Console.WriteLine("Files: userStore.ts, appRouter.ts, productQueries.ts");
Console.WriteLine("\nTo validate the generated TypeScript:");
Console.WriteLine("1. cd to the Moj.Sandbox directory");
Console.WriteLine("2. Run: npm install");
Console.WriteLine("3. Run: npm run typecheck");
