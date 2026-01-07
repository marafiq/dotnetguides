using Impulse.Core;
using Impulse.IntegrationTests.Features.Dashboard;
using Impulse.IntegrationTests.Features.Residents;
using Impulse.IntegrationTests.Features.Wizard;
using Impulse.IntegrationTests.Features.DynamicForms;
using Impulse.IntegrationTests.Features.Modal;
using Impulse.IntegrationTests.Features.Pane;
using System.Text.Json;

using DashboardHandler = Impulse.IntegrationTests.Features.Dashboard.Handler;
using ResidentsHandler = Impulse.IntegrationTests.Features.Residents.Handler;
using WizardHandler = Impulse.IntegrationTests.Features.Wizard.Handler;
using DynamicFormHandler = Impulse.IntegrationTests.Features.DynamicForms.DynamicFormHandler;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddImpulse(config =>
{
    config.Version = "1.0.0";
    config.BundlePath = "/js/Shared/index.js";
});

var app = builder.Build();

app.UseStaticFiles();

// ============================================================================
// Dashboard
// ============================================================================
app.MapGet("/", () => DashboardHandler.Get())
   .AsComponent<DashboardProps>();

// ============================================================================
// Residents CRUD
// ============================================================================
app.MapGet("/residents", () => ResidentsHandler.GetList())
   .AsComponent<ResidentsListProps>();

app.MapGet("/residents/{id:int}", (int id) =>
{
    var resident = ResidentsHandler.GetById(id);
    return resident is not null ? Results.Ok(resident) : Results.NotFound();
})
.AsComponent<ResidentDetailProps>()
.Deferred<MedicationsProps>("medications", "/residents/{id}/medications");

app.MapGet("/residents/{id:int}/medications", (int id) =>
    ResidentsHandler.GetMedications(id));

// ============================================================================
// Wizard - Multi-step Resident Onboarding
// ============================================================================
app.MapGet("/wizard", () => WizardHandler.GetStep(1))
   .AsComponent<WizardProps>();

app.MapGet("/wizard/step/{step:int}", (int step) => WizardHandler.GetStep(step))
   .AsComponent<WizardProps>();

app.MapPost("/wizard/next", async (HttpContext ctx) =>
{
    var body = await ctx.Request.ReadFromJsonAsync<WizardStepRequest>();
    return WizardHandler.NextStep(body!);
});

app.MapPost("/wizard/back", async (HttpContext ctx) =>
{
    var body = await ctx.Request.ReadFromJsonAsync<WizardBackRequest>();
    return WizardHandler.PreviousStep(body!.CurrentStep);
});

app.MapPost("/wizard/submit", async (HttpContext ctx) =>
{
    var body = await ctx.Request.ReadFromJsonAsync<WizardSubmitRequest>();
    return WizardHandler.Submit(body!);
});

// ============================================================================
// Dynamic Forms - Insurance Application
// ============================================================================
app.MapGet("/forms/insurance", () => DynamicFormHandler.CreateInsuranceForm())
   .AsComponent<DynamicFormProps>();

app.MapPost("/forms/submit", async (HttpContext ctx) =>
{
    var body = await ctx.Request.ReadFromJsonAsync<FormSubmitRequest>();
    // Simulate validation
    return new FormSubmitResponse(
        Success: true,
        ValidationErrors: null,
        RedirectUrl: "/forms/success",
        Message: "Form submitted successfully"
    );
});

app.MapGet("/forms/success", () => new { message = "Form submitted successfully!" });

// ============================================================================
// Modal - Delete Confirmation
// ============================================================================
app.MapGet("/residents/{id:int}/delete", (int id) =>
{
    var resident = ResidentsHandler.GetById(id);
    return new DeleteConfirmationProps(
        ItemType: "Resident",
        ItemName: resident?.Name ?? "Unknown",
        ItemId: id,
        DeleteEndpoint: $"/api/residents/{id}",
        CancelUrl: $"/residents/{id}"
    );
})
.AsComponent<DeleteConfirmationProps>();

app.MapDelete("/api/residents/{id:int}", (int id) =>
    new DeleteResponse(Success: true, Message: "Resident deleted", RedirectUrl: "/residents"));

// ============================================================================
// Modal - Edit Resident
// ============================================================================
app.MapGet("/residents/{id:int}/edit", (int id) =>
{
    var resident = ResidentsHandler.GetById(id);
    return new EditResidentModalProps(
        ResidentId: id,
        CurrentName: resident?.Name ?? "",
        CurrentRoom: resident?.Room ?? "",
        CurrentAdmitDate: DateTime.Now.AddDays(-30),
        AvailableRooms: new List<RoomOptionData>
        {
            new("101A", "Building A", true),
            new("102A", "Building A", false),
            new("201B", "Building B", true),
            new("202B", "Building B", true)
        }
    );
})
.AsComponent<EditResidentModalProps>();

app.MapPut("/residents/update", async (HttpContext ctx) =>
{
    var body = await ctx.Request.ReadFromJsonAsync<UpdateResidentRequest>();
    return new UpdateResidentResponse(Success: true, Message: "Updated", Errors: null);
});

// ============================================================================
// Pane - Resident Detail Pane
// ============================================================================
app.MapGet("/pane/residents/{id:int}", (int id) =>
{
    var resident = ResidentsHandler.GetById(id);
    return new PaneContainerProps(
        IsOpen: true,
        Pane: new PaneConfig(
            Id: $"resident-{id}",
            Title: resident?.Name ?? "Resident Details",
            Subtitle: $"Room {resident?.Room}",
            Position: PanePosition.Right,
            Size: PaneSize.Medium,
            CloseOnOverlay: true,
            CloseOnEscape: true,
            ShowCloseButton: true,
            Content: new PaneContent(
                Type: PaneContentType.Sections,
                ComponentPath: null,
                ComponentProps: null,
                Sections: new List<PaneSection>
                {
                    new PaneSection(
                        Id: "basic",
                        Title: "Basic Information",
                        IsCollapsible: true,
                        IsCollapsed: false,
                        Items: new List<PaneSectionItem>
                        {
                            new("Name", resident?.Name, PaneSectionItemType.Text, null, null),
                            new("Room", resident?.Room, PaneSectionItemType.Badge, null, null),
                            new("Admit Date", resident?.AdmitDate.ToString("yyyy-MM-dd"), PaneSectionItemType.Date, null, null),
                            new("Status", "Active", PaneSectionItemType.Status, null, null)
                        }
                    ),
                    new PaneSection(
                        Id: "allergies",
                        Title: "Allergies",
                        IsCollapsible: true,
                        IsCollapsed: false,
                        Items: resident?.Allergies.Select(a =>
                            new PaneSectionItem(a, null, PaneSectionItemType.Badge, null, "⚠️")).ToList()
                            ?? new List<PaneSectionItem>()
                    )
                },
                Url: null
            ),
            HeaderActions: new List<PaneAction>
            {
                new("edit", "Edit", PaneActionType.Navigate, PaneActionStyle.Ghost, "✏️",
                    $"/residents/{id}/edit", false, false)
            },
            FooterActions: new List<PaneAction>
            {
                new("close", "Close", PaneActionType.Close, PaneActionStyle.Secondary, null,
                    null, false, false),
                new("view", "View Full Profile", PaneActionType.Navigate, PaneActionStyle.Primary, null,
                    $"/residents/{id}", false, false)
            }
        ),
        ReturnUrl: "/residents"
    );
})
.AsComponent<PaneContainerProps>();

// ============================================================================
// Pane - Filter Pane
// ============================================================================
app.MapGet("/residents/filter", () =>
{
    return new FilterPaneProps(
        Title: "Filter Residents",
        FilterGroups: new List<FilterGroupData>
        {
            new FilterGroupData(
                Id: "status",
                Label: "Status",
                Type: FilterType.Checkbox,
                Options: new List<FilterOptionData>
                {
                    new("active", "Active", 45),
                    new("discharged", "Discharged", 12),
                    new("onleave", "On Leave", 3)
                },
                Min: null, Max: null, DateFormat: null
            ),
            new FilterGroupData(
                Id: "building",
                Label: "Building",
                Type: FilterType.Radio,
                Options: new List<FilterOptionData>
                {
                    new("all", "All Buildings", null),
                    new("a", "Building A", 20),
                    new("b", "Building B", 25),
                    new("c", "Building C", 15)
                },
                Min: null, Max: null, DateFormat: null
            ),
            new FilterGroupData(
                Id: "admitDate",
                Label: "Admit Date",
                Type: FilterType.DateRange,
                Options: null,
                Min: null, Max: null, DateFormat: "yyyy-MM-dd"
            ),
            new FilterGroupData(
                Id: "search",
                Label: "Search",
                Type: FilterType.Search,
                Options: null,
                Min: null, Max: null, DateFormat: null
            )
        },
        CurrentFilters: new Dictionary<string, object>(),
        ApplyUrl: "/api/residents/filter",
        ResetUrl: "/residents"
    );
})
.AsComponent<FilterPaneProps>();

app.MapPost("/api/residents/filter", async (HttpContext ctx) =>
{
    var body = await ctx.Request.ReadFromJsonAsync<ApplyFiltersRequest>();
    return new ApplyFiltersResponse(Success: true, RedirectUrl: "/residents", ResultCount: 42);
});

// ============================================================================
// Activity Feed Pane
// ============================================================================
app.MapGet("/activity", () =>
{
    return new ActivityFeedPaneProps(
        Title: "Recent Activity",
        Activities: new List<ActivityItemData>
        {
            new("1", "medication", "Administered medication to John Doe", DateTime.Now.AddMinutes(-5),
                "Nurse Sarah", null, null),
            new("2", "admission", "New resident admitted: Jane Smith", DateTime.Now.AddHours(-2),
                "Admin", null, null),
            new("3", "discharge", "Resident Robert Johnson discharged", DateTime.Now.AddHours(-4),
                "Dr. Williams", null, null),
            new("4", "update", "Updated care plan for Mary Davis", DateTime.Now.AddDays(-1),
                "Care Coordinator", null, null),
            new("5", "alert", "Fall alert triggered in Room 204B", DateTime.Now.AddDays(-1).AddHours(-3),
                "System", null, null)
        },
        HasMore: true,
        LoadMoreUrl: "/activity?page=2"
    );
})
.AsComponent<ActivityFeedPaneProps>();

// ============================================================================
// Modal Container (Generic)
// ============================================================================
app.MapGet("/modal/confirm", () =>
{
    return new ModalContainerProps(
        IsOpen: true,
        Modal: new ModalConfig(
            Id: "confirm-action",
            Type: ModalType.Confirm,
            Title: "Confirm Action",
            Description: "Are you sure you want to proceed with this action?",
            Size: ModalSize.Small,
            CloseOnOverlay: true,
            CloseOnEscape: true,
            ShowCloseButton: true,
            Content: new ModalContent(
                Type: ModalContentType.Message,
                Message: "This action cannot be undone. Please confirm you want to proceed.",
                AlertSeverity: null,
                FormFields: null,
                FormValues: null,
                ComponentPath: null,
                ComponentProps: null
            ),
            Actions: new List<ModalAction>
            {
                new("cancel", "Cancel", ModalActionType.Close, ModalActionStyle.Secondary,
                    false, false, null, null),
                new("confirm", "Confirm", ModalActionType.Submit, ModalActionStyle.Primary,
                    false, false, "/api/confirm", null)
            }
        ),
        ReturnUrl: "/"
    );
})
.AsComponent<ModalContainerProps>();

app.MapGet("/modal/alert", () =>
{
    return new ModalContainerProps(
        IsOpen: true,
        Modal: new ModalConfig(
            Id: "success-alert",
            Type: ModalType.Alert,
            Title: "Success!",
            Description: null,
            Size: ModalSize.Small,
            CloseOnOverlay: true,
            CloseOnEscape: true,
            ShowCloseButton: true,
            Content: new ModalContent(
                Type: ModalContentType.Alert,
                Message: "Your changes have been saved successfully.",
                AlertSeverity: AlertSeverity.Success,
                FormFields: null,
                FormValues: null,
                ComponentPath: null,
                ComponentProps: null
            ),
            Actions: new List<ModalAction>
            {
                new("ok", "OK", ModalActionType.Close, ModalActionStyle.Primary,
                    false, false, null, null)
            }
        ),
        ReturnUrl: "/"
    );
})
.AsComponent<ModalContainerProps>();

app.MapGet("/modal/form", () =>
{
    return new ModalContainerProps(
        IsOpen: true,
        Modal: new ModalConfig(
            Id: "quick-add",
            Type: ModalType.Form,
            Title: "Quick Add Resident",
            Description: "Enter basic information to add a new resident.",
            Size: ModalSize.Medium,
            CloseOnOverlay: false,
            CloseOnEscape: true,
            ShowCloseButton: true,
            Content: new ModalContent(
                Type: ModalContentType.Form,
                Message: null,
                AlertSeverity: null,
                FormFields: new List<ModalFormField>
                {
                    new("name", "Full Name", ModalFieldType.Text, true, "Enter full name", null, null, null),
                    new("email", "Email", ModalFieldType.Email, true, "email@example.com", null, null, null),
                    new("room", "Room Assignment", ModalFieldType.Select, true, null, null,
                        new List<SelectOptionData>
                        {
                            new("101A", "Room 101A"),
                            new("102A", "Room 102A"),
                            new("201B", "Room 201B")
                        }, null),
                    new("admitDate", "Admit Date", ModalFieldType.Date, true, null, null, null, null),
                    new("notes", "Notes", ModalFieldType.Textarea, false, "Any additional notes...", null, null, null)
                },
                FormValues: null,
                ComponentPath: null,
                ComponentProps: null
            ),
            Actions: new List<ModalAction>
            {
                new("cancel", "Cancel", ModalActionType.Close, ModalActionStyle.Secondary,
                    false, false, null, null),
                new("save", "Add Resident", ModalActionType.Submit, ModalActionStyle.Primary,
                    false, false, "/api/residents", null)
            }
        ),
        ReturnUrl: "/residents"
    );
})
.AsComponent<ModalContainerProps>();

// ============================================================================
// Shell endpoint - serves the HTML with embedded payload
// ============================================================================
app.MapFallback(async (HttpContext ctx) =>
{
    var renderer = ctx.RequestServices.GetRequiredService<ImpulseShellRenderer>();
    var config = ctx.RequestServices.GetRequiredService<ImpulseConfiguration>();

    var path = ctx.Request.Path.Value ?? "/";

    // Route matching
    var (props, component, title) = GetRouteData(path);

    var payload = new ImpulsePayload
    {
        Url = path,
        Version = config.Version,
        Props = props,
        Context = new { user = "admin", theme = "light" },
        Deferred = GetDeferredUrls(path)
    };

    var html = renderer.RenderShell(payload, title);
    html = html.Replace("data-impulse='", $"data-component=\"{component}\" data-impulse='");

    ctx.Response.ContentType = "text/html";
    await ctx.Response.WriteAsync(html);
});

app.Run();

// Helper methods
static (object props, string component, string title) GetRouteData(string path)
{
    return path switch
    {
        "/" => (DashboardHandler.Get(), ComponentPathConvention.GetPath<DashboardProps>(), "Dashboard"),

        "/residents" => (ResidentsHandler.GetList(), ComponentPathConvention.GetPath<ResidentsListProps>(), "Residents"),

        var p when p.StartsWith("/residents/") && p.EndsWith("/delete") =>
            GetDeleteModal(p),

        var p when p.StartsWith("/residents/") && p.EndsWith("/edit") =>
            GetEditModal(p),

        var p when p.StartsWith("/pane/residents/") =>
            GetResidentPane(p),

        var p when p.StartsWith("/residents/") && int.TryParse(p.Split('/').ElementAtOrDefault(2), out var id) =>
            (ResidentsHandler.GetById(id) ?? new ResidentDetailProps(0, "", "", DateTime.Now, Array.Empty<string>()),
             ComponentPathConvention.GetPath<ResidentDetailProps>(), "Resident Detail"),

        "/wizard" =>
            (WizardHandler.GetStep(1), ComponentPathConvention.GetPath<WizardProps>(), "New Resident Wizard"),

        var w when w.StartsWith("/wizard/step/") =>
            (WizardHandler.GetStep(GetStepNumber(path)), ComponentPathConvention.GetPath<WizardProps>(), "New Resident Wizard"),

        "/forms/insurance" =>
            (DynamicFormHandler.CreateInsuranceForm(), ComponentPathConvention.GetPath<DynamicFormProps>(), "Insurance Application"),

        "/residents/filter" =>
            GetFilterPane(),

        "/activity" =>
            GetActivityFeed(),

        "/modal/confirm" =>
            GetConfirmModal(),

        "/modal/alert" =>
            GetAlertModal(),

        "/modal/form" =>
            GetFormModal(),

        _ => (DashboardHandler.Get(), ComponentPathConvention.GetPath<DashboardProps>(), "Impulse App")
    };
}

static (object, string, string) GetDeleteModal(string path)
{
    var idStr = path.Replace("/residents/", "").Replace("/delete", "");
    var id = int.TryParse(idStr, out var parsed) ? parsed : 0;
    var resident = ResidentsHandler.GetById(id);
    return (
        new DeleteConfirmationProps("Resident", resident?.Name ?? "Unknown", id, $"/api/residents/{id}", $"/residents/{id}"),
        "./Modal/DeleteConfirmation",
        "Delete Resident"
    );
}

static (object, string, string) GetEditModal(string path)
{
    var idStr = path.Replace("/residents/", "").Replace("/edit", "");
    var id = int.TryParse(idStr, out var parsed) ? parsed : 0;
    var resident = ResidentsHandler.GetById(id);
    return (
        new EditResidentModalProps(id, resident?.Name ?? "", resident?.Room ?? "", DateTime.Now.AddDays(-30),
            new List<RoomOptionData>
            {
                new("101A", "Building A", true),
                new("102A", "Building A", false),
                new("201B", "Building B", true),
            }),
        "./Modal/EditResident",
        "Edit Resident"
    );
}

static (object, string, string) GetResidentPane(string path)
{
    var idStr = path.Replace("/pane/residents/", "");
    var id = int.TryParse(idStr, out var parsed) ? parsed : 0;
    var resident = ResidentsHandler.GetById(id);

    return (
        new PaneContainerProps(
            IsOpen: true,
            Pane: new PaneConfig(
                Id: $"resident-{id}",
                Title: resident?.Name ?? "Resident",
                Subtitle: $"Room {resident?.Room}",
                Position: PanePosition.Right,
                Size: PaneSize.Medium,
                CloseOnOverlay: true,
                CloseOnEscape: true,
                ShowCloseButton: true,
                Content: new PaneContent(
                    Type: PaneContentType.Sections,
                    ComponentPath: null, ComponentProps: null,
                    Sections: new List<PaneSection>
                    {
                        new("info", "Information", true, false, new List<PaneSectionItem>
                        {
                            new("Name", resident?.Name, PaneSectionItemType.Text, null, null),
                            new("Room", resident?.Room, PaneSectionItemType.Badge, null, null),
                            new("Status", "Active", PaneSectionItemType.Status, null, null)
                        })
                    },
                    Url: null
                ),
                HeaderActions: null,
                FooterActions: new List<PaneAction>
                {
                    new("close", "Close", PaneActionType.Close, PaneActionStyle.Secondary, null, null, false, false)
                }
            ),
            ReturnUrl: "/residents"
        ),
        "./Pane",
        "Resident Details"
    );
}

static (object, string, string) GetFilterPane()
{
    return (
        new FilterPaneProps(
            Title: "Filter Residents",
            FilterGroups: new List<FilterGroupData>
            {
                new("status", "Status", FilterType.Checkbox,
                    new List<FilterOptionData> { new("active", "Active", 45), new("discharged", "Discharged", 12) },
                    null, null, null),
                new("search", "Search", FilterType.Search, null, null, null, null)
            },
            CurrentFilters: new Dictionary<string, object>(),
            ApplyUrl: "/api/filter",
            ResetUrl: "/residents"
        ),
        "./Pane/Filter",
        "Filter Residents"
    );
}

static (object, string, string) GetActivityFeed()
{
    return (
        new ActivityFeedPaneProps(
            Title: "Recent Activity",
            Activities: new List<ActivityItemData>
            {
                new("1", "medication", "Administered medication", DateTime.Now.AddMinutes(-5), "Nurse", null, null),
                new("2", "admission", "New resident admitted", DateTime.Now.AddHours(-2), "Admin", null, null)
            },
            HasMore: true,
            LoadMoreUrl: "/activity?page=2"
        ),
        "./Pane/ActivityFeed",
        "Activity Feed"
    );
}

static (object, string, string) GetConfirmModal()
{
    return (
        new ModalContainerProps(
            IsOpen: true,
            Modal: new ModalConfig(
                "confirm", ModalType.Confirm, "Confirm Action", "Are you sure?",
                ModalSize.Small, true, true, true,
                new ModalContent(ModalContentType.Message, "This action cannot be undone.", null, null, null, null, null),
                new List<ModalAction>
                {
                    new("cancel", "Cancel", ModalActionType.Close, ModalActionStyle.Secondary, false, false, null, null),
                    new("confirm", "Confirm", ModalActionType.Submit, ModalActionStyle.Primary, false, false, "/api/confirm", null)
                }
            ),
            ReturnUrl: "/"
        ),
        "./Modal",
        "Confirm"
    );
}

static (object, string, string) GetAlertModal()
{
    return (
        new ModalContainerProps(
            IsOpen: true,
            Modal: new ModalConfig(
                "alert", ModalType.Alert, "Success!", null,
                ModalSize.Small, true, true, true,
                new ModalContent(ModalContentType.Alert, "Changes saved successfully.", AlertSeverity.Success, null, null, null, null),
                new List<ModalAction>
                {
                    new("ok", "OK", ModalActionType.Close, ModalActionStyle.Primary, false, false, null, null)
                }
            ),
            ReturnUrl: "/"
        ),
        "./Modal",
        "Alert"
    );
}

static (object, string, string) GetFormModal()
{
    return (
        new ModalContainerProps(
            IsOpen: true,
            Modal: new ModalConfig(
                "form", ModalType.Form, "Quick Add", "Enter details",
                ModalSize.Medium, false, true, true,
                new ModalContent(
                    ModalContentType.Form, null, null,
                    new List<ModalFormField>
                    {
                        new("name", "Name", ModalFieldType.Text, true, "Full name", null, null, null),
                        new("email", "Email", ModalFieldType.Email, true, "email@example.com", null, null, null)
                    },
                    null, null, null
                ),
                new List<ModalAction>
                {
                    new("cancel", "Cancel", ModalActionType.Close, ModalActionStyle.Secondary, false, false, null, null),
                    new("save", "Save", ModalActionType.Submit, ModalActionStyle.Primary, false, false, "/api/save", null)
                }
            ),
            ReturnUrl: "/residents"
        ),
        "./Modal",
        "Quick Add"
    );
}

static int GetStepNumber(string path)
{
    if (path == "/wizard") return 1;
    var parts = path.Split('/');
    return int.TryParse(parts.LastOrDefault(), out var step) ? step : 1;
}

static Dictionary<string, string>? GetDeferredUrls(string path)
{
    if (path.StartsWith("/residents/") && path.Split('/').Length == 3 &&
        !path.EndsWith("/delete") && !path.EndsWith("/edit"))
    {
        return new Dictionary<string, string> { ["medications"] = $"{path}/medications" };
    }
    return null;
}
