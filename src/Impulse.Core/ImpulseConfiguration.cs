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
    /// The path to the main JavaScript bundle (production).
    /// Used when not in development mode.
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
    public List<string> AdditionalScripts { get; set; } = [];

    /// <summary>
    /// Additional stylesheets to include in the shell.
    /// </summary>
    public List<string> Stylesheets { get; set; } = [];

    // ============================================================================
    // Development Mode (Vite HMR)
    // ============================================================================

    /// <summary>
    /// Whether to use Vite development server for HMR.
    /// Set automatically based on ASPNETCORE_ENVIRONMENT.
    /// </summary>
    public bool UseDevelopmentServer { get; set; }

    /// <summary>
    /// The Vite development server URL for HMR.
    /// Default: http://localhost:5173
    /// </summary>
    public string ViteDevServerUrl { get; set; } = "http://localhost:5173";

    /// <summary>
    /// Entry point name in vite.config.ts.
    /// Used to construct the dev server script URL.
    /// </summary>
    public string EntryPoint { get; set; } = "impulse";

    // ============================================================================
    // Production Mode (Hashed Assets)
    // ============================================================================

    /// <summary>
    /// Path to Vite manifest.json for resolving hashed asset paths.
    /// Relative to wwwroot.
    /// </summary>
    public string ManifestPath { get; set; } = "js/.vite/manifest.json";

    /// <summary>
    /// Base path for static assets (usually /js in production).
    /// </summary>
    public string AssetBasePath { get; set; } = "/js";
}
