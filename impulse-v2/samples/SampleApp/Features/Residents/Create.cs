using Impulse.Core;

namespace SampleApp.Features.Residents;

// ========================================
// Create Resident - POST /residents
// Request/Response colocated with endpoint
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

        if (request.EmergencyContacts is null or { Count: 0 })
            errors["emergencyContacts"] = ["At least one emergency contact is required"];

        if (errors.Count > 0)
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));

        // Simulate created resident with new ID
        var created = new CreateResidentResponse(42, request.FirstName, request.LastName);

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Created($"/residents/{created.Id}", created));
    }
}
