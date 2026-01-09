using Impulse.Core;

namespace SampleApp.Features.CarePlans;

// ========================================
// Add Care Goal - POST /residents/{residentId}/care-plan/goals
// ========================================

public record InterventionRequest(
    string Description,
    InterventionFrequency Frequency,
    string? ResponsibleRole,
    string? SpecialInstructions);

public record AddCareGoalRequest(
    int ResidentId,
    string Category,
    string Description,
    string TargetOutcome,
    DateTime TargetDate,
    List<InterventionRequest>? Interventions);

public record AddCareGoalResponse(int GoalId, int CarePlanId);

[ImpulseEndpoint("/residents/{residentId}/care-plan/goals", ImpulseMethod.Post)]
public class AddCareGoalEndpoint : ImpulseEndpoint<AddCareGoalRequest, AddCareGoalResponse>
{
    public override Task<IImpulseResult> Handle(AddCareGoalRequest request, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Category))
            errors["category"] = ["Category is required"];

        if (string.IsNullOrWhiteSpace(request.Description))
            errors["description"] = ["Description is required"];

        if (string.IsNullOrWhiteSpace(request.TargetOutcome))
            errors["targetOutcome"] = ["Target outcome is required"];

        if (request.TargetDate <= DateTime.Today)
            errors["targetDate"] = ["Target date must be in the future"];

        if (request.Interventions != null)
        {
            for (int i = 0; i < request.Interventions.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(request.Interventions[i].Description))
                    errors[$"interventions[{i}].description"] = ["Intervention description is required"];
            }
        }

        if (errors.Count > 0)
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Created($"/residents/{request.ResidentId}/care-plan/goals/42",
                new AddCareGoalResponse(42, 1)));
    }
}
