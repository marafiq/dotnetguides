namespace Impulse.IntegrationTests.Features.Residents;

public static class Handler
{
    private static readonly List<ResidentDetailProps> _residents =
    [
        new(1, "Margaret Chen", "A-101", new DateTime(2024, 3, 15), ["Penicillin"]),
        new(2, "Robert Williams", "A-102", new DateTime(2024, 5, 20), ["Latex"]),
        new(3, "Dorothy Johnson", "B-201", new DateTime(2023, 11, 8), []),
    ];

    private static readonly Dictionary<int, List<Medication>> _medications = new()
    {
        [1] = [new(1, "Lisinopril", "10mg", "Daily"), new(2, "Metformin", "500mg", "Twice daily")],
        [2] = [new(3, "Atorvastatin", "20mg", "Daily")],
        [3] = [],
    };

    public static ResidentsListProps GetList() => new(
        Residents: _residents.Select(r => new ResidentSummary(r.Id, r.Name, r.Room)).ToList(),
        TotalCount: _residents.Count
    );

    public static ResidentDetailProps? GetById(int id) =>
        _residents.FirstOrDefault(r => r.Id == id);

    public static MedicationsProps GetMedications(int residentId) =>
        new(_medications.GetValueOrDefault(residentId, []));
}
