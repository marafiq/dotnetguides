namespace Impulse.Core;

/// <summary>
/// Configuration options for Impulse.
/// </summary>
public sealed class ImpulseConfiguration
{
    /// <summary>
    /// The current bundle version/hash for mismatch detection.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// The path to the main JavaScript bundle.
    /// </summary>
    public string BundlePath { get; set; } = "/assets/app.js";

    /// <summary>
    /// The ID of the root element where the app mounts.
    /// </summary>
    public string RootElementId { get; set; } = "app";

    /// <summary>
    /// Whether to use module scripts.
    /// </summary>
    public bool UseModuleScript { get; set; } = true;

    /// <summary>
    /// Additional scripts to include in the shell.
    /// </summary>
    public List<string> AdditionalScripts { get; set; } = new();

    /// <summary>
    /// Additional stylesheets to include in the shell.
    /// </summary>
    public List<string> Stylesheets { get; set; } = new();
}
