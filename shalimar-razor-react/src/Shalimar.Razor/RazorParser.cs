using System.Text.RegularExpressions;

namespace Shalimar.Razor;

/// <summary>
/// Parses .razor files using regex-based parsing
/// </summary>
public class RazorParser
{
    private static readonly Regex DirectiveRegex = new(@"^@(server|client)\s*$", RegexOptions.Multiline);
    private static readonly Regex PropsRegex = new(@"@Props\.(\w+)");
    private static readonly Regex ImportRegex = new(@"^@using\s+(.+)$", RegexOptions.Multiline);
    private static readonly Regex CodeBlockRegex = new(@"@code\s*\{([\s\S]*?)\}(?=\s*$|\s*@|\s*<)", RegexOptions.Multiline);
    private static readonly Regex PropertyRegex = new(@"\[Parameter\]\s*public\s+(\w+(?:<[^>]+>)?)\s+(\w+)\s*\{\s*get;\s*set;\s*\}(?:\s*=\s*([^;]+);)?");
    private static readonly Regex ComponentTagRegex = new(@"<([A-Z][a-zA-Z0-9]*)\s*([^>]*?)(/?)>", RegexOptions.Singleline);

    /// <summary>
    /// Parse a .razor file into a RazorComponent
    /// </summary>
    public RazorComponent Parse(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var directory = Path.GetDirectoryName(filePath) ?? "";
        var featureFolder = GetFeatureFolder(directory);

        // Parse directive (@server or @client)
        var directive = ParseDirective(content);

        // Parse imports
        var imports = ParseImports(content);

        // Parse code block and extract props
        var (codeBlock, props) = ParseCodeBlock(content);

        // Parse template (everything except directives, imports, and code block)
        var templateContent = ExtractTemplate(content);

        // Find child components
        var children = FindChildComponents(templateContent);

        return new RazorComponent
        {
            Name = fileName,
            FilePath = filePath,
            FeatureFolder = featureFolder,
            Directive = directive,
            Props = props,
            Imports = imports,
            Children = children,
            TemplateContent = templateContent,
            CodeBlock = codeBlock
        };
    }

    /// <summary>
    /// Parse multiple .razor files from a directory
    /// </summary>
    public IEnumerable<RazorComponent> ParseDirectory(string directory, bool recursive = true)
    {
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var razorFiles = Directory.GetFiles(directory, "*.razor", searchOption);

        foreach (var file in razorFiles)
        {
            yield return Parse(file);
        }
    }

    private static string GetFeatureFolder(string directory)
    {
        // Extract feature folder from path (e.g., "Features/Users" -> "Users")
        var parts = directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var featuresIndex = Array.FindIndex(parts, p => p.Equals("Features", StringComparison.OrdinalIgnoreCase));

        if (featuresIndex >= 0 && featuresIndex < parts.Length - 1)
        {
            return parts[featuresIndex + 1];
        }

        return parts.LastOrDefault() ?? "Root";
    }

    private static ComponentDirective ParseDirective(string content)
    {
        var match = DirectiveRegex.Match(content);
        if (!match.Success) return ComponentDirective.Default;

        return match.Groups[1].Value.ToLower() switch
        {
            "server" => ComponentDirective.Server,
            "client" => ComponentDirective.Client,
            _ => ComponentDirective.Default
        };
    }

    private static List<string> ParseImports(string content)
    {
        var imports = new List<string>();
        var matches = ImportRegex.Matches(content);

        foreach (Match match in matches)
        {
            imports.Add(match.Groups[1].Value.Trim());
        }

        return imports;
    }

    private static (string? codeBlock, List<ComponentProp> props) ParseCodeBlock(string content)
    {
        var props = new List<ComponentProp>();
        var match = CodeBlockRegex.Match(content);

        if (!match.Success) return (null, props);

        var codeBlock = match.Groups[1].Value.Trim();
        var propMatches = PropertyRegex.Matches(codeBlock);

        foreach (Match propMatch in propMatches)
        {
            props.Add(new ComponentProp
            {
                Type = propMatch.Groups[1].Value,
                Name = propMatch.Groups[2].Value,
                DefaultValue = propMatch.Groups[3].Success ? propMatch.Groups[3].Value.Trim() : null,
                IsRequired = !propMatch.Groups[3].Success
            });
        }

        return (codeBlock, props);
    }

    private static string ExtractTemplate(string content)
    {
        // Remove directives, imports, and code block
        var template = DirectiveRegex.Replace(content, "");
        template = ImportRegex.Replace(template, "");
        template = CodeBlockRegex.Replace(template, "");

        return template.Trim();
    }

    private static List<ComponentChild> FindChildComponents(string template)
    {
        var children = new List<ComponentChild>();
        var matches = ComponentTagRegex.Matches(template);

        foreach (Match match in matches)
        {
            var tagName = match.Groups[1].Value;
            var attributes = match.Groups[2].Value;
            var isSelfClosing = match.Groups[3].Value == "/";

            // Skip HTML elements (start with lowercase)
            if (char.IsLower(tagName[0])) continue;

            var props = ParseAttributes(attributes);

            children.Add(new ComponentChild
            {
                Name = tagName,
                TagName = tagName,
                Props = props,
                IsSelfClosing = isSelfClosing
            });
        }

        return children;
    }

    private static Dictionary<string, string> ParseAttributes(string attributes)
    {
        var props = new Dictionary<string, string>();
        var attrRegex = new Regex(@"(\w+)(?:=""([^""]*)""|=\{([^}]+)\}|=@([^\s/>]+))?");

        foreach (Match match in attrRegex.Matches(attributes))
        {
            var name = match.Groups[1].Value;
            var value = match.Groups[2].Success ? match.Groups[2].Value :
                        match.Groups[3].Success ? match.Groups[3].Value :
                        match.Groups[4].Success ? match.Groups[4].Value : "true";

            props[name] = value;
        }

        return props;
    }
}
