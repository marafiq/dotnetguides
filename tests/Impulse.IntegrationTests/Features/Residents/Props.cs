namespace Impulse.IntegrationTests.Features.Residents;

public record ResidentsListProps(
    IReadOnlyList<ResidentSummary> Residents,
    int TotalCount
);

public record ResidentSummary(int Id, string Name, string Room);

public record ResidentDetailProps(
    int Id,
    string Name,
    string Room,
    DateTime AdmitDate,
    IReadOnlyList<string> Allergies
);

public record MedicationsProps(IReadOnlyList<Medication> Medications);

public record Medication(
    int Id,
    string Name,
    string Dosage,
    string Frequency
);
