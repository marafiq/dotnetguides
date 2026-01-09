using Impulse.Core;

namespace SampleApp.Features.Residents;

// ========================================
// Update Resident - PUT /residents/{id}
// Request/Response colocated with endpoint
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
        // Only residents 1-5 exist
        if (request.Id <= 0 || request.Id > 5)
            return Task.FromResult<IImpulseResult>(
                ImpulseResults.NotFound($"Resident {request.Id} not found"));

        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.FirstName))
            errors["firstName"] = ["First name is required"];

        if (string.IsNullOrWhiteSpace(request.LastName))
            errors["lastName"] = ["Last name is required"];

        if (errors.Count > 0)
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));

        var updated = new UpdateResidentResponse(
            request.Id, request.FirstName, request.LastName, DateTime.UtcNow);

        return Task.FromResult<IImpulseResult>(ImpulseResults.Ok(updated));
    }
}
