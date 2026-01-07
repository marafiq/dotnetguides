using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Impulse.Core;

/// <summary>
/// Renders the Impulse shell HTML with embedded payload.
/// </summary>
public sealed class ImpulseShellRenderer
{
    private readonly ImpulseConfiguration _config;
    private readonly JsonSerializerOptions _jsonOptions;

    public ImpulseShellRenderer(ImpulseConfiguration config)
    {
        _config = config;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    /// <summary>
    /// Renders the full shell HTML page.
    /// </summary>
    public string RenderShell(ImpulsePayload payload, string? title = null)
    {
        var payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);
        var escapedPayload = HtmlEncode(payloadJson);

        var stylesheets = string.Join("\n    ",
            _config.Stylesheets.Select(s => $"<link rel=\"stylesheet\" href=\"{HtmlEncode(s)}\">"));

        var additionalScripts = string.Join("\n    ",
            _config.AdditionalScripts.Select(s => $"<script src=\"{HtmlEncode(s)}\"></script>"));

        var scriptType = _config.UseModuleScript ? " type=\"module\"" : "";

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
                {{additionalScripts}}
                <script{{scriptType}} src="{{HtmlEncode(_config.BundlePath)}}"></script>
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

    private static string HtmlEncode(string value)
    {
        return System.Net.WebUtility.HtmlEncode(value);
    }
}
