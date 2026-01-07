using Impulse.Sample.Server.Models;

namespace Impulse.Sample.Server.Handlers;

/// <summary>
/// Handlers for resident-related endpoints.
/// </summary>
public static class ResidentHandlers
{
    // In-memory data store (for demo purposes)
    private static readonly List<ResidentDetailProps> _residents =
    [
        new(1, "Margaret Chen", "A-101", new DateTime(2024, 3, 15), ["Penicillin", "Sulfa"]),
        new(2, "Robert Williams", "A-102", new DateTime(2024, 5, 20), ["Latex"]),
        new(3, "Dorothy Johnson", "B-201", new DateTime(2023, 11, 8), []),
        new(4, "James Brown", "B-202", new DateTime(2024, 1, 12), ["Aspirin"]),
        new(5, "Patricia Davis", "C-301", new DateTime(2024, 7, 3), ["Shellfish", "Iodine"]),
    ];

    public static ResidentsListProps GetList(int page = 1, int pageSize = 10)
    {
        var skip = (page - 1) * pageSize;
        var residents = _residents
            .Skip(skip)
            .Take(pageSize)
            .Select(r => new ResidentSummary(r.Id, r.Name, r.Room))
            .ToList();

        return new ResidentsListProps(residents, _residents.Count, page, pageSize);
    }

    public static ResidentDetailProps? GetById(int id)
    {
        return _residents.FirstOrDefault(r => r.Id == id);
    }

    public static ResidentDetailProps Create(CreateResidentRequest request)
    {
        var newId = _residents.Count > 0 ? _residents.Max(r => r.Id) + 1 : 1;
        var resident = new ResidentDetailProps(
            newId,
            request.Name,
            request.Room,
            request.AdmitDate,
            request.Allergies?.ToList() ?? []
        );
        _residents.Add(resident);
        return resident;
    }

    public static ResidentDetailProps? Update(int id, UpdateResidentRequest request)
    {
        var index = _residents.FindIndex(r => r.Id == id);
        if (index < 0) return null;

        var existing = _residents[index];
        var updated = existing with
        {
            Name = request.Name,
            Room = request.Room,
            Allergies = request.Allergies.ToList()
        };
        _residents[index] = updated;
        return updated;
    }

    public static bool Delete(int id)
    {
        var index = _residents.FindIndex(r => r.Id == id);
        if (index < 0) return false;
        _residents.RemoveAt(index);
        return true;
    }
}
