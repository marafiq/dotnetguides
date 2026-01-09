using Impulse.Core;

namespace SampleApp.Features.Dashboard;

// ========================================
// Dashboard Overview - GET /dashboard
// Server-driven aggregation - all data computed server-side
// This is the "Impulse Way" - no client-side data fetching
// ========================================

public record GetDashboardResponse(
    DashboardStats Stats,
    List<RecentActivity> RecentActivities,
    List<UpcomingTask> UpcomingTasks,
    List<ResidentQuickView> ResidentsNeedingAttention,
    MedicationComplianceData MedicationCompliance,
    List<DailyScheduleItem> TodaysSchedule);

[ImpulseEndpoint("/dashboard")]
public class GetDashboardEndpoint : ImpulseEndpoint<GetDashboardResponse>
{
    public override Task<IImpulseResult> Handle(CancellationToken ct = default)
    {
        // In real app, this aggregates from database
        // Key point: ALL aggregation happens server-side
        var stats = new DashboardStats(
            TotalResidents: 5,
            ActiveResidents: 4,
            HospitalizedResidents: 0,
            OnLeaveResidents: 1,
            TotalMedications: 23,
            MedicationsDueToday: 8,
            OverdueMedications: 1,
            ActiveCarePlans: 5,
            UpcomingAssessments: 3
        );

        var recentActivities = new List<RecentActivity>
        {
            new(1, "medication_given", "Medication Administered",
                "Lisinopril 10mg given as scheduled",
                "John Smith", 1, DateTime.Now.AddMinutes(-30), "Nurse Sarah"),
            new(2, "assessment_completed", "Quarterly Assessment",
                "Completed quarterly health assessment",
                "Mary Johnson", 2, DateTime.Now.AddHours(-2), "Dr. Williams"),
            new(3, "care_plan_updated", "Care Plan Updated",
                "Fall prevention interventions added",
                "Robert Williams", 3, DateTime.Now.AddHours(-4), "Care Coordinator"),
            new(4, "medication_given", "PRN Medication",
                "Acetaminophen 650mg for headache",
                "Dorothy Brown", 4, DateTime.Now.AddHours(-5), "Nurse Mike"),
            new(5, "resident_admitted", "New Admission",
                "Admitted from Memorial Hospital",
                "James Davis", 5, DateTime.Now.AddDays(-1), "Admissions"),
        };

        var upcomingTasks = new List<UpcomingTask>
        {
            new(1, "medication", "Metformin 500mg",
                "Morning dose with breakfast",
                1, "John Smith", DateTime.Now.AddMinutes(30), "normal", false),
            new(2, "medication", "Amlodipine 5mg",
                "Evening dose - overdue by 15 min",
                2, "Mary Johnson", DateTime.Now.AddMinutes(-15), "urgent", true),
            new(3, "assessment", "Annual Assessment Due",
                "Comprehensive annual health review",
                3, "Robert Williams", DateTime.Now.AddDays(2), "high", false),
            new(4, "care_review", "Care Plan Review",
                "Monthly progress evaluation",
                4, "Dorothy Brown", DateTime.Now.AddDays(3), "normal", false),
            new(5, "medication", "Furosemide 40mg",
                "Afternoon dose",
                5, "James Davis", DateTime.Now.AddHours(2), "normal", false),
        };

        var residentsNeedingAttention = new List<ResidentQuickView>
        {
            new(2, "Mary", "Johnson", "102B", "Active", 6, DateTime.Now.AddMinutes(-15), true, 2),
            new(3, "Robert", "Williams", "103A", "Active", 4, DateTime.Now.AddHours(1), false, 3),
            new(1, "John", "Smith", "101A", "Active", 5, DateTime.Now.AddMinutes(30), false, 1),
        };

        var compliance = new MedicationComplianceData(
            TotalAdministrations: 156,
            OnTimeAdministrations: 142,
            LateAdministrations: 11,
            RefusedAdministrations: 3,
            ComplianceRate: 91.0
        );

        var schedule = new List<DailyScheduleItem>
        {
            new(1, "6:00 AM", "medication", "Morning Medications",
                "5 residents - 12 medications", 0, "", "completed"),
            new(2, "7:30 AM", "meal", "Breakfast",
                "All residents", 0, "", "completed"),
            new(3, "9:00 AM", "activity", "Physical Therapy",
                "Group session", 0, "", "completed"),
            new(4, "10:00 AM", "medication", "Mid-Morning Medications",
                "3 residents - 4 medications", 0, "", "pending"),
            new(5, "12:00 PM", "meal", "Lunch",
                "All residents", 0, "", "pending"),
            new(6, "2:00 PM", "appointment", "Dr. Smith Visit",
                "Scheduled checkup", 1, "John Smith", "pending"),
            new(7, "3:00 PM", "activity", "Social Hour",
                "Recreation room", 0, "", "pending"),
            new(8, "5:00 PM", "medication", "Evening Medications",
                "4 residents - 8 medications", 0, "", "pending"),
            new(9, "6:00 PM", "meal", "Dinner",
                "All residents", 0, "", "pending"),
            new(10, "9:00 PM", "medication", "Bedtime Medications",
                "5 residents - 6 medications", 0, "", "pending"),
        };

        var response = new GetDashboardResponse(
            stats,
            recentActivities,
            upcomingTasks,
            residentsNeedingAttention,
            compliance,
            schedule
        );

        return Task.FromResult<IImpulseResult>(ImpulseResults.Ok(response));
    }
}
