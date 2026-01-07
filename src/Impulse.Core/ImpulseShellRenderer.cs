using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Impulse.Core;

/// <summary>
/// Renders the Impulse shell HTML with embedded payload.
/// Supports both development (Vite HMR) and production (hashed assets) modes.
/// </summary>
public sealed class ImpulseShellRenderer
{
    private readonly ImpulseConfiguration _config;
    private readonly ViteManifest _manifest;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _resolvedBundlePath;

    public ImpulseShellRenderer(ImpulseConfiguration config, ViteManifest manifest)
    {
        _config = config;
        _manifest = manifest;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    /// <summary>
    /// Gets the resolved bundle path (from manifest in production, or dev server URL).
    /// </summary>
    public string GetBundlePath()
    {
        if (_resolvedBundlePath is not null)
            return _resolvedBundlePath;

        if (_config.UseDevelopmentServer)
        {
            // Vite dev server URL with entry point
            _resolvedBundlePath = $"{_config.ViteDevServerUrl.TrimEnd('/')}/Features/Shared/index.ts";
        }
        else if (_manifest.IsLoaded)
        {
            // Resolve from manifest (hashed path)
            _resolvedBundlePath = _manifest.ResolveEntry(_config.EntryPoint) ?? _config.BundlePath;
        }
        else
        {
            // Fallback to configured path
            _resolvedBundlePath = _config.BundlePath;
        }

        return _resolvedBundlePath;
    }

    /// <summary>
    /// Renders the full shell HTML page.
    /// </summary>
    public string RenderShell(ImpulsePayload payload, string? title = null)
    {
        var payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);
        var escapedPayload = HtmlEncode(payloadJson);

        var stylesheets = RenderStylesheets();
        var viteClient = RenderViteClient();
        var additionalScripts = RenderAdditionalScripts();
        var mainScript = RenderMainScript();

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>{{HtmlEncode(title ?? "Impulse App")}}</title>
                {{stylesheets}}
            </head>
            <body>
                <div id="{{HtmlEncode(_config.RootElementId)}}" data-impulse='{{escapedPayload}}'></div>
                {{viteClient}}{{additionalScripts}}
                {{mainScript}}
            </body>
            </html>
            """;
    }

    /// <summary>
    /// Renders just the root element with payload (for partial responses).
    /// </summary>
    public string RenderRootElement(ImpulsePayload payload)
    {
        var payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);
        var escapedPayload = HtmlEncode(payloadJson);

        return $"<div id=\"{HtmlEncode(_config.RootElementId)}\" data-impulse='{escapedPayload}'></div>";
    }

    private string RenderStylesheets()
    {
        var sheets = new List<string>(_config.Stylesheets);

        // Add CSS from manifest in production
        if (!_config.UseDevelopmentServer && _manifest.IsLoaded)
        {
            sheets.AddRange(_manifest.GetCssFiles(_config.EntryPoint));
        }

        if (sheets.Count == 0)
            return string.Empty;

        return string.Join("\n    ",
            sheets.Select(s => $"<link rel=\"stylesheet\" href=\"{HtmlEncode(s)}\">"));
    }

    private string RenderViteClient()
    {
        if (!_config.UseDevelopmentServer)
            return string.Empty;

        // Vite HMR client script for hot module replacement
        var viteUrl = _config.ViteDevServerUrl.TrimEnd('/');
        return $"""

                <script type="module" src="{HtmlEncode(viteUrl)}/@vite/client"></script>
        """;
    }

    private string RenderAdditionalScripts()
    {
        if (_config.AdditionalScripts.Count == 0)
            return string.Empty;

        return "\n    " + string.Join("\n    ",
            _config.AdditionalScripts.Select(s => $"<script src=\"{HtmlEncode(s)}\"></script>"));
    }

    private string RenderMainScript()
    {
        var bundlePath = GetBundlePath();
        var scriptType = _config.UseModuleScript ? " type=\"module\"" : "";

        return $"<script{scriptType} src=\"{HtmlEncode(bundlePath)}\"></script>";
    }

    private static string HtmlEncode(string value)
    {
        return System.Net.WebUtility.HtmlEncode(value);
    }
}
