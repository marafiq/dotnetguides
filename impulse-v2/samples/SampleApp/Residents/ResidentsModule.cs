using Impulse.Core;
using Microsoft.AspNetCore.Routing;

namespace SampleApp.Residents;

/// <summary>
/// Example ImpulseModule demonstrating Nancy-style endpoint registration.
/// This is an alternative to using individual endpoint classes.
///
/// Choose the pattern that fits your team:
/// - Individual endpoint classes (ImpulseEndpoint<TRequest, TResponse>) for complex logic
/// - Module pattern for simple endpoints or when you prefer grouping
/// </summary>
public class ResidentsModule : ImpulseModule
{
    public override string BasePath => "/api/v2/residents";

    public override void Configure(IEndpointRouteBuilder app)
    {
        // Using minimal API style with module grouping
        // Note: The ImpulseEndpoint classes are still used for the main routes
        // This module shows an alternative for simpler endpoints

        // Health check for this domain
        app.MapGet("/health", () => new { status = "healthy", domain = "residents" });

        // Stats endpoint (doesn't need full ImpulseEndpoint class)
        app.MapGet("/stats", GetStats);
    }

    // Simple handler method - no need for a full endpoint class
    private static object GetStats()
    {
        return new
        {
            TotalResidents = 42,
            ActiveResidents = 38,
            DischargedThisMonth = 2,
            NewAdmissionsThisMonth = 5,
            AverageAge = 79.5,
            OccupancyRate = 0.85
        };
    }
}

/// <summary>
/// Example showing deferred loading configuration.
/// The GetResident endpoint can lazily load medications.
/// </summary>
[Deferred("medications", "/residents/{id}/medications", ResponseType = typeof(SampleApp.Medications.ListMedicationsResponse))]
[Deferred("carePlan", "/residents/{id}/care-plan", ResponseType = typeof(SampleApp.CarePlans.GetCarePlanResponse))]
public class GetResidentWithDeferredEndpoint : ImpulseEndpoint<GetResidentRequest, GetResidentResponse>
{
    public override Task<IImpulseResult> Handle(GetResidentRequest request, CancellationToken ct = default)
    {
        if (request.Id <= 0 || request.Id > 3)
        {
            return Task.FromResult<IImpulseResult>(ImpulseResults.NotFound("Resident not found"));
        }

        var resident = new GetResidentResponse(
            Id: request.Id,
            FirstName: "John",
            LastName: "Smith",
            DateOfBirth: new DateTime(1945, 6, 15),
            RoomNumber: "101A",
            Address: new Address("123 Main St", "Springfield", "IL", "62701"),
            EmergencyContacts: [
                new("Jane Smith", "Daughter", "555-123-4567", "jane@email.com"),
            ],
            Preferences: new ResidentPreferences(
                DietaryRestrictions: "Low sodium",
                MobilityAids: "Walker",
                CommunicationPreferences: null,
                PrefersMorningCare: true,
                PrefersEveningCare: false),
            AdmissionDate: new DateTime(2023, 1, 15),
            Status: "Active");

        return Task.FromResult<IImpulseResult>(ImpulseResults.Ok(resident));
    }
}
