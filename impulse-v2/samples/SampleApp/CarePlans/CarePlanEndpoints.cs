using Impulse.Core;

namespace SampleApp.CarePlans;

// ========================================
// Value Objects
// ========================================

public enum CareGoalStatus { Active, Completed, OnHold, Cancelled }
public enum AssessmentType { Initial, Quarterly, Annual, ChangeInCondition }
public enum InterventionFrequency { AsNeeded, Daily, BID, TID, QID, Weekly }

public record Assessment(
    int Id,
    AssessmentType Type,
    DateTime AssessmentDate,
    string AssessorName,
    Dictionary<string, string> Findings,
    List<string> Recommendations);

public record CareGoal(
    int Id,
    string Category, // ADL, Nutrition, Mobility, Cognitive, Social
    string Description,
    string TargetOutcome,
    DateTime TargetDate,
    CareGoalStatus Status,
    List<Intervention> Interventions);

public record Intervention(
    int Id,
    string Description,
    InterventionFrequency Frequency,
    string? ResponsibleRole,
    string? SpecialInstructions,
    bool RequiresDocumentation);

// ========================================
// Care Plan Entity
// ========================================

public record CarePlan(
    int Id,
    int ResidentId,
    string ResidentName,
    DateTime EffectiveDate,
    DateTime? ReviewDate,
    List<Assessment> Assessments,
    List<CareGoal> Goals,
    string Status, // Draft, Active, UnderReview, Archived
    string? Notes);

// ========================================
// Get Care Plan - GET /residents/{residentId}/care-plan
// ========================================

public record GetCarePlanRequest(int ResidentId);

public record GetCarePlanResponse(
    int Id,
    int ResidentId,
    string ResidentName,
    DateTime EffectiveDate,
    DateTime? ReviewDate,
    List<Assessment> Assessments,
    List<CareGoal> Goals,
    string Status,
    string? Notes);

[ImpulseEndpoint("/residents/{residentId}/care-plan")]
public class GetCarePlanEndpoint : ImpulseEndpoint<GetCarePlanRequest, GetCarePlanResponse>
{
    public override Task<IImpulseResult> Handle(GetCarePlanRequest request, CancellationToken ct = default)
    {
        if (request.ResidentId <= 0)
        {
            return Task.FromResult<IImpulseResult>(ImpulseResults.NotFound("Care plan not found"));
        }

        // Complex nested structure
        var carePlan = new GetCarePlanResponse(
            Id: 1,
            ResidentId: request.ResidentId,
            ResidentName: "John Smith",
            EffectiveDate: new DateTime(2024, 1, 1),
            ReviewDate: new DateTime(2024, 4, 1),
            Assessments: [
                new(1, AssessmentType.Initial, new DateTime(2024, 1, 1), "Dr. Johnson",
                    new Dictionary<string, string>
                    {
                        ["Mobility"] = "Requires walker for ambulation",
                        ["Cognition"] = "Mild cognitive impairment",
                        ["Nutrition"] = "Good appetite, follows low-sodium diet"
                    },
                    ["Physical therapy 3x/week", "Monitor weight weekly"]),
                new(2, AssessmentType.Quarterly, new DateTime(2024, 4, 1), "Nurse Williams",
                    new Dictionary<string, string>
                    {
                        ["Mobility"] = "Improved stability with walker",
                        ["Cognition"] = "Stable",
                        ["Nutrition"] = "Maintaining weight"
                    },
                    ["Continue current interventions"])
            ],
            Goals: [
                new(1, "Mobility", "Improve walking endurance",
                    "Walk 100 feet with walker independently",
                    new DateTime(2024, 6, 1),
                    CareGoalStatus.Active,
                    Interventions: [
                        new(1, "Assisted walking exercises", InterventionFrequency.Daily,
                            "CNA", "Use gait belt", true),
                        new(2, "Physical therapy session", InterventionFrequency.TID,
                            "PT", null, true)
                    ]),
                new(2, "Nutrition", "Maintain current weight",
                    "Weight within 5% of baseline",
                    new DateTime(2024, 12, 31),
                    CareGoalStatus.Active,
                    Interventions: [
                        new(3, "Weekly weight monitoring", InterventionFrequency.Weekly,
                            "Nurse", "Document in chart", true),
                        new(4, "Dietary consultation", InterventionFrequency.AsNeeded,
                            "Dietitian", null, false)
                    ])
            ],
            Status: "Active",
            Notes: "Resident is engaged and cooperative with care plan goals.");

        return Task.FromResult<IImpulseResult>(ImpulseResults.Ok(carePlan));
    }
}

// ========================================
// Add Care Goal - POST /residents/{residentId}/care-plan/goals
// ========================================

public record AddCareGoalRequest(
    int ResidentId,
    string Category,
    string Description,
    string TargetOutcome,
    DateTime TargetDate,
    List<InterventionRequest>? Interventions);

public record InterventionRequest(
    string Description,
    InterventionFrequency Frequency,
    string? ResponsibleRole,
    string? SpecialInstructions);

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

        // Validate nested interventions
        if (request.Interventions != null)
        {
            for (int i = 0; i < request.Interventions.Count; i++)
            {
                var intervention = request.Interventions[i];
                if (string.IsNullOrWhiteSpace(intervention.Description))
                    errors[$"interventions[{i}].description"] = ["Intervention description is required"];
            }
        }

        if (errors.Count > 0)
        {
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));
        }

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Created($"/residents/{request.ResidentId}/care-plan/goals/42",
                new AddCareGoalResponse(42, 1)));
    }
}

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
        var errors = new Dictionary<string, string[]>();

        if (request.AssessmentDate > DateTime.Today)
            errors["assessmentDate"] = ["Assessment date cannot be in the future"];

        if (request.Findings.Count == 0)
            errors["findings"] = ["At least one finding is required"];

        if (errors.Count > 0)
        {
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));
        }

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Created($"/residents/{request.ResidentId}/care-plan/assessments/42",
                new AddAssessmentResponse(42)));
    }
}
