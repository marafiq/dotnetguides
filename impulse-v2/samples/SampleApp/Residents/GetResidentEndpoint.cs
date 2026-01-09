using Impulse.Core;

namespace SampleApp.Residents;

// Request type - defines what data comes in
public record GetResidentRequest(int Id);

// Response type - defines what data goes out
public record GetResidentResponse(int Id, string Name, string Email, DateTime CreatedAt);

/// <summary>
/// Get a resident by ID.
/// Returns HTML for browser requests, JSON for Impulse client requests.
/// </summary>
[ImpulseEndpoint("/residents/{id}")]
public class GetResidentEndpoint : ImpulseEndpoint<GetResidentRequest, GetResidentResponse>
{
    public override Task<IImpulseResult> Handle(GetResidentRequest request, CancellationToken ct = default)
    {
        // Simulated data - in real app would come from database
        if (request.Id <= 0)
        {
            return Task.FromResult<IImpulseResult>(ImpulseResults.NotFound("Resident not found"));
        }

        var resident = new GetResidentResponse(
            Id: request.Id,
            Name: "John Doe",
            Email: "john@example.com",
            CreatedAt: DateTime.UtcNow);

        return Task.FromResult<IImpulseResult>(ImpulseResults.Ok(resident));
    }
}
