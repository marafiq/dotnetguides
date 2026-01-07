using Impulse.CodeGen;

namespace Impulse.Sample.Server.Models;

/// <summary>
/// Request to add a new medication.
/// </summary>
[ImpulseRequest]
public record AddMedicationRequest(
    string Name,
    string Dosage,
    MedicationFrequency Frequency,
    DateTime StartDate
);

/// <summary>
/// Request to update a resident.
/// </summary>
[ImpulseRequest]
public record UpdateResidentRequest(
    string Name,
    string Room,
    IReadOnlyList<string> Allergies
);

/// <summary>
/// Request to create a new resident.
/// </summary>
[ImpulseRequest]
public record CreateResidentRequest(
    string Name,
    string Room,
    DateTime AdmitDate,
    IReadOnlyList<string>? Allergies
);
