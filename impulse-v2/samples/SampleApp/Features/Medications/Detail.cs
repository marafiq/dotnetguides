using Impulse.Core;

namespace SampleApp.Features.Medications;

// ========================================
// Get Medication - GET /residents/{residentId}/medications/{medicationId}
// ========================================

public record GetMedicationRequest(int ResidentId, int MedicationId);

public record GetMedicationResponse(
    int Id,
    int ResidentId,
    string DrugName,
    string? GenericName,
    Dosage PrescribedDosage,
    MedicationRoute Route,
    MedicationSchedule Schedule,
    string Prescriber,
    string? Pharmacy,
    string? Purpose,
    List<string>? Warnings,
    MedicationStatus Status,
    List<AdministrationRecord> RecentAdministrations);

[ImpulseEndpoint("/residents/{residentId}/medications/{medicationId}")]
public class GetMedicationEndpoint : ImpulseEndpoint<GetMedicationRequest, GetMedicationResponse>
{
    public override Task<IImpulseResult> Handle(GetMedicationRequest request, CancellationToken ct = default)
    {
        if (request.ResidentId <= 0 || request.ResidentId > 5)
            return Task.FromResult<IImpulseResult>(
                ImpulseResults.NotFound($"Resident {request.ResidentId} not found"));

        if (request.MedicationId <= 0 || request.MedicationId > 5)
            return Task.FromResult<IImpulseResult>(
                ImpulseResults.NotFound($"Medication {request.MedicationId} not found"));

        var medication = new GetMedicationResponse(
            Id: request.MedicationId,
            ResidentId: request.ResidentId,
            DrugName: "Lisinopril",
            GenericName: "Lisinopril",
            PrescribedDosage: new Dosage(10, "mg", "Take with water"),
            Route: MedicationRoute.Oral,
            Schedule: new MedicationSchedule(
                [AdministrationTime.Morning],
                "Once daily in the morning",
                new DateTime(2024, 1, 1),
                null),
            Prescriber: "Dr. Sarah Johnson, MD",
            Pharmacy: "CVS Pharmacy - Main St",
            Purpose: "Blood pressure management - Hypertension",
            Warnings: ["Monitor for dry cough", "Check potassium levels monthly", "Avoid NSAIDs"],
            Status: MedicationStatus.Active,
            RecentAdministrations: [
                new(1, DateTime.Today.AddHours(8), "Nurse Williams, RN",
                    new Dosage(10, "mg", null), null, false, null),
                new(2, DateTime.Today.AddDays(-1).AddHours(8), "Nurse Johnson, RN",
                    new Dosage(10, "mg", null), null, false, null),
                new(3, DateTime.Today.AddDays(-2).AddHours(8), "Nurse Williams, RN",
                    new Dosage(10, "mg", null), "Resident reported slight dizziness - BP 118/72", false, null)
            ]);

        return Task.FromResult<IImpulseResult>(ImpulseResults.Ok(medication));
    }
}
