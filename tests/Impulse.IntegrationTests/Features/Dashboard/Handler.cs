namespace Impulse.IntegrationTests.Features.Dashboard;

public static class Handler
{
    public static DashboardProps Get() => new(
        TotalResidents: 5,
        TotalMedications: 12,
        PendingTasks: 3
    );
}
