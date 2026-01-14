using Moj.TanStack.Dsl.TanStack.Router;

namespace Moj.Sandbox.Definitions.SeniorLiving;

/// <summary>
/// Route params for resident detail page.
/// </summary>
public record ResidentRouteParams
{
    public string ResidentId { get; init; } = "";
}

/// <summary>
/// Route params for room detail page.
/// </summary>
public record RoomRouteParams
{
    public string RoomId { get; init; } = "";
}

/// <summary>
/// Route params for staff detail page.
/// </summary>
public record StaffRouteParams
{
    public string StaffId { get; init; } = "";
}

/// <summary>
/// Route params for activity detail page.
/// </summary>
public record ActivityRouteParams
{
    public string ActivityId { get; init; } = "";
}

/// <summary>
/// TanStack Router definition for Senior Living application.
/// TypeScript is automatically inferred from C# types!
/// </summary>
public static class SeniorLivingRouter
{
    // ===== Route Definitions =====

    public static RouteDefinition DashboardRoute =>
        TanStackTypedRouter.Route()
            .Path("/")
            .Named("dashboardRoute")
            .Component("DashboardPage")
            .Build();

    public static RouteDefinition ResidentsListRoute =>
        TanStackTypedRouter.Route()
            .Path("/residents")
            .Named("residentsRoute")
            .Component("ResidentsListPage")
            .Build();

    public static RouteDefinition ResidentDetailRoute =>
        TanStackTypedRouter.Route<ResidentRouteParams>()
            .Path("/residents/$residentId")
            .Named("residentDetailRoute")
            .Component("ResidentDetailPage")
            .Build();

    public static RouteDefinition ResidentHealthRoute =>
        TanStackTypedRouter.Route<ResidentRouteParams>()
            .Path("/residents/$residentId/health")
            .Named("residentHealthRoute")
            .Component("ResidentHealthPage")
            .Parent("residentDetailRoute")
            .Build();

    public static RouteDefinition ResidentCarePlanRoute =>
        TanStackTypedRouter.Route<ResidentRouteParams>()
            .Path("/residents/$residentId/careplan")
            .Named("residentCarePlanRoute")
            .Component("ResidentCarePlanPage")
            .Parent("residentDetailRoute")
            .Build();

    public static RouteDefinition ResidentMedicationsRoute =>
        TanStackTypedRouter.Route<ResidentRouteParams>()
            .Path("/residents/$residentId/medications")
            .Named("residentMedicationsRoute")
            .Component("ResidentMedicationsPage")
            .Parent("residentDetailRoute")
            .Build();

    public static RouteDefinition RoomsListRoute =>
        TanStackTypedRouter.Route()
            .Path("/rooms")
            .Named("roomsRoute")
            .Component("RoomsListPage")
            .Build();

    public static RouteDefinition RoomDetailRoute =>
        TanStackTypedRouter.Route<RoomRouteParams>()
            .Path("/rooms/$roomId")
            .Named("roomDetailRoute")
            .Component("RoomDetailPage")
            .Build();

    public static RouteDefinition StaffListRoute =>
        TanStackTypedRouter.Route()
            .Path("/staff")
            .Named("staffRoute")
            .Component("StaffListPage")
            .Build();

    public static RouteDefinition StaffDetailRoute =>
        TanStackTypedRouter.Route<StaffRouteParams>()
            .Path("/staff/$staffId")
            .Named("staffDetailRoute")
            .Component("StaffDetailPage")
            .Build();

    public static RouteDefinition ActivitiesRoute =>
        TanStackTypedRouter.Route()
            .Path("/activities")
            .Named("activitiesRoute")
            .Component("ActivitiesPage")
            .Build();

    public static RouteDefinition ActivityDetailRoute =>
        TanStackTypedRouter.Route<ActivityRouteParams>()
            .Path("/activities/$activityId")
            .Named("activityDetailRoute")
            .Component("ActivityDetailPage")
            .Build();

    public static RouteDefinition AlertsRoute =>
        TanStackTypedRouter.Route()
            .Path("/alerts")
            .Named("alertsRoute")
            .Component("AlertsPage")
            .Build();

    public static RouteDefinition SettingsRoute =>
        TanStackTypedRouter.Route()
            .Path("/settings")
            .Named("settingsRoute")
            .Component("SettingsPage")
            .Build();

    // ===== Complete Router Definition =====

    public static RouterDefinition Definition =>
        TanStackTypedRouter.Router()
            .RouterName("seniorLivingRouter")
            .RootRoute("rootRoute")
            .RootComponent("RootLayout")
            .DefaultPreload("intent")
            .DefaultPreloadDelay(100)
            .AddRoute(DashboardRoute)
            .AddRoute(ResidentsListRoute)
            .AddRoute(ResidentDetailRoute)
            .AddRoute(ResidentHealthRoute)
            .AddRoute(ResidentCarePlanRoute)
            .AddRoute(ResidentMedicationsRoute)
            .AddRoute(RoomsListRoute)
            .AddRoute(RoomDetailRoute)
            .AddRoute(StaffListRoute)
            .AddRoute(StaffDetailRoute)
            .AddRoute(ActivitiesRoute)
            .AddRoute(ActivityDetailRoute)
            .AddRoute(AlertsRoute)
            .AddRoute(SettingsRoute)
            .Build();
}
