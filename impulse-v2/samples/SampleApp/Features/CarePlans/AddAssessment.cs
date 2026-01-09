using Impulse.Core;

namespace SampleApp.Features.CarePlans;

// ========================================
// Add Assessment - POST /residents/{residentId}/care-plan/assessments
// ========================================

public record AddAssessmentRequest(
    int ResidentId,
    AssessmentType Type,
    DateTime AssessmentDate,
    Dictionary<string, string> Findings,
    List<string>? Recommendations);

public record AddAssessmentResponse(int AssessmentId);

[ImpulseEndpoint("/residents/{residentId}/care-plan/assessments", ImpulseMethod.Post)]
public class AddAssessmentEndpoint : ImpulseEndpoint<AddAssessmentRequest, AddAssessmentResponse>
{
    public override Task<IImpulseResult> Handle(AddAssessmentRequest request, CancellationToken ct = default)
    {
        if (request.ResidentId <= 0 || request.ResidentId > 5)
            return Task.FromResult<IImpulseResult>(
                ImpulseResults.NotFound($"Resident {request.ResidentId} not found"));

        var errors = new Dictionary<string, string[]>();

        if (request.AssessmentDate > DateTime.Today)
            errors["assessmentDate"] = ["Assessment date cannot be in the future"];

        if (request.Findings is null or { Count: 0 })
            errors["findings"] = ["At least one finding is required"];

        if (errors.Count > 0)
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Created($"/residents/{request.ResidentId}/care-plan/assessments/42",
                new AddAssessmentResponse(42)));
    }
}
