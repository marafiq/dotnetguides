using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Shalimar.Razor;

/// <summary>
/// Server-side HTML renderer for Razor components.
/// Renders @server components to static HTML (no JS needed).
/// Marks @client components for hydration.
/// </summary>
public class ServerRenderer
{
    private readonly Dictionary<string, ComponentDef> _components = new();

    public void RegisterComponent(string name, string razorSource)
    {
        var def = new ComponentDef
        {
            Name = name,
            Source = razorSource,
            IsClient = razorSource.Contains("@client"),
            IsStreaming = razorSource.Contains("@streaming")
        };
        _components[name] = def;
    }

    /// <summary>
    /// Render a component tree to HTML.
    /// Server components become static HTML.
    /// Client components get hydration markers.
    /// </summary>
    public string RenderToHtml(string componentName, Dictionary<string, object> props)
    {
        if (!_components.TryGetValue(componentName, out var def))
        {
            return $"<!-- Unknown component: {componentName} -->";
        }

        if (def.IsClient)
        {
            // Client component: render placeholder with hydration marker
            return RenderClientComponent(componentName, props, def);
        }

        // Server component: render to static HTML
        return RenderServerComponent(componentName, props, def);
    }

    private string RenderServerComponent(string name, Dictionary<string, object> props, ComponentDef def)
    {
        var html = def.Source;

        // Remove directives
        html = Regex.Replace(html, @"@(server|client|streaming)\s*\n?", "");
        html = Regex.Replace(html, @"@inherits[^\n]+\n?", "");

        // Transform class → class (keep as-is for HTML)
        // Transform @Props.X → actual value
        foreach (var prop in props)
        {
            var pattern = $@"@Props\.{prop.Key}";
            var value = FormatValue(prop.Value);
            html = Regex.Replace(html, pattern, value);
        }

        // Handle @if blocks
        html = EvaluateIfBlocks(html, props);

        // Handle @foreach blocks
        html = EvaluateForeachBlocks(html, props);

        // Render nested components
        html = RenderNestedComponents(html, props);

        // Clean up remaining @ expressions
        html = Regex.Replace(html, @"@\w+\.?\w*", "");

        return html.Trim();
    }

    private string RenderClientComponent(string name, Dictionary<string, object> props, ComponentDef def)
    {
        // Pre-render the client component for initial HTML
        var preRendered = RenderServerComponent(name, props, def);
        var propsJson = JsonSerializer.Serialize(props);

        // Wrap in hydration boundary
        return $@"<div data-shalimar-hydrate=""{name}"" data-props='{propsJson}'>{preRendered}</div>";
    }

    private string EvaluateIfBlocks(string html, Dictionary<string, object> props)
    {
        var result = html;

        while (true)
        {
            var match = Regex.Match(result, @"@if\s*\(([^)]+)\)\s*\{");
            if (!match.Success) break;

            var condition = match.Groups[1].Value;
            var startBrace = match.Index + match.Length - 1;
            var content = ExtractBalancedBraces(result, startBrace);
            var endIndex = startBrace + content.Length + 2;

            // Evaluate condition
            var conditionResult = EvaluateCondition(condition, props);

            if (conditionResult)
            {
                result = result.Substring(0, match.Index) + content.Trim() + result.Substring(endIndex);
            }
            else
            {
                result = result.Substring(0, match.Index) + result.Substring(endIndex);
            }
        }

        return result;
    }

    private string EvaluateForeachBlocks(string html, Dictionary<string, object> props)
    {
        var result = html;

        while (true)
        {
            var match = Regex.Match(result, @"@foreach\s*\(\s*var\s+(\w+)\s+in\s+Props\.(\w+)\s*\)\s*\{");
            if (!match.Success) break;

            var itemVar = match.Groups[1].Value;
            var collectionName = match.Groups[2].Value;
            var startBrace = match.Index + match.Length - 1;
            var template = ExtractBalancedBraces(result, startBrace);
            var endIndex = startBrace + template.Length + 2;

            var sb = new StringBuilder();

            if (props.TryGetValue(collectionName, out var collection) && collection is IEnumerable<object> items)
            {
                foreach (var item in items)
                {
                    var itemHtml = template;
                    var itemDict = item as Dictionary<string, object> ?? new Dictionary<string, object>();

                    // First pass: replace all @itemVar.Prop patterns with actual values
                    foreach (var kvp in itemDict)
                    {
                        var pattern = $@"@{itemVar}\.{kvp.Key}";
                        itemHtml = Regex.Replace(itemHtml, pattern, FormatValue(kvp.Value));

                        // Also handle attribute patterns: Attr="@itemVar.Prop"
                        var attrPattern = $@"(\w+)=""@{itemVar}\.{kvp.Key}""";
                        itemHtml = Regex.Replace(itemHtml, attrPattern, $@"$1=""{FormatValue(kvp.Value)}""");
                    }

                    // Render any nested components in the loop body
                    itemHtml = RenderNestedComponentsInLoop(itemHtml, props, itemDict);

                    sb.Append(itemHtml.Trim());
                    sb.AppendLine();
                }
            }

            result = result.Substring(0, match.Index) + sb.ToString() + result.Substring(endIndex);
        }

        return result;
    }

    private string RenderNestedComponentsInLoop(string html, Dictionary<string, object> parentProps, Dictionary<string, object> loopItem)
    {
        var result = html;

        foreach (var comp in _components.Keys)
        {
            // Match self-closing components with attributes: <Component Attr="value" />
            var selfClosingPattern = $@"<{comp}\s+([^/]*?)\s*/>";
            result = Regex.Replace(result, selfClosingPattern, m =>
            {
                var attrs = m.Groups[1].Value;
                var childProps = ParseAttributesFromLoop(attrs, parentProps, loopItem);
                return RenderToHtml(comp, childProps);
            }, RegexOptions.Singleline);

            // Match components with children: <Component attr="val">content</Component>
            var withChildrenPattern = $@"<{comp}(\s+[^>]*)?>([\s\S]*?)</{comp}>";
            result = Regex.Replace(result, withChildrenPattern, m =>
            {
                var attrs = m.Groups[1].Value;
                var children = m.Groups[2].Value;
                var childProps = ParseAttributesFromLoop(attrs, parentProps, loopItem);
                childProps["Children"] = children.Trim();
                return RenderToHtml(comp, childProps);
            }, RegexOptions.Singleline);
        }

        return result;
    }

    private Dictionary<string, object> ParseAttributesFromLoop(string attrs, Dictionary<string, object> parentProps, Dictionary<string, object> loopItem)
    {
        var props = new Dictionary<string, object>();

        // Match Attr="value" patterns
        var matches = Regex.Matches(attrs, @"(\w+)=""([^""]+)""");
        foreach (Match m in matches)
        {
            var name = m.Groups[1].Value;
            var value = m.Groups[2].Value;
            props[name] = value;
        }

        // Also inherit from loop item for complex nested props
        foreach (var kvp in loopItem)
        {
            if (!props.ContainsKey(kvp.Key))
            {
                props[kvp.Key] = kvp.Value;
            }
        }

        return props;
    }

    private string RenderNestedComponents(string html, Dictionary<string, object> parentProps, object? loopItem = null)
    {
        var result = html;
        var maxIterations = 20; // Prevent infinite loops
        var iteration = 0;

        while (iteration++ < maxIterations)
        {
            var changed = false;

            foreach (var comp in _components.Keys)
            {
                // Match self-closing: <Component prop="value" />
                var selfClosingPattern = $@"<{comp}\s+([^>]*?)\s*/>";
                var selfClosingMatch = Regex.Match(result, selfClosingPattern);
                if (selfClosingMatch.Success)
                {
                    var attrs = selfClosingMatch.Groups[1].Value;
                    var childProps = ParseAttributes(attrs, parentProps, loopItem);
                    var rendered = RenderToHtml(comp, childProps);
                    result = result.Substring(0, selfClosingMatch.Index) + rendered +
                             result.Substring(selfClosingMatch.Index + selfClosingMatch.Length);
                    changed = true;
                    break; // Re-scan from beginning
                }

                // Match with children: <Component attrs>...</Component>
                // Use balanced tag matching for nested content
                var openTagPattern = $@"<{comp}(\s+[^>]*)?>";
                var openMatch = Regex.Match(result, openTagPattern);
                if (openMatch.Success)
                {
                    var startIndex = openMatch.Index;
                    var afterOpenTag = startIndex + openMatch.Length;
                    var closeTag = $"</{comp}>";
                    var closeIndex = FindMatchingCloseTag(result, afterOpenTag, comp);

                    if (closeIndex >= 0)
                    {
                        var children = result.Substring(afterOpenTag, closeIndex - afterOpenTag);
                        var attrs = openMatch.Groups[1].Value;
                        var childProps = ParseAttributes(attrs, parentProps, loopItem);
                        childProps["Children"] = children.Trim();
                        var rendered = RenderToHtml(comp, childProps);
                        result = result.Substring(0, startIndex) + rendered +
                                 result.Substring(closeIndex + closeTag.Length);
                        changed = true;
                        break; // Re-scan from beginning
                    }
                }
            }

            if (!changed) break;
        }

        return result;
    }

    private int FindMatchingCloseTag(string html, int startIndex, string tagName)
    {
        var depth = 1;
        var openTag = $"<{tagName}";
        var closeTag = $"</{tagName}>";
        var i = startIndex;

        while (i < html.Length && depth > 0)
        {
            var nextOpen = html.IndexOf(openTag, i);
            var nextClose = html.IndexOf(closeTag, i);

            if (nextClose < 0) return -1; // No matching close tag

            if (nextOpen >= 0 && nextOpen < nextClose)
            {
                depth++;
                i = nextOpen + openTag.Length;
            }
            else
            {
                depth--;
                if (depth == 0) return nextClose;
                i = nextClose + closeTag.Length;
            }
        }

        return -1;
    }

    private Dictionary<string, object> ParseAttributes(string attrs, Dictionary<string, object> parentProps, object? loopItem = null)
    {
        var props = new Dictionary<string, object>();

        // Match Attr="@Props.X" or Attr="@item.X" or Attr="literal"
        var matches = Regex.Matches(attrs, @"(\w+)=""([^""]+)""");
        foreach (Match m in matches)
        {
            var name = m.Groups[1].Value;
            var value = m.Groups[2].Value;

            if (value.StartsWith("@Props."))
            {
                var propName = value.Substring(7);
                if (parentProps.TryGetValue(propName, out var propValue))
                {
                    props[name] = propValue;
                }
            }
            else if (value.StartsWith("@") && loopItem is Dictionary<string, object> itemDict)
            {
                var itemPath = value.Substring(1);
                var parts = itemPath.Split('.');
                if (parts.Length == 2 && itemDict.TryGetValue(parts[1], out var itemValue))
                {
                    props[name] = itemValue;
                }
            }
            else
            {
                props[name] = value;
            }
        }

        return props;
    }

    private bool EvaluateCondition(string condition, Dictionary<string, object> props)
    {
        // Simple evaluation: Props.X → check if truthy
        var match = Regex.Match(condition, @"Props\.(\w+)");
        if (match.Success)
        {
            var propName = match.Groups[1].Value;
            if (props.TryGetValue(propName, out var value))
            {
                return value switch
                {
                    bool b => b,
                    string s => !string.IsNullOrEmpty(s),
                    null => false,
                    _ => true
                };
            }
        }
        return false;
    }

    private string ExtractBalancedBraces(string source, int openBraceIndex)
    {
        var depth = 1;
        var i = openBraceIndex + 1;
        while (i < source.Length && depth > 0)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}') depth--;
            i++;
        }
        return source.Substring(openBraceIndex + 1, i - openBraceIndex - 2);
    }

    private string FormatValue(object? value)
    {
        return value?.ToString() ?? "";
    }

    private class ComponentDef
    {
        public string Name { get; set; } = "";
        public string Source { get; set; } = "";
        public bool IsClient { get; set; }
        public bool IsStreaming { get; set; }
    }
}
