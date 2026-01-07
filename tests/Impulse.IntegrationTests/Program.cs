using Impulse.Core;
using Impulse.IntegrationTests.Features.Dashboard;
using Impulse.IntegrationTests.Features.Residents;

using DashboardHandler = Impulse.IntegrationTests.Features.Dashboard.Handler;
using ResidentsHandler = Impulse.IntegrationTests.Features.Residents.Handler;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddImpulse(config =>
{
    config.Version = "1.0.0";
    config.BundlePath = "/js/Shared/index.js";
});

var app = builder.Build();

app.UseStaticFiles();

// Dashboard - type-safe component path derived from DashboardProps
app.MapGet("/", () => DashboardHandler.Get())
   .AsComponent<DashboardProps>();

// Residents List - type-safe component path derived from ResidentsListProps
app.MapGet("/residents", () => ResidentsHandler.GetList())
   .AsComponent<ResidentsListProps>();

// Resident Detail with deferred medications
app.MapGet("/residents/{id:int}", (int id) =>
{
    var resident = ResidentsHandler.GetById(id);
    return resident is not null ? Results.Ok(resident) : Results.NotFound();
})
.AsComponent<ResidentDetailProps>()
.Deferred<MedicationsProps>("medications", "/residents/{id}/medications");

// Medications endpoint
app.MapGet("/residents/{id:int}/medications", (int id) =>
    ResidentsHandler.GetMedications(id));

// Shell endpoint - serves the HTML with embedded payload
app.MapFallback(async (HttpContext ctx) =>
{
    var renderer = ctx.RequestServices.GetRequiredService<ImpulseShellRenderer>();
    var config = ctx.RequestServices.GetRequiredService<ImpulseConfiguration>();

    // Determine component based on path - derive from Props type convention
    var path = ctx.Request.Path.Value ?? "/";
    var (props, component) = path switch
    {
        "/" => ((object)DashboardHandler.Get(), ComponentPathConvention.GetPath<DashboardProps>()),
        "/residents" => (ResidentsHandler.GetList(), ComponentPathConvention.GetPath<ResidentsListProps>()),
        var p when p.StartsWith("/residents/") && int.TryParse(p.Split('/').Last(), out var id)
            => (ResidentsHandler.GetById(id) ?? (object)new { }, ComponentPathConvention.GetPath<ResidentDetailProps>()),
        _ => (new { }, ComponentPathConvention.GetPath<DashboardProps>())
    };

    var payload = new ImpulsePayload
    {
        Url = path,
        Version = config.Version,
        Props = props,
        Context = new { },
        Deferred = path.StartsWith("/residents/") && path.Split('/').Length == 3
            ? new Dictionary<string, string> { ["medications"] = $"{path}/medications" }
            : null
    };

    var html = renderer.RenderShell(payload, "Impulse App");
    // Inject component path
    html = html.Replace("data-impulse='", $"data-component=\"{component}\" data-impulse='");

    ctx.Response.ContentType = "text/html";
    await ctx.Response.WriteAsync(html);
});

app.Run();
