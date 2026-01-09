namespace SampleApp.Features.Dashboard;

// ========================================
// Dashboard Types - Server-Driven Aggregations
// This demonstrates the "Impulse Way" - the server computes
// all aggregated data rather than the client
// ========================================

public record DashboardStats(
    int TotalResidents,
    int ActiveResidents,
    int HospitalizedResidents,
    int OnLeaveResidents,
    int TotalMedications,
    int MedicationsDueToday,
    int OverdueMedications,
    int ActiveCarePlans,
    int UpcomingAssessments);

public record RecentActivity(
    int Id,
    string Type, // "medication_given", "assessment_completed", "care_plan_updated", "resident_admitted"
    string Title,
    string Description,
    string ResidentName,
    int ResidentId,
    DateTime Timestamp,
    string PerformedBy);

public record UpcomingTask(
    int Id,
    string Type, // "medication", "assessment", "care_review"
    string Title,
    string Description,
    int ResidentId,
    string ResidentName,
    DateTime DueAt,
    string Priority, // "urgent", "high", "normal"
    bool IsOverdue);

public record ResidentQuickView(
    int Id,
    string FirstName,
    string LastName,
    string RoomNumber,
    string Status,
    int ActiveMedicationsCount,
    DateTime? NextMedicationDue,
    bool HasOverdueMedications,
    int OpenCareGoals);

public record MedicationComplianceData(
    int TotalAdministrations,
    int OnTimeAdministrations,
    int LateAdministrations,
    int RefusedAdministrations,
    double ComplianceRate);

public record DailyScheduleItem(
    int Id,
    string Time,
    string Type, // "medication", "meal", "activity", "appointment"
    string Title,
    string Description,
    int ResidentId,
    string ResidentName,
    string Status); // "pending", "completed", "missed"
