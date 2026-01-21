namespace Shalimar.Razor;

/// <summary>
/// Represents a parsed Razor component
/// </summary>
public class RazorComponent
{
    public required string Name { get; init; }
    public required string FilePath { get; init; }
    public required string FeatureFolder { get; init; }
    public ComponentDirective Directive { get; init; } = ComponentDirective.Default;
    public List<ComponentProp> Props { get; init; } = new();
    public List<string> Imports { get; init; } = new();
    public List<ComponentChild> Children { get; init; } = new();
    public required string TemplateContent { get; init; }
    public string? CodeBlock { get; init; }
}

/// <summary>
/// A component property defined in @code block or @Props directive
/// </summary>
public class ComponentProp
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public string? DefaultValue { get; init; }
    public bool IsRequired { get; init; }
}

/// <summary>
/// A child component reference found in the template
/// </summary>
public class ComponentChild
{
    public required string Name { get; init; }
    public required string TagName { get; init; }
    public Dictionary<string, string> Props { get; init; } = new();
    public bool IsSelfClosing { get; init; }
}
