namespace Impulse.IntegrationTests.Features.Modal;

/// <summary>
/// Modal system for confirmations, alerts, and edit dialogs.
/// Server-driven: props define what modal to show and when.
/// </summary>

// ============================================================================
// Main Modal Props
// ============================================================================

/// <summary>
/// Props for a page that can display modals.
/// The modal configuration is passed from the server.
/// </summary>
public record ModalContainerProps(
    bool IsOpen,
    ModalConfig? Modal,
    string? ReturnUrl
);

/// <summary>
/// Configuration for a modal dialog.
/// </summary>
public record ModalConfig(
    string Id,
    ModalType Type,
    string Title,
    string? Description,
    ModalSize Size,
    bool CloseOnOverlay,
    bool CloseOnEscape,
    bool ShowCloseButton,
    ModalContent Content,
    IReadOnlyList<ModalAction> Actions
);

public enum ModalType
{
    Alert,
    Confirm,
    Form,
    Custom
}

public enum ModalSize
{
    Small,
    Medium,
    Large,
    FullScreen
}

// ============================================================================
// Modal Content Types
// ============================================================================

/// <summary>
/// Content to display inside the modal.
/// Can be simple message, form, or custom component.
/// </summary>
public record ModalContent(
    ModalContentType Type,
    string? Message,
    AlertSeverity? AlertSeverity,
    IReadOnlyList<ModalFormField>? FormFields,
    IReadOnlyDictionary<string, object>? FormValues,
    string? ComponentPath,
    IReadOnlyDictionary<string, object>? ComponentProps
);

public enum ModalContentType
{
    Message,
    Alert,
    Form,
    Component
}

public enum AlertSeverity
{
    Info,
    Success,
    Warning,
    Error
}

// ============================================================================
// Modal Form Fields
// ============================================================================

public record ModalFormField(
    string Name,
    string Label,
    ModalFieldType Type,
    bool Required,
    string? Placeholder,
    string? DefaultValue,
    IReadOnlyList<SelectOptionData>? Options,
    string? ValidationMessage
);

public enum ModalFieldType
{
    Text,
    Email,
    Password,
    Number,
    Textarea,
    Select,
    Checkbox,
    Date
}

public record SelectOptionData(
    string Value,
    string Label
);

// ============================================================================
// Modal Actions (Buttons)
// ============================================================================

public record ModalAction(
    string Id,
    string Label,
    ModalActionType Type,
    ModalActionStyle Style,
    bool IsLoading,
    bool IsDisabled,
    string? Endpoint,
    string? ConfirmMessage
);

public enum ModalActionType
{
    Close,
    Submit,
    Navigate,
    Custom
}

public enum ModalActionStyle
{
    Primary,
    Secondary,
    Danger,
    Ghost
}

// ============================================================================
// Mutation Types
// ============================================================================

public record ModalSubmitRequest(
    string ModalId,
    string ActionId,
    IReadOnlyDictionary<string, object>? FormData
);

public record ModalSubmitResponse(
    bool Success,
    string? Message,
    string? RedirectUrl,
    IReadOnlyDictionary<string, string[]>? ValidationErrors,
    ModalConfig? NextModal
);

// ============================================================================
// Example: Delete Confirmation Modal
// ============================================================================

public record DeleteConfirmationProps(
    string ItemType,
    string ItemName,
    int ItemId,
    string DeleteEndpoint,
    string CancelUrl
);

public record DeleteRequest(
    string ItemType,
    int ItemId
);

public record DeleteResponse(
    bool Success,
    string? Message,
    string? RedirectUrl
);

// ============================================================================
// Example: Edit Modal with Form
// ============================================================================

public record EditResidentModalProps(
    int ResidentId,
    string CurrentName,
    string CurrentRoom,
    DateTime CurrentAdmitDate,
    IReadOnlyList<RoomOptionData> AvailableRooms
);

public record RoomOptionData(
    string RoomNumber,
    string BuildingName,
    bool IsAvailable
);

public record UpdateResidentRequest(
    int ResidentId,
    string Name,
    string Room,
    DateTime AdmitDate
);

public record UpdateResidentResponse(
    bool Success,
    string? Message,
    IReadOnlyDictionary<string, string[]>? Errors
);

// ============================================================================
// Example: Multi-step Modal Flow
// ============================================================================

public record MultiStepModalProps(
    int CurrentStep,
    int TotalSteps,
    IReadOnlyList<ModalStepData> Steps,
    IReadOnlyDictionary<string, object> AccumulatedData
);

public record ModalStepData(
    int StepNumber,
    string Title,
    string Description,
    IReadOnlyList<ModalFormField> Fields,
    bool IsComplete
);

public record ModalStepRequest(
    int CurrentStep,
    IReadOnlyDictionary<string, object> StepData
);

public record ModalStepResponse(
    bool Success,
    int? NextStep,
    IReadOnlyDictionary<string, string[]>? Errors,
    object? FinalResult
);
