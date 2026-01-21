namespace Shalimar.Razor;

/// <summary>
/// Specifies where a component runs
/// </summary>
public enum ComponentDirective
{
    /// <summary>
    /// Default - component type determined by usage
    /// </summary>
    Default,

    /// <summary>
    /// @server - Renders to RSC wire format on server, no React needed on client
    /// </summary>
    Server,

    /// <summary>
    /// @client - Bundled by Vite, hydrated on client
    /// </summary>
    Client
}
