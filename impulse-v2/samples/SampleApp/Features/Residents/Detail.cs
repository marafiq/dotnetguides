using Impulse.Core;

namespace SampleApp.Features.Residents;

// ========================================
// Get Resident Detail - GET /residents/{id}
// Request/Response colocated with endpoint
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
    // Simulated resident data
    private static readonly Dictionary<int, GetResidentResponse> _residents = new()
    {
        [1] = new(1, "John", "Smith", new DateTime(1945, 6, 15), "101A",
            new Address("123 Main St", "Springfield", "IL", "62701"),
            [new("Jane Smith", "Daughter", "555-123-4567", "jane@email.com"),
             new("Bob Smith", "Son", "555-987-6543", null)],
            new ResidentPreferences("Low sodium", "Walker", "Large print", true, false),
            new DateTime(2023, 1, 15), "Active"),

        [2] = new(2, "Mary", "Johnson", new DateTime(1941, 3, 22), "102B",
            new Address("456 Oak Ave", "Springfield", "IL", "62702"),
            [new("Tom Johnson", "Son", "555-222-3333", "tom@email.com")],
            new ResidentPreferences("Diabetic diet", null, "Hearing aid", false, true),
            new DateTime(2022, 8, 10), "Active"),

        [3] = new(3, "Robert", "Williams", new DateTime(1948, 11, 8), "103A",
            new Address("789 Elm St", "Springfield", "IL", "62703"),
            [new("Lisa Williams", "Daughter", "555-444-5555", "lisa@email.com"),
             new("Mike Williams", "Son", "555-666-7777", "mike@email.com")],
            new ResidentPreferences(null, "Wheelchair", null, true, true),
            new DateTime(2023, 5, 20), "Active"),
    };

    public override Task<IImpulseResult> Handle(GetResidentRequest request, CancellationToken ct = default)
    {
        if (!_residents.TryGetValue(request.Id, out var resident))
        {
            return Task.FromResult<IImpulseResult>(
                ImpulseResults.NotFound($"Resident {request.Id} not found"));
        }

        return Task.FromResult<IImpulseResult>(ImpulseResults.Ok(resident));
    }
}
