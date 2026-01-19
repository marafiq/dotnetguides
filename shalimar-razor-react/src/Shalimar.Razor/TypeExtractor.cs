using System.Text.RegularExpressions;

namespace Shalimar.Razor;

/// <summary>
/// Extracts type information from Razor source for TypeScript interface generation.
///
/// Analyzes:
/// - @inherits SliceComponent<TProps> → Extract TProps
/// - @Props.X usages → Infer property types
/// - Event handler usages → Generate handler types
/// </summary>
public class TypeExtractor
{
    /// <summary>
    /// Extract component metadata from Razor source.
    /// </summary>
    public ComponentMetadata Extract(string razorSource, string fileName)
    {
        var metadata = new ComponentMetadata
        {
            ComponentName = Path.GetFileNameWithoutExtension(fileName),
            FileName = fileName
        };

        // Extract props type from @inherits
        var inheritsMatch = Regex.Match(razorSource, @"@inherits\s+SliceComponent<(\w+)>");
        if (inheritsMatch.Success)
        {
            metadata.PropsTypeName = inheritsMatch.Groups[1].Value;
        }

        // Extract prop usages (@Props.X)
        var propMatches = Regex.Matches(razorSource, @"@Props\.(\w+)");
        foreach (Match match in propMatches)
        {
            var propName = match.Groups[1].Value;
            if (!metadata.PropUsages.Contains(propName))
            {
                metadata.PropUsages.Add(propName);
            }
        }

        // Extract event handlers (@onclick, @onchange, etc.)
        var eventMatches = Regex.Matches(razorSource, @"@on(\w+)=""@(\w+)""");
        foreach (Match match in eventMatches)
        {
            var eventType = match.Groups[1].Value;
            var handlerName = match.Groups[2].Value;
            metadata.EventHandlers[handlerName] = eventType;
        }

        // Extract child components (PascalCase tags)
        var componentMatches = Regex.Matches(razorSource, @"<([A-Z]\w+)[\s/>]");
        foreach (Match match in componentMatches)
        {
            var componentName = match.Groups[1].Value;
            if (!metadata.ChildComponents.Contains(componentName))
            {
                metadata.ChildComponents.Add(componentName);
            }
        }

        // Infer prop types from usage patterns
        InferPropTypes(razorSource, metadata);

        return metadata;
    }

    private void InferPropTypes(string source, ComponentMetadata metadata)
    {
        foreach (var prop in metadata.PropUsages)
        {
            var type = InferTypeFromUsage(source, prop);
            metadata.InferredPropTypes[prop] = type;
        }
    }

    private string InferTypeFromUsage(string source, string propName)
    {
        var pattern = $@"Props\.{propName}";

        // Check if used in foreach (likely array)
        if (Regex.IsMatch(source, $@"@foreach\s*\([^)]*\s+in\s+Props\.{propName}\)"))
        {
            return "any[]";
        }

        // Check if used in if condition (likely boolean)
        if (Regex.IsMatch(source, $@"@if\s*\(\s*Props\.{propName}\s*\)"))
        {
            return "boolean";
        }

        // Check if used with .Count or .Length (likely array)
        if (Regex.IsMatch(source, $@"Props\.{propName}\.(Count|Length)"))
        {
            return "any[]";
        }

        // Check if used with .Any() (likely array)
        if (Regex.IsMatch(source, $@"Props\.{propName}\.Any\s*\("))
        {
            return "any[]";
        }

        // Default to string
        return "string";
    }

    /// <summary>
    /// Generate TypeScript interface from metadata.
    /// </summary>
    public string GenerateInterface(ComponentMetadata metadata)
    {
        if (string.IsNullOrEmpty(metadata.PropsTypeName))
        {
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder();

        // Props interface
        sb.AppendLine($"export interface {metadata.PropsTypeName} {{");
        foreach (var prop in metadata.PropUsages)
        {
            var type = metadata.InferredPropTypes.GetValueOrDefault(prop, "any");
            sb.AppendLine($"  {prop}: {type};");
        }
        sb.AppendLine("}");

        // Handlers interface if we have event handlers
        if (metadata.EventHandlers.Any())
        {
            sb.AppendLine();
            sb.AppendLine($"export interface {metadata.ComponentName}Handlers {{");
            foreach (var handler in metadata.EventHandlers)
            {
                var eventType = GetReactEventType(handler.Value);
                sb.AppendLine($"  {handler.Key}: {eventType};");
            }
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private string GetReactEventType(string blazorEventType)
    {
        return blazorEventType.ToLower() switch
        {
            "click" => "(event: React.MouseEvent) => void",
            "change" => "(event: React.ChangeEvent) => void",
            "input" => "(event: React.FormEvent) => void",
            "submit" => "(event: React.FormEvent) => void",
            "keydown" => "(event: React.KeyboardEvent) => void",
            "keyup" => "(event: React.KeyboardEvent) => void",
            "focus" => "(event: React.FocusEvent) => void",
            "blur" => "(event: React.FocusEvent) => void",
            "mouseenter" => "(event: React.MouseEvent) => void",
            "mouseleave" => "(event: React.MouseEvent) => void",
            _ => "() => void"
        };
    }
}

/// <summary>
/// Metadata extracted from a Razor component.
/// </summary>
public class ComponentMetadata
{
    public string ComponentName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? PropsTypeName { get; set; }
    public List<string> PropUsages { get; } = new();
    public Dictionary<string, string> EventHandlers { get; } = new();
    public List<string> ChildComponents { get; } = new();
    public Dictionary<string, string> InferredPropTypes { get; } = new();
}
