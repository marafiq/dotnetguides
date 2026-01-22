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
            StoreVars = new List<StoreVarInfo>(),
            Methods = new List<MethodInfo>(),
            Imports = new List<ImportInfo>(),
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

        // Extract @using imports
        var usingPattern = @"@using\s+""([^""]+)""(?:\s+as\s+(\w+))?";
        foreach (Match match in Regex.Matches(content, usingPattern))
        {
            info.Imports.Add(new ImportInfo
            {
                Path = match.Groups[1].Value,
                Alias = match.Groups[2].Success ? match.Groups[2].Value : null,
                IsDefault = true
            });
        }

        // Extract @using { named } from "path"
        var namedUsingPattern = @"@using\s+\{\s*([^}]+)\s*\}\s+from\s+""([^""]+)""";
        foreach (Match match in Regex.Matches(content, namedUsingPattern))
        {
            var names = match.Groups[1].Value.Split(',').Select(n => n.Trim()).ToArray();
            info.Imports.Add(new ImportInfo
            {
                Path = match.Groups[2].Value,
                NamedImports = names,
                IsDefault = false
            });
        }

        // Extract @code block
        var codeBlockMatch = Regex.Match(content, @"@code\s*\{([\s\S]*)\}\s*$", RegexOptions.Multiline);
        if (codeBlockMatch.Success)
        {
            var codeBlock = codeBlockMatch.Groups[1].Value;
            info.Props = ExtractProps(codeBlock);
            info.StoreVars = ExtractStoreVars(codeBlock);
            info.Methods = ExtractMethods(codeBlock);
        }

        return info;
    }

    /// <summary>
    /// Extract [Store] variables from code block
    /// </summary>
    private List<StoreVarInfo> ExtractStoreVars(string codeBlock)
    {
        var vars = new List<StoreVarInfo>();
        var storePattern = @"\[Store\]\s+(\w+(?:\?)?)\s+(\w+)\s*=\s*([^;]+);";

        foreach (Match match in Regex.Matches(codeBlock, storePattern))
        {
            vars.Add(new StoreVarInfo
            {
                CSharpType = match.Groups[1].Value,
                Name = match.Groups[2].Value,
                DefaultValue = match.Groups[3].Value.Trim(),
                TypeScriptType = ConvertToTypeScript(match.Groups[1].Value)
            });
        }

        return vars;
    }

    /// <summary>
    /// Extract methods from code block
    /// </summary>
    private List<MethodInfo> ExtractMethods(string codeBlock)
    {
        var methods = new List<MethodInfo>();
        var methodPattern = @"(?:async\s+)?void\s+(\w+)\s*\([^)]*\)\s*\{";

        foreach (Match match in Regex.Matches(codeBlock, methodPattern))
        {
            var methodName = match.Groups[1].Value;
            var startIdx = match.Index + match.Length - 1;

            // Find method body with brace matching
            int depth = 1;
            int i = startIdx + 1;
            while (i < codeBlock.Length && depth > 0)
            {
                if (codeBlock[i] == '{') depth++;
                if (codeBlock[i] == '}') depth--;
                i++;
            }

            var body = codeBlock.Substring(startIdx + 1, i - startIdx - 2).Trim();
            var isAsync = match.Value.StartsWith("async");

            methods.Add(new MethodInfo
            {
                Name = methodName,
                Body = body,
                IsAsync = isAsync
            });
        }

        return methods;
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

        // Add TanStack Store imports if using [Store]
        if (component.StoreVars.Any())
        {
            sb.AppendLine("import { useStore } from '@tanstack/react-store';");
            sb.AppendLine("import { Store } from '@tanstack/store';");
        }

        // Add explicit @using imports
        foreach (var import in component.Imports)
        {
            if (import.IsDefault)
            {
                var name = import.Alias ?? Path.GetFileNameWithoutExtension(import.Path);
                sb.AppendLine($"import {{ {name} }} from '{import.Path}';");
            }
            else if (import.NamedImports != null)
            {
                sb.AppendLine($"import {{ {string.Join(", ", import.NamedImports)} }} from '{import.Path}';");
            }
        }

        foreach (var child in childComponents)
        {
            // Skip if already imported via @using
            if (!component.Imports.Any(i => i.Path.Contains(child) || i.Alias == child))
            {
                sb.AppendLine($"import {{ {child} }} from './{child}';");
            }
        }
        sb.AppendLine();

        // Generate TanStack Store if using [Store] vars
        if (component.StoreVars.Any())
        {
            sb.AppendLine($"interface {component.Name}State {{");
            foreach (var v in component.StoreVars)
            {
                sb.AppendLine($"  {v.Name}: {v.TypeScriptType};");
            }
            sb.AppendLine("}");
            sb.AppendLine();

            var storeVarName = char.ToLower(component.Name[0]) + component.Name.Substring(1) + "Store";
            sb.AppendLine($"const {storeVarName} = new Store<{component.Name}State>({{");
            foreach (var v in component.StoreVars)
            {
                var tsValue = ConvertValueToTypeScript(v.DefaultValue, v.CSharpType);
                sb.AppendLine($"  {v.Name}: {tsValue},");
            }
            sb.AppendLine("});");
            sb.AppendLine();
        }

        // Props interface (including children if used)
        var hasChildren = template.Contains("@children");
        if (component.Props.Any() || hasChildren)
        {
            sb.AppendLine($"export interface {component.Name}Props {{");
            foreach (var prop in component.Props)
            {
                // Skip children as we handle it separately
                if (prop.Name.ToLower() == "children") continue;
                var optional = prop.DefaultValue != null ? "?" : "";
                sb.AppendLine($"  {prop.Name}{optional}: {prop.TypeScriptType};");
            }
            if (hasChildren)
            {
                sb.AppendLine("  children?: React.ReactNode;");
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }

        // Component function
        var propsItems = component.Props.Where(p => p.Name.ToLower() != "children").Select(p => p.Name).ToList();
        if (hasChildren) propsItems.Add("children");

        var propsParam = (component.Props.Any() || hasChildren)
            ? $"{{ {string.Join(", ", propsItems)} }}: {component.Name}Props"
            : "";

        sb.AppendLine($"export function {component.Name}({propsParam}) {{");

        // Generate useState hooks for store vars
        if (component.StoreVars.Any())
        {
            var storeVarName = char.ToLower(component.Name[0]) + component.Name.Substring(1) + "Store";
            foreach (var v in component.StoreVars)
            {
                sb.AppendLine($"  const {v.Name} = useStore({storeVarName}, (s) => s.{v.Name});");
            }
            sb.AppendLine();

            // Generate setter functions for store vars
            foreach (var v in component.StoreVars)
            {
                var setterName = "set" + char.ToUpper(v.Name[0]) + v.Name.Substring(1);
                sb.AppendLine($"  const {setterName} = (value: {v.TypeScriptType}) => {storeVarName}.setState((s) => ({{ ...s, {v.Name}: value }}));");
            }
            sb.AppendLine();
        }

        // Generate methods
        foreach (var method in component.Methods)
        {
            var asyncPrefix = method.IsAsync ? "async " : "";
            var methodBody = TransformMethodBody(method.Body, component);
            sb.AppendLine($"  const {method.Name} = {asyncPrefix}() => {{");
            sb.AppendLine($"    {methodBody}");
            sb.AppendLine("  };");
            sb.AppendLine();
        }

        sb.AppendLine("  return (");

        // Transform template to JSX
        var jsx = TransformToJsx(template, component);

        // Check if JSX needs fragment wrapper (starts with expression or has multiple roots)
        var trimmedJsx = jsx.Trim();
        var needsFragment = trimmedJsx.StartsWith("{") || HasMultipleRootElements(trimmedJsx);

        if (needsFragment)
        {
            sb.AppendLine("    <>");
            var indentedJsx = IndentLines(jsx, "      ");
            sb.AppendLine(indentedJsx);
            sb.AppendLine("    </>");
        }
        else
        {
            var indentedJsx = IndentLines(jsx, "    ");
            sb.AppendLine(indentedJsx);
        }

        sb.AppendLine("  );");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Convert C# value to TypeScript value
    /// </summary>
    private string ConvertValueToTypeScript(string value, string csharpType)
    {
        if (value == "new()" || value == "new List<>()" || value.StartsWith("new List"))
            return "[]";
        if (value == "\"\"")
            return "\"\"";
        if (value == "null")
            return "null";
        if (value == "true" || value == "false")
            return value;
        if (int.TryParse(value, out _) || double.TryParse(value, out _))
            return value;
        return value;
    }

    /// <summary>
    /// Transform C# method body to TypeScript
    /// </summary>
    private string TransformMethodBody(string body, ComponentInfo component)
    {
        var result = body;

        // Transform store var mutations (e.g., count++ or count = value)
        var storeVarName = char.ToLower(component.Name[0]) + component.Name.Substring(1) + "Store";

        foreach (var v in component.StoreVars)
        {
            // count++
            result = Regex.Replace(result, $@"\b{v.Name}\+\+", $"{storeVarName}.setState((s) => ({{ ...s, {v.Name}: s.{v.Name} + 1 }}))");

            // count--
            result = Regex.Replace(result, $@"\b{v.Name}--", $"{storeVarName}.setState((s) => ({{ ...s, {v.Name}: s.{v.Name} - 1 }}))");

            // count = value
            result = Regex.Replace(result, $@"\b{v.Name}\s*=\s*([^;]+);", m =>
            {
                var newValue = m.Groups[1].Value.Trim();
                return $"{storeVarName}.setState((s) => ({{ ...s, {v.Name}: {newValue} }}));";
            });
        }

        return result;
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
    private string TransformToJsx(string template, ComponentInfo? component = null)
    {
        var result = template;

        // Transform @foreach blocks with proper brace matching (must be BEFORE @if)
        // This allows nested @if to be handled correctly without double-wrapping
        result = TransformForeachBlocks(result);

        // Transform @if/else if/else blocks with proper brace matching
        result = TransformIfElseBlocks(result);

        // Transform explicit expressions @(...)
        result = TransformExplicitExpressions(result);

        // Transform @bind directives
        result = TransformBindDirectives(result, component);

        // Transform event handlers @onclick, @onchange, etc.
        result = TransformEventHandlers(result, component);

        // Transform @Props.X to {X}
        result = Regex.Replace(result, @"@Props\.(\w+)", "{$1}");

        // Transform @children to {children}
        result = Regex.Replace(result, @"@children", "{children}");

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
    /// Transform event handlers (@onclick → onClick, etc.)
    /// </summary>
    private string TransformEventHandlers(string template, ComponentInfo? component = null)
    {
        var result = template;

        // Map of Razor event handlers to React
        var eventMap = new Dictionary<string, string>
        {
            { "onclick", "onClick" },
            { "onchange", "onChange" },
            { "onsubmit", "onSubmit" },
            { "oninput", "onInput" },
            { "onkeydown", "onKeyDown" },
            { "onkeyup", "onKeyUp" },
            { "onkeypress", "onKeyPress" },
            { "onmousedown", "onMouseDown" },
            { "onmouseup", "onMouseUp" },
            { "onmousemove", "onMouseMove" },
            { "onmouseenter", "onMouseEnter" },
            { "onmouseleave", "onMouseLeave" },
            { "onfocus", "onFocus" },
            { "onblur", "onBlur" },
            { "ondblclick", "onDoubleClick" },
            { "onscroll", "onScroll" },
            { "onclose", "onClose" }
        };

        foreach (var (razorEvent, reactEvent) in eventMap)
        {
            // @onclick="handler" -> onClick={handler}
            result = Regex.Replace(result, $@"@{razorEvent}=""([^""]+)""", match =>
            {
                var handler = match.Groups[1].Value;

                // Transform inline store mutations if component has store vars
                if (component != null)
                {
                    var storeVarName = char.ToLower(component.Name[0]) + component.Name.Substring(1) + "Store";
                    foreach (var v in component.StoreVars)
                    {
                        // () => varName++ -> () => store.setState(...)
                        handler = Regex.Replace(handler, $@"\(\)\s*=>\s*{v.Name}\+\+",
                            $"() => {storeVarName}.setState((s) => ({{ ...s, {v.Name}: s.{v.Name} + 1 }}))");

                        // () => varName-- -> () => store.setState(...)
                        handler = Regex.Replace(handler, $@"\(\)\s*=>\s*{v.Name}--",
                            $"() => {storeVarName}.setState((s) => ({{ ...s, {v.Name}: s.{v.Name} - 1 }}))");
                    }
                }

                return $"{reactEvent}={{{handler}}}";
            });
        }

        return result;
    }

    /// <summary>
    /// Transform explicit expressions @(...)
    /// </summary>
    private string TransformExplicitExpressions(string template)
    {
        var result = template;

        // Find @(...) patterns with proper parenthesis matching
        var pattern = @"@\(";
        int offset = 0;

        while (true)
        {
            var match = Regex.Match(result.Substring(offset), pattern);
            if (!match.Success) break;

            var startIdx = offset + match.Index;
            var parenStart = startIdx + 2; // After @(

            // Find matching closing paren
            int depth = 1;
            int i = parenStart;
            while (i < result.Length && depth > 0)
            {
                if (result[i] == '(') depth++;
                if (result[i] == ')') depth--;
                i++;
            }

            var content = result.Substring(parenStart, i - parenStart - 1);

            // Transform Props.X to X
            content = Regex.Replace(content, @"Props\.(\w+)", "$1");

            // Transform C# string interpolation to JS template literal
            if (content.StartsWith("$\""))
            {
                content = TransformStringInterpolation(content);
            }

            // Transform == to === for string comparisons
            content = Regex.Replace(content, @"(\w+)\s*==\s*""", "$1 === \"");

            var replacement = $"{{{content}}}";
            result = result.Substring(0, startIdx) + replacement + result.Substring(i);

            offset = startIdx + replacement.Length;
        }

        return result;
    }

    /// <summary>
    /// Transform C# string interpolation to JS template literal
    /// </summary>
    private string TransformStringInterpolation(string expr)
    {
        // $"Hello {name}!" -> `Hello ${name}!`
        var content = expr.Substring(2, expr.Length - 3); // Remove $" and "
        content = Regex.Replace(content, @"\{(\w+)\}", "${$1}");
        return $"`{content}`";
    }

    /// <summary>
    /// Transform @bind directives to value + onChange
    /// </summary>
    private string TransformBindDirectives(string template, ComponentInfo? component)
    {
        var result = template;

        // @bind="varName" for input
        result = Regex.Replace(result, @"<input([^>]*)\s+@bind=""(\w+)""([^>]*)/>", match =>
        {
            var before = match.Groups[1].Value;
            var varName = match.Groups[2].Value;
            var after = match.Groups[3].Value;
            var fullAttrs = before + after;

            // Check if it's a checkbox
            if (fullAttrs.Contains("type=\"checkbox\"") || fullAttrs.Contains("type='checkbox'"))
            {
                var setterName = "set" + char.ToUpper(varName[0]) + varName.Substring(1);
                return $"<input{before} checked={{{varName}}} onChange={{(e) => {setterName}(e.target.checked)}}{after}/>";
            }
            else
            {
                var setterName = "set" + char.ToUpper(varName[0]) + varName.Substring(1);
                return $"<input{before} value={{{varName}}} onChange={{(e) => {setterName}(e.target.value)}}{after}/>";
            }
        });

        // @bind for select
        result = Regex.Replace(result, @"<select([^>]*)\s+@bind=""(\w+)""([^>]*)>", match =>
        {
            var before = match.Groups[1].Value;
            var varName = match.Groups[2].Value;
            var after = match.Groups[3].Value;
            var setterName = "set" + char.ToUpper(varName[0]) + varName.Substring(1);
            return $"<select{before} value={{{varName}}} onChange={{(e) => {setterName}(e.target.value)}}{after}>";
        });

        // @bind for textarea
        result = Regex.Replace(result, @"<textarea([^>]*)\s+@bind=""(\w+)""([^>]*)>", match =>
        {
            var before = match.Groups[1].Value;
            var varName = match.Groups[2].Value;
            var after = match.Groups[3].Value;
            var setterName = "set" + char.ToUpper(varName[0]) + varName.Substring(1);
            return $"<textarea{before} value={{{varName}}} onChange={{(e) => {setterName}(e.target.value)}}{after}>";
        });

        return result;
    }

    /// <summary>
    /// Transform @if/else if/else blocks with proper brace matching
    /// </summary>
    private string TransformIfElseBlocks(string template)
    {
        var result = template;
        var ifPattern = @"@if\s*\(([^)]+)\)\s*\{";

        while (true)
        {
            var match = Regex.Match(result, ifPattern);
            if (!match.Success) break;

            var condition = match.Groups[1].Value.Trim()
                .Replace("Props.", "");

            // Transform == to === for string comparisons
            condition = Regex.Replace(condition, @"(\w+)\s*==\s*""", "$1 === \"");

            var startIdx = match.Index;
            var braceStart = match.Index + match.Length - 1;

            // Find matching closing brace for if block
            int depth = 1;
            int i = braceStart + 1;
            while (i < result.Length && depth > 0)
            {
                if (result[i] == '{') depth++;
                if (result[i] == '}') depth--;
                i++;
            }

            var ifContent = result.Substring(braceStart + 1, i - braceStart - 2).Trim();
            var endIdx = i;

            // Check for else if / else
            var remaining = result.Substring(i).TrimStart();
            var elseIfPattern = @"^else\s+if\s*\(([^)]+)\)\s*\{";
            var elsePattern = @"^else\s*\{";

            var elseIfConditions = new List<(string condition, string content)>();
            string? elseContent = null;

            while (true)
            {
                var elseIfMatch = Regex.Match(remaining, elseIfPattern);
                var elseMatch = Regex.Match(remaining, elsePattern);

                if (elseIfMatch.Success)
                {
                    var elseIfCond = elseIfMatch.Groups[1].Value.Trim().Replace("Props.", "");
                    elseIfCond = Regex.Replace(elseIfCond, @"(\w+)\s*==\s*""", "$1 === \"");

                    var elseIfBraceStart = elseIfMatch.Length - 1;
                    depth = 1;
                    int j = elseIfBraceStart + 1;
                    while (j < remaining.Length && depth > 0)
                    {
                        if (remaining[j] == '{') depth++;
                        if (remaining[j] == '}') depth--;
                        j++;
                    }

                    var elseIfContent = remaining.Substring(elseIfBraceStart + 1, j - elseIfBraceStart - 2).Trim();
                    elseIfConditions.Add((elseIfCond, elseIfContent));

                    endIdx += (result.Substring(i).Length - result.Substring(i).TrimStart().Length) + j;
                    remaining = remaining.Substring(j).TrimStart();
                }
                else if (elseMatch.Success)
                {
                    var elseBraceStart = elseMatch.Length - 1;
                    depth = 1;
                    int j = elseBraceStart + 1;
                    while (j < remaining.Length && depth > 0)
                    {
                        if (remaining[j] == '{') depth++;
                        if (remaining[j] == '}') depth--;
                        j++;
                    }

                    elseContent = remaining.Substring(elseBraceStart + 1, j - elseBraceStart - 2).Trim();
                    endIdx += (result.Substring(i).Length - result.Substring(i).TrimStart().Length) + j;
                    break;
                }
                else
                {
                    break;
                }
            }

            // Generate JSX
            string replacement;
            if (elseIfConditions.Any() || elseContent != null)
            {
                // Use ternary expression: condition ? if : (elseIfCond ? elseIf : else)
                var ternary = BuildTernary(condition, ifContent, elseIfConditions, elseContent);
                replacement = $"{{{ternary}}}";
            }
            else
            {
                // Simple && pattern
                replacement = $"{{({condition}) && (\n{ifContent}\n)}}";
            }

            result = result.Substring(0, startIdx) + replacement + result.Substring(endIdx);
        }

        return result;
    }

    /// <summary>
    /// Build nested ternary expression for if/else if/else
    /// </summary>
    private string BuildTernary(string condition, string ifContent, List<(string condition, string content)> elseIfs, string? elseContent)
    {
        var sb = new StringBuilder();
        sb.Append($"({condition}) ? (\n{ifContent}\n)");

        foreach (var (cond, content) in elseIfs)
        {
            sb.Append($" : ({cond}) ? (\n{content}\n)");
        }

        if (elseContent != null)
        {
            sb.Append($" : (\n{elseContent}\n)");
        }
        else
        {
            sb.Append(" : null");
        }

        return sb.ToString();
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

            // Transform nested @if inside foreach - use expression form without outer braces
            content = TransformNestedIfInLoop(content, itemVar);

            var replacement = $"{{{collection}.map(({itemVar}, index) => (\n{content}\n))}}";

            result = result.Substring(0, startIdx) + replacement + result.Substring(i);
        }

        return result;
    }

    /// <summary>
    /// Transform nested @if blocks inside a loop (returns expression, not JSX)
    /// </summary>
    private string TransformNestedIfInLoop(string content, string itemVar)
    {
        var result = content;
        var ifPattern = @"@if\s*\(([^)]+)\)\s*\{";

        while (true)
        {
            var match = Regex.Match(result, ifPattern);
            if (!match.Success) break;

            var condition = match.Groups[1].Value.Trim();
            // Replace item. references with item var
            condition = condition.Replace($"{itemVar}.", $"{itemVar}.");

            var startIdx = match.Index;
            var braceStart = match.Index + match.Length - 1;

            // Find matching closing brace
            int depth = 1;
            int idx = braceStart + 1;
            while (idx < result.Length && depth > 0)
            {
                if (result[idx] == '{') depth++;
                if (result[idx] == '}') depth--;
                idx++;
            }

            var ifContent = result.Substring(braceStart + 1, idx - braceStart - 2).Trim();
            var endIdx = idx;

            // Check for else
            var remaining = result.Substring(idx).TrimStart();
            string? elseContent = null;

            var elseMatch = Regex.Match(remaining, @"^else\s*\{");
            if (elseMatch.Success)
            {
                var elseBraceStart = elseMatch.Length - 1;
                depth = 1;
                int j = elseBraceStart + 1;
                while (j < remaining.Length && depth > 0)
                {
                    if (remaining[j] == '{') depth++;
                    if (remaining[j] == '}') depth--;
                    j++;
                }
                elseContent = remaining.Substring(elseBraceStart + 1, j - elseBraceStart - 2).Trim();
                endIdx += (result.Substring(idx).Length - result.Substring(idx).TrimStart().Length) + j;
            }

            // Generate expression without outer braces (since we're already in a map callback)
            string replacement;
            if (elseContent != null)
            {
                replacement = $"({condition}) ? (\n{ifContent}\n) : (\n{elseContent}\n)";
            }
            else
            {
                replacement = $"({condition}) && (\n{ifContent}\n)";
            }

            result = result.Substring(0, startIdx) + replacement + result.Substring(endIdx);
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
    /// Check if JSX has multiple root elements (needs fragment wrapper)
    /// </summary>
    private bool HasMultipleRootElements(string jsx)
    {
        var trimmed = jsx.Trim();
        if (string.IsNullOrEmpty(trimmed)) return false;

        // Count top-level elements by tracking depth
        int depth = 0;
        int rootCount = 0;
        int i = 0;

        while (i < trimmed.Length)
        {
            if (trimmed[i] == '<')
            {
                // Check for closing tag
                if (i + 1 < trimmed.Length && trimmed[i + 1] == '/')
                {
                    depth--;
                    // Skip to end of tag
                    while (i < trimmed.Length && trimmed[i] != '>') i++;
                }
                // Check for self-closing or opening tag
                else if (i + 1 < trimmed.Length && trimmed[i + 1] != '!')
                {
                    if (depth == 0) rootCount++;
                    depth++;
                    // Check for self-closing
                    while (i < trimmed.Length && trimmed[i] != '>')
                    {
                        if (trimmed[i] == '/' && i + 1 < trimmed.Length && trimmed[i + 1] == '>')
                        {
                            depth--;
                            break;
                        }
                        i++;
                    }
                }
            }
            i++;
        }

        return rootCount > 1;
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
    public List<StoreVarInfo> StoreVars { get; set; } = new();
    public List<MethodInfo> Methods { get; set; } = new();
    public List<ImportInfo> Imports { get; set; } = new();
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

public class StoreVarInfo
{
    public string Name { get; set; } = "";
    public string CSharpType { get; set; } = "";
    public string TypeScriptType { get; set; } = "";
    public string DefaultValue { get; set; } = "";
}

public class MethodInfo
{
    public string Name { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsAsync { get; set; }
}

public class ImportInfo
{
    public string Path { get; set; } = "";
    public string? Alias { get; set; }
    public string[]? NamedImports { get; set; }
    public bool IsDefault { get; set; }
}
