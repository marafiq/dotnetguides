using Impulse.Sample.Server.Models;

namespace Impulse.Sample.Server.Handlers;

/// <summary>
/// Handlers for medication-related endpoints.
/// </summary>
public static class MedicationHandlers
{
    // In-memory data store (for demo purposes)
    private static readonly Dictionary<int, List<Medication>> _medicationsByResident = new()
    {
        [1] = [
            new(1, "Lisinopril", "10mg", MedicationFrequency.Daily, new DateTime(2024, 3, 15), null),
            new(2, "Metformin", "500mg", MedicationFrequency.TwiceDaily, new DateTime(2024, 4, 1), null),
        ],
        [2] = [
            new(3, "Atorvastatin", "20mg", MedicationFrequency.Daily, new DateTime(2024, 5, 20), null),
        ],
        [3] = [
            new(4, "Omeprazole", "20mg", MedicationFrequency.Daily, new DateTime(2023, 11, 8), null),
            new(5, "Vitamin D", "1000IU", MedicationFrequency.Daily, new DateTime(2023, 11, 8), null),
        ],
    };

    private static int _nextId = 6;

    public static MedicationsProps GetByResidentId(int residentId)
    {
        var medications = _medicationsByResident.GetValueOrDefault(residentId, []);
        return new MedicationsProps(medications);
    }

    public static Medication? Add(int residentId, AddMedicationRequest request)
    {
        if (!_medicationsByResident.ContainsKey(residentId))
        {
            _medicationsByResident[residentId] = [];
        }

        var medication = new Medication(
            _nextId++,
            request.Name,
            request.Dosage,
            request.Frequency,
            request.StartDate,
            null
        );

        _medicationsByResident[residentId].Add(medication);
        return medication;
    }

    public static bool Remove(int residentId, int medicationId)
    {
        if (!_medicationsByResident.TryGetValue(residentId, out var medications))
        {
            return false;
        }

        var index = medications.FindIndex(m => m.Id == medicationId);
        if (index < 0) return false;

        medications.RemoveAt(index);
        return true;
    }
}
