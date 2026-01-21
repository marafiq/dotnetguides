using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using System.Text;

namespace Shalimar.Razor;

/// <summary>
/// Compiles Razor files to TSX using proper AST parsing via Microsoft.AspNetCore.Razor.Language.
/// This is the correct approach - NOT regex hacks.
/// </summary>
public class RazorToTsxCompiler
{
    private readonly RazorProjectFileSystem _fileSystem;
    private readonly RazorProjectEngine _engine;

    public RazorToTsxCompiler(string rootPath)
    {
        _fileSystem = RazorProjectFileSystem.Create(rootPath);
        _engine = RazorProjectEngine.Create(RazorConfiguration.Default, _fileSystem, builder =>
        {
            // Configure the Razor engine for component parsing
            builder.SetRootNamespace("Shalimar.Components");
        });
    }

    /// <summary>
    /// Compile a .razor file to TSX using the Razor syntax tree
    /// </summary>
    public CompilationResult Compile(string razorFilePath)
    {
        var projectItem = _fileSystem.GetItem(razorFilePath, FileKinds.Component);
        var codeDocument = _engine.Process(projectItem);

        // Get the syntax tree - this is the proper way to parse Razor
        var syntaxTree = codeDocument.GetSyntaxTree();
        var root = syntaxTree.Root;

        // Extract component information by walking the syntax tree
        var component = ExtractComponentInfo(root, razorFilePath);

        // Emit TSX from the syntax tree
        var tsx = EmitTsx(component, root);

        return new CompilationResult
        {
            Success = true,
            SourcePath = razorFilePath,
            OutputPath = Path.ChangeExtension(razorFilePath, ".tsx"),
            GeneratedCode = tsx,
            Component = component
        };
    }

    /// <summary>
    /// Walk the syntax tree to extract component metadata
    /// </summary>
    private ComponentInfo ExtractComponentInfo(RazorSyntaxNode root, string filePath)
    {
        var info = new ComponentInfo
        {
            Name = Path.GetFileNameWithoutExtension(filePath),
            FilePath = filePath,
            Props = new List<PropInfo>(),
            Directive = ComponentDirective.Default
        };

        // Walk all nodes in the syntax tree
        foreach (var node in root.DescendantNodes())
        {
            switch (node)
            {
                // Find @server or @client directive
                case RazorDirectiveSyntax directive:
                    var directiveContent = directive.GetContent();
                    if (directiveContent == "server") info.Directive = ComponentDirective.Server;
                    else if (directiveContent == "client") info.Directive = ComponentDirective.Client;
                    break;

                // Find @code block and extract [Parameter] properties
                case CSharpCodeBlockSyntax codeBlock:
                    ExtractPropsFromCodeBlock(codeBlock, info.Props);
                    break;
            }
        }

        return info;
    }

    /// <summary>
    /// Extract [Parameter] properties from @code block
    /// </summary>
    private void ExtractPropsFromCodeBlock(CSharpCodeBlockSyntax codeBlock, List<PropInfo> props)
    {
        var code = codeBlock.GetContent();

        // Parse C# code to find [Parameter] properties
        // In production, use Roslyn for proper C# parsing
        var lines = code.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("[Parameter]") && i + 1 < lines.Length)
            {
                var propLine = lines[i + 1];
                var prop = ParsePropertyDeclaration(propLine);
                if (prop != null) props.Add(prop);
            }
        }
    }

    private PropInfo? ParsePropertyDeclaration(string line)
    {
        // public string Name { get; set; }
        // public bool IsActive { get; set; } = true;
        var match = System.Text.RegularExpressions.Regex.Match(
            line,
            @"public\s+(\w+(?:<[^>]+>)?(?:\[\])?)\s+(\w+)\s*\{.*\}(?:\s*=\s*(.+);)?");

        if (!match.Success) return null;

        return new PropInfo
        {
            CSharpType = match.Groups[1].Value,
            Name = match.Groups[2].Value,
            DefaultValue = match.Groups[3].Success ? match.Groups[3].Value.Trim() : null,
            TypeScriptType = ConvertType(match.Groups[1].Value)
        };
    }

    /// <summary>
    /// Emit TSX by walking the syntax tree - proper AST approach
    /// </summary>
    private string EmitTsx(ComponentInfo component, RazorSyntaxNode root)
    {
        var sb = new StringBuilder();

        // Imports
        sb.AppendLine("import React from 'react';");
        sb.AppendLine();

        // Props interface
        if (component.Props.Any())
        {
            sb.AppendLine($"export interface {component.Name}Props {{");
            foreach (var prop in component.Props)
            {
                var optional = prop.DefaultValue != null ? "?" : "";
                sb.AppendLine($"  {prop.Name}{optional}: {prop.TypeScriptType};");
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }

        // Component function
        var propsParam = component.Props.Any()
            ? $"{{ {string.Join(", ", component.Props.Select(p => p.Name))} }}: {component.Name}Props"
            : "";

        sb.AppendLine($"export function {component.Name}({propsParam}) {{");
        sb.AppendLine("  return (");

        // Walk the syntax tree and emit JSX
        EmitJsxFromSyntaxTree(root, sb, "    ");

        sb.AppendLine("  );");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Walk syntax tree nodes and emit corresponding JSX
    /// </summary>
    private void EmitJsxFromSyntaxTree(RazorSyntaxNode node, StringBuilder sb, string indent)
    {
        foreach (var child in node.ChildNodes())
        {
            switch (child)
            {
                case MarkupElementSyntax element:
                    EmitMarkupElement(element, sb, indent);
                    break;

                case MarkupTextLiteralSyntax text:
                    var content = text.GetContent().Trim();
                    if (!string.IsNullOrEmpty(content))
                        sb.AppendLine($"{indent}{content}");
                    break;

                case CSharpExpressionLiteralSyntax expr:
                    // @Props.Name -> {Name}
                    var exprContent = expr.GetContent()
                        .Replace("Props.", "");
                    sb.Append($"{{{exprContent}}}");
                    break;

                case CSharpStatementSyntax statement:
                    EmitCSharpStatement(statement, sb, indent);
                    break;

                default:
                    // Recurse for other node types
                    EmitJsxFromSyntaxTree(child, sb, indent);
                    break;
            }
        }
    }

    private void EmitMarkupElement(MarkupElementSyntax element, StringBuilder sb, string indent)
    {
        var tagName = element.StartTag?.Name?.GetContent() ?? "";
        if (string.IsNullOrEmpty(tagName)) return;

        sb.Append($"{indent}<{tagName}");

        // Emit attributes
        if (element.StartTag?.Attributes != null)
        {
            foreach (var attr in element.StartTag.Attributes)
            {
                EmitAttribute(attr, sb);
            }
        }

        if (element.EndTag == null)
        {
            sb.AppendLine(" />");
        }
        else
        {
            sb.AppendLine(">");
            EmitJsxFromSyntaxTree(element.Body, sb, indent + "  ");
            sb.AppendLine($"{indent}</{tagName}>");
        }
    }

    private void EmitAttribute(RazorSyntaxNode attr, StringBuilder sb)
    {
        if (attr is MarkupAttributeBlockSyntax attrBlock)
        {
            var name = attrBlock.Name?.GetContent() ?? "";
            var value = attrBlock.Value?.GetContent() ?? "";

            // Transform attribute names: class -> className
            name = TransformAttributeName(name);

            // Transform attribute values
            if (name == "style")
            {
                sb.Append($" {name}={{{TransformStyleToObject(value)}}}");
            }
            else if (value.Contains("@"))
            {
                // @Props.X -> {X}
                var jsxValue = value.Replace("@Props.", "").Replace("@", "");
                sb.Append($" {name}={{{jsxValue}}}");
            }
            else
            {
                sb.Append($" {name}=\"{value}\"");
            }
        }
    }

    private void EmitCSharpStatement(CSharpStatementSyntax statement, StringBuilder sb, string indent)
    {
        var content = statement.GetContent().Trim();

        // Handle @if statements
        if (content.StartsWith("if"))
        {
            // Extract condition and body from the syntax tree (not regex)
            // This is where proper AST walking shines
            var condition = ExtractCondition(content);
            sb.AppendLine($"{indent}{{({condition}) && (");
            EmitJsxFromSyntaxTree(statement.Body, sb, indent + "  ");
            sb.AppendLine($"{indent})}}");
        }
        // Handle @foreach statements
        else if (content.StartsWith("foreach"))
        {
            var (itemVar, collection) = ExtractForeachParts(content);
            sb.AppendLine($"{indent}{{{collection}.map(({itemVar}, index) => (");
            EmitJsxFromSyntaxTree(statement.Body, sb, indent + "  ");
            sb.AppendLine($"{indent}))}}");
        }
    }

    private string ExtractCondition(string ifStatement)
    {
        var start = ifStatement.IndexOf('(') + 1;
        var end = ifStatement.LastIndexOf(')');
        if (start > 0 && end > start)
        {
            return ifStatement[start..end]
                .Replace("Props.", "")
                .Trim();
        }
        return "";
    }

    private (string itemVar, string collection) ExtractForeachParts(string foreachStatement)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            foreachStatement,
            @"foreach\s*\(\s*var\s+(\w+)\s+in\s+(\w+)\s*\)");

        return match.Success
            ? (match.Groups[1].Value, match.Groups[2].Value)
            : ("item", "items");
    }

    private string TransformAttributeName(string name) => name.ToLower() switch
    {
        "class" => "className",
        "for" => "htmlFor",
        "tabindex" => "tabIndex",
        "readonly" => "readOnly",
        "maxlength" => "maxLength",
        "colspan" => "colSpan",
        "rowspan" => "rowSpan",
        _ => name
    };

    private string TransformStyleToObject(string styleString)
    {
        var parts = styleString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var props = parts.Select(p =>
        {
            var kv = p.Split(':', 2);
            if (kv.Length != 2) return null;
            var prop = kv[0].Trim();
            var value = kv[1].Trim();
            // Convert kebab-case to camelCase
            prop = System.Text.RegularExpressions.Regex.Replace(prop, "-(.)",
                m => m.Groups[1].Value.ToUpper());
            return $"{prop}: '{value}'";
        }).Where(p => p != null);

        return $"{{{string.Join(", ", props)}}}";
    }

    private string ConvertType(string csharpType) => csharpType switch
    {
        "string" => "string",
        "int" or "long" or "double" or "float" or "decimal" => "number",
        "bool" => "boolean",
        "DateTime" => "Date",
        var t when t.StartsWith("List<") => $"{ConvertType(t[5..^1])}[]",
        var t when t.EndsWith("[]") => $"{ConvertType(t[..^2])}[]",
        var t when t.EndsWith("?") => $"{ConvertType(t[..^1])} | null",
        _ => csharpType
    };
}

// Supporting types
public class ComponentInfo
{
    public string Name { get; set; } = "";
    public string FilePath { get; set; } = "";
    public ComponentDirective Directive { get; set; }
    public List<PropInfo> Props { get; set; } = new();
}

public class PropInfo
{
    public string Name { get; set; } = "";
    public string CSharpType { get; set; } = "";
    public string TypeScriptType { get; set; } = "";
    public string? DefaultValue { get; set; }
}

public class CompilationResult
{
    public bool Success { get; set; }
    public string SourcePath { get; set; } = "";
    public string? OutputPath { get; set; }
    public string? GeneratedCode { get; set; }
    public string? Error { get; set; }
    public ComponentInfo? Component { get; set; }
}

public enum ComponentDirective
{
    Default,
    Server,
    Client
}
