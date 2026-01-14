using Moj.Sandbox.DomainModels;
using Moj.TanStack.Dsl.TanStack.Store;
using Moj.TanStack.SourceGenerator.Attributes;

namespace Moj.Sandbox.Stores;

/// <summary>
/// TanStack Store defined using C# types directly.
/// No manual TypeScript construction - everything inferred from C# domain model.
/// </summary>
[TsStore(Name = "incidentStore")]
public static class IncidentStore
{
    /// <summary>
    /// The store definition - uses C# types and expressions.
    /// TypeScript is automatically generated from this.
    /// </summary>
    public static StoreDefinition<IncidentAppState> Definition =>
        TanStack.Store(new IncidentAppState
        {
            Incidents = [],
            SelectedIncident = null,
            CurrentUser = null,
            IsLoading = false,
            Error = null,
            Filters = new IncidentFilters
            {
                Statuses = [],
                Priorities = [],
                Categories = [],
                AssigneeId = null,
                SearchQuery = null,
                DateFrom = null,
                DateTo = null,
                Labels = []
            },
            Ui = new UiState
            {
                SidebarOpen = true,
                ActiveView = "list",
                Sort = new SortConfig
                {
                    Field = "createdAt",
                    Direction = "desc"
                },
                Pagination = new PaginationState
                {
                    Page = 1,
                    PageSize = 20,
                    TotalItems = 0,
                    TotalPages = 0
                }
            }
        })
        .As("incidentStore")

        // Actions - defined with C# lambdas, converted to TypeScript
        .Action<List<Incident>>("setIncidents",
            (state, incidents) => state with { Incidents = incidents, IsLoading = false })

        .Action<Incident>("addIncident",
            (state, incident) => state with { Incidents = [.. state.Incidents, incident] })

        .Action<Incident>("updateIncident",
            (state, updated) => state with
            {
                Incidents = state.Incidents.Select(i => i.Id == updated.Id ? updated : i).ToList(),
                SelectedIncident = state.SelectedIncident?.Id == updated.Id ? updated : state.SelectedIncident
            })

        .Action<string>("removeIncident",
            (state, id) => state with
            {
                Incidents = state.Incidents.Where(i => i.Id != id).ToList(),
                SelectedIncident = state.SelectedIncident?.Id == id ? null : state.SelectedIncident
            })

        .Action<Incident?>("selectIncident",
            (state, incident) => state with { SelectedIncident = incident })

        .Action<User>("setCurrentUser",
            (state, user) => state with { CurrentUser = user })

        .Action("startLoading",
            state => state with { IsLoading = true, Error = null })

        .Action<string>("setError",
            (state, error) => state with { Error = error, IsLoading = false })

        .Action("clearError",
            state => state with { Error = null })

        // Filter actions
        .Action<IncidentFilters>("setFilters",
            (state, filters) => state with { Filters = filters })

        .Action<List<IncidentStatus>>("setStatusFilter",
            (state, statuses) => state with { Filters = state.Filters with { Statuses = statuses } })

        .Action<List<IncidentPriority>>("setPriorityFilter",
            (state, priorities) => state with { Filters = state.Filters with { Priorities = priorities } })

        .Action<string?>("setSearchQuery",
            (state, query) => state with { Filters = state.Filters with { SearchQuery = query } })

        .Action("clearFilters",
            state => state with
            {
                Filters = new IncidentFilters
                {
                    Statuses = [],
                    Priorities = [],
                    Categories = [],
                    Labels = []
                }
            })

        // UI actions
        .Action<bool>("setSidebarOpen",
            (state, open) => state with { Ui = state.Ui with { SidebarOpen = open } })

        .Action("toggleSidebar",
            state => state with { Ui = state.Ui with { SidebarOpen = !state.Ui.SidebarOpen } })

        .Action<string>("setActiveView",
            (state, view) => state with { Ui = state.Ui with { ActiveView = view } })

        .Action<SortConfig>("setSort",
            (state, sort) => state with { Ui = state.Ui with { Sort = sort } })

        .Action<int>("setPage",
            (state, page) => state with { Ui = state.Ui with { Pagination = state.Ui.Pagination with { Page = page } } })

        .Action<PaginationState>("setPagination",
            (state, pagination) => state with { Ui = state.Ui with { Pagination = pagination } })

        // Selectors - derive computed state from the store
        .Selector("totalIncidents",
            state => state.Incidents.Count)

        .Selector("openIncidents",
            state => state.Incidents.Where(i => i.Status == IncidentStatus.Open).ToList())

        .Selector("criticalIncidents",
            state => state.Incidents.Where(i => i.Priority == IncidentPriority.Critical).ToList())

        .Selector("assignedToCurrentUser",
            state => state.Incidents.Where(i =>
                state.CurrentUser != null && i.AssignedTo?.Id == state.CurrentUser.Id).ToList())

        .Selector("filteredIncidents",
            state => FilterIncidents(state.Incidents, state.Filters))

        .Selector("incidentCountByStatus",
            state => state.Incidents
                .GroupBy(i => i.Status)
                .ToDictionary(g => g.Key, g => g.Count()))

        .Selector("incidentCountByPriority",
            state => state.Incidents
                .GroupBy(i => i.Priority)
                .ToDictionary(g => g.Key, g => g.Count()))

        .Selector("hasActiveFilters",
            state => state.Filters.Statuses.Count > 0 ||
                     state.Filters.Priorities.Count > 0 ||
                     state.Filters.Categories.Count > 0 ||
                     !string.IsNullOrEmpty(state.Filters.SearchQuery) ||
                     !string.IsNullOrEmpty(state.Filters.AssigneeId) ||
                     state.Filters.Labels.Count > 0)

        .Selector("isSelectedIncidentResolved",
            state => state.SelectedIncident?.Status == IncidentStatus.Resolved ||
                     state.SelectedIncident?.Status == IncidentStatus.Closed)

        .Build();

    // Helper method for complex filtering logic
    private static List<Incident> FilterIncidents(List<Incident> incidents, IncidentFilters filters)
    {
        var query = incidents.AsEnumerable();

        if (filters.Statuses.Count > 0)
            query = query.Where(i => filters.Statuses.Contains(i.Status));

        if (filters.Priorities.Count > 0)
            query = query.Where(i => filters.Priorities.Contains(i.Priority));

        if (filters.Categories.Count > 0)
            query = query.Where(i => filters.Categories.Contains(i.Category));

        if (!string.IsNullOrEmpty(filters.AssigneeId))
            query = query.Where(i => i.AssignedTo?.Id == filters.AssigneeId);

        if (!string.IsNullOrEmpty(filters.SearchQuery))
        {
            var search = filters.SearchQuery.ToLowerInvariant();
            query = query.Where(i =>
                i.Title.ToLowerInvariant().Contains(search) ||
                i.Description.ToLowerInvariant().Contains(search));
        }

        if (filters.Labels.Count > 0)
            query = query.Where(i => filters.Labels.Any(l => i.Labels.Contains(l)));

        if (filters.DateFrom.HasValue)
            query = query.Where(i => i.Metadata.CreatedAt >= filters.DateFrom.Value);

        if (filters.DateTo.HasValue)
            query = query.Where(i => i.Metadata.CreatedAt <= filters.DateTo.Value);

        return query.ToList();
    }
}
