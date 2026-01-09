using Impulse.Core;

namespace SampleApp.Features.Medications;

// ========================================
// List Medications - GET /residents/{residentId}/medications
// ========================================

public record ListMedicationsRequest(int ResidentId, bool IncludeDiscontinued = false);

public record ListMedicationsResponse(int ResidentId, List<MedicationSummary> Medications);

[ImpulseEndpoint("/residents/{residentId}/medications")]
public class ListMedicationsEndpoint : ImpulseEndpoint<ListMedicationsRequest, ListMedicationsResponse>
{
    public override Task<IImpulseResult> Handle(ListMedicationsRequest request, CancellationToken ct = default)
    {
        if (request.ResidentId <= 0 || request.ResidentId > 5)
            return Task.FromResult<IImpulseResult>(
                ImpulseResults.NotFound($"Resident {request.ResidentId} not found"));

        var medications = new List<MedicationSummary>
        {
            new(1, "Lisinopril", "10mg once daily", "Oral", "Morning", MedicationStatus.Active),
            new(2, "Metformin", "500mg twice daily", "Oral", "Morning & Evening", MedicationStatus.Active),
            new(3, "Aspirin", "81mg once daily", "Oral", "Morning with food", MedicationStatus.Active),
            new(4, "Tylenol", "650mg as needed", "Oral", "As needed for pain", MedicationStatus.Active),
            new(5, "Vitamin D", "1000 IU daily", "Oral", "Morning", MedicationStatus.Active),
        };

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Ok(new ListMedicationsResponse(request.ResidentId, medications)));
    }
}
