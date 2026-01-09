using Impulse.Core;

namespace SampleApp.Residents;

// ========================================
// Value Objects
// ========================================

public record Address(
    string Street,
    string City,
    string State,
    string ZipCode);

public record EmergencyContact(
    string Name,
    string Relationship,
    string Phone,
    string? Email);

public record ResidentPreferences(
    string? DietaryRestrictions,
    string? MobilityAids,
    string? CommunicationPreferences,
    bool PrefersMorningCare,
    bool PrefersEveningCare);

// ========================================
// Resident Entity
// ========================================

public record Resident(
    int Id,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string? RoomNumber,
    Address? Address,
    List<EmergencyContact> EmergencyContacts,
    ResidentPreferences? Preferences,
    DateTime AdmissionDate,
    DateTime? DischargeDate,
    string Status); // Active, Discharged, Deceased

// ========================================
// List Residents - GET /residents
// ========================================

public record ListResidentsRequest(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    string? SearchTerm = null);

public record ListResidentsResponse(
    List<ResidentSummary> Residents,
    int TotalCount,
    int Page,
    int PageSize);

public record ResidentSummary(
    int Id,
    string FirstName,
    string LastName,
    string? RoomNumber,
    string Status,
    int Age);

[ImpulseEndpoint("/residents")]
public class ListResidentsEndpoint : ImpulseEndpoint<ListResidentsResponse>
{
    public override Task<IImpulseResult> Handle(CancellationToken ct = default)
    {
        // Simulated data
        var residents = new List<ResidentSummary>
        {
            new(1, "John", "Smith", "101A", "Active", 78),
            new(2, "Mary", "Johnson", "102B", "Active", 82),
            new(3, "Robert", "Williams", "103A", "Active", 75),
        };

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Ok(new ListResidentsResponse(residents, 3, 1, 20)));
    }
}

// ========================================
// Get Resident - GET /residents/{id}
// ========================================

public record GetResidentRequest(int Id);

public record GetResidentResponse(
    int Id,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string? RoomNumber,
    Address? Address,
    List<EmergencyContact> EmergencyContacts,
    ResidentPreferences? Preferences,
    DateTime AdmissionDate,
    string Status);

[ImpulseEndpoint("/residents/{id}")]
public class GetResidentEndpoint : ImpulseEndpoint<GetResidentRequest, GetResidentResponse>
{
    public override Task<IImpulseResult> Handle(GetResidentRequest request, CancellationToken ct = default)
    {
        // Simulated data only exists for residents 1-3
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
                new("Bob Smith", "Son", "555-987-6543", null)
            ],
            Preferences: new ResidentPreferences(
                DietaryRestrictions: "Low sodium",
                MobilityAids: "Walker",
                CommunicationPreferences: "Large print materials",
                PrefersMorningCare: true,
                PrefersEveningCare: false),
            AdmissionDate: new DateTime(2023, 1, 15),
            Status: "Active");

        return Task.FromResult<IImpulseResult>(ImpulseResults.Ok(resident));
    }
}

// ========================================
// Create Resident - POST /residents
// ========================================

public record CreateResidentRequest(
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string? RoomNumber,
    Address? Address,
    List<EmergencyContact>? EmergencyContacts);

public record CreateResidentResponse(int Id, string FirstName, string LastName);

[ImpulseEndpoint("/residents", ImpulseMethod.Post)]
public class CreateResidentEndpoint : ImpulseEndpoint<CreateResidentRequest, CreateResidentResponse>
{
    public override Task<IImpulseResult> Handle(CreateResidentRequest request, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.FirstName))
            errors["firstName"] = ["First name is required"];

        if (string.IsNullOrWhiteSpace(request.LastName))
            errors["lastName"] = ["Last name is required"];

        if (request.DateOfBirth > DateTime.Today.AddYears(-18))
            errors["dateOfBirth"] = ["Resident must be at least 18 years old"];

        if (request.EmergencyContacts?.Count == 0)
            errors["emergencyContacts"] = ["At least one emergency contact is required"];

        if (errors.Count > 0)
        {
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));
        }

        var created = new CreateResidentResponse(42, request.FirstName, request.LastName);

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Created($"/residents/{created.Id}", created));
    }
}

// ========================================
// Update Resident - PUT /residents/{id}
// ========================================

public record UpdateResidentRequest(
    int Id,
    string FirstName,
    string LastName,
    string? RoomNumber,
    Address? Address,
    ResidentPreferences? Preferences);

public record UpdateResidentResponse(int Id, string FirstName, string LastName, DateTime UpdatedAt);

[ImpulseEndpoint("/residents/{id}", ImpulseMethod.Put)]
public class UpdateResidentEndpoint : ImpulseEndpoint<UpdateResidentRequest, UpdateResidentResponse>
{
    public override Task<IImpulseResult> Handle(UpdateResidentRequest request, CancellationToken ct = default)
    {
        // Simulated data only exists for residents 1-3
        if (request.Id <= 0 || request.Id > 3)
        {
            return Task.FromResult<IImpulseResult>(ImpulseResults.NotFound("Resident not found"));
        }

        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.FirstName))
            errors["firstName"] = ["First name is required"];

        if (string.IsNullOrWhiteSpace(request.LastName))
            errors["lastName"] = ["Last name is required"];

        if (errors.Count > 0)
        {
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));
        }

        var updated = new UpdateResidentResponse(request.Id, request.FirstName, request.LastName, DateTime.UtcNow);

        return Task.FromResult<IImpulseResult>(ImpulseResults.Ok(updated));
    }
}
