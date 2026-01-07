namespace Impulse.IntegrationTests.Features.Wizard;

public static class Handler
{
    private static readonly WizardStepData[] StepDefinitions =
    [
        new(1, "Personal Information", "Basic details about the resident", false, true),
        new(2, "Medical History", "Health information and insurance", false, false),
        new(3, "Emergency Contact", "Who to contact in emergencies", false, false),
        new(4, "Room Assignment", "Where the resident will stay", false, false)
    ];

    public static WizardProps Get(int step = 1, WizardFormData? formData = null)
    {
        formData ??= new WizardFormData(
            null, null, null, null,
            null, null, null, null, false, null, null,
            null, null, null,
            null, null, null
        );

        var steps = StepDefinitions.Select(s => s with
        {
            IsCurrent = s.StepNumber == step,
            IsCompleted = s.StepNumber < step
        }).ToArray();

        return new WizardProps(
            CurrentStep: step,
            TotalSteps: 4,
            Steps: steps,
            FormData: formData,
            CanGoBack: step > 1,
            CanGoNext: step < 4,
            CanSubmit: step == 4
        );
    }

    public static WizardStepResponse ValidateAndNext(WizardStepRequest request)
    {
        var errors = ValidateStep(request.CurrentStep, request.FormData);

        if (errors.Count > 0)
        {
            return new WizardStepResponse(false, request.CurrentStep, errors);
        }

        return new WizardStepResponse(true, request.CurrentStep + 1, null);
    }

    public static WizardStepResponse GoBack(int currentStep)
    {
        var prevStep = Math.Max(1, currentStep - 1);
        return new WizardStepResponse(true, prevStep, null);
    }

    public static WizardSubmitResponse Submit(WizardSubmitRequest request)
    {
        // Validate all steps
        for (int step = 1; step <= 4; step++)
        {
            var errors = ValidateStep(step, request.FormData);
            if (errors.Count > 0)
            {
                return new WizardSubmitResponse(false, null, $"Validation failed on step {step}");
            }
        }

        // Simulate creating resident
        var residentId = Random.Shared.Next(1000, 9999);
        return new WizardSubmitResponse(
            true,
            residentId,
            $"Resident {request.FormData.FirstName} {request.FormData.LastName} created successfully!"
        );
    }

    private static Dictionary<string, string[]> ValidateStep(int step, WizardFormData data)
    {
        var errors = new Dictionary<string, string[]>();

        switch (step)
        {
            case 1:
                if (string.IsNullOrWhiteSpace(data.FirstName))
                    errors["firstName"] = ["First name is required"];
                if (string.IsNullOrWhiteSpace(data.LastName))
                    errors["lastName"] = ["Last name is required"];
                if (!data.DateOfBirth.HasValue)
                    errors["dateOfBirth"] = ["Date of birth is required"];
                break;

            case 2:
                if (string.IsNullOrWhiteSpace(data.PrimaryPhysician))
                    errors["primaryPhysician"] = ["Primary physician is required"];
                // Conditional: If has insurance, require provider and policy
                if (data.HasInsurance)
                {
                    if (string.IsNullOrWhiteSpace(data.InsuranceProvider))
                        errors["insuranceProvider"] = ["Insurance provider is required when insured"];
                    if (string.IsNullOrWhiteSpace(data.InsurancePolicyNumber))
                        errors["insurancePolicyNumber"] = ["Policy number is required when insured"];
                }
                break;

            case 3:
                if (string.IsNullOrWhiteSpace(data.EmergencyContactName))
                    errors["emergencyContactName"] = ["Emergency contact name is required"];
                if (string.IsNullOrWhiteSpace(data.EmergencyContactPhone))
                    errors["emergencyContactPhone"] = ["Emergency contact phone is required"];
                break;

            case 4:
                if (!data.BuildingId.HasValue)
                    errors["buildingId"] = ["Building is required"];
                if (!data.RoomNumber.HasValue)
                    errors["roomNumber"] = ["Room number is required"];
                break;
        }

        return errors;
    }
}
