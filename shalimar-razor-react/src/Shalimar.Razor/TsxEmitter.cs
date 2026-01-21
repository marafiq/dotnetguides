using System.Text;
using System.Text.RegularExpressions;

namespace Shalimar.Razor;

/// <summary>
/// Transforms Razor components to TypeScript/TSX
/// </summary>
public class TsxEmitter
{
    private static readonly HashSet<string> HtmlElements = new(StringComparer.OrdinalIgnoreCase)
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

    private static readonly Dictionary<string, string> AttributeMap = new()
    {
        { "class", "className" },
        { "for", "htmlFor" },
        { "tabindex", "tabIndex" },
        { "readonly", "readOnly" },
        { "maxlength", "maxLength" },
        { "minlength", "minLength" },
        { "colspan", "colSpan" },
        { "rowspan", "rowSpan" },
        { "cellpadding", "cellPadding" },
        { "cellspacing", "cellSpacing" },
        { "usemap", "useMap" },
        { "frameborder", "frameBorder" },
        { "contenteditable", "contentEditable" },
        { "crossorigin", "crossOrigin" },
        { "datetime", "dateTime" },
        { "enctype", "encType" },
        { "formaction", "formAction" },
        { "formenctype", "formEncType" },
        { "formmethod", "formMethod" },
        { "formnovalidate", "formNoValidate" },
        { "formtarget", "formTarget" },
        { "hreflang", "hrefLang" },
        { "inputmode", "inputMode" },
        { "srcset", "srcSet" },
        { "autofocus", "autoFocus" },
        { "autoplay", "autoPlay" }
    };

    /// <summary>
    /// Emit TSX code for a Razor component
    /// </summary>
    public string Emit(RazorComponent component)
    {
        var sb = new StringBuilder();

        // Generate imports
        EmitImports(sb, component);

        // Generate props interface
        EmitPropsInterface(sb, component);

        // Generate component function
        EmitComponent(sb, component);

        return sb.ToString();
    }

    /// <summary>
    /// Emit TSX and write to file alongside the .razor file
    /// </summary>
    public string EmitToFile(RazorComponent component)
    {
        var tsx = Emit(component);
        var tsxPath = Path.ChangeExtension(component.FilePath, ".tsx");
        File.WriteAllText(tsxPath, tsx);
        return tsxPath;
    }

    private void EmitImports(StringBuilder sb, RazorComponent component)
    {
        sb.AppendLine("import React from 'react';");

        // Import child components from same feature folder
        var uniqueChildren = component.Children
            .Select(c => c.Name)
            .Distinct()
            .Where(name => !HtmlElements.Contains(name))
            .ToList();

        foreach (var child in uniqueChildren)
        {
            sb.AppendLine($"import {{ {child} }} from './{child}';");
        }

        if (uniqueChildren.Any())
        {
            sb.AppendLine();
        }
    }

    private void EmitPropsInterface(StringBuilder sb, RazorComponent component)
    {
        if (!component.Props.Any())
        {
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"export interface {component.Name}Props {{");

        foreach (var prop in component.Props)
        {
            var tsType = ConvertToTypeScriptType(prop.Type);
            var optional = prop.IsRequired ? "" : "?";
            sb.AppendLine($"  {prop.Name}{optional}: {tsType};");
        }

        sb.AppendLine("}");
        sb.AppendLine();
    }

    private void EmitComponent(StringBuilder sb, RazorComponent component)
    {
        var propsParam = component.Props.Any()
            ? $"{{ {string.Join(", ", component.Props.Select(p => p.Name))} }}: {component.Name}Props"
            : "";

        sb.AppendLine($"export function {component.Name}({propsParam}) {{");

        // Transform template to JSX
        var jsx = TransformToJsx(component.TemplateContent);

        sb.AppendLine("  return (");
        sb.AppendLine($"    {IndentJsx(jsx, 4)}");
        sb.AppendLine("  );");
        sb.AppendLine("}");
    }

    private string TransformToJsx(string template)
    {
        var result = template;

        // Transform @Props.X to {props.X} then to just {X} (destructured)
        result = Regex.Replace(result, @"@Props\.(\w+)", "{$1}");

        // Transform @if statements to conditional rendering
        result = TransformIfStatements(result);

        // Transform @foreach to .map()
        result = TransformForeachStatements(result);

        // Transform class to className
        result = TransformAttributes(result);

        // Transform @expression to {expression}
        result = Regex.Replace(result, @"@(\w+)", "{$1}");

        return result;
    }

    private string TransformIfStatements(string template)
    {
        // Simple @if (condition) { content }
        var ifRegex = new Regex(@"@if\s*\(([^)]+)\)\s*\{([^}]+)\}", RegexOptions.Singleline);
        return ifRegex.Replace(template, match =>
        {
            var condition = match.Groups[1].Value.Trim();
            var content = match.Groups[2].Value.Trim();
            // Transform C# condition to JS
            condition = TransformCondition(condition);
            return $"{{({condition}) && (\n{content}\n)}}";
        });
    }

    private string TransformForeachStatements(string template)
    {
        // @foreach (var item in Items) { content }
        var foreachRegex = new Regex(@"@foreach\s*\(\s*var\s+(\w+)\s+in\s+(\w+)\s*\)\s*\{([^}]+)\}", RegexOptions.Singleline);
        return foreachRegex.Replace(template, match =>
        {
            var itemVar = match.Groups[1].Value;
            var collection = match.Groups[2].Value;
            var content = match.Groups[3].Value.Trim();

            // Transform @item.Property to {item.property}
            content = Regex.Replace(content, $@"@{itemVar}\.(\w+)", $"{{{itemVar}.$1}}");

            return $"{{{collection}.map(({itemVar}, index) => (\n{content}\n))}}";
        });
    }

    private string TransformAttributes(string template)
    {
        foreach (var (razorAttr, reactAttr) in AttributeMap)
        {
            // Match attribute in tag context
            template = Regex.Replace(template, $@"\b{razorAttr}=", $"{reactAttr}=", RegexOptions.IgnoreCase);
        }

        // Transform style strings to objects
        template = TransformStyleAttributes(template);

        return template;
    }

    private string TransformStyleAttributes(string template)
    {
        // Transform style="color: red; margin: 10px" to style={{color: 'red', margin: '10px'}}
        var styleRegex = new Regex(@"style=""([^""]+)""");
        return styleRegex.Replace(template, match =>
        {
            var styleString = match.Groups[1].Value;
            var styleObj = ConvertStyleToObject(styleString);
            return $"style={{{{{styleObj}}}}}";
        });
    }

    private string ConvertStyleToObject(string styleString)
    {
        var parts = styleString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var properties = new List<string>();

        foreach (var part in parts)
        {
            var colonIndex = part.IndexOf(':');
            if (colonIndex <= 0) continue;

            var property = part[..colonIndex].Trim();
            var value = part[(colonIndex + 1)..].Trim();

            // Convert kebab-case to camelCase
            property = ConvertToCamelCase(property);

            // Quote string values, leave numbers unquoted
            if (!double.TryParse(value.TrimEnd('p', 'x', 'e', 'm', '%'), out _))
            {
                value = $"'{value}'";
            }
            else
            {
                value = $"'{value}'"; // Keep units as strings
            }

            properties.Add($"{property}: {value}");
        }

        return string.Join(", ", properties);
    }

    private string ConvertToCamelCase(string kebabCase)
    {
        var parts = kebabCase.Split('-');
        if (parts.Length == 1) return parts[0];

        return parts[0] + string.Concat(parts.Skip(1).Select(p =>
            char.ToUpper(p[0]) + p[1..]));
    }

    private string TransformCondition(string condition)
    {
        // Transform C# null checks to JS
        condition = condition.Replace(" != null", "");
        condition = condition.Replace(" == null", " === null");
        condition = condition.Replace(" == ", " === ");
        condition = condition.Replace(" != ", " !== ");
        return condition;
    }

    private string ConvertToTypeScriptType(string csharpType)
    {
        return csharpType switch
        {
            "string" => "string",
            "int" => "number",
            "long" => "number",
            "double" => "number",
            "float" => "number",
            "decimal" => "number",
            "bool" => "boolean",
            "DateTime" => "Date",
            "Guid" => "string",
            var t when t.StartsWith("List<") => $"{ConvertToTypeScriptType(t[5..^1])}[]",
            var t when t.StartsWith("IEnumerable<") => $"{ConvertToTypeScriptType(t[12..^1])}[]",
            var t when t.EndsWith("[]") => $"{ConvertToTypeScriptType(t[..^2])}[]",
            var t when t.EndsWith("?") => $"{ConvertToTypeScriptType(t[..^1])} | null",
            _ => csharpType // Keep custom types as-is
        };
    }

    private string IndentJsx(string jsx, int spaces)
    {
        var indent = new string(' ', spaces);
        var lines = jsx.Split('\n');

        if (lines.Length == 1) return jsx;

        return string.Join($"\n{indent}", lines);
    }
}
