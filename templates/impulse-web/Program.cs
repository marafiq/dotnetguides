using Impulse.Core;
using ImpulseApp.Features.Home;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddImpulse(config =>
{
    config.Version = "1.0.0";
    config.BundlePath = "/js/Shared/index.js";
});

var app = builder.Build();

app.UseStaticFiles();

// Home page - type-safe component registration
app.MapGet("/", () => HomeHandler.Get())
   .AsComponent<HomeProps>();

// Shell endpoint - serves HTML with embedded Impulse payload
app.MapFallback(async (HttpContext ctx) =>
{
    var renderer = ctx.RequestServices.GetRequiredService<ImpulseShellRenderer>();
    var config = ctx.RequestServices.GetRequiredService<ImpulseConfiguration>();

    var path = ctx.Request.Path.Value ?? "/";
    var props = HomeHandler.Get();
    var component = ComponentPathConvention.GetPath<HomeProps>();

    var payload = new ImpulsePayload
    {
        Url = path,
        Version = config.Version,
        Props = props,
        Context = new { }
    };

    var html = renderer.RenderShell(payload, "ImpulseApp");
    html = html.Replace("data-impulse='", $"data-component=\"{component}\" data-impulse='");

    ctx.Response.ContentType = "text/html";
    await ctx.Response.WriteAsync(html);
});

app.Run();
