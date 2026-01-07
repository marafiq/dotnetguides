using System.Text.Json;
using System.Text.Json.Serialization;

namespace Impulse.Core;

/// <summary>
/// Reads and resolves Vite manifest.json for production asset hashing.
/// </summary>
public sealed class ViteManifest
{
    private readonly Dictionary<string, ManifestEntry> _entries = [];
    private readonly string _basePath;
    private bool _loaded;

    public ViteManifest(string basePath = "/js")
    {
        _basePath = basePath.TrimEnd('/');
    }

    /// <summary>
    /// Loads the manifest from the specified file path.
    /// </summary>
    public void Load(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            _loaded = false;
            return;
        }

        try
        {
            var json = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<Dictionary<string, ManifestEntry>>(json);

            if (manifest is not null)
            {
                _entries.Clear();
                foreach (var (key, value) in manifest)
                {
                    _entries[key] = value;
                }
                _loaded = true;
            }
        }
        catch
        {
            _loaded = false;
        }
    }

    /// <summary>
    /// Gets whether the manifest was successfully loaded.
    /// </summary>
    public bool IsLoaded => _loaded;

    /// <summary>
    /// Resolves an entry point name to its hashed file path.
    /// </summary>
    /// <param name="entryPoint">The entry point name (e.g., "impulse")</param>
    /// <returns>The resolved path with hash, or null if not found</returns>
    public string? ResolveEntry(string entryPoint)
    {
        // Try direct lookup first
        if (_entries.TryGetValue(entryPoint, out var entry))
        {
            return $"{_basePath}/{entry.File}";
        }

        // Try with .ts extension (Vite uses source paths as keys)
        var tsKey = $"Features/Shared/index.ts";
        if (_entries.TryGetValue(tsKey, out entry))
        {
            return $"{_basePath}/{entry.File}";
        }

        // Try finding by entry point name in isEntry entries
        foreach (var (key, value) in _entries)
        {
            if (value.IsEntry && key.Contains(entryPoint, StringComparison.OrdinalIgnoreCase))
            {
                return $"{_basePath}/{value.File}";
            }
        }

        return null;
    }

    /// <summary>
    /// Gets CSS files associated with an entry point.
    /// </summary>
    public IEnumerable<string> GetCssFiles(string entryPoint)
    {
        // Try with .ts extension
        var tsKey = $"Features/Shared/index.ts";
        if (_entries.TryGetValue(tsKey, out var entry) && entry.Css is not null)
        {
            return entry.Css.Select(c => $"{_basePath}/{c}");
        }

        return [];
    }
}

/// <summary>
/// A single entry in the Vite manifest.
/// </summary>
public sealed class ManifestEntry
{
    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("src")]
    public string? Src { get; set; }

    [JsonPropertyName("isEntry")]
    public bool IsEntry { get; set; }

    [JsonPropertyName("css")]
    public List<string>? Css { get; set; }

    [JsonPropertyName("imports")]
    public List<string>? Imports { get; set; }
}
