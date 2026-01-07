using Impulse.Sample.Server.Models;

namespace Impulse.Sample.Server.Handlers;

/// <summary>
/// Handlers for dashboard-related endpoints.
/// </summary>
public static class DashboardHandlers
{
    public static DashboardProps Get()
    {
        return new DashboardProps(
            TotalResidents: 5,
            TotalMedications: 12,
            PendingTasks: 3,
            RecentActivities:
            [
                new("Added medication for Margaret Chen", DateTime.Now.AddHours(-2), "Sarah"),
                new("Updated room assignment for Robert Williams", DateTime.Now.AddHours(-5), "Mike"),
                new("New resident admitted: Patricia Davis", DateTime.Now.AddDays(-1), "Sarah"),
            ]
        );
    }
}
