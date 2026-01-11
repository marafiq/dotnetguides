using CareHome.Models;
using Impulse;

namespace CareHome.Endpoints;

/// <summary>
/// Get wizard configuration and initial state.
/// </summary>
[Endpoint("/admission/wizard", HttpMethod.Get)]
public class GetAdmissionWizardEndpoint : Endpoint<AdmissionWizardResponse>
{
    public override Task<IResult<AdmissionWizardResponse>> HandleAsync(CancellationToken ct)
    {
        var steps = new List<WizardStepConfig>
        {
            new("basic-info", "Basic Information", "Enter resident's basic details"),
            new("medical-history", "Medical History", "Medical conditions and medications"),
            new("care-preferences", "Care Preferences", "Care level and dietary needs"),
            new("emergency-contacts", "Emergency Contacts", "Emergency contact information"),
            new("review", "Review & Submit", "Review and submit the admission")
        };

        return Task.FromResult(Result.Ok(new AdmissionWizardResponse(
            steps,
            Enum.GetValues<CareLevel>().Select(c => c.ToString()).ToList(),
            Enum.GetValues<DietaryRequirement>().Select(d => d.ToString()).ToList())));
    }
}

/// <summary>
/// Wizard configuration response.
/// </summary>
public record AdmissionWizardResponse(
    List<WizardStepConfig> Steps,
    List<string> CareLevels,
    List<string> DietaryRequirements);

/// <summary>
/// Configuration for a wizard step.
/// </summary>
public record WizardStepConfig(
    string Id,
    string Title,
    string Description);

/// <summary>
/// Validate Step 1: Basic Info.
/// </summary>
[Endpoint("/admission/wizard/validate/basic-info", HttpMethod.Post)]
public class ValidateBasicInfoEndpoint : Endpoint<BasicInfoRequest, ValidateStepResponse>
{
    public override Task<IResult<ValidateStepResponse>> HandleAsync(
        BasicInfoRequest request, CancellationToken ct)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.FirstName))
            AddError(errors, "firstName", "First name is required");
        else if (request.FirstName.Length < 2)
            AddError(errors, "firstName", "First name must be at least 2 characters");

        if (string.IsNullOrWhiteSpace(request.LastName))
            AddError(errors, "lastName", "Last name is required");
        else if (request.LastName.Length < 2)
            AddError(errors, "lastName", "Last name must be at least 2 characters");

        if (request.DateOfBirth == default)
            AddError(errors, "dateOfBirth", "Date of birth is required");
        else if (request.DateOfBirth > DateTime.Today.AddYears(-55))
            AddError(errors, "dateOfBirth", "Resident must be at least 55 years old");
        else if (request.DateOfBirth < DateTime.Today.AddYears(-120))
            AddError(errors, "dateOfBirth", "Please enter a valid date of birth");

        return Task.FromResult(Result.Ok(ToResponse(errors)));
    }

    private void AddError(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.ContainsKey(field))
            errors[field] = new List<string>();
        errors[field].Add(message);
    }

    private ValidateStepResponse ToResponse(Dictionary<string, List<string>> errors)
    {
        if (errors.Count == 0)
            return new ValidateStepResponse(true, null);

        return new ValidateStepResponse(false,
            errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()));
    }
}

/// <summary>
/// Validate Step 2: Medical History.
/// </summary>
[Endpoint("/admission/wizard/validate/medical-history", HttpMethod.Post)]
public class ValidateMedicalHistoryEndpoint : Endpoint<MedicalHistoryRequest, ValidateStepResponse>
{
    public override Task<IResult<ValidateStepResponse>> HandleAsync(
        MedicalHistoryRequest request, CancellationToken ct)
    {
        // Medical history is optional - basic validation only
        return Task.FromResult(Result.Ok(new ValidateStepResponse(true, null)));
    }
}

/// <summary>
/// Validate Step 3: Care Preferences.
/// </summary>
[Endpoint("/admission/wizard/validate/care-preferences", HttpMethod.Post)]
public class ValidateCarePreferencesEndpoint : Endpoint<CarePreferencesRequest, ValidateStepResponse>
{
    public override Task<IResult<ValidateStepResponse>> HandleAsync(
        CarePreferencesRequest request, CancellationToken ct)
    {
        // Care level is an enum, will be validated by model binding
        return Task.FromResult(Result.Ok(new ValidateStepResponse(true, null)));
    }
}

/// <summary>
/// Validate Step 4: Emergency Contacts.
/// </summary>
[Endpoint("/admission/wizard/validate/emergency-contacts", HttpMethod.Post)]
public class ValidateEmergencyContactsEndpoint : Endpoint<EmergencyContactsRequest, ValidateStepResponse>
{
    public override Task<IResult<ValidateStepResponse>> HandleAsync(
        EmergencyContactsRequest request, CancellationToken ct)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.PrimaryContactName))
            AddError(errors, "primaryContactName", "Primary contact name is required");

        if (string.IsNullOrWhiteSpace(request.PrimaryContactPhone))
            AddError(errors, "primaryContactPhone", "Primary contact phone is required");

        if (string.IsNullOrWhiteSpace(request.PrimaryContactRelationship))
            AddError(errors, "primaryContactRelationship", "Relationship is required");

        return Task.FromResult(Result.Ok(ToResponse(errors)));
    }

    private void AddError(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.ContainsKey(field))
            errors[field] = new List<string>();
        errors[field].Add(message);
    }

    private ValidateStepResponse ToResponse(Dictionary<string, List<string>> errors)
    {
        if (errors.Count == 0)
            return new ValidateStepResponse(true, null);

        return new ValidateStepResponse(false,
            errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()));
    }
}

/// <summary>
/// Complete the admission process.
/// </summary>
[Endpoint("/admission/wizard/complete", HttpMethod.Post)]
public class CompleteAdmissionEndpoint : Endpoint<CompleteAdmissionRequest, CompleteAdmissionResponse>
{
    public override Task<IResult<CompleteAdmissionResponse>> HandleAsync(
        CompleteAdmissionRequest request, CancellationToken ct)
    {
        var errors = new ValidationBuilder();

        if (!request.ConsentGiven)
            errors.Add("consentGiven", "Consent is required");

        if (!request.TermsAccepted)
            errors.Add("termsAccepted", "Terms must be accepted");

        if (errors.HasErrors)
            return Task.FromResult(errors.ToResult<CompleteAdmissionResponse>());

        // In a real app, this would save to a database
        var residentId = Random.Shared.Next(100, 999);
        var roomNumber = $"{Random.Shared.Next(100, 300)}{(char)('A' + Random.Shared.Next(0, 4))}";

        return Task.FromResult(Result.Ok(new CompleteAdmissionResponse(
            residentId,
            roomNumber,
            $"Welcome to Green Valley Care Home! Room {roomNumber} has been assigned.")));
    }
}
