namespace Impulse.IntegrationTests.Features.Wizard;

/// <summary>
/// Multi-step wizard for resident onboarding.
/// Demonstrates complex multi-step form with state preservation.
/// </summary>
public record WizardProps(
    int CurrentStep,
    int TotalSteps,
    WizardStepData[] Steps,
    WizardFormData FormData,
    bool CanGoBack,
    bool CanGoNext,
    bool CanSubmit
);

public record WizardStepData(
    int StepNumber,
    string Title,
    string Description,
    bool IsCompleted,
    bool IsCurrent
);

public record WizardFormData(
    // Step 1: Personal Info
    string? FirstName,
    string? LastName,
    DateTime? DateOfBirth,
    string? Gender,

    // Step 2: Medical History
    string? PrimaryPhysician,
    string[]? Allergies,
    string[]? Medications,
    string? BloodType,
    bool HasInsurance,
    string? InsuranceProvider,
    string? InsurancePolicyNumber,

    // Step 3: Emergency Contact
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? EmergencyContactRelation,

    // Step 4: Room Assignment
    int? BuildingId,
    int? FloorNumber,
    int? RoomNumber
);

// Request/Response types for wizard mutations
public record WizardStepRequest(
    int CurrentStep,
    WizardFormData FormData
);

public record WizardStepResponse(
    bool Success,
    int NextStep,
    Dictionary<string, string[]>? ValidationErrors
);

public record WizardSubmitRequest(
    WizardFormData FormData
);

public record WizardSubmitResponse(
    bool Success,
    int? ResidentId,
    string? Message
);
