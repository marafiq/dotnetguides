using Impulse.Core;

namespace Impulse.Core.Tests;

/// <summary>
/// TDD Level 1: Basic component rendering tests.
/// Tests shell HTML generation, payload serialization, and Vite integration.
/// </summary>
public static class ImpulseShellRendererTests
{
    public static void RunAll()
    {
        TestRunner.Group("ImpulseShellRenderer - Basic Rendering", () =>
        {
            TestRunner.Run("RenderShell produces valid HTML5 document",
                RenderShell_ProducesValidHtml5Document);

            TestRunner.Run("RenderShell includes payload in data-impulse attribute",
                RenderShell_IncludesPayloadInDataAttribute);

            TestRunner.Run("RenderShell uses configured root element ID",
                RenderShell_UsesConfiguredRootElementId);

            TestRunner.Run("RenderShell sets page title",
                RenderShell_SetsPageTitle);

            TestRunner.Run("Payload JSON uses camelCase property names",
                PayloadJson_UsesCamelCase);

            TestRunner.Run("RenderShell HTML-encodes payload to prevent XSS",
                RenderShell_HtmlEncodesPayload);
        });

        TestRunner.Group("ImpulseShellRenderer - Development Mode (Vite HMR)", () =>
        {
            TestRunner.Run("Dev mode includes Vite client script",
                DevMode_IncludesViteClientScript);

            TestRunner.Run("Dev mode uses Vite dev server URL for bundle",
                DevMode_UsesViteDevServerUrl);

            TestRunner.Run("Dev mode sets script type to module",
                DevMode_SetsScriptTypeModule);
        });

        TestRunner.Group("ImpulseShellRenderer - Production Mode", () =>
        {
            TestRunner.Run("Prod mode does not include Vite client",
                ProdMode_NoViteClient);

            TestRunner.Run("Prod mode resolves hashed bundle from manifest",
                ProdMode_ResolvesHashedBundle);

            TestRunner.Run("Prod mode includes CSS from manifest",
                ProdMode_IncludesCssFromManifest);
        });

        TestRunner.Group("ImpulsePayload - Serialization", () =>
        {
            TestRunner.Run("Payload serializes props correctly",
                Payload_SerializesProps);

            TestRunner.Run("Payload serializes context correctly",
                Payload_SerializesContext);

            TestRunner.Run("Payload includes deferred URLs when present",
                Payload_IncludesDeferredUrls);

            TestRunner.Run("Payload includes lazy URLs when present",
                Payload_IncludesLazyUrls);

            TestRunner.Run("Payload omits null deferred/lazy",
                Payload_OmitsNullDeferredLazy);
        });
    }

    // ═══════════════════════════════════════════════════════
    // Basic Rendering Tests
    // ═══════════════════════════════════════════════════════

    private static void RenderShell_ProducesValidHtml5Document()
    {
        var (renderer, _) = CreateRenderer();
        var payload = CreateTestPayload();

        var html = renderer.RenderShell(payload);

        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Contains("<html lang=\"en\">", html);
        Assert.Contains("<head>", html);
        Assert.Contains("<body>", html);
        Assert.Contains("</html>", html);
    }

    private static void RenderShell_IncludesPayloadInDataAttribute()
    {
        var (renderer, _) = CreateRenderer();
        var payload = CreateTestPayload();

        var html = renderer.RenderShell(payload);

        Assert.Contains("data-impulse='", html);
        // JSON is HTML-encoded in the attribute
        Assert.Contains("&quot;url&quot;:&quot;/dashboard&quot;", html);
        Assert.Contains("&quot;version&quot;:&quot;1.0.0&quot;", html);
    }

    private static void RenderShell_UsesConfiguredRootElementId()
    {
        var config = new ImpulseConfiguration { RootElementId = "my-custom-root" };
        var (renderer, _) = CreateRenderer(config);
        var payload = CreateTestPayload();

        var html = renderer.RenderShell(payload);

        Assert.Contains("id=\"my-custom-root\"", html);
    }

    private static void RenderShell_SetsPageTitle()
    {
        var (renderer, _) = CreateRenderer();
        var payload = CreateTestPayload();

        var html = renderer.RenderShell(payload, "My Dashboard");

        Assert.Contains("<title>My Dashboard</title>", html);
    }

    private static void PayloadJson_UsesCamelCase()
    {
        var (renderer, _) = CreateRenderer();
        var payload = new ImpulsePayload
        {
            Url = "/test",
            Version = "1.0.0",
            Props = new { UserName = "John", UserId = 123 },
            Context = new { CurrentUser = "admin" }
        };

        var html = renderer.RenderShell(payload);

        // Should be camelCase, not PascalCase (HTML-encoded in attribute)
        Assert.Contains("&quot;userName&quot;", html);
        Assert.Contains("&quot;userId&quot;", html);
        Assert.Contains("&quot;currentUser&quot;", html);
    }

    private static void RenderShell_HtmlEncodesPayload()
    {
        var (renderer, _) = CreateRenderer();
        var payload = new ImpulsePayload
        {
            Url = "/test",
            Version = "1.0.0",
            Props = new { Message = "<script>alert('xss')</script>" },
            Context = new { }
        };

        var html = renderer.RenderShell(payload);

        // Should NOT contain raw script tags in the data attribute
        Assert.False(html.Contains("<script>alert"), "Payload should be HTML encoded");
    }

    // ═══════════════════════════════════════════════════════
    // Development Mode Tests
    // ═══════════════════════════════════════════════════════

    private static void DevMode_IncludesViteClientScript()
    {
        var config = new ImpulseConfiguration
        {
            UseDevelopmentServer = true,
            ViteDevServerUrl = "http://localhost:5173"
        };
        var (renderer, _) = CreateRenderer(config);
        var payload = CreateTestPayload();

        var html = renderer.RenderShell(payload);

        Assert.Contains("/@vite/client", html);
        Assert.Contains("http://localhost:5173/@vite/client", html);
    }

    private static void DevMode_UsesViteDevServerUrl()
    {
        var config = new ImpulseConfiguration
        {
            UseDevelopmentServer = true,
            ViteDevServerUrl = "http://localhost:5173"
        };
        var (renderer, _) = CreateRenderer(config);

        var bundlePath = renderer.GetBundlePath();

        Assert.StartsWith("http://localhost:5173", bundlePath);
        Assert.Contains("/Features/Shared/index.ts", bundlePath);
    }

    private static void DevMode_SetsScriptTypeModule()
    {
        var config = new ImpulseConfiguration
        {
            UseDevelopmentServer = true,
            UseModuleScript = true
        };
        var (renderer, _) = CreateRenderer(config);
        var payload = CreateTestPayload();

        var html = renderer.RenderShell(payload);

        Assert.Contains("type=\"module\"", html);
    }

    // ═══════════════════════════════════════════════════════
    // Production Mode Tests
    // ═══════════════════════════════════════════════════════

    private static void ProdMode_NoViteClient()
    {
        var config = new ImpulseConfiguration { UseDevelopmentServer = false };
        var (renderer, _) = CreateRenderer(config);
        var payload = CreateTestPayload();

        var html = renderer.RenderShell(payload);

        Assert.False(html.Contains("@vite/client"), "Prod mode should not include Vite client");
    }

    private static void ProdMode_ResolvesHashedBundle()
    {
        var config = new ImpulseConfiguration
        {
            UseDevelopmentServer = false,
            EntryPoint = "Features/Shared/index.ts"
        };
        var manifest = new ViteManifest("");  // Empty basePath for testing
        manifest.LoadFromJson("""
        {
            "Features/Shared/index.ts": {
                "file": "assets/impulse-abc123.js",
                "isEntry": true
            }
        }
        """);
        var renderer = new ImpulseShellRenderer(config, manifest);

        var bundlePath = renderer.GetBundlePath();

        Assert.Equal("/assets/impulse-abc123.js", bundlePath);
    }

    private static void ProdMode_IncludesCssFromManifest()
    {
        var config = new ImpulseConfiguration
        {
            UseDevelopmentServer = false,
            EntryPoint = "Features/Shared/index.ts"
        };
        var manifest = new ViteManifest("");  // Empty basePath for testing
        manifest.LoadFromJson("""
        {
            "Features/Shared/index.ts": {
                "file": "assets/impulse-abc123.js",
                "css": ["assets/style-def456.css"],
                "isEntry": true
            }
        }
        """);
        var renderer = new ImpulseShellRenderer(config, manifest);
        var payload = CreateTestPayload();

        var html = renderer.RenderShell(payload);

        Assert.Contains("/assets/style-def456.css", html);
        Assert.Contains("<link rel=\"stylesheet\"", html);
    }

    // ═══════════════════════════════════════════════════════
    // Payload Serialization Tests
    // ═══════════════════════════════════════════════════════

    private static void Payload_SerializesProps()
    {
        var (renderer, _) = CreateRenderer();
        var payload = new ImpulsePayload
        {
            Url = "/dashboard",
            Version = "1.0.0",
            Props = new { Title = "Welcome", Count = 42 },
            Context = new { }
        };

        var html = renderer.RenderShell(payload);

        // HTML-encoded JSON in attribute
        Assert.Contains("&quot;title&quot;:&quot;Welcome&quot;", html);
        Assert.Contains("&quot;count&quot;:42", html);
    }

    private static void Payload_SerializesContext()
    {
        var (renderer, _) = CreateRenderer();
        var payload = new ImpulsePayload
        {
            Url = "/dashboard",
            Version = "1.0.0",
            Props = new { },
            Context = new { UserId = "user-123", Theme = "dark" }
        };

        var html = renderer.RenderShell(payload);

        // HTML-encoded JSON in attribute
        Assert.Contains("&quot;userId&quot;:&quot;user-123&quot;", html);
        Assert.Contains("&quot;theme&quot;:&quot;dark&quot;", html);
    }

    private static void Payload_IncludesDeferredUrls()
    {
        var (renderer, _) = CreateRenderer();
        var payload = new ImpulsePayload
        {
            Url = "/dashboard",
            Version = "1.0.0",
            Props = new { },
            Context = new { },
            Deferred = new Dictionary<string, string>
            {
                ["stats"] = "/api/dashboard/stats",
                ["notifications"] = "/api/notifications"
            }
        };

        var html = renderer.RenderShell(payload);

        // HTML-encoded JSON in attribute
        Assert.Contains("&quot;deferred&quot;", html);
        Assert.Contains("&quot;stats&quot;:&quot;/api/dashboard/stats&quot;", html);
        Assert.Contains("&quot;notifications&quot;:&quot;/api/notifications&quot;", html);
    }

    private static void Payload_IncludesLazyUrls()
    {
        var (renderer, _) = CreateRenderer();
        var payload = new ImpulsePayload
        {
            Url = "/residents",
            Version = "1.0.0",
            Props = new { },
            Context = new { },
            Lazy = new Dictionary<string, string>
            {
                ["details"] = "/api/residents/{id}"
            }
        };

        var html = renderer.RenderShell(payload);

        // HTML-encoded JSON in attribute
        Assert.Contains("&quot;lazy&quot;", html);
        Assert.Contains("&quot;details&quot;:&quot;/api/residents/{id}&quot;", html);
    }

    private static void Payload_OmitsNullDeferredLazy()
    {
        var (renderer, _) = CreateRenderer();
        var payload = new ImpulsePayload
        {
            Url = "/simple",
            Version = "1.0.0",
            Props = new { Title = "Simple" },
            Context = new { },
            Deferred = null,
            Lazy = null
        };

        var html = renderer.RenderShell(payload);

        // HTML-encoded version of the keys
        Assert.False(html.Contains("&quot;deferred&quot;"), "Null deferred should be omitted");
        Assert.False(html.Contains("&quot;lazy&quot;"), "Null lazy should be omitted");
    }

    // ═══════════════════════════════════════════════════════
    // Test Helpers
    // ═══════════════════════════════════════════════════════

    private static (ImpulseShellRenderer renderer, ImpulseConfiguration config) CreateRenderer(
        ImpulseConfiguration? config = null)
    {
        config ??= new ImpulseConfiguration();
        var manifest = new ViteManifest();
        var renderer = new ImpulseShellRenderer(config, manifest);
        return (renderer, config);
    }

    private static ImpulsePayload CreateTestPayload()
    {
        return new ImpulsePayload
        {
            Url = "/dashboard",
            Version = "1.0.0",
            Props = new { Title = "Dashboard" },
            Context = new { UserId = "test-user" }
        };
    }
}
