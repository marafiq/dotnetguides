using Impulse.Core;

namespace SampleApp.Medications;

// ========================================
// Value Objects
// ========================================

public enum MedicationRoute { Oral, Topical, Subcutaneous, Intramuscular, Intravenous, Inhalation, Rectal }
public enum MedicationStatus { Active, Discontinued, OnHold, Completed }
public enum AdministrationTime { Morning, Noon, Evening, Bedtime, WithMeals, AsNeeded }

public record Dosage(
    decimal Amount,
    string Unit, // mg, ml, tablets, etc.
    string? SpecialInstructions);

public record MedicationSchedule(
    List<AdministrationTime> Times,
    string? FrequencyDescription,
    DateTime StartDate,
    DateTime? EndDate);

public record AdministrationRecord(
    int Id,
    DateTime AdministeredAt,
    string AdministeredBy,
    Dosage DosageGiven,
    string? Notes,
    bool WasRefused,
    string? RefusalReason);

// ========================================
// Medication Entity
// ========================================

public record Medication(
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
    List<AdministrationRecord>? RecentAdministrations);

// ========================================
// List Medications - GET /residents/{residentId}/medications
// ========================================

public record ListMedicationsRequest(int ResidentId, bool IncludeDiscontinued = false);

public record MedicationSummary(
    int Id,
    string DrugName,
    string DosageDescription,
    string RouteDescription,
    string ScheduleDescription,
    MedicationStatus Status);

public record ListMedicationsResponse(
    int ResidentId,
    List<MedicationSummary> Medications);

[ImpulseEndpoint("/residents/{residentId}/medications")]
public class ListMedicationsEndpoint : ImpulseEndpoint<ListMedicationsRequest, ListMedicationsResponse>
{
    public override Task<IImpulseResult> Handle(ListMedicationsRequest request, CancellationToken ct = default)
    {
        var medications = new List<MedicationSummary>
        {
            new(1, "Lisinopril", "10mg once daily", "Oral", "Morning", MedicationStatus.Active),
            new(2, "Metformin", "500mg twice daily", "Oral", "Morning & Evening", MedicationStatus.Active),
            new(3, "Aspirin", "81mg once daily", "Oral", "Morning with food", MedicationStatus.Active),
            new(4, "Tylenol", "650mg as needed", "Oral", "As needed for pain", MedicationStatus.Active)
        };

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Ok(new ListMedicationsResponse(request.ResidentId, medications)));
    }
}

// ========================================
// Get Medication Details - GET /residents/{residentId}/medications/{medicationId}
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
        // Simulated data only exists for medications 1-5 and residents 1-3
        if (request.MedicationId <= 0 || request.MedicationId > 5 || request.ResidentId > 3)
        {
            return Task.FromResult<IImpulseResult>(ImpulseResults.NotFound("Medication not found"));
        }

        var medication = new GetMedicationResponse(
            Id: request.MedicationId,
            ResidentId: request.ResidentId,
            DrugName: "Lisinopril",
            GenericName: "Lisinopril",
            PrescribedDosage: new Dosage(10, "mg", "Take with water"),
            Route: MedicationRoute.Oral,
            Schedule: new MedicationSchedule(
                [AdministrationTime.Morning],
                "Once daily",
                new DateTime(2024, 1, 1),
                null),
            Prescriber: "Dr. Johnson",
            Pharmacy: "CVS Pharmacy",
            Purpose: "Blood pressure management",
            Warnings: ["Monitor for dry cough", "Check potassium levels"],
            Status: MedicationStatus.Active,
            RecentAdministrations: [
                new(1, DateTime.Today.AddHours(8), "Nurse Williams",
                    new Dosage(10, "mg", null), null, false, null),
                new(2, DateTime.Today.AddDays(-1).AddHours(8), "Nurse Johnson",
                    new Dosage(10, "mg", null), null, false, null),
                new(3, DateTime.Today.AddDays(-2).AddHours(8), "Nurse Williams",
                    new Dosage(10, "mg", null), "Resident complained of dizziness", false, null)
            ]);

        return Task.FromResult<IImpulseResult>(ImpulseResults.Ok(medication));
    }
}

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

        if (request.AdministrationTimes.Count == 0)
            errors["administrationTimes"] = ["At least one administration time is required"];

        if (string.IsNullOrWhiteSpace(request.Prescriber))
            errors["prescriber"] = ["Prescriber is required"];

        if (request.StartDate < DateTime.Today.AddMonths(-1))
            errors["startDate"] = ["Start date cannot be more than 1 month in the past"];

        if (request.EndDate.HasValue && request.EndDate < request.StartDate)
            errors["endDate"] = ["End date must be after start date"];

        if (errors.Count > 0)
        {
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));
        }

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Created($"/residents/{request.ResidentId}/medications/42",
                new AddMedicationResponse(42)));
    }
}

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
        {
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));
        }

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Created($"/residents/{request.ResidentId}/medications/{request.MedicationId}/administrations/42",
                new RecordAdministrationResponse(42)));
    }
}
