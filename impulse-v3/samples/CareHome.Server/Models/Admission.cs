namespace CareHome.Models;

/// <summary>
/// Step 1: Basic info for admission.
/// </summary>
public record BasicInfoRequest(
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string? RoomPreference);

/// <summary>
/// Step 2: Medical history.
/// </summary>
public record MedicalHistoryRequest(
    List<string> Conditions,
    List<string> Medications,
    string? Allergies,
    string? PrimaryCarePhysician);

/// <summary>
/// Step 3: Care preferences.
/// </summary>
public record CarePreferencesRequest(
    CareLevel CareLevel,
    List<DietaryRequirement> DietaryRequirements,
    string? SpecialInstructions,
    bool RequiresNightChecks);

/// <summary>
/// Step 4: Emergency contacts.
/// </summary>
public record EmergencyContactsRequest(
    string PrimaryContactName,
    string PrimaryContactPhone,
    string PrimaryContactRelationship,
    string? SecondaryContactName,
    string? SecondaryContactPhone);

/// <summary>
/// Complete admission request with all steps.
/// </summary>
public record CompleteAdmissionRequest(
    BasicInfoRequest BasicInfo,
    MedicalHistoryRequest MedicalHistory,
    CarePreferencesRequest CarePreferences,
    EmergencyContactsRequest EmergencyContacts,
    bool ConsentGiven,
    bool TermsAccepted);

/// <summary>
/// Response after validating a step.
/// </summary>
public record ValidateStepResponse(
    bool IsValid,
    Dictionary<string, string[]>? Errors);

/// <summary>
/// Response after completing admission.
/// </summary>
public record CompleteAdmissionResponse(
    int ResidentId,
    string RoomAssigned,
    string Message);
