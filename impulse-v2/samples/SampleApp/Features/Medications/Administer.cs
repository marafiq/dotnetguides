using Impulse.Core;

namespace SampleApp.Features.Medications;

// ========================================
// Record Administration - POST /residents/{residentId}/medications/{medicationId}/administer
// ========================================

public record RecordAdministrationRequest(
    int ResidentId,
    int MedicationId,
    DateTime AdministeredAt,
    Dosage DosageGiven,
    string? Notes,
    bool WasRefused,
    string? RefusalReason);

public record RecordAdministrationResponse(int AdministrationId);

[ImpulseEndpoint("/residents/{residentId}/medications/{medicationId}/administer", ImpulseMethod.Post)]
public class RecordAdministrationEndpoint : ImpulseEndpoint<RecordAdministrationRequest, RecordAdministrationResponse>
{
    public override Task<IImpulseResult> Handle(RecordAdministrationRequest request, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.AdministeredAt > DateTime.Now.AddMinutes(5))
            errors["administeredAt"] = ["Administration time cannot be in the future"];

        if (!request.WasRefused)
        {
            if (request.DosageGiven.Amount <= 0)
                errors["dosageGiven.amount"] = ["Dosage amount must be greater than 0"];
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.RefusalReason))
                errors["refusalReason"] = ["Refusal reason is required when medication was refused"];
        }

        if (errors.Count > 0)
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Created(
                $"/residents/{request.ResidentId}/medications/{request.MedicationId}/administrations/42",
                new RecordAdministrationResponse(42)));
    }
}
