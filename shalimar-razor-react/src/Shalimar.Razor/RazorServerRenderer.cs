using System.Text.RegularExpressions;
using System.Text.Json;

namespace Shalimar.Razor;

/// <summary>
/// Server-side renderer that takes a Razor template and props,
/// and outputs HTML that can be hydrated by React on the client.
///
/// This is the KEY to SSR: same Razor file serves both:
/// 1. Server rendering (this class) → HTML
/// 2. Client hydration (TsxEmitter) → React component
/// </summary>
public class RazorServerRenderer
{
    /// <summary>
    /// Renders a Razor template with the given props to HTML.
    /// The output includes data attributes for React hydration.
    /// </summary>
    public string RenderToHtml(string razorSource, Dictionary<string, object> props, string componentName)
    {
        var html = razorSource;

        // Remove @inherits directive
        html = Regex.Replace(html, @"@inherits\s+\w+<\w+>\s*\n?", "");

        // Process @if blocks - evaluate and include/exclude content
        html = ProcessConditionals(html, props);

        // Process @foreach blocks - expand loops
        html = ProcessLoops(html, props);

        // Replace @Props.PropertyName with actual values
        foreach (var prop in props)
        {
            var pattern = $@"@Props\.{prop.Key}";
            var value = prop.Value?.ToString() ?? "";
            html = Regex.Replace(html, pattern, value);
        }

        // Remove @onclick handlers (they'll be attached by React hydration)
        html = Regex.Replace(html, @"\s*@onclick=""[^""]*""", "");

        // Add hydration markers
        var propsJson = JsonSerializer.Serialize(props);
        var hydrationScript = $@"<script type=""application/json"" id=""__SHALIMAR_PROPS__"">{propsJson}</script>";

        // Wrap in hydration container
        var result = $@"<div id=""shalimar-root"" data-component=""{componentName}"">{html.Trim()}</div>
{hydrationScript}";

        return result;
    }

    private string ProcessConditionals(string html, Dictionary<string, object> props)
    {
        // Match @if (Props.PropertyName) { ... }
        var pattern = @"@if\s*\(Props\.(\w+)\)\s*\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}";

        return Regex.Replace(html, pattern, match =>
        {
            var propName = match.Groups[1].Value;
            var content = match.Groups[2].Value;

            if (props.TryGetValue(propName, out var value))
            {
                var isTruthy = value switch
                {
                    bool b => b,
                    string s => !string.IsNullOrEmpty(s),
                    null => false,
                    _ => true
                };

                return isTruthy ? content.Trim() : "";
            }
            return "";
        });
    }

    private string ProcessLoops(string html, Dictionary<string, object> props)
    {
        // Match @foreach (var item in Props.PropertyName) { ... }
        var pattern = @"@foreach\s*\(var\s+(\w+)\s+in\s+Props\.(\w+)\)\s*\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}";

        return Regex.Replace(html, pattern, match =>
        {
            var itemVar = match.Groups[1].Value;
            var propName = match.Groups[2].Value;
            var template = match.Groups[3].Value;

            if (props.TryGetValue(propName, out var value) && value is IEnumerable<object> items)
            {
                var results = new List<string>();
                foreach (var item in items)
                {
                    var itemHtml = template.Replace($"@{itemVar}", item?.ToString() ?? "");
                    results.Add(itemHtml.Trim());
                }
                return string.Join("\n", results);
            }
            return "";
        });
    }
}
