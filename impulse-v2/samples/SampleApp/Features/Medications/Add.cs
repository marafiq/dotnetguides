using Impulse.Core;

namespace SampleApp.Features.Medications;

// ========================================
// Add Medication - POST /residents/{residentId}/medications
// ========================================

public record AddMedicationRequest(
    int ResidentId,
    string DrugName,
    string? GenericName,
    Dosage PrescribedDosage,
    MedicationRoute Route,
    List<AdministrationTime> AdministrationTimes,
    string? FrequencyDescription,
    DateTime StartDate,
    DateTime? EndDate,
    string Prescriber,
    string? Pharmacy,
    string? Purpose,
    List<string>? Warnings);

public record AddMedicationResponse(int MedicationId);

[ImpulseEndpoint("/residents/{residentId}/medications", ImpulseMethod.Post)]
public class AddMedicationEndpoint : ImpulseEndpoint<AddMedicationRequest, AddMedicationResponse>
{
    public override Task<IImpulseResult> Handle(AddMedicationRequest request, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.DrugName))
            errors["drugName"] = ["Drug name is required"];

        if (request.PrescribedDosage.Amount <= 0)
            errors["prescribedDosage.amount"] = ["Dosage amount must be greater than 0"];

        if (string.IsNullOrWhiteSpace(request.PrescribedDosage.Unit))
            errors["prescribedDosage.unit"] = ["Dosage unit is required"];

        if (request.AdministrationTimes is null or { Count: 0 })
            errors["administrationTimes"] = ["At least one administration time is required"];

        if (string.IsNullOrWhiteSpace(request.Prescriber))
            errors["prescriber"] = ["Prescriber is required"];

        if (request.EndDate.HasValue && request.EndDate < request.StartDate)
            errors["endDate"] = ["End date must be after start date"];

        if (errors.Count > 0)
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Created($"/residents/{request.ResidentId}/medications/42",
                new AddMedicationResponse(42)));
    }
}
