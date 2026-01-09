using Impulse.Core;

namespace SampleApp.Residents;

// Request type for creating a resident
public record CreateResidentRequest(string Name, string Email);

// Response type after creation
public record CreateResidentResponse(int Id, string Name, string Email);

/// <summary>
/// Create a new resident.
/// </summary>
[ImpulseEndpoint("/residents")]
public class CreateResidentEndpoint : ImpulseEndpoint<CreateResidentRequest, CreateResidentResponse>
{
    public override Task<IImpulseResult> Handle(CreateResidentRequest request, CancellationToken ct = default)
    {
        // Validation
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors["name"] = ["Name is required"];

        if (string.IsNullOrWhiteSpace(request.Email))
            errors["email"] = ["Email is required"];
        else if (!request.Email.Contains('@'))
            errors["email"] = ["Invalid email format"];

        if (errors.Count > 0)
        {
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));
        }

        // Simulated creation - in real app would save to database
        var created = new CreateResidentResponse(
            Id: 42,
            Name: request.Name,
            Email: request.Email);

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Created($"/residents/{created.Id}", created));
    }
}
