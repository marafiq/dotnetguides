using Impulse.Core;

namespace Impulse.Core.Tests;

public class ImpulseShellRendererTests
{
    [Fact]
    public void RenderShell_RendersValidHtml()
    {
        var config = new ImpulseConfiguration
        {
            Version = "1.0.0",
            BundlePath = "/assets/app.js",
            RootElementId = "app"
        };
        var renderer = new ImpulseShellRenderer(config);

        var payload = new ImpulsePayload
        {
            Url = "/test",
            Version = "1.0.0",
            Props = new { Id = 1, Name = "Test" },
            Context = new { User = "TestUser" }
        };

        var html = renderer.RenderShell(payload, "Test Page");

        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<title>Test Page</title>", html);
        Assert.Contains("id=\"app\"", html);
        Assert.Contains("data-impulse=", html);
        Assert.Contains("/assets/app.js", html);
    }

    [Fact]
    public void RenderShell_IncludesPayloadData()
    {
        var config = new ImpulseConfiguration
        {
            Version = "1.0.0",
            BundlePath = "/assets/app.js",
            RootElementId = "app"
        };
        var renderer = new ImpulseShellRenderer(config);

        var payload = new ImpulsePayload
        {
            Url = "/residents/1",
            Version = "abc123",
            Props = new { Id = 1, Name = "Margaret Chen" },
            Context = new { UserId = 42 }
        };

        var html = renderer.RenderShell(payload);

        Assert.Contains("\"url\":\"/residents/1\"", html);
        Assert.Contains("\"version\":\"abc123\"", html);
        Assert.Contains("Margaret Chen", html);
    }

    [Fact]
    public void RenderShell_IncludesDeferredAndLazy()
    {
        var config = new ImpulseConfiguration
        {
            Version = "1.0.0",
            BundlePath = "/assets/app.js"
        };
        var renderer = new ImpulseShellRenderer(config);

        var payload = new ImpulsePayload
        {
            Url = "/residents/1",
            Version = "1.0.0",
            Props = new { Id = 1 },
            Context = new { },
            Deferred = new Dictionary<string, string>
            {
                ["medications"] = "/residents/1/medications"
            },
            Lazy = new Dictionary<string, string>
            {
                ["documents"] = "/residents/1/documents"
            }
        };

        var html = renderer.RenderShell(payload);

        Assert.Contains("medications", html);
        Assert.Contains("documents", html);
    }

    [Fact]
    public void RenderShell_UsesModuleScript_WhenConfigured()
    {
        var config = new ImpulseConfiguration
        {
            Version = "1.0.0",
            BundlePath = "/assets/app.js",
            UseModuleScript = true
        };
        var renderer = new ImpulseShellRenderer(config);

        var payload = new ImpulsePayload
        {
            Url = "/",
            Version = "1.0.0",
            Props = new { },
            Context = new { }
        };

        var html = renderer.RenderShell(payload);

        Assert.Contains("type=\"module\"", html);
    }

    [Fact]
    public void RenderRootElement_RendersOnlyRootDiv()
    {
        var config = new ImpulseConfiguration
        {
            RootElementId = "root"
        };
        var renderer = new ImpulseShellRenderer(config);

        var payload = new ImpulsePayload
        {
            Url = "/",
            Version = "1.0.0",
            Props = new { },
            Context = new { }
        };

        var html = renderer.RenderRootElement(payload);

        Assert.StartsWith("<div", html);
        Assert.Contains("id=\"root\"", html);
        Assert.Contains("data-impulse=", html);
        Assert.DoesNotContain("<!DOCTYPE", html);
        Assert.DoesNotContain("<script", html);
    }
}
