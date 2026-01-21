using System.Text.Json;
using Shalimar.Razor;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<ShalimarCompiler>();
builder.Services.AddSingleton(sp =>
{
    var manifestPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "dist", ".vite", "manifest.json");
    return ViteManifest.Load(manifestPath);
});

var app = builder.Build();

// Serve static files from wwwroot
app.UseStaticFiles();

// RSC API endpoint - returns wire format for server components
app.MapGet("/api/rsc/{feature}/{component}", (string feature, string component, ShalimarCompiler compiler, ViteManifest manifest) =>
{
    var razorPath = Path.Combine(app.Environment.ContentRootPath, "Features", feature, $"{component}.razor");

    if (!File.Exists(razorPath))
    {
        return Results.NotFound($"Component not found: {feature}/{component}");
    }

    var parser = new RazorParser();
    var razorComponent = parser.Parse(razorPath);

    // Only serve RSC for @server components
    if (razorComponent.Directive != ComponentDirective.Server)
    {
        return Results.BadRequest($"Component {component} is not a server component");
    }

    var generator = new RscWireGenerator(manifest);
    var response = generator.GenerateResponse(razorComponent);

    return Results.Text(response.WireFormat, response.ContentType);
});

// RSC API with data - POST endpoint for server components with props
app.MapPost("/api/rsc/{feature}/{component}", async (string feature, string component, HttpRequest request, ShalimarCompiler compiler, ViteManifest manifest) =>
{
    var razorPath = Path.Combine(app.Environment.ContentRootPath, "Features", feature, $"{component}.razor");

    if (!File.Exists(razorPath))
    {
        return Results.NotFound($"Component not found: {feature}/{component}");
    }

    var parser = new RazorParser();
    var razorComponent = parser.Parse(razorPath);

    if (razorComponent.Directive != ComponentDirective.Server)
    {
        return Results.BadRequest($"Component {component} is not a server component");
    }

    // Read props from request body
    object? props = null;
    if (request.ContentLength > 0)
    {
        props = await JsonSerializer.DeserializeAsync<JsonElement>(request.Body);
    }

    var generator = new RscWireGenerator(manifest);
    var response = generator.GenerateResponse(razorComponent, props);

    return Results.Text(response.WireFormat, response.ContentType);
});

// Component manifest endpoint - returns all components for routing
app.MapGet("/api/components", (ShalimarCompiler compiler) =>
{
    var featuresPath = Path.Combine(app.Environment.ContentRootPath, "Features");
    var parser = new RazorParser();
    var components = parser.ParseDirectory(featuresPath).Select(c => new
    {
        c.Name,
        c.FeatureFolder,
        Directive = c.Directive.ToString().ToLower(),
        Props = c.Props.Select(p => new { p.Name, p.Type, p.IsRequired })
    });

    return Results.Json(components);
});

// Development: compile Razor files on demand
app.MapPost("/api/compile", (ShalimarCompiler compiler) =>
{
    var featuresPath = Path.Combine(app.Environment.ContentRootPath, "Features");
    var summary = compiler.CompileDirectory(featuresPath);

    return Results.Json(new
    {
        success = summary.FailureCount == 0,
        compiled = summary.SuccessCount,
        failed = summary.FailureCount,
        results = summary.Results.Select(r => new
        {
            source = Path.GetFileName(r.SourcePath),
            output = r.OutputPath != null ? Path.GetFileName(r.OutputPath) : null,
            error = r.Error
        })
    });
});

// SPA fallback - serve index.html for client-side routing
app.MapFallbackToFile("index.html");

Console.WriteLine("Shalimar Sample App");
Console.WriteLine("-------------------");
Console.WriteLine("Endpoints:");
Console.WriteLine("  GET  /api/rsc/{feature}/{component} - Get RSC wire format");
Console.WriteLine("  POST /api/rsc/{feature}/{component} - Get RSC with props");
Console.WriteLine("  GET  /api/components                - List all components");
Console.WriteLine("  POST /api/compile                   - Compile Razor files");
Console.WriteLine();

app.Run();
