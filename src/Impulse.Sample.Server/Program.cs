using Impulse.Core;
using Impulse.Sample.Server.Handlers;
using Impulse.Sample.Server.Models;

var builder = WebApplication.CreateBuilder(args);

// Add Impulse services
builder.Services.AddImpulse(config =>
{
    config.Version = "1.0.0"; // In production, use build hash
    config.BundlePath = "/assets/app.js";
    config.RootElementId = "app";
});

// Add Impulse context provider
builder.Services.AddImpulseContext<AppContext>(async ctx =>
{
    // In production, get user from authentication
    return new AppContext(
        User: new CurrentUser(1, "Sarah", "nurse"),
        Permissions: [
            Permissions.Residents.Read,
            Permissions.Residents.Write,
            Permissions.Medications.Read,
            Permissions.Medications.Write,
            Permissions.Documents.Read
        ],
        Tenant: new Tenant(42, "Sunrise Care")
    );
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders(ImpulseHeaders.Reload);
    });
});

var app = builder.Build();

app.UseCors();

// ============================================
// Dashboard endpoints
// ============================================

app.MapGet("/", () => DashboardHandlers.Get())
   .AsComponent<DashboardProps>("./Dashboard/Dashboard");

// ============================================
// Resident endpoints
// ============================================

app.MapGet("/residents", (int page = 1, int pageSize = 10) =>
    ResidentHandlers.GetList(page, pageSize))
   .AsComponent<ResidentsListProps>("./Residents/ResidentsList");

app.MapGet("/residents/{id:int}", (int id) =>
{
    var resident = ResidentHandlers.GetById(id);
    return resident is not null
        ? Results.Ok(resident)
        : Results.NotFound();
})
.AsComponent<ResidentDetailProps>("./Residents/ResidentDetail")
.Deferred<MedicationsProps>("medications", "/residents/{id}/medications")
.Lazy<DocumentsProps>("documents", "/residents/{id}/documents");

app.MapPost("/residents", (CreateResidentRequest request) =>
{
    var resident = ResidentHandlers.Create(request);
    return Impulse.Created($"/residents/{resident.Id}", resident);
})
.AsMutation<CreateResidentRequest, ResidentDetailProps>()
.Invalidates<ResidentsListProps>();

app.MapPut("/residents/{id:int}", (int id, UpdateResidentRequest request) =>
{
    var resident = ResidentHandlers.Update(id, request);
    return resident is not null
        ? Impulse.Ok(resident)
        : Impulse.NotFound();
})
.AsMutation<UpdateResidentRequest, ResidentDetailProps>()
.Invalidates<ResidentDetailProps>()
.Invalidates<ResidentsListProps>();

app.MapDelete("/residents/{id:int}", (int id) =>
{
    return ResidentHandlers.Delete(id)
        ? Impulse.NoContent()
        : Impulse.NotFound();
});

// ============================================
// Medication endpoints
// ============================================

app.MapGet("/residents/{id:int}/medications", (int id) =>
    MedicationHandlers.GetByResidentId(id));

app.MapPost("/residents/{id:int}/medications", (int id, AddMedicationRequest request) =>
{
    var medication = MedicationHandlers.Add(id, request);
    return medication is not null
        ? Impulse.Created(medication)
        : Impulse.NotFound();
})
.AsMutation<AddMedicationRequest, Medication>()
.Invalidates<MedicationsProps>()
.Invalidates<ResidentDetailProps>();

app.MapDelete("/residents/{residentId:int}/medications/{medicationId:int}",
    (int residentId, int medicationId) =>
{
    return MedicationHandlers.Remove(residentId, medicationId)
        ? Impulse.NoContent()
        : Impulse.NotFound();
});

// ============================================
// Document endpoints
// ============================================

app.MapGet("/residents/{id:int}/documents", (int id) =>
    DocumentHandlers.GetByResidentId(id));

// ============================================
// Shell endpoint (serves initial HTML)
// ============================================

app.MapGet("/shell/{**path}", async (HttpContext ctx, string? path) =>
{
    var shellRenderer = ctx.RequestServices.GetRequiredService<ImpulseShellRenderer>();
    var contextProvider = ctx.RequestServices.GetRequiredService<IImpulseContextProvider>();
    var config = ctx.RequestServices.GetRequiredService<ImpulseConfiguration>();

    var appContext = await contextProvider.GetContextAsync(ctx);

    // Default to dashboard
    var url = string.IsNullOrEmpty(path) ? "/" : $"/{path}";
    object props = url switch
    {
        "/" => DashboardHandlers.Get(),
        "/residents" => ResidentHandlers.GetList(),
        _ when url.StartsWith("/residents/") && int.TryParse(url.Split('/').Last(), out var id)
            => ResidentHandlers.GetById(id) ?? (object)new { error = "Not found" },
        _ => new { error = "Not found" }
    };

    var payload = new ImpulsePayload
    {
        Url = url,
        Version = config.Version,
        Props = props,
        Context = appContext
    };

    var html = shellRenderer.RenderShell(payload, "Impulse Sample App");
    return Results.Content(html, "text/html");
});

app.Run();
