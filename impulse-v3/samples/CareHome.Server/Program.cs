using System.Reflection;
using Impulse;

var builder = WebApplication.CreateBuilder(args);

// Configure CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("X-Impulse-Reload");
    });
});

var app = builder.Build();

app.UseCors();

// Serve static files for React client
app.UseDefaultFiles();
app.UseStaticFiles();

// Map all Impulse endpoints from this assembly
app.MapImpulseEndpoints(Assembly.GetExecutingAssembly());

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

app.Run();
