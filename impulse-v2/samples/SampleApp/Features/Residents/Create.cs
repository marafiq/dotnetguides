using Impulse.Core;

namespace SampleApp.Features.Residents;

// ========================================
// Create Resident - POST /residents
// Request/Response colocated with endpoint
// Demonstrates: Impulse server-side validation
// returning ProblemDetails (RFC 7807)
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
    // Reserved names that cannot be used (business rule - server-side only)
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Test", "Admin", "System", "Root", "Administrator", "Guest"
    };

    public override Task<IImpulseResult> Handle(CreateResidentRequest request, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string[]>();

        // Basic validation
        if (string.IsNullOrWhiteSpace(request.FirstName))
            errors["firstName"] = ["First name is required"];

        if (string.IsNullOrWhiteSpace(request.LastName))
            errors["lastName"] = ["Last name is required"];

        // Business rule: Disallow reserved names (Impulse way - server validation)
        if (!string.IsNullOrWhiteSpace(request.FirstName) && ReservedNames.Contains(request.FirstName))
            errors["firstName"] = [$"'{request.FirstName}' is a reserved name and cannot be used"];

        if (!string.IsNullOrWhiteSpace(request.LastName) && ReservedNames.Contains(request.LastName))
            errors["lastName"] = [$"'{request.LastName}' is a reserved name and cannot be used"];

        // Age validation
        if (request.DateOfBirth > DateTime.Today.AddYears(-18))
            errors["dateOfBirth"] = ["Resident must be at least 18 years old"];

        // Emergency contacts required
        if (request.EmergencyContacts is null or { Count: 0 })
            errors["emergencyContacts"] = ["At least one emergency contact is required"];

        // Return ProblemDetails if validation fails
        if (errors.Count > 0)
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));

        // Simulate created resident with new ID
        var created = new CreateResidentResponse(42, request.FirstName, request.LastName);

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Created($"/residents/{created.Id}", created));
    }
}
