using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Impulse.SourceGen;

/// <summary>
/// Source generator that produces TypeScript interfaces from C# types.
/// Entry points: *Props, *Request, *Response records.
/// Includes all dependent types transitively, including enums.
/// Outputs TypeScript as commented C# code for MSBuild extraction.
/// </summary>
[Generator]
public class ImpulseTypeScriptGenerator : IIncrementalGenerator
{
    // Type name suffixes that mark entry points for TypeScript generation
    private static readonly string[] EntryPointSuffixes = ["Props", "Request", "Response", "Data", "Schema"];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all record declarations
        var recordProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is RecordDeclarationSyntax,
                transform: static (ctx, _) => GetRecordTypeInfo(ctx))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!);

        // Find all enum declarations
        var enumProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is EnumDeclarationSyntax,
                transform: static (ctx, _) => GetEnumTypeInfo(ctx))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!);

        // Combine records and enums
        var allTypes = recordProvider.Collect()
            .Combine(enumProvider.Collect());

        context.RegisterSourceOutput(allTypes, static (ctx, combined) =>
        {
            var (records, enums) = combined;
            if (records.IsEmpty && enums.IsEmpty) return;

            var source = GenerateTypeScriptSource(records, enums);
            ctx.AddSource("ImpulseTypes.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    private static TypeInfo? GetRecordTypeInfo(GeneratorSyntaxContext context)
    {
        var record = (RecordDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(record);

        if (symbol is null) return null;

        var properties = new List<PropertyInfo>();
        var dependentTypes = new HashSet<string>();

        // Get properties from primary constructor parameters
        if (record.ParameterList is not null)
        {
            foreach (var param in record.ParameterList.Parameters)
            {
                var paramSymbol = context.SemanticModel.GetDeclaredSymbol(param);
                if (paramSymbol is IParameterSymbol ps)
                {
                    var (tsType, deps) = GetTypeScriptTypeWithDeps(ps.Type);
                    properties.Add(new PropertyInfo(
                        ps.Name,
                        tsType,
                        IsNullable(ps.Type)));
                    foreach (var dep in deps)
                    {
                        dependentTypes.Add(dep);
                    }
                }
            }
        }

        return new TypeInfo(
            symbol.Name,
            symbol.ContainingNamespace.ToDisplayString(),
            properties,
            IsEnum: false,
            EnumMembers: null,
            dependentTypes.ToList());
    }

    private static TypeInfo? GetEnumTypeInfo(GeneratorSyntaxContext context)
    {
        var enumDecl = (EnumDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(enumDecl);

        if (symbol is null) return null;

        // Get enum member names
        var members = symbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.HasConstantValue)
            .Select(f => f.Name)
            .ToList();

        return new TypeInfo(
            symbol.Name,
            symbol.ContainingNamespace.ToDisplayString(),
            Properties: new List<PropertyInfo>(),
            IsEnum: true,
            EnumMembers: members,
            DependentTypes: new List<string>());
    }

    private static string GenerateTypeScriptSource(
        ImmutableArray<TypeInfo> records,
        ImmutableArray<TypeInfo> enums)
    {
        var sb = new StringBuilder();

        // Collect all types indexed by name
        var allTypes = new Dictionary<string, TypeInfo>();
        var entryPoints = new List<TypeInfo>();

        foreach (var record in records)
        {
            allTypes[record.Name] = record;
            if (IsEntryPoint(record.Name))
            {
                entryPoints.Add(record);
            }
        }

        foreach (var enumInfo in enums)
        {
            allTypes[enumInfo.Name] = enumInfo;
        }

        // Find all dependent types recursively starting from entry points
        var typesToGenerate = new HashSet<string>();
        var queue = new Queue<string>();

        foreach (var entry in entryPoints)
        {
            typesToGenerate.Add(entry.Name);
            queue.Enqueue(entry.Name);
        }

        while (queue.Count > 0)
        {
            var typeName = queue.Dequeue();
            if (allTypes.TryGetValue(typeName, out var typeInfo))
            {
                foreach (var dep in typeInfo.DependentTypes)
                {
                    if (!typesToGenerate.Contains(dep) && allTypes.ContainsKey(dep))
                    {
                        typesToGenerate.Add(dep);
                        queue.Enqueue(dep);
                    }
                }
            }
        }

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("// This file contains generated TypeScript as comments.");
        sb.AppendLine("// MSBuild extracts this to .ts files during build.");
        sb.AppendLine();
        sb.AppendLine("namespace Impulse.Generated;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Generated TypeScript interfaces and types.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("internal static class ImpulseTypes");
        sb.AppendLine("{");
        sb.AppendLine("    // <impulse-typescript file=\"types.g.ts\">");
        sb.AppendLine("    // // This file is auto-generated by Impulse.SourceGen");
        sb.AppendLine("    // // Do not edit manually");
        sb.AppendLine("    //");

        // Generate enums first (as union types)
        foreach (var typeName in typesToGenerate.OrderBy(n => n))
        {
            if (allTypes.TryGetValue(typeName, out var typeInfo) && typeInfo.IsEnum)
            {
                GenerateEnumType(sb, typeInfo);
            }
        }

        // Then generate interfaces
        foreach (var typeName in typesToGenerate.OrderBy(n => n))
        {
            if (allTypes.TryGetValue(typeName, out var typeInfo) && !typeInfo.IsEnum)
            {
                GenerateInterface(sb, typeInfo);
            }
        }

        sb.AppendLine("    // </impulse-typescript>");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static bool IsEntryPoint(string name)
    {
        foreach (var suffix in EntryPointSuffixes)
        {
            if (name.EndsWith(suffix, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private static void GenerateEnumType(StringBuilder sb, TypeInfo enumInfo)
    {
        if (enumInfo.EnumMembers is null || enumInfo.EnumMembers.Count == 0)
        {
            sb.AppendLine($"    // export type {enumInfo.Name} = string;");
        }
        else
        {
            var members = enumInfo.EnumMembers
                .Select(m => $"'{m}'")
                .ToList();

            sb.AppendLine($"    // export type {enumInfo.Name} = {string.Join(" | ", members)};");
        }
        sb.AppendLine("    //");
    }

    private static void GenerateInterface(StringBuilder sb, TypeInfo record)
    {
        sb.AppendLine($"    // export interface {record.Name} {{");

        foreach (var prop in record.Properties)
        {
            var optionalMarker = prop.IsNullable ? "?" : "";
            var propName = ToCamelCase(prop.Name);
            sb.AppendLine($"    //   {propName}{optionalMarker}: {prop.TypeScriptType};");
        }

        sb.AppendLine("    // }");
        sb.AppendLine("    //");
    }

    private static (string Type, List<string> Dependencies) GetTypeScriptTypeWithDeps(ITypeSymbol type)
    {
        var deps = new List<string>();

        // Handle nullable value types
        if (type is INamedTypeSymbol nullable && nullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            var (innerType, innerDeps) = GetTypeScriptTypeWithDeps(nullable.TypeArguments[0]);
            deps.AddRange(innerDeps);
            return (innerType + " | null", deps);
        }

        // Primitives
        var tsType = type.SpecialType switch
        {
            SpecialType.System_Int16 or
            SpecialType.System_Int32 or
            SpecialType.System_Int64 or
            SpecialType.System_Byte or
            SpecialType.System_Double or
            SpecialType.System_Single or
            SpecialType.System_Decimal => "number",

            SpecialType.System_String => "string",
            SpecialType.System_Boolean => "boolean",
            SpecialType.System_DateTime => "string",

            _ => null
        };

        if (tsType != null) return (tsType, deps);

        // Complex types
        var name = type.Name;

        // Handle System.Object -> unknown
        if (type.SpecialType == SpecialType.System_Object || name == "Object")
        {
            return ("unknown", deps);
        }

        // Handle common types
        if (name is "Guid" or "DateOnly" or "TimeOnly" or "DateTimeOffset")
        {
            return ("string", deps);
        }

        // Handle arrays
        if (type is IArrayTypeSymbol arrayType)
        {
            var (elemType, elemDeps) = GetTypeScriptTypeWithDeps(arrayType.ElementType);
            deps.AddRange(elemDeps);
            return ($"readonly {elemType}[]", deps);
        }

        // Handle generic collections
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var genericName = namedType.ConstructedFrom.Name;

            // List, IReadOnlyList, IEnumerable -> array
            if (genericName is "List" or "IList" or "IReadOnlyList" or "IEnumerable" or "ICollection" or "IReadOnlyCollection")
            {
                var (elemType, elemDeps) = GetTypeScriptTypeWithDeps(namedType.TypeArguments[0]);
                deps.AddRange(elemDeps);
                return ($"readonly {elemType}[]", deps);
            }

            // Dictionary -> Record
            if (genericName is "Dictionary" or "IDictionary" or "IReadOnlyDictionary")
            {
                var (keyType, keyDeps) = GetTypeScriptTypeWithDeps(namedType.TypeArguments[0]);
                var (valType, valDeps) = GetTypeScriptTypeWithDeps(namedType.TypeArguments[1]);
                deps.AddRange(keyDeps);
                deps.AddRange(valDeps);
                return ($"Record<{keyType}, {valType}>", deps);
            }
        }

        // Enum or complex type - add as dependency
        if (type.TypeKind == TypeKind.Enum || type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct)
        {
            deps.Add(name);
        }

        return (name, deps);
    }

    private static bool IsNullable(ITypeSymbol type)
    {
        if (type.NullableAnnotation == NullableAnnotation.Annotated)
            return true;

        if (type is INamedTypeSymbol named && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            return true;

        return false;
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}

internal record TypeInfo(
    string Name,
    string Namespace,
    List<PropertyInfo> Properties,
    bool IsEnum,
    List<string>? EnumMembers,
    List<string> DependentTypes);

internal record PropertyInfo(string Name, string TypeScriptType, bool IsNullable);
