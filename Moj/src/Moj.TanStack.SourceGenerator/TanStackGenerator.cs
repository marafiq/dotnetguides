using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Moj.TanStack.SourceGenerator;

/// <summary>
/// Source generator that transforms TanStack DSL code into TypeScript
/// </summary>
[Generator]
public sealed class TanStackGenerator : IIncrementalGenerator
{
    private const string TsGenerateAttribute = "Moj.TanStack.SourceGenerator.Attributes.TsGenerateAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register the attribute source
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("TsGenerateAttributes.g.cs", SourceText.From(AttributeSource, Encoding.UTF8));
        });

        // Find all classes with [TsGenerate] attribute
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        // Combine with compilation
        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

        // Generate source
        context.RegisterSourceOutput(compilationAndClasses, Execute);
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };
    }

    private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        foreach (var attributeListSyntax in classDeclaration.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;
                if (symbol is IMethodSymbol { ContainingType.Name: "TsGenerateAttribute" or "TsStoreAttribute" or "TsRouterAttribute" or "TsQueryAttribute" })
                {
                    return classDeclaration;
                }
            }
        }

        return null;
    }

    private void Execute(SourceProductionContext context, (Compilation Compilation, ImmutableArray<ClassDeclarationSyntax?> Classes) source)
    {
        if (source.Classes.IsDefaultOrEmpty)
            return;

        var distinctClasses = source.Classes
            .Where(c => c is not null)
            .Cast<ClassDeclarationSyntax>()
            .Distinct();

        foreach (var classDeclaration in distinctClasses)
        {
            var semanticModel = source.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);

            if (classSymbol is null)
                continue;

            var tsCode = GenerateTypeScript(classDeclaration, classSymbol, semanticModel);
            var fileName = GetOutputFileName(classSymbol);

            // Generate C# file with embedded TypeScript
            var generatedSource = GenerateCSharpWrapper(classSymbol, tsCode, fileName);
            context.AddSource($"{fileName}.g.cs", SourceText.From(generatedSource, Encoding.UTF8));
        }
    }

    private static string GetOutputFileName(INamedTypeSymbol classSymbol)
    {
        // Check for custom file name in attribute
        var attr = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name is "TsGenerateAttribute" or "TsStoreAttribute" or "TsRouterAttribute" or "TsQueryAttribute");

        if (attr != null)
        {
            var fileNameArg = attr.NamedArguments.FirstOrDefault(a => a.Key == "FileName");
            if (!fileNameArg.Equals(default) && fileNameArg.Value.Value is string customName)
                return customName;

            var nameArg = attr.NamedArguments.FirstOrDefault(a => a.Key == "Name");
            if (!nameArg.Equals(default) && nameArg.Value.Value is string name)
                return name;
        }

        return classSymbol.Name;
    }

    private static string GenerateTypeScript(ClassDeclarationSyntax classDeclaration, INamedTypeSymbol classSymbol, SemanticModel semanticModel)
    {
        var sb = new StringBuilder();

        // Check what type of TanStack DSL this is
        var attributes = classSymbol.GetAttributes();
        var isStore = attributes.Any(a => a.AttributeClass?.Name == "TsStoreAttribute");
        var isRouter = attributes.Any(a => a.AttributeClass?.Name == "TsRouterAttribute");
        var isQuery = attributes.Any(a => a.AttributeClass?.Name == "TsQueryAttribute");

        // Generate appropriate imports
        if (isStore)
        {
            sb.AppendLine("import { Store } from '@tanstack/store';");
            sb.AppendLine("import { useStore } from '@tanstack/react-store';");
        }
        else if (isRouter)
        {
            sb.AppendLine("import { createRouter, createRoute, createRootRoute } from '@tanstack/react-router';");
        }
        else if (isQuery)
        {
            sb.AppendLine("import { useQuery, useMutation, QueryClient, QueryClientProvider, queryOptions } from '@tanstack/react-query';");
        }

        sb.AppendLine();

        // Analyze the class to find DSL method calls
        var analyzer = new DslAnalyzer(semanticModel);
        var tsNodes = analyzer.AnalyzeClass(classDeclaration);

        foreach (var node in tsNodes)
        {
            sb.AppendLine(node);
        }

        return sb.ToString();
    }

    private static string GenerateCSharpWrapper(INamedTypeSymbol classSymbol, string tsCode, string fileName)
    {
        var escapedTs = tsCode.Replace("*/", "*\\/").Replace("\"", "\"\"");
        var ns = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;
        var fullName = classSymbol.ToDisplayString();

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// This code was generated by Moj.TanStack.SourceGenerator");
        sb.AppendLine("// Do not modify this file directly.");
        sb.AppendLine($"// Generated from: {fullName}");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns}.Generated");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Generated TypeScript code for {className}");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public static partial class {className}Ts");
        sb.AppendLine("    {");
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// The file name for the generated TypeScript");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public const string FileName = \"{fileName}.ts\";");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Gets the raw TypeScript code");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public static string GetTypeScript() => @\"{escapedTs}\";");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private const string AttributeSource = @"// <auto-generated/>
#nullable enable

namespace Moj.TanStack.SourceGenerator.Attributes
{
    /// <summary>
    /// Marks a class or method for TypeScript generation
    /// </summary>
    [global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class TsGenerateAttribute : global::System.Attribute
    {
        /// <summary>
        /// The output file name (without extension)
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// Output directory relative to project root
        /// </summary>
        public string OutputDir { get; set; } = ""Generated/ts"";
    }

    /// <summary>
    /// Marks a class as a TanStack Store definition
    /// </summary>
    [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false)]
    internal sealed class TsStoreAttribute : global::System.Attribute
    {
        public string? Name { get; set; }
    }

    /// <summary>
    /// Marks a class as TanStack Router routes definition
    /// </summary>
    [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false)]
    internal sealed class TsRouterAttribute : global::System.Attribute
    {
        public string? Name { get; set; }
    }

    /// <summary>
    /// Marks a class as TanStack Query definitions
    /// </summary>
    [global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false)]
    internal sealed class TsQueryAttribute : global::System.Attribute
    {
        public string? Name { get; set; }
    }

    /// <summary>
    /// Specifies imports for the generated file
    /// </summary>
    [global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Method, AllowMultiple = true)]
    internal sealed class TsImportAttribute : global::System.Attribute
    {
        public string Module { get; }
        public string[] Names { get; }
        public string? DefaultImport { get; set; }
        public bool TypeOnly { get; set; }

        public TsImportAttribute(string module, params string[] names)
        {
            Module = module;
            Names = names;
        }
    }

    /// <summary>
    /// Marks a property or method to be exported in the generated TS
    /// </summary>
    [global::System.AttributeUsage(global::System.AttributeTargets.Property | global::System.AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class TsExportAttribute : global::System.Attribute
    {
        public string? Name { get; set; }
        public bool IsDefault { get; set; }
    }
}";
}
