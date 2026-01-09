using Impulse.Core;

namespace SampleApp.Features.CarePlans;

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
        if (request.ResidentId <= 0 || request.ResidentId > 5)
            return Task.FromResult<IImpulseResult>(
                ImpulseResults.NotFound($"Care plan for resident {request.ResidentId} not found"));

        var carePlan = new GetCarePlanResponse(
            Id: 1,
            ResidentId: request.ResidentId,
            ResidentName: "John Smith",
            EffectiveDate: new DateTime(2024, 1, 1),
            ReviewDate: new DateTime(2024, 4, 1),
            Assessments: [
                new(1, AssessmentType.Initial, new DateTime(2024, 1, 1), "Dr. Sarah Johnson, MD",
                    new Dictionary<string, string>
                    {
                        ["Mobility"] = "Requires walker for ambulation. Steady gait with device.",
                        ["Cognition"] = "Mild cognitive impairment. Alert and oriented x3.",
                        ["Nutrition"] = "Good appetite. Follows low-sodium diet. Weight stable.",
                        ["ADLs"] = "Independent with eating. Requires assistance with bathing and dressing."
                    },
                    ["Physical therapy 3x/week", "Monitor weight weekly", "Fall risk precautions"]),
                new(2, AssessmentType.Quarterly, new DateTime(2024, 4, 1), "Nurse Williams, RN",
                    new Dictionary<string, string>
                    {
                        ["Mobility"] = "Improved stability with walker. Walking distance increased.",
                        ["Cognition"] = "Stable. Engaged in activities.",
                        ["Nutrition"] = "Maintaining weight at 165 lbs.",
                        ["ADLs"] = "Progressing with self-care tasks."
                    },
                    ["Continue current interventions", "Consider reducing PT to 2x/week"])
            ],
            Goals: [
                new(1, "Mobility", "Improve walking endurance and balance",
                    "Walk 100 feet with walker independently without rest breaks",
                    new DateTime(2024, 6, 1),
                    CareGoalStatus.Active,
                    [
                        new(1, "Assisted walking exercises in hallway", InterventionFrequency.Daily,
                            "CNA", "Use gait belt. Monitor for fatigue.", true),
                        new(2, "Physical therapy session", InterventionFrequency.TID,
                            "Physical Therapist", "Focus on balance and strength exercises", true),
                        new(3, "Range of motion exercises", InterventionFrequency.BID,
                            "CNA", "Gentle stretching - do not force", true)
                    ]),
                new(2, "Nutrition", "Maintain optimal nutritional status",
                    "Weight within 5% of baseline (165 lbs)",
                    new DateTime(2024, 12, 31),
                    CareGoalStatus.Active,
                    [
                        new(4, "Weekly weight monitoring", InterventionFrequency.Weekly,
                            "Nurse", "Document in chart. Report >3 lb change.", true),
                        new(5, "Dietary consultation as needed", InterventionFrequency.AsNeeded,
                            "Registered Dietitian", "For appetite changes or weight loss", false),
                        new(6, "Ensure adequate fluid intake", InterventionFrequency.Daily,
                            "CNA", "Encourage 6-8 glasses water daily", true)
                    ]),
                new(3, "Safety", "Prevent falls and injuries",
                    "Zero falls during care plan period",
                    new DateTime(2024, 12, 31),
                    CareGoalStatus.Active,
                    [
                        new(7, "Fall risk assessment", InterventionFrequency.Weekly,
                            "Nurse", "Use Morse Fall Scale", true),
                        new(8, "Environmental safety check", InterventionFrequency.Daily,
                            "CNA", "Clear pathways, adequate lighting, call light within reach", true)
                    ])
            ],
            Status: "Active",
            Notes: "Resident is engaged and cooperative with care plan goals. Family visits weekly and is involved in care decisions.");

        return Task.FromResult<IImpulseResult>(ImpulseResults.Ok(carePlan));
    }
}
