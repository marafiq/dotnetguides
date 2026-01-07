namespace ImpulseApp.Features.Home;

/// <summary>
/// Handler for the Home feature.
/// Returns props for server-side rendering and client hydration.
/// </summary>
public static class HomeHandler
{
    public static HomeProps Get() => new(
        Title: "Welcome to Impulse",
        Message: "Your .NET + React app is ready!",
        GeneratedAt: DateTime.UtcNow
    );
}
