using Impulse.Core;
using System.ComponentModel.DataAnnotations;

namespace SampleApp.Features.Admission;

// ========================================
// Impulse Admission Wizard - Server-Driven Multi-Step Form
// Demonstrates:
// - Server-driven wizard configuration
// - Step-by-step validation with ProblemDetails
// - Business rules that can only run server-side
// ========================================

#region Wizard Types

/// <summary>
/// Wizard step configuration - sent from server to drive the UI
/// </summary>
public record WizardStepConfig(
    int StepNumber,
    string Id,
    string Title,
    string Description,
    bool IsRequired,
    bool IsComplete,
    WizardFieldConfig[] Fields);

/// <summary>
/// Field configuration for dynamic form rendering
/// </summary>
public record WizardFieldConfig(
    string Name,
    string Label,
    string Type, // text, date, select, checkbox, textarea
    bool IsRequired,
    string? Placeholder,
    string? HelpText,
    string[]? Options); // For select fields

/// <summary>
/// Wizard state - tracks progress across steps
/// </summary>
public record WizardState(
    string WizardId,
    int CurrentStep,
    int TotalSteps,
    bool CanGoBack,
    bool CanGoNext,
    bool CanSubmit,
    Dictionary<string, object?> Data);

#endregion

#region Step DTOs

/// <summary>
/// Step 1: Basic Information
/// </summary>
public record BasicInfoStepRequest(
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be 2-50 characters")]
    string FirstName,

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be 2-50 characters")]
    string LastName,

    [Required(ErrorMessage = "Date of birth is required")]
    DateTime DateOfBirth,

    [Required(ErrorMessage = "Admission date is required")]
    DateTime AdmissionDate,

    string? RoomPreference);

/// <summary>
/// Step 2: Medical History
/// </summary>
public record MedicalHistoryStepRequest(
    string[] ExistingConditions,
    string[] Allergies,
    string[] CurrentMedications,
    string? PrimaryCarePhysician,
    string? PhysicianPhone,
    string? SpecialInstructions);

/// <summary>
/// Step 3: Care Preferences
/// </summary>
public record CarePreferencesStepRequest(
    string? DietaryRestrictions,
    string MobilityLevel, // Independent, AssistanceNeeded, Wheelchair, Bedridden
    string CommunicationPreference, // Verbal, Written, ASL, Interpreter
    bool PrefersMorningCare,
    bool PrefersEveningCare,
    bool RequiresPrivateRoom,
    string? AdditionalNotes);

/// <summary>
/// Step 4: Emergency Contacts (at least one required)
/// </summary>
public record EmergencyContactsStepRequest(
    List<WizardEmergencyContact> Contacts);

public record WizardEmergencyContact(
    [Required(ErrorMessage = "Contact name is required")]
    string Name,
    [Required(ErrorMessage = "Relationship is required")]
    string Relationship,
    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    string Phone,
    [EmailAddress(ErrorMessage = "Invalid email format")]
    string? Email,
    bool IsPrimaryContact);

/// <summary>
/// Step 5: Review (combines all steps for final validation)
/// </summary>
public record ReviewStepRequest(
    BasicInfoStepRequest BasicInfo,
    MedicalHistoryStepRequest MedicalHistory,
    CarePreferencesStepRequest CarePreferences,
    EmergencyContactsStepRequest EmergencyContacts,
    bool AcceptsTerms,
    bool AuthorizesRelease,
    string? AdditionalComments);

/// <summary>
/// Complete admission request - sent on final submit
/// </summary>
public record CompleteAdmissionRequest(
    BasicInfoStepRequest BasicInfo,
    MedicalHistoryStepRequest MedicalHistory,
    CarePreferencesStepRequest CarePreferences,
    EmergencyContactsStepRequest EmergencyContacts,
    bool AcceptsTerms,
    bool AuthorizesRelease);

public record CompleteAdmissionResponse(
    int ResidentId,
    string ConfirmationNumber,
    DateTime CreatedAt);

#endregion

#region Endpoints

/// <summary>
/// GET /admission/wizard - Get wizard configuration
/// Returns the step configuration for the admission wizard
/// </summary>
[ImpulseEndpoint("/admission/wizard", ImpulseMethod.Get)]
public class GetAdmissionWizardEndpoint : ImpulseEndpoint<object, GetAdmissionWizardResponse>
{
    public override Task<IImpulseResult> Handle(object request, CancellationToken ct = default)
    {
        var response = new GetAdmissionWizardResponse(
            WizardId: Guid.NewGuid().ToString(),
            Title: "Resident Admission",
            Description: "Complete the following steps to admit a new resident to our facility.",
            Steps: GetWizardSteps(),
            CurrentStep: 1,
            TotalSteps: 5
        );

        return Task.FromResult<IImpulseResult>(ImpulseResults.Ok(response));
    }

    private WizardStepConfig[] GetWizardSteps() =>
    [
        new WizardStepConfig(
            StepNumber: 1,
            Id: "basic-info",
            Title: "Basic Information",
            Description: "Enter the resident's personal information",
            IsRequired: true,
            IsComplete: false,
            Fields:
            [
                new("firstName", "First Name", "text", true, "Enter first name", null, null),
                new("lastName", "Last Name", "text", true, "Enter last name", null, null),
                new("dateOfBirth", "Date of Birth", "date", true, null, "Must be at least 18 years old", null),
                new("admissionDate", "Admission Date", "date", true, null, "When will the resident be admitted?", null),
                new("roomPreference", "Room Preference", "select", false, null, null, ["Private Room", "Semi-Private", "No Preference"]),
            ]),

        new WizardStepConfig(
            StepNumber: 2,
            Id: "medical-history",
            Title: "Medical History",
            Description: "Document medical conditions, allergies, and current medications",
            IsRequired: true,
            IsComplete: false,
            Fields:
            [
                new("existingConditions", "Existing Conditions", "multiselect", false, null, "Select all that apply",
                    ["Diabetes", "Hypertension", "Heart Disease", "Dementia", "Arthritis", "COPD", "Depression", "Anxiety"]),
                new("allergies", "Allergies", "tags", false, "Add allergies", "List any known allergies", null),
                new("currentMedications", "Current Medications", "tags", false, "Add medications", "List current medications", null),
                new("primaryCarePhysician", "Primary Care Physician", "text", false, "Dr. Name", null, null),
                new("physicianPhone", "Physician Phone", "tel", false, "(555) 555-5555", null, null),
                new("specialInstructions", "Special Medical Instructions", "textarea", false, null, "Any special care instructions", null),
            ]),

        new WizardStepConfig(
            StepNumber: 3,
            Id: "care-preferences",
            Title: "Care Preferences",
            Description: "Set up care preferences and daily routine",
            IsRequired: true,
            IsComplete: false,
            Fields:
            [
                new("dietaryRestrictions", "Dietary Restrictions", "textarea", false, null, "List any dietary requirements or restrictions", null),
                new("mobilityLevel", "Mobility Level", "select", true, null, null,
                    ["Independent", "Assistance Needed", "Wheelchair", "Bedridden"]),
                new("communicationPreference", "Communication Preference", "select", true, null, null,
                    ["Verbal", "Written", "Sign Language", "Interpreter Required"]),
                new("prefersMorningCare", "Prefers Morning Care", "checkbox", false, null, null, null),
                new("prefersEveningCare", "Prefers Evening Care", "checkbox", false, null, null, null),
                new("requiresPrivateRoom", "Requires Private Room", "checkbox", false, null, "For medical or personal reasons", null),
                new("additionalNotes", "Additional Notes", "textarea", false, null, "Any other preferences or requirements", null),
            ]),

        new WizardStepConfig(
            StepNumber: 4,
            Id: "emergency-contacts",
            Title: "Emergency Contacts",
            Description: "Add at least one emergency contact",
            IsRequired: true,
            IsComplete: false,
            Fields:
            [
                new("contacts", "Emergency Contacts", "contact-list", true, null, "At least one contact required", null),
            ]),

        new WizardStepConfig(
            StepNumber: 5,
            Id: "review",
            Title: "Review & Confirm",
            Description: "Review all information and complete the admission",
            IsRequired: true,
            IsComplete: false,
            Fields:
            [
                new("acceptsTerms", "Accept Terms & Conditions", "checkbox", true, null, "I accept the facility terms and conditions", null),
                new("authorizesRelease", "Authorize Information Release", "checkbox", true, null, "I authorize release of medical information", null),
                new("additionalComments", "Additional Comments", "textarea", false, null, null, null),
            ]),
    ];
}

public record GetAdmissionWizardResponse(
    string WizardId,
    string Title,
    string Description,
    WizardStepConfig[] Steps,
    int CurrentStep,
    int TotalSteps);

/// <summary>
/// POST /admission/wizard/validate/{step} - Validate a specific step
/// Returns ProblemDetails if validation fails
/// </summary>
[ImpulseEndpoint("/admission/wizard/validate/basic-info", ImpulseMethod.Post)]
public class ValidateBasicInfoEndpoint : ImpulseEndpoint<BasicInfoStepRequest, ValidateStepResponse>
{
    // Reserved names - server-only business rule
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Test", "Admin", "System", "Root", "Demo", "Sample"
    };

    public override Task<IImpulseResult> Handle(BasicInfoStepRequest request, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string[]>();

        // Required field validation
        if (string.IsNullOrWhiteSpace(request.FirstName))
            errors["firstName"] = ["First name is required"];
        else if (request.FirstName.Length < 2)
            errors["firstName"] = ["First name must be at least 2 characters"];

        if (string.IsNullOrWhiteSpace(request.LastName))
            errors["lastName"] = ["Last name is required"];
        else if (request.LastName.Length < 2)
            errors["lastName"] = ["Last name must be at least 2 characters"];

        // Business rule: Reserved names (server-only validation)
        if (!string.IsNullOrWhiteSpace(request.FirstName) && ReservedNames.Contains(request.FirstName))
            errors["firstName"] = [$"'{request.FirstName}' is a reserved name and cannot be used"];

        if (!string.IsNullOrWhiteSpace(request.LastName) && ReservedNames.Contains(request.LastName))
            errors["lastName"] = [$"'{request.LastName}' is a reserved name and cannot be used"];

        // Date validations
        if (request.DateOfBirth == default)
            errors["dateOfBirth"] = ["Date of birth is required"];
        else if (request.DateOfBirth > DateTime.Today.AddYears(-18))
            errors["dateOfBirth"] = ["Resident must be at least 18 years old"];
        else if (request.DateOfBirth < DateTime.Today.AddYears(-120))
            errors["dateOfBirth"] = ["Please enter a valid date of birth"];

        if (request.AdmissionDate == default)
            errors["admissionDate"] = ["Admission date is required"];
        else if (request.AdmissionDate < DateTime.Today)
            errors["admissionDate"] = ["Admission date cannot be in the past"];
        else if (request.AdmissionDate > DateTime.Today.AddYears(1))
            errors["admissionDate"] = ["Admission date cannot be more than 1 year in the future"];

        if (errors.Count > 0)
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Ok(new ValidateStepResponse(true, "basic-info", "Basic information validated successfully")));
    }
}

[ImpulseEndpoint("/admission/wizard/validate/medical-history", ImpulseMethod.Post)]
public class ValidateMedicalHistoryEndpoint : ImpulseEndpoint<MedicalHistoryStepRequest, ValidateStepResponse>
{
    public override Task<IImpulseResult> Handle(MedicalHistoryStepRequest request, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string[]>();

        // Business rule: If taking medications, must have physician
        if (request.CurrentMedications?.Length > 0 && string.IsNullOrWhiteSpace(request.PrimaryCarePhysician))
            errors["primaryCarePhysician"] = ["Primary care physician is required when medications are listed"];

        // Phone format validation
        if (!string.IsNullOrWhiteSpace(request.PhysicianPhone) &&
            !System.Text.RegularExpressions.Regex.IsMatch(request.PhysicianPhone, @"^[\d\s\-\(\)]+$"))
            errors["physicianPhone"] = ["Please enter a valid phone number"];

        if (errors.Count > 0)
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Ok(new ValidateStepResponse(true, "medical-history", "Medical history validated successfully")));
    }
}

[ImpulseEndpoint("/admission/wizard/validate/care-preferences", ImpulseMethod.Post)]
public class ValidateCarePreferencesEndpoint : ImpulseEndpoint<CarePreferencesStepRequest, ValidateStepResponse>
{
    public override Task<IImpulseResult> Handle(CarePreferencesStepRequest request, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string[]>();

        // Required select fields
        if (string.IsNullOrWhiteSpace(request.MobilityLevel))
            errors["mobilityLevel"] = ["Mobility level is required"];
        else if (!new[] { "Independent", "Assistance Needed", "Wheelchair", "Bedridden" }.Contains(request.MobilityLevel))
            errors["mobilityLevel"] = ["Please select a valid mobility level"];

        if (string.IsNullOrWhiteSpace(request.CommunicationPreference))
            errors["communicationPreference"] = ["Communication preference is required"];

        // Business rule: Bedridden requires private room
        if (request.MobilityLevel == "Bedridden" && !request.RequiresPrivateRoom)
            errors["requiresPrivateRoom"] = ["Bedridden residents require a private room"];

        if (errors.Count > 0)
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Ok(new ValidateStepResponse(true, "care-preferences", "Care preferences validated successfully")));
    }
}

[ImpulseEndpoint("/admission/wizard/validate/emergency-contacts", ImpulseMethod.Post)]
public class ValidateEmergencyContactsEndpoint : ImpulseEndpoint<EmergencyContactsStepRequest, ValidateStepResponse>
{
    public override Task<IImpulseResult> Handle(EmergencyContactsStepRequest request, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Contacts == null || request.Contacts.Count == 0)
        {
            errors["contacts"] = ["At least one emergency contact is required"];
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));
        }

        // Validate each contact
        for (int i = 0; i < request.Contacts.Count; i++)
        {
            var contact = request.Contacts[i];

            if (string.IsNullOrWhiteSpace(contact.Name))
                errors[$"contacts.{i}.name"] = ["Contact name is required"];

            if (string.IsNullOrWhiteSpace(contact.Relationship))
                errors[$"contacts.{i}.relationship"] = ["Relationship is required"];

            if (string.IsNullOrWhiteSpace(contact.Phone))
                errors[$"contacts.{i}.phone"] = ["Phone number is required"];
            else if (!System.Text.RegularExpressions.Regex.IsMatch(contact.Phone, @"^[\d\s\-\(\)]+$"))
                errors[$"contacts.{i}.phone"] = ["Please enter a valid phone number"];

            if (!string.IsNullOrWhiteSpace(contact.Email) &&
                !System.Text.RegularExpressions.Regex.IsMatch(contact.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errors[$"contacts.{i}.email"] = ["Please enter a valid email address"];
        }

        // Business rule: Must have exactly one primary contact
        var primaryCount = request.Contacts.Count(c => c.IsPrimaryContact);
        if (primaryCount == 0)
            errors["contacts"] = ["One contact must be designated as primary"];
        else if (primaryCount > 1)
            errors["contacts"] = ["Only one contact can be designated as primary"];

        if (errors.Count > 0)
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Ok(new ValidateStepResponse(true, "emergency-contacts", "Emergency contacts validated successfully")));
    }
}

public record ValidateStepResponse(bool IsValid, string StepId, string Message);

/// <summary>
/// POST /admission/wizard/complete - Complete the admission
/// Final submission with all data
/// </summary>
[ImpulseEndpoint("/admission/wizard/complete", ImpulseMethod.Post)]
public class CompleteAdmissionEndpoint : ImpulseEndpoint<CompleteAdmissionRequest, CompleteAdmissionResponse>
{
    public override Task<IImpulseResult> Handle(CompleteAdmissionRequest request, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string[]>();

        // Final validation
        if (!request.AcceptsTerms)
            errors["acceptsTerms"] = ["You must accept the terms and conditions"];

        if (!request.AuthorizesRelease)
            errors["authorizesRelease"] = ["You must authorize the release of medical information"];

        // Re-validate critical business rules
        if (request.EmergencyContacts?.Contacts?.Count == 0)
            errors["emergencyContacts"] = ["At least one emergency contact is required"];

        if (errors.Count > 0)
            return Task.FromResult<IImpulseResult>(ImpulseResults.ValidationProblem(errors));

        // Simulate creating the resident
        var response = new CompleteAdmissionResponse(
            ResidentId: 42,
            ConfirmationNumber: $"ADM-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            CreatedAt: DateTime.UtcNow
        );

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Created($"/residents/{response.ResidentId}", response));
    }
}

#endregion
