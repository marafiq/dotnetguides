using Impulse.CodeGen;

namespace Impulse.Sample.Server.Models;

/// <summary>
/// Props for the resident detail component.
/// </summary>
[ImpulseProps]
public record ResidentDetailProps(
    int Id,
    string Name,
    string Room,
    DateTime AdmitDate,
    IReadOnlyList<string> Allergies
);

/// <summary>
/// Props for the residents list component.
/// </summary>
[ImpulseProps]
public record ResidentsListProps(
    IReadOnlyList<ResidentSummary> Residents,
    int TotalCount,
    int Page,
    int PageSize
);

/// <summary>
/// Summary of a resident for list views.
/// </summary>
public record ResidentSummary(
    int Id,
    string Name,
    string Room
);

/// <summary>
/// Props for medications list.
/// </summary>
[ImpulseProps]
public record MedicationsProps(
    IReadOnlyList<Medication> Medications
);

/// <summary>
/// A medication record.
/// </summary>
public record Medication(
    int Id,
    string Name,
    string Dosage,
    MedicationFrequency Frequency,
    DateTime StartDate,
    DateTime? EndDate
);

/// <summary>
/// Frequency of medication administration.
/// </summary>
public enum MedicationFrequency
{
    Daily,
    TwiceDaily,
    ThreeTimesDaily,
    AsNeeded,
    Weekly
}

/// <summary>
/// Props for documents list.
/// </summary>
[ImpulseProps]
public record DocumentsProps(
    IReadOnlyList<Document> Documents
);

/// <summary>
/// A document record.
/// </summary>
public record Document(
    int Id,
    string Name,
    string Type,
    DateTime UploadDate,
    long SizeBytes
);

/// <summary>
/// Props for the dashboard component.
/// </summary>
[ImpulseProps]
public record DashboardProps(
    int TotalResidents,
    int TotalMedications,
    int PendingTasks,
    IReadOnlyList<RecentActivity> RecentActivities
);

/// <summary>
/// Recent activity item.
/// </summary>
public record RecentActivity(
    string Description,
    DateTime Timestamp,
    string UserName
);
