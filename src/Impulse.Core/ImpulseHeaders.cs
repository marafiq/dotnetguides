namespace Impulse.Core;

/// <summary>
/// HTTP headers used by Impulse for navigation requests.
/// </summary>
public static class ImpulseHeaders
{
    /// <summary>
    /// Request header indicating this is an Impulse navigation request.
    /// </summary>
    public const string Impulse = "X-Impulse";

    /// <summary>
    /// Request header containing the client's bundle version.
    /// </summary>
    public const string Version = "X-Impulse-Version";

    /// <summary>
    /// Response header indicating the client should perform a full page reload.
    /// </summary>
    public const string Reload = "X-Impulse-Reload";
}
