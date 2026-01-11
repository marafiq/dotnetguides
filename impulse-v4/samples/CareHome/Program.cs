using System.Reflection;
using Impulse;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Serve static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// Map Impulse endpoints from this assembly
app.MapImpulseEndpoints(Assembly.GetExecutingAssembly());

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

app.Run();
