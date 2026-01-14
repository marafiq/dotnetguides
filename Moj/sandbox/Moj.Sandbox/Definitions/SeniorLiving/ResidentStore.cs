using Moj.TanStack.Dsl.TanStack.Store;
using Moj.Sandbox.DomainModels.SeniorLiving;

namespace Moj.Sandbox.Definitions.SeniorLiving;

/// <summary>
/// TanStack Store definition for Senior Living Resident Management.
/// TypeScript is automatically inferred from C# types - no manual TS construction needed!
/// </summary>
public static class ResidentStore
{
    /// <summary>
    /// The complete store definition - generates TypeScript interfaces, enums, store, actions, and selectors.
    /// </summary>
    public static StoreDefinition<ResidentAppState> Definition =>
        TanStackTypedStore.Store(new ResidentAppState
        {
            Residents = [],
            SelectedResident = null,
            Rooms = [],
            Staff = [],
            Activities = [],
            UnacknowledgedAlerts = [],
            CurrentUser = null,
            IsLoading = false,
            Error = null,
            Filters = new ResidentFilters
            {
                Statuses = [],
                CareLevels = [],
                Building = null,
                Floor = null,
                SearchQuery = null,
                AssignedStaffId = null,
                HasActiveAlerts = null
            },
            Ui = new ResidentUiState
            {
                SidebarOpen = true,
                ActiveTab = "overview",
                ViewMode = "list",
                Sort = new ResidentSortConfig
                {
                    Field = "lastName",
                    Direction = "asc"
                },
                Pagination = new PaginationState
                {
                    Page = 1,
                    PageSize = 25,
                    TotalItems = 0,
                    TotalPages = 0
                },
                ShowAlertPanel = false,
                ShowActivityCalendar = false
            }
        })
        .As("residentStore")

        // ===== Resident Actions =====
        .Action<List<Resident>>("setResidents",
            (state, residents) => state with { Residents = residents, IsLoading = false })

        .Action<Resident>("addResident",
            (state, resident) => state with { Residents = [.. state.Residents, resident] })

        .Action<Resident>("updateResident",
            (state, updated) => state with
            {
                Residents = state.Residents.Select(r => r.Id == updated.Id ? updated : r).ToList(),
                SelectedResident = state.SelectedResident?.Id == updated.Id ? updated : state.SelectedResident
            })

        .Action<string>("removeResident",
            (state, id) => state with
            {
                Residents = state.Residents.Where(r => r.Id != id).ToList(),
                SelectedResident = state.SelectedResident?.Id == id ? null : state.SelectedResident
            })

        .Action<Resident?>("selectResident",
            (state, resident) => state with { SelectedResident = resident })

        // ===== Loading & Error Actions =====
        .Action("startLoading",
            state => state with { IsLoading = true, Error = null })

        .Action<string>("setError",
            (state, error) => state with { Error = error, IsLoading = false })

        .Action("clearError",
            state => state with { Error = null })

        // ===== Filter Actions =====
        .Action<ResidentFilters>("setFilters",
            (state, filters) => state with { Filters = filters })

        .Action<List<ResidentStatus>>("setStatusFilter",
            (state, statuses) => state with
            {
                Filters = state.Filters with { Statuses = statuses }
            })

        .Action<List<CareLevel>>("setCareLevelFilter",
            (state, careLevels) => state with
            {
                Filters = state.Filters with { CareLevels = careLevels }
            })

        .Action<string?>("setSearchQuery",
            (state, query) => state with
            {
                Filters = state.Filters with { SearchQuery = query }
            })

        .Action("clearFilters",
            state => state with
            {
                Filters = new ResidentFilters
                {
                    Statuses = [],
                    CareLevels = [],
                    Building = null,
                    Floor = null,
                    SearchQuery = null,
                    AssignedStaffId = null,
                    HasActiveAlerts = null
                }
            })

        // ===== UI Actions =====
        .Action("toggleSidebar",
            state => state with
            {
                Ui = state.Ui with { SidebarOpen = !state.Ui.SidebarOpen }
            })

        .Action<string>("setActiveTab",
            (state, tab) => state with
            {
                Ui = state.Ui with { ActiveTab = tab }
            })

        .Action<string>("setViewMode",
            (state, mode) => state with
            {
                Ui = state.Ui with { ViewMode = mode }
            })

        .Action<int>("setPage",
            (state, page) => state with
            {
                Ui = state.Ui with
                {
                    Pagination = state.Ui.Pagination with { Page = page }
                }
            })

        .Action("toggleAlertPanel",
            state => state with
            {
                Ui = state.Ui with { ShowAlertPanel = !state.Ui.ShowAlertPanel }
            })

        .Action("toggleActivityCalendar",
            state => state with
            {
                Ui = state.Ui with { ShowActivityCalendar = !state.Ui.ShowActivityCalendar }
            })

        // ===== Alert Actions =====
        .Action<ResidentAlert>("addAlert",
            (state, alert) => state with
            {
                UnacknowledgedAlerts = [.. state.UnacknowledgedAlerts, alert]
            })

        .Action<string>("acknowledgeAlert",
            (state, alertId) => state with
            {
                UnacknowledgedAlerts = state.UnacknowledgedAlerts.Where(a => a.Id != alertId).ToList()
            })

        // ===== Staff & Room Actions =====
        .Action<List<StaffMember>>("setStaff",
            (state, staff) => state with { Staff = staff })

        .Action<List<Room>>("setRooms",
            (state, rooms) => state with { Rooms = rooms })

        .Action<List<FacilityActivity>>("setActivities",
            (state, activities) => state with { Activities = activities })

        .Action<StaffMember?>("setCurrentUser",
            (state, user) => state with { CurrentUser = user })

        // ===== Selectors =====
        .Selector("totalResidents",
            state => state.Residents.Count)

        .Selector("activeResidents",
            state => state.Residents.Where(r => r.Status == ResidentStatus.Active).ToList())

        .Selector("criticalAlerts",
            state => state.UnacknowledgedAlerts.Where(a => a.Severity == AlertSeverity.Critical).ToList())

        .Selector("availableRooms",
            state => state.Rooms.Where(r => r.IsAvailable).ToList())

        .Selector("onDutyStaff",
            state => state.Staff.Where(s => s.IsOnDuty).ToList())

        .Selector("hasUnacknowledgedAlerts",
            state => state.UnacknowledgedAlerts.Count > 0)

        // ===== Derived (Computed/Reactive) State =====
        // These use TanStack Store's derived() - they reactively update when store changes
        .Derived<int>("filteredResidentsCount$",
            state => state.Residents
                .Where(r => state.Filters.Statuses.Count == 0 || state.Filters.Statuses.Contains(r.Status))
                .Where(r => state.Filters.CareLevels.Count == 0 || state.Filters.CareLevels.Contains(r.CareLevel))
                .Count())

        .Derived<int>("urgentAlertCount$",
            state => state.UnacknowledgedAlerts
                .Count(a => a.Severity == AlertSeverity.High || a.Severity == AlertSeverity.Critical))

        .Derived<decimal>("occupancyRate$",
            state => state.Rooms.Count > 0
                ? (decimal)state.Rooms.Count(r => !r.IsAvailable) / state.Rooms.Count * 100
                : 0m)

        .Build();
}
