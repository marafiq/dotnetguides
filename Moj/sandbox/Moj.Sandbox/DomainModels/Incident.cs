namespace Moj.Sandbox.DomainModels;

/// <summary>
/// Complex domain model - regular C# types.
/// The DSL infers TypeScript from these types automatically.
/// </summary>

public enum IncidentStatus
{
    Open,
    InProgress,
    OnHold,
    Resolved,
    Closed
}

public enum IncidentPriority
{
    Critical,
    High,
    Medium,
    Low
}

public enum IncidentCategory
{
    Bug,
    Feature,
    Support,
    Security,
    Performance
}

public record User
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string? AvatarUrl { get; init; }
    public string Department { get; init; } = "";
    public List<string> Roles { get; init; } = [];
}

public record Comment
{
    public string Id { get; init; } = "";
    public string Text { get; init; } = "";
    public User Author { get; init; } = new();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; init; }
    public List<Attachment> Attachments { get; init; } = [];
    public List<CommentReaction> Reactions { get; init; } = [];
}

public record CommentReaction
{
    public string UserId { get; init; } = "";
    public string Emoji { get; init; } = "";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public record Attachment
{
    public string Id { get; init; } = "";
    public string FileName { get; init; } = "";
    public string ContentType { get; init; } = "";
    public long Size { get; init; }
    public string Url { get; init; } = "";
    public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
}

public record IncidentMetadata
{
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    public string CreatedBy { get; init; } = "";
    public int ViewCount { get; init; }
    public TimeSpan? TimeToFirstResponse { get; init; }
    public TimeSpan? TimeToResolution { get; init; }
}

public record RelatedIncident
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public IncidentStatus Status { get; init; }
    public string RelationType { get; init; } = ""; // "blocks", "blocked_by", "related", "duplicate"
}

public record CustomField
{
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";
    public object? Value { get; init; }
    public string FieldType { get; init; } = ""; // "text", "number", "date", "select", "multiselect"
}

public record Incident
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public IncidentStatus Status { get; init; } = IncidentStatus.Open;
    public IncidentPriority Priority { get; init; } = IncidentPriority.Medium;
    public IncidentCategory Category { get; init; } = IncidentCategory.Bug;
    public User? AssignedTo { get; init; }
    public User? Reporter { get; init; }
    public List<User> Watchers { get; init; } = [];
    public List<string> Labels { get; init; } = [];
    public List<Comment> Comments { get; init; } = [];
    public List<Attachment> Attachments { get; init; } = [];
    public List<RelatedIncident> RelatedIncidents { get; init; } = [];
    public Dictionary<string, CustomField> CustomFields { get; init; } = new();
    public IncidentMetadata Metadata { get; init; } = new();

    // Nested object for SLA tracking
    public SlaInfo? Sla { get; init; }
}

public record SlaInfo
{
    public string PolicyId { get; init; } = "";
    public string PolicyName { get; init; } = "";
    public DateTime? ResponseDueAt { get; init; }
    public DateTime? ResolutionDueAt { get; init; }
    public bool ResponseBreached { get; init; }
    public bool ResolutionBreached { get; init; }
    public SlaMetrics Metrics { get; init; } = new();
}

public record SlaMetrics
{
    public TimeSpan? TimeToResponse { get; init; }
    public TimeSpan? TimeToResolution { get; init; }
    public double ResponsePercentage { get; init; }
    public double ResolutionPercentage { get; init; }
}

/// <summary>
/// App state containing incidents and UI state
/// </summary>
public record IncidentAppState
{
    public List<Incident> Incidents { get; init; } = [];
    public Incident? SelectedIncident { get; init; }
    public User? CurrentUser { get; init; }
    public bool IsLoading { get; init; }
    public string? Error { get; init; }

    // Filters
    public IncidentFilters Filters { get; init; } = new();

    // UI state
    public UiState Ui { get; init; } = new();
}

public record IncidentFilters
{
    public List<IncidentStatus> Statuses { get; init; } = [];
    public List<IncidentPriority> Priorities { get; init; } = [];
    public List<IncidentCategory> Categories { get; init; } = [];
    public string? AssigneeId { get; init; }
    public string? SearchQuery { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public List<string> Labels { get; init; } = [];
}

public record UiState
{
    public bool SidebarOpen { get; init; } = true;
    public string ActiveView { get; init; } = "list"; // "list", "board", "timeline"
    public SortConfig Sort { get; init; } = new();
    public PaginationState Pagination { get; init; } = new();
}

public record SortConfig
{
    public string Field { get; init; } = "createdAt";
    public string Direction { get; init; } = "desc"; // "asc" or "desc"
}

public record PaginationState
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
}
