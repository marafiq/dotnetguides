using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Shalimar.Razor;

/// <summary>
/// Emits React Server Components wire format from Razor.
///
/// RSC Wire Format (React 19):
/// - Line-delimited JSON
/// - Each line: id:["$","ComponentType",key,{props}]
/// - $L prefix = lazy/suspense reference
/// - $Suspense = suspense boundary
/// </summary>
public class RscEmitter
{
    private int _nextId = 0;
    private readonly StringBuilder _wireFormat = new();
    private readonly List<string> _clientComponents = new();

    public class RscOutput
    {
        public string WireFormat { get; set; } = "";
        public string[] ClientComponents { get; set; } = Array.Empty<string>();
        public string ClientManifest { get; set; } = "";
    }

    public RscOutput Emit(string razorSource, string componentName)
    {
        _nextId = 0;
        _wireFormat.Clear();
        _clientComponents.Clear();

        var isServerComponent = razorSource.Contains("@server") || !razorSource.Contains("@client");
        var isClientComponent = razorSource.Contains("@client");

        // Extract component structure
        var structure = ParseRazorStructure(razorSource);

        if (isClientComponent)
        {
            // Client components get a reference in wire format
            _clientComponents.Add(componentName);
            EmitClientReference(componentName, structure);
        }
        else
        {
            // Server components get fully rendered
            EmitServerComponent(componentName, structure);
        }

        return new RscOutput
        {
            WireFormat = _wireFormat.ToString(),
            ClientComponents = _clientComponents.ToArray(),
            ClientManifest = GenerateClientManifest()
        };
    }

    private void EmitServerComponent(string name, RazorStructure structure)
    {
        var id = _nextId++;

        // Handle suspense boundaries
        foreach (var suspense in structure.SuspenseBoundaries)
        {
            var suspenseId = _nextId++;
            var childId = _nextId++;

            // Emit suspense boundary
            _wireFormat.AppendLine($"{suspenseId}:[\"$\",\"$Suspense\",null,{{\"fallback\":{JsonSerializer.Serialize(suspense.Fallback)},\"children\":\"$L{childId}\"}}]");

            // Emit lazy child (will be streamed later)
            _wireFormat.AppendLine($"{childId}:I[\"__STREAM__{suspense.ChildComponent}\",\"{suspense.ChildComponent}\"]");
        }

        // Emit main component
        var childrenJson = structure.HasChildren
            ? JsonSerializer.Serialize(structure.ChildrenHtml)
            : "null";

        _wireFormat.AppendLine($"{id}:[\"$\",\"{name}\",null,{{\"children\":{childrenJson}}}]");
    }

    private void EmitClientReference(string name, RazorStructure structure)
    {
        var id = _nextId++;

        // Client components are referenced, not rendered server-side
        // The client bundle will handle them
        _wireFormat.AppendLine($"{id}:[\"$\",\"$L{name}\",null,{{}}]");

        // Add to client manifest
        _clientComponents.Add(name);
    }

    private string GenerateClientManifest()
    {
        var manifest = new Dictionary<string, object>();

        foreach (var component in _clientComponents)
        {
            manifest[component] = new
            {
                id = $"./{component}.tsx",
                chunks = new[] { $"{component.ToLower()}-chunk.js" },
                name = component
            };
        }

        return JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
    }

    private RazorStructure ParseRazorStructure(string source)
    {
        var structure = new RazorStructure();

        // Find Suspense boundaries
        var suspensePattern = @"<Suspense\s+fallback=""([^""]+)""\s*>([\s\S]*?)</Suspense>";
        foreach (Match match in Regex.Matches(source, suspensePattern))
        {
            structure.SuspenseBoundaries.Add(new SuspenseBoundary
            {
                Fallback = match.Groups[1].Value,
                ChildComponent = ExtractComponentName(match.Groups[2].Value)
            });
        }

        // Check for children
        structure.HasChildren = Regex.IsMatch(source, @"<\w+[^>]*>");

        // Extract HTML content (simplified)
        var html = source;
        html = Regex.Replace(html, @"@(server|client)\s*", "");
        html = Regex.Replace(html, @"@inherits[^\n]+\n?", "");
        html = Regex.Replace(html, @"@using[^\n]+\n?", "");
        html = Regex.Replace(html, @"@code\s*\{[\s\S]*?\}", "");
        structure.ChildrenHtml = html.Trim();

        return structure;
    }

    private string ExtractComponentName(string content)
    {
        var match = Regex.Match(content, @"<(\w+)");
        return match.Success ? match.Groups[1].Value : "Unknown";
    }

    private class RazorStructure
    {
        public List<SuspenseBoundary> SuspenseBoundaries { get; } = new();
        public bool HasChildren { get; set; }
        public string ChildrenHtml { get; set; } = "";
    }

    private class SuspenseBoundary
    {
        public string Fallback { get; set; } = "";
        public string ChildComponent { get; set; } = "";
    }
}
