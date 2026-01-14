using Moj.TanStack.Ast.CodeGen;
using Moj.TanStack.Ast.Core;
using Moj.TanStack.Dsl.Core.TypeInference;
using Moj.Sandbox.DomainModels;
using Moj.Sandbox.Definitions;

Console.WriteLine("=== Moj TanStack DSL - TypeScript Code Generator ===");
Console.WriteLine("=== Using C# Type Inference ===\n");

// Output directory
var outputDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "test-app", "src", "generated");
Directory.CreateDirectory(outputDir);

var emitter = new TypeScriptEmitter();

// ============================================
// Generate UserStore TypeScript
// ============================================

Console.WriteLine("--- Generating userStore.ts ---\n");

var userStoreNodes = new List<TsNode>
{
    UserStore.Imports,
    UserStore.ReactImports,
    UserStore.PreferencesInterface,
    UserStore.UserStateInterface,
    UserStore.Store,
    UserStore.SetUserAction,
    UserStore.LogoutAction,
    UserStore.UpdatePreferencesAction,
    UserStore.IsAdminSelector,
    UserStore.UseUserStore
};

var userStoreProgram = new TsProgram(userStoreNodes);
var userStoreTs = emitter.Emit(userStoreProgram);

Console.WriteLine(userStoreTs);

var userStorePath = Path.Combine(outputDir, "userStore.ts");
File.WriteAllText(userStorePath, userStoreTs);
Console.WriteLine($"\nWritten: {userStorePath}");

// ============================================
// Generate Router TypeScript
// ============================================

Console.WriteLine("\n--- Generating appRouter.ts ---\n");

var routerNodes = new List<TsNode>
{
    AppRouter.Imports,
    AppRouter.HooksImport,
    AppRouter.ProductsSearchParams,
    AppRouter.ProductParams,
    AppRouter.RootRoute,
    AppRouter.IndexRoute,
    AppRouter.AboutRoute,
    AppRouter.ProductsRoute,
    AppRouter.ProductRoute,
    AppRouter.DashboardRoute,
    AppRouter.ProfileRoute,
    AppRouter.SettingsRoute,
    AppRouter.RouteTree,
    AppRouter.Router,
    AppRouter.RouterType
};

var routerProgram = new TsProgram(routerNodes);
var routerTs = emitter.Emit(routerProgram);

Console.WriteLine(routerTs);

var routerPath = Path.Combine(outputDir, "appRouter.ts");
File.WriteAllText(routerPath, routerTs);
Console.WriteLine($"\nWritten: {routerPath}");

// ============================================
// Generate domain types from C# classes
// ============================================

Console.WriteLine("\n--- Generating types.ts (domain types) ---\n");

var typeNodes = new List<TsNode>();

// Generate all enums
typeNodes.Add(EnumToTsConverter.ToTsEnum(typeof(IncidentStatus)));
typeNodes.Add(EnumToTsConverter.ToTsEnum(typeof(IncidentPriority)));
typeNodes.Add(EnumToTsConverter.ToTsEnum(typeof(IncidentCategory)));

// Generate interfaces from C# types
var allTypes = new[]
{
    typeof(User),
    typeof(Comment),
    typeof(Attachment),
    typeof(IncidentMetadata),
    typeof(Incident),
    typeof(IncidentFilters),
    typeof(SortConfig),
    typeof(PaginationState),
    typeof(UiState),
    typeof(IncidentAppState)
};

foreach (var type in allTypes)
{
    typeNodes.Add(TypeToTsConverter.ToInterface(type, export: true));
}

var typesProgram = new TsProgram(typeNodes);
var typesTs = emitter.Emit(typesProgram);

Console.WriteLine(typesTs);

var typesPath = Path.Combine(outputDir, "types.ts");
File.WriteAllText(typesPath, typesTs);
Console.WriteLine($"\nWritten: {typesPath}");

Console.WriteLine("\n=== Generation Complete ===");
Console.WriteLine($"\nOutput directory: {outputDir}");
Console.WriteLine("Files generated:");
Console.WriteLine("  - userStore.ts (TanStack Store)");
Console.WriteLine("  - appRouter.ts (TanStack Router)");
Console.WriteLine("  - types.ts (TypeScript interfaces from C# types)");

Console.WriteLine("\n=== To validate the TypeScript: ===");
Console.WriteLine("cd Moj/sandbox/Moj.Sandbox/test-app");
Console.WriteLine("npm install");
Console.WriteLine("npm run typecheck");
