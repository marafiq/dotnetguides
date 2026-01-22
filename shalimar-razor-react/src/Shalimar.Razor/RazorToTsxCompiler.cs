using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System.Text;
using System.Text.RegularExpressions;

namespace Shalimar.Razor;

/// <summary>
/// Compiles Razor files to TSX using Microsoft.AspNetCore.Razor.Language.
/// Uses the Intermediate Representation (IR) which is the public API.
/// </summary>
public class RazorToTsxCompiler
{
    private readonly string _rootPath;

    public RazorToTsxCompiler(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath);
    }

    /// <summary>
    /// Compile a .razor file to TSX
    /// </summary>
    public CompilationResult Compile(string razorFilePath)
    {
        try
        {
            var absolutePath = Path.GetFullPath(razorFilePath);
            var content = File.ReadAllText(absolutePath);
            var fileName = Path.GetFileNameWithoutExtension(absolutePath);

            // Parse using Razor engine
            var fileSystem = RazorProjectFileSystem.Create(_rootPath);
            var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem);

            var sourceDocument = RazorSourceDocument.Create(content, absolutePath);
            var codeDocument = projectEngine.Process(sourceDocument, FileKinds.Component, null, null);

            // Get the intermediate representation (public API)
            var documentNode = codeDocument.GetDocumentIntermediateNode();

            // Extract component info from the IR and source
            var component = ExtractComponentInfo(content, fileName, absolutePath);

            // Generate TSX
            var tsx = GenerateTsx(component, content);

            // Write to file
            var outputPath = Path.ChangeExtension(absolutePath, ".tsx");
            File.WriteAllText(outputPath, tsx);

            return new CompilationResult
            {
                Success = true,
                SourcePath = absolutePath,
                OutputPath = outputPath,
                GeneratedCode = tsx,
                Component = component
            };
        }
        catch (Exception ex)
        {
            return new CompilationResult
            {
                Success = false,
                SourcePath = razorFilePath,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Compile all .razor files in a directory
    /// </summary>
    public List<CompilationResult> CompileDirectory(string directory)
    {
        var results = new List<CompilationResult>();
        var razorFiles = Directory.GetFiles(directory, "*.razor", SearchOption.AllDirectories);

        foreach (var file in razorFiles)
        {
            var result = Compile(file);
            results.Add(result);
            Console.WriteLine(result.Success
                ? $"  Compiled: {Path.GetFileName(file)} -> {Path.GetFileName(result.OutputPath)}"
                : $"  Failed: {Path.GetFileName(file)} - {result.Error}");
        }

        return results;
    }

    /// <summary>
    /// Extract component information from source
    /// </summary>
    private ComponentInfo ExtractComponentInfo(string content, string name, string filePath)
    {
        var info = new ComponentInfo
        {
            Name = name,
            FilePath = filePath,
            Props = new List<PropInfo>(),
            Directive = ComponentDirective.Default
        };

        // Extract directive (@server or @client) - must be at the very start of the file
        var directiveMatch = Regex.Match(content.TrimStart(), @"^@(server|client)\s*$", RegexOptions.Multiline);
        if (directiveMatch.Success && directiveMatch.Index == 0)
        {
            info.Directive = directiveMatch.Groups[1].Value.ToLower() switch
            {
                "server" => ComponentDirective.Server,
                "client" => ComponentDirective.Client,
                _ => ComponentDirective.Default
            };
        }

        // Extract @code block
        var codeBlockMatch = Regex.Match(content, @"@code\s*\{([\s\S]*)\}\s*$", RegexOptions.Multiline);
        if (codeBlockMatch.Success)
        {
            var codeBlock = codeBlockMatch.Groups[1].Value;
            info.Props = ExtractProps(codeBlock);
        }

        return info;
    }

    /// <summary>
    /// Extract [Parameter] properties from code block
    /// </summary>
    private List<PropInfo> ExtractProps(string codeBlock)
    {
        var props = new List<PropInfo>();
        var propPattern = @"\[Parameter\]\s*public\s+(\w+(?:<[^>]+>)?(?:\[\])?(?:\?)?)\s+(\w+)\s*\{\s*get;\s*set;\s*\}(?:\s*=\s*([^;]+);)?";

        foreach (Match match in Regex.Matches(codeBlock, propPattern))
        {
            props.Add(new PropInfo
            {
                CSharpType = match.Groups[1].Value,
                Name = match.Groups[2].Value,
                DefaultValue = match.Groups[3].Success ? match.Groups[3].Value.Trim() : null,
                TypeScriptType = ConvertToTypeScript(match.Groups[1].Value)
            });
        }

        return props;
    }

    /// <summary>
    /// Generate TSX from component info and source template
    /// </summary>
    private string GenerateTsx(ComponentInfo component, string source)
    {
        var sb = new StringBuilder();

        // Extract template (everything except @code block, directives, and imports)
        var template = ExtractTemplate(source);

        // Find child component references
        var childComponents = FindChildComponents(template);

        // Imports
        sb.AppendLine("import React from 'react';");
        foreach (var child in childComponents)
        {
            sb.AppendLine($"import {{ {child} }} from './{child}';");
        }
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

        // Transform template to JSX
        var jsx = TransformToJsx(template);
        var indentedJsx = IndentLines(jsx, "    ");
        sb.AppendLine(indentedJsx);

        sb.AppendLine("  );");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Extract template from Razor source
    /// </summary>
    private string ExtractTemplate(string source)
    {
        var template = source;

        // Remove @server/@client directive
        template = Regex.Replace(template, @"^@(server|client)\s*$", "", RegexOptions.Multiline);

        // Remove @using directives
        template = Regex.Replace(template, @"^@using\s+.+$", "", RegexOptions.Multiline);

        // Remove @code block (with proper brace matching)
        var codeStart = template.IndexOf("@code");
        if (codeStart >= 0)
        {
            var braceStart = template.IndexOf('{', codeStart);
            if (braceStart >= 0)
            {
                int depth = 1;
                int i = braceStart + 1;
                while (i < template.Length && depth > 0)
                {
                    if (template[i] == '{') depth++;
                    if (template[i] == '}') depth--;
                    i++;
                }
                template = template.Substring(0, codeStart) + template.Substring(i);
            }
        }

        return template.Trim();
    }

    /// <summary>
    /// Find child component references in template
    /// </summary>
    private HashSet<string> FindChildComponents(string template)
    {
        var components = new HashSet<string>();
        var tagPattern = @"<([A-Z][a-zA-Z0-9]*)\s";

        foreach (Match match in Regex.Matches(template, tagPattern))
        {
            var tagName = match.Groups[1].Value;
            if (!IsHtmlElement(tagName))
            {
                components.Add(tagName);
            }
        }

        return components;
    }

    /// <summary>
    /// Transform Razor template to JSX
    /// </summary>
    private string TransformToJsx(string template)
    {
        var result = template;

        // Transform @if blocks with proper brace matching
        result = TransformIfBlocks(result);

        // Transform @foreach blocks with proper brace matching
        result = TransformForeachBlocks(result);

        // Transform @Props.X to {X}
        result = Regex.Replace(result, @"@Props\.(\w+)", "{$1}");

        // Transform remaining @variable to {variable}
        result = Regex.Replace(result, @"@(\w+)", "{$1}");

        // Transform class to className
        result = Regex.Replace(result, @"\bclass=", "className=");

        // Transform for to htmlFor
        result = Regex.Replace(result, @"\bfor=", "htmlFor=");

        // Transform style strings to objects
        result = TransformStyles(result);

        // Transform attribute values with JSX: attr="{X}" -> attr={X}
        result = Regex.Replace(result, @"(\w+)=""\{([^}]+)\}""", "$1={$2}");

        return result;
    }

    /// <summary>
    /// Transform @if blocks with proper brace matching
    /// </summary>
    private string TransformIfBlocks(string template)
    {
        var result = template;
        var ifPattern = @"@if\s*\(([^)]+)\)\s*\{";

        while (true)
        {
            var match = Regex.Match(result, ifPattern);
            if (!match.Success) break;

            var condition = match.Groups[1].Value.Trim()
                .Replace("Props.", "");

            var startIdx = match.Index;
            var braceStart = match.Index + match.Length - 1;

            // Find matching closing brace
            int depth = 1;
            int i = braceStart + 1;
            while (i < result.Length && depth > 0)
            {
                if (result[i] == '{') depth++;
                if (result[i] == '}') depth--;
                i++;
            }

            var content = result.Substring(braceStart + 1, i - braceStart - 2).Trim();
            var replacement = $"{{({condition}) && (\n{content}\n)}}";

            result = result.Substring(0, startIdx) + replacement + result.Substring(i);
        }

        return result;
    }

    /// <summary>
    /// Transform @foreach blocks with proper brace matching
    /// </summary>
    private string TransformForeachBlocks(string template)
    {
        var result = template;
        var foreachPattern = @"@foreach\s*\(\s*var\s+(\w+)\s+in\s+(\w+)\s*\)\s*\{";

        while (true)
        {
            var match = Regex.Match(result, foreachPattern);
            if (!match.Success) break;

            var itemVar = match.Groups[1].Value;
            var collection = match.Groups[2].Value;

            var startIdx = match.Index;
            var braceStart = match.Index + match.Length - 1;

            // Find matching closing brace
            int depth = 1;
            int i = braceStart + 1;
            while (i < result.Length && depth > 0)
            {
                if (result[i] == '{') depth++;
                if (result[i] == '}') depth--;
                i++;
            }

            var content = result.Substring(braceStart + 1, i - braceStart - 2).Trim();
            // Transform @item.X to {item.X}
            content = Regex.Replace(content, $@"@{itemVar}\.(\w+)", $"{{{itemVar}.$1}}");

            var replacement = $"{{{collection}.map(({itemVar}, index) => (\n{content}\n))}}";

            result = result.Substring(0, startIdx) + replacement + result.Substring(i);
        }

        return result;
    }

    /// <summary>
    /// Transform inline styles to React style objects
    /// </summary>
    private string TransformStyles(string template)
    {
        return Regex.Replace(template, @"style=""([^""]+)""", match =>
        {
            var styleString = match.Groups[1].Value;
            var props = new List<string>();

            foreach (var part in styleString.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var colonIdx = part.IndexOf(':');
                if (colonIdx <= 0) continue;

                var prop = part.Substring(0, colonIdx).Trim();
                var value = part.Substring(colonIdx + 1).Trim();

                // Convert kebab-case to camelCase
                prop = Regex.Replace(prop, @"-(\w)", m => m.Groups[1].Value.ToUpper());

                props.Add($"{prop}: '{value}'");
            }

            return $"style={{{{{string.Join(", ", props)}}}}}";
        });
    }

    /// <summary>
    /// Convert C# type to TypeScript type
    /// </summary>
    private string ConvertToTypeScript(string csharpType)
    {
        return csharpType switch
        {
            "string" => "string",
            "int" or "long" or "double" or "float" or "decimal" => "number",
            "bool" => "boolean",
            "DateTime" => "Date",
            "Guid" => "string",
            var t when t.StartsWith("List<") => $"{ConvertToTypeScript(t[5..^1])}[]",
            var t when t.EndsWith("[]") => $"{ConvertToTypeScript(t[..^2])}[]",
            var t when t.EndsWith("?") => $"{ConvertToTypeScript(t[..^1])} | null",
            _ => csharpType
        };
    }

    /// <summary>
    /// Check if tag is an HTML element
    /// </summary>
    private bool IsHtmlElement(string tagName)
    {
        var htmlElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "abbr", "address", "area", "article", "aside", "audio", "b", "base", "bdi", "bdo",
            "blockquote", "body", "br", "button", "canvas", "caption", "cite", "code", "col", "colgroup",
            "data", "datalist", "dd", "del", "details", "dfn", "dialog", "div", "dl", "dt", "em", "embed",
            "fieldset", "figcaption", "figure", "footer", "form", "h1", "h2", "h3", "h4", "h5", "h6",
            "head", "header", "hgroup", "hr", "html", "i", "iframe", "img", "input", "ins", "kbd", "label",
            "legend", "li", "link", "main", "map", "mark", "menu", "meta", "meter", "nav", "noscript",
            "object", "ol", "optgroup", "option", "output", "p", "picture", "pre", "progress", "q", "rp",
            "rt", "ruby", "s", "samp", "script", "section", "select", "slot", "small", "source", "span",
            "strong", "style", "sub", "summary", "sup", "svg", "table", "tbody", "td", "template", "textarea",
            "tfoot", "th", "thead", "time", "title", "tr", "track", "u", "ul", "var", "video", "wbr",
            "path", "circle", "rect", "line", "polygon", "polyline", "ellipse", "g", "text", "defs", "use"
        };
        return htmlElements.Contains(tagName);
    }

    /// <summary>
    /// Indent all lines of a string
    /// </summary>
    private string IndentLines(string text, string indent)
    {
        var lines = text.Split('\n');
        return string.Join("\n", lines.Select(l => string.IsNullOrWhiteSpace(l) ? l : indent + l));
    }
}

// Types
public enum ComponentDirective { Default, Server, Client }

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
