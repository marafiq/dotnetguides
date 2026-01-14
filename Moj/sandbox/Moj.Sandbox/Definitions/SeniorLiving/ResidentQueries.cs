using Moj.TanStack.Dsl.TanStack.Query;
using Moj.Sandbox.DomainModels.SeniorLiving;

namespace Moj.Sandbox.Definitions.SeniorLiving;

/// <summary>
/// TanStack Query definitions for Senior Living Resident Management.
/// TypeScript is automatically inferred from C# types!
/// </summary>
public static class ResidentQueries
{
    /// <summary>
    /// Query to fetch all residents.
    /// </summary>
    public static QueryDefinition<List<Resident>> AllResidents =>
        TanStackTypedQuery.Query<List<Resident>>()
            .Named("residents")
            .Key("residents", "list")
            .Endpoint("/api/residents")
            .StaleTime(30000) // 30 seconds
            .Build();

    /// <summary>
    /// Query to fetch a single resident by ID.
    /// </summary>
    public static QueryDefinition<Resident> ResidentById =>
        TanStackTypedQuery.Query<Resident>()
            .Named("resident")
            .Key("residents", "detail")
            .Endpoint("/api/residents/{id}")
            .StaleTime(60000) // 1 minute
            .Build();

    /// <summary>
    /// Query to fetch all rooms.
    /// </summary>
    public static QueryDefinition<List<Room>> AllRooms =>
        TanStackTypedQuery.Query<List<Room>>()
            .Named("rooms")
            .Key("rooms", "list")
            .Endpoint("/api/rooms")
            .StaleTime(300000) // 5 minutes (rooms don't change often)
            .Build();

    /// <summary>
    /// Query to fetch available rooms only.
    /// </summary>
    public static QueryDefinition<List<Room>> AvailableRooms =>
        TanStackTypedQuery.Query<List<Room>>()
            .Named("availableRooms")
            .Key("rooms", "available")
            .Endpoint("/api/rooms?available=true")
            .StaleTime(60000)
            .Build();

    /// <summary>
    /// Query to fetch all staff members.
    /// </summary>
    public static QueryDefinition<List<StaffMember>> AllStaff =>
        TanStackTypedQuery.Query<List<StaffMember>>()
            .Named("staff")
            .Key("staff", "list")
            .Endpoint("/api/staff")
            .StaleTime(120000) // 2 minutes
            .Build();

    /// <summary>
    /// Query to fetch on-duty staff.
    /// </summary>
    public static QueryDefinition<List<StaffMember>> OnDutyStaff =>
        TanStackTypedQuery.Query<List<StaffMember>>()
            .Named("onDutyStaff")
            .Key("staff", "on-duty")
            .Endpoint("/api/staff?onDuty=true")
            .StaleTime(30000)
            .RefetchInterval(60000) // Refresh every minute
            .Build();

    /// <summary>
    /// Query to fetch facility activities.
    /// </summary>
    public static QueryDefinition<List<FacilityActivity>> AllActivities =>
        TanStackTypedQuery.Query<List<FacilityActivity>>()
            .Named("activities")
            .Key("activities", "list")
            .Endpoint("/api/activities")
            .StaleTime(120000)
            .Build();

    /// <summary>
    /// Query to fetch today's activities.
    /// </summary>
    public static QueryDefinition<List<FacilityActivity>> TodaysActivities =>
        TanStackTypedQuery.Query<List<FacilityActivity>>()
            .Named("todaysActivities")
            .Key("activities", "today")
            .Endpoint("/api/activities/today")
            .StaleTime(60000)
            .Build();

    /// <summary>
    /// Query to fetch unacknowledged alerts.
    /// </summary>
    public static QueryDefinition<List<ResidentAlert>> UnacknowledgedAlerts =>
        TanStackTypedQuery.Query<List<ResidentAlert>>()
            .Named("alerts")
            .Key("alerts", "unacknowledged")
            .Endpoint("/api/alerts?acknowledged=false")
            .StaleTime(15000) // 15 seconds (critical data)
            .RefetchInterval(30000) // Refresh every 30 seconds
            .Build();

    /// <summary>
    /// Query to fetch resident's health record.
    /// </summary>
    public static QueryDefinition<HealthRecord> ResidentHealth =>
        TanStackTypedQuery.Query<HealthRecord>()
            .Named("residentHealth")
            .Key("residents", "health")
            .Endpoint("/api/residents/{id}/health")
            .StaleTime(60000)
            .Build();

    /// <summary>
    /// Query to fetch resident's care plan.
    /// </summary>
    public static QueryDefinition<CarePlan> ResidentCarePlan =>
        TanStackTypedQuery.Query<CarePlan>()
            .Named("residentCarePlan")
            .Key("residents", "careplan")
            .Endpoint("/api/residents/{id}/careplan")
            .StaleTime(120000)
            .Build();

    // ===== Mutations =====

    /// <summary>
    /// Mutation to create a new resident.
    /// </summary>
    public static MutationDefinition<Resident, Resident> CreateResident =>
        TanStackTypedQuery.Mutation<Resident, Resident>()
            .Named("createResident")
            .Key("residents", "create")
            .Endpoint("/api/residents")
            .Method("POST")
            .Build();

    /// <summary>
    /// Mutation to update a resident.
    /// </summary>
    public static MutationDefinition<Resident, Resident> UpdateResident =>
        TanStackTypedQuery.Mutation<Resident, Resident>()
            .Named("updateResident")
            .Key("residents", "update")
            .Endpoint("/api/residents/{id}")
            .Method("PUT")
            .Build();

    /// <summary>
    /// Mutation to acknowledge an alert.
    /// </summary>
    public static MutationDefinition<ResidentAlert, ResidentAlert> AcknowledgeAlert =>
        TanStackTypedQuery.Mutation<ResidentAlert, ResidentAlert>()
            .Named("acknowledgeAlert")
            .Key("alerts", "acknowledge")
            .Endpoint("/api/alerts/{id}/acknowledge")
            .Method("POST")
            .Build();

    /// <summary>
    /// Combined query definitions for generating a single file.
    /// </summary>
    public static QueryDefinitions AllDefinitions =>
        TanStackTypedQuery.Queries()
            .Add(AllResidents)
            .Add(ResidentById)
            .Add(AllRooms)
            .Add(AvailableRooms)
            .Add(AllStaff)
            .Add(OnDutyStaff)
            .Add(AllActivities)
            .Add(TodaysActivities)
            .Add(UnacknowledgedAlerts)
            .Add(ResidentHealth)
            .Add(ResidentCarePlan)
            .Add(CreateResident)
            .Add(UpdateResident)
            .Add(AcknowledgeAlert)
            .Build();
}
