using Moj.TanStack.Ast.CodeGen;
using Moj.TanStack.Dsl.Core.TypeInference;
using Moj.Sandbox.DomainModels;
using Moj.Sandbox.Stores;

Console.WriteLine("=== Moj TanStack DSL - TypeScript Code Generator ===");
Console.WriteLine("=== Using C# Type Inference (No manual Ts.String!) ===\n");

// Output directory
var outputDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "test-app", "src", "generated");
Directory.CreateDirectory(outputDir);

// ============================================
// Generate TypeScript from C# Domain Model
// ============================================

Console.WriteLine("--- Generating incidentStore.ts from C# types ---\n");

// Generate the store TypeScript using the new type-inferred DSL
var incidentStoreTs = IncidentStore.Definition.ToTypeScript();

Console.WriteLine(incidentStoreTs);
Console.WriteLine("\n");

// Write to file
var incidentStorePath = Path.Combine(outputDir, "incidentStore.ts");
File.WriteAllText(incidentStorePath, incidentStoreTs);
Console.WriteLine($"Written: {incidentStorePath}");

// ============================================
// Also generate types separately for reference
// ============================================

Console.WriteLine("\n--- Generating types.ts (all domain types) ---\n");

var emitter = new TypeScriptEmitter();
var typeNodes = new List<Moj.TanStack.Ast.Core.TsNode>();

// Generate all enums
typeNodes.Add(EnumToTsConverter.ToTsEnum(typeof(IncidentStatus)));
typeNodes.Add(EnumToTsConverter.ToTsEnum(typeof(IncidentPriority)));
typeNodes.Add(EnumToTsConverter.ToTsEnum(typeof(IncidentCategory)));

// Generate all interfaces from the type graph
var allTypes = new[]
{
    typeof(User),
    typeof(Comment),
    typeof(CommentReaction),
    typeof(Attachment),
    typeof(IncidentMetadata),
    typeof(RelatedIncident),
    typeof(CustomField),
    typeof(Incident),
    typeof(SlaInfo),
    typeof(SlaMetrics),
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

var typesProgram = new Moj.TanStack.Ast.Core.TsProgram(typeNodes);
var typesTs = emitter.Emit(typesProgram);

Console.WriteLine(typesTs);

var typesPath = Path.Combine(outputDir, "types.ts");
File.WriteAllText(typesPath, typesTs);
Console.WriteLine($"\nWritten: {typesPath}");

Console.WriteLine("\n=== Generation Complete ===");
Console.WriteLine($"\nOutput directory: {outputDir}");
Console.WriteLine("Files generated:");
Console.WriteLine("  - incidentStore.ts (TanStack Store with actions & selectors)");
Console.WriteLine("  - types.ts (All TypeScript interfaces)");

Console.WriteLine("\n=== To validate the TypeScript: ===");
Console.WriteLine("cd Moj/sandbox/Moj.Sandbox/test-app");
Console.WriteLine("npm install");
Console.WriteLine("npm run typecheck");
