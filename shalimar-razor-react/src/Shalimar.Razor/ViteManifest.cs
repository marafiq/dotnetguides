using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shalimar.Razor;

/// <summary>
/// Reads and parses Vite's manifest.json to get component chunk IDs
/// </summary>
public class ViteManifest
{
    private readonly Dictionary<string, ViteChunk> _chunks = new();

    public IReadOnlyDictionary<string, ViteChunk> Chunks => _chunks;

    /// <summary>
    /// Load manifest from file path
    /// </summary>
    public static ViteManifest Load(string manifestPath)
    {
        var manifest = new ViteManifest();

        if (!File.Exists(manifestPath))
        {
            return manifest;
        }

        var json = File.ReadAllText(manifestPath);
        var chunks = JsonSerializer.Deserialize<Dictionary<string, ViteChunk>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (chunks != null)
        {
            foreach (var (key, chunk) in chunks)
            {
                manifest._chunks[key] = chunk;
            }
        }

        return manifest;
    }

    /// <summary>
    /// Get the chunk ID for a component by name
    /// </summary>
    public string? GetChunkId(string componentName)
    {
        // Look for the component in the manifest
        // Vite uses the file path as key, e.g., "Features/Users/UserProfile.tsx"
        var matchingKey = _chunks.Keys.FirstOrDefault(k =>
            k.EndsWith($"{componentName}.tsx", StringComparison.OrdinalIgnoreCase) ||
            k.EndsWith($"{componentName}.js", StringComparison.OrdinalIgnoreCase));

        if (matchingKey != null && _chunks.TryGetValue(matchingKey, out var chunk))
        {
            return chunk.File;
        }

        return null;
    }

    /// <summary>
    /// Get all component mappings (name -> chunk file)
    /// </summary>
    public Dictionary<string, string> GetComponentMap()
    {
        var map = new Dictionary<string, string>();

        foreach (var (key, chunk) in _chunks)
        {
            // Extract component name from path
            var fileName = Path.GetFileNameWithoutExtension(key);
            if (!string.IsNullOrEmpty(fileName) && char.IsUpper(fileName[0]))
            {
                map[fileName] = chunk.File;
            }
        }

        return map;
    }
}

/// <summary>
/// Represents a chunk entry in Vite's manifest.json
/// </summary>
public class ViteChunk
{
    [JsonPropertyName("file")]
    public string File { get; set; } = "";

    [JsonPropertyName("src")]
    public string? Src { get; set; }

    [JsonPropertyName("isEntry")]
    public bool IsEntry { get; set; }

    [JsonPropertyName("isDynamicEntry")]
    public bool IsDynamicEntry { get; set; }

    [JsonPropertyName("imports")]
    public List<string>? Imports { get; set; }

    [JsonPropertyName("dynamicImports")]
    public List<string>? DynamicImports { get; set; }

    [JsonPropertyName("css")]
    public List<string>? Css { get; set; }

    [JsonPropertyName("assets")]
    public List<string>? Assets { get; set; }
}
