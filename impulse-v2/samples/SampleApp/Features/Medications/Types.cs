namespace SampleApp.Features.Medications;

// Shared value objects for Medications feature
public enum MedicationRoute { Oral, Topical, Subcutaneous, Intramuscular, Intravenous, Inhalation, Rectal }
public enum MedicationStatus { Active, Discontinued, OnHold, Completed }
public enum AdministrationTime { Morning, Noon, Evening, Bedtime, WithMeals, AsNeeded }

public record Dosage(
    decimal Amount,
    string Unit,
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

public record MedicationSummary(
    int Id,
    string DrugName,
    string DosageDescription,
    string RouteDescription,
    string ScheduleDescription,
    MedicationStatus Status);
