using System.Text;
using System.Text.Json;

namespace Shalimar.Razor;

/// <summary>
/// Generates React Server Component (RSC) wire format for @server components.
/// RSC wire format: Line-delimited JSON like ["$","Component",key,{props}]
/// </summary>
public class RscWireGenerator
{
    private readonly ViteManifest _manifest;
    private int _nodeIndex = 0;

    public RscWireGenerator(ViteManifest manifest)
    {
        _manifest = manifest;
    }

    public RscWireGenerator() : this(new ViteManifest())
    {
    }

    /// <summary>
    /// Generate RSC wire format for a server component with data
    /// </summary>
    public string Generate(RazorComponent component, object? props = null)
    {
        _nodeIndex = 0;
        var sb = new StringBuilder();

        // Generate the component tree in RSC wire format
        var rootNode = GenerateNode(component, props);
        sb.AppendLine(rootNode);

        return sb.ToString();
    }

    /// <summary>
    /// Generate RSC wire format for multiple components (streaming)
    /// </summary>
    public IEnumerable<string> GenerateStream(RazorComponent component, object? props = null)
    {
        _nodeIndex = 0;

        // Yield the root component
        yield return GenerateNode(component, props);

        // Yield any referenced client components as placeholders
        foreach (var child in component.Children)
        {
            if (IsClientComponent(child.Name))
            {
                yield return GenerateClientReference(child);
            }
        }
    }

    private string GenerateNode(RazorComponent component, object? props)
    {
        var index = _nodeIndex++;
        var propsJson = props != null
            ? JsonSerializer.Serialize(props)
            : "{}";

        // RSC format: index:["$","ComponentType",key,{props},children...]
        // For server components, we render them directly
        if (component.Directive == ComponentDirective.Server)
        {
            return $"{index}:[\"$\",\"{component.Name}\",null,{propsJson}]";
        }

        // For client components, we include a reference to the bundled chunk
        var chunkId = _manifest.GetChunkId(component.Name) ?? component.Name;
        return $"{index}:[\"$\",\"$L{chunkId}\",null,{propsJson}]";
    }

    private string GenerateClientReference(ComponentChild child)
    {
        var index = _nodeIndex++;
        var chunkId = _manifest.GetChunkId(child.Name) ?? child.Name;
        var propsJson = JsonSerializer.Serialize(child.Props);

        // $L prefix indicates a lazy/client component reference
        return $"{index}:[\"$\",\"$L{chunkId}\",null,{propsJson}]";
    }

    private bool IsClientComponent(string name)
    {
        // In real implementation, this would check the component's directive
        // For now, check if there's a chunk for it in the manifest
        return _manifest.GetChunkId(name) != null;
    }

    /// <summary>
    /// Generate a complete RSC response with proper headers info
    /// </summary>
    public RscResponse GenerateResponse(RazorComponent component, object? props = null)
    {
        var wireFormat = Generate(component, props);

        return new RscResponse
        {
            WireFormat = wireFormat,
            ContentType = "text/x-component",
            ComponentName = component.Name,
            ChunkIds = component.Children
                .Select(c => _manifest.GetChunkId(c.Name))
                .Where(id => id != null)
                .Cast<string>()
                .ToList()
        };
    }
}

/// <summary>
/// RSC response containing wire format and metadata
/// </summary>
public class RscResponse
{
    public required string WireFormat { get; init; }
    public required string ContentType { get; init; }
    public required string ComponentName { get; init; }
    public List<string> ChunkIds { get; init; } = new();
}
