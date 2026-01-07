namespace Impulse.IntegrationTests.Features.Pane;

/// <summary>
/// Pane system for slide-out detail views.
/// Used for showing details without leaving the current context.
/// </summary>

// ============================================================================
// Main Pane Props
// ============================================================================

/// <summary>
/// Container that can show a slide-out pane.
/// </summary>
public record PaneContainerProps(
    bool IsOpen,
    PaneConfig? Pane,
    string? ReturnUrl
);

/// <summary>
/// Configuration for a pane.
/// </summary>
public record PaneConfig(
    string Id,
    string Title,
    string? Subtitle,
    PanePosition Position,
    PaneSize Size,
    bool CloseOnOverlay,
    bool CloseOnEscape,
    bool ShowCloseButton,
    PaneContent Content,
    IReadOnlyList<PaneAction>? HeaderActions,
    IReadOnlyList<PaneAction>? FooterActions
);

public enum PanePosition
{
    Right,
    Left,
    Bottom,
    Top
}

public enum PaneSize
{
    Small,
    Medium,
    Large,
    Half,
    Full
}

// ============================================================================
// Pane Content
// ============================================================================

public record PaneContent(
    PaneContentType Type,
    string? ComponentPath,
    IReadOnlyDictionary<string, object>? ComponentProps,
    IReadOnlyList<PaneSection>? Sections,
    string? Url
);

public enum PaneContentType
{
    Component,
    Sections,
    Iframe,
    Loading
}

/// <summary>
/// Structured section within a pane.
/// </summary>
public record PaneSection(
    string Id,
    string Title,
    bool IsCollapsible,
    bool IsCollapsed,
    IReadOnlyList<PaneSectionItem> Items
);

public record PaneSectionItem(
    string Label,
    string? Value,
    PaneSectionItemType Type,
    string? Href,
    string? Icon
);

public enum PaneSectionItemType
{
    Text,
    Link,
    Badge,
    Date,
    Currency,
    Status
}

// ============================================================================
// Pane Actions
// ============================================================================

public record PaneAction(
    string Id,
    string Label,
    PaneActionType Type,
    PaneActionStyle Style,
    string? Icon,
    string? Endpoint,
    bool IsLoading,
    bool IsDisabled
);

public enum PaneActionType
{
    Close,
    Navigate,
    Submit,
    OpenModal,
    Custom
}

public enum PaneActionStyle
{
    Primary,
    Secondary,
    Danger,
    Ghost,
    Icon
}

// ============================================================================
// Mutation Types
// ============================================================================

public record PaneActionRequest(
    string PaneId,
    string ActionId,
    IReadOnlyDictionary<string, object>? Data
);

public record PaneActionResponse(
    bool Success,
    string? Message,
    string? RedirectUrl,
    PaneConfig? UpdatedPane
);

// ============================================================================
// Example: Resident Detail Pane
// ============================================================================

public record ResidentDetailPaneProps(
    int ResidentId,
    string Name,
    string PhotoUrl,
    ResidentStatus Status,
    IReadOnlyList<PaneSection> InfoSections,
    IReadOnlyList<PaneAction> Actions
);

public enum ResidentStatus
{
    Active,
    Discharged,
    OnLeave,
    Pending
}

// ============================================================================
// Example: Activity Feed Pane
// ============================================================================

public record ActivityFeedPaneProps(
    string Title,
    IReadOnlyList<ActivityItemData> Activities,
    bool HasMore,
    string LoadMoreUrl
);

public record ActivityItemData(
    string Id,
    string Type,
    string Description,
    DateTime Timestamp,
    string? ActorName,
    string? ActorAvatar,
    IReadOnlyDictionary<string, object>? Metadata
);

// ============================================================================
// Example: Quick Actions Pane
// ============================================================================

public record QuickActionsPaneProps(
    string Title,
    IReadOnlyList<QuickActionData> Actions,
    IReadOnlyList<RecentItemData>? RecentItems
);

public record QuickActionData(
    string Id,
    string Label,
    string Description,
    string Icon,
    string Url,
    bool IsNew
);

public record RecentItemData(
    string Id,
    string Label,
    string Type,
    string Url,
    DateTime AccessedAt
);

// ============================================================================
// Example: Filter Pane
// ============================================================================

public record FilterPaneProps(
    string Title,
    IReadOnlyList<FilterGroupData> FilterGroups,
    IReadOnlyDictionary<string, object> CurrentFilters,
    string ApplyUrl,
    string ResetUrl
);

public record FilterGroupData(
    string Id,
    string Label,
    FilterType Type,
    IReadOnlyList<FilterOptionData>? Options,
    decimal? Min,
    decimal? Max,
    string? DateFormat
);

public enum FilterType
{
    Checkbox,
    Radio,
    Range,
    DateRange,
    Search
}

public record FilterOptionData(
    string Value,
    string Label,
    int? Count
);

public record ApplyFiltersRequest(
    IReadOnlyDictionary<string, object> Filters
);

public record ApplyFiltersResponse(
    bool Success,
    string RedirectUrl,
    int ResultCount
);
