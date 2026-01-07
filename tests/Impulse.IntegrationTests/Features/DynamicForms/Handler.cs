using Impulse.Core;
using Impulse.Validation;
using FluentValidation;

namespace Impulse.IntegrationTests.Features.DynamicForms;

/// <summary>
/// Handler for dynamic forms with conditional visibility.
/// Demonstrates server-driven form schema with runtime visibility evaluation.
/// </summary>
public static class DynamicFormHandler
{
    /// <summary>
    /// Creates the dynamic form props with all sections and fields.
    /// The client uses the visibility conditions to show/hide fields.
    /// </summary>
    public static DynamicFormProps CreateInsuranceForm(FormValues? existingValues = null)
    {
        var values = existingValues ?? CreateEmptyFormValues();

        return new DynamicFormProps(
            FormTitle: "Insurance Application",
            FormDescription: "Complete all required fields. Some sections will appear based on your selections.",
            Sections: CreateInsuranceFormSections(),
            Values: values,
            ValidationErrors: new Dictionary<string, string[]>(),
            IsSubmitting: false
        );
    }

    private static FormValues CreateEmptyFormValues() => new(
        StringValues: new Dictionary<string, string>(),
        NumberValues: new Dictionary<string, decimal>(),
        BooleanValues: new Dictionary<string, bool>(),
        MultiSelectValues: new Dictionary<string, string[]>(),
        DateValues: new Dictionary<string, DateTime>()
    );

    private static IReadOnlyList<FormSection> CreateInsuranceFormSections()
    {
        return new List<FormSection>
        {
            // ================================================================
            // Section 1: Personal Information (Always visible)
            // ================================================================
            new FormSection(
                Id: "personal",
                Title: "Personal Information",
                Description: "Please provide your personal details.",
                Fields: new List<FormField>
                {
                    new FormField(
                        Name: "firstName",
                        Label: "First Name",
                        Type: FieldType.Text,
                        Required: true,
                        Placeholder: "Enter your first name",
                        HelpText: null,
                        Options: null,
                        Constraints: new FieldConstraints(MinLength: 2, MaxLength: 50, Min: null, Max: null, Pattern: null, PatternMessage: null),
                        VisibleWhen: null
                    ),
                    new FormField(
                        Name: "lastName",
                        Label: "Last Name",
                        Type: FieldType.Text,
                        Required: true,
                        Placeholder: "Enter your last name",
                        HelpText: null,
                        Options: null,
                        Constraints: new FieldConstraints(MinLength: 2, MaxLength: 50, Min: null, Max: null, Pattern: null, PatternMessage: null),
                        VisibleWhen: null
                    ),
                    new FormField(
                        Name: "dateOfBirth",
                        Label: "Date of Birth",
                        Type: FieldType.Date,
                        Required: true,
                        Placeholder: null,
                        HelpText: "You must be at least 18 years old.",
                        Options: null,
                        Constraints: null,
                        VisibleWhen: null
                    ),
                    new FormField(
                        Name: "email",
                        Label: "Email Address",
                        Type: FieldType.Email,
                        Required: true,
                        Placeholder: "you@example.com",
                        HelpText: null,
                        Options: null,
                        Constraints: null,
                        VisibleWhen: null
                    ),
                    new FormField(
                        Name: "phone",
                        Label: "Phone Number",
                        Type: FieldType.Phone,
                        Required: true,
                        Placeholder: "(555) 123-4567",
                        HelpText: null,
                        Options: null,
                        Constraints: new FieldConstraints(MinLength: 10, MaxLength: 15, Min: null, Max: null, Pattern: @"^\+?[\d\s\-\(\)]+$", PatternMessage: "Enter a valid phone number"),
                        VisibleWhen: null
                    )
                },
                VisibleWhen: null
            ),

            // ================================================================
            // Section 2: Employment (Always visible, but salary fields conditional)
            // ================================================================
            new FormSection(
                Id: "employment",
                Title: "Employment Information",
                Description: null,
                Fields: new List<FormField>
                {
                    new FormField(
                        Name: "employmentStatus",
                        Label: "Employment Status",
                        Type: FieldType.Select,
                        Required: true,
                        Placeholder: null,
                        HelpText: null,
                        Options: new List<SelectOption>
                        {
                            new("employed", "Employed Full-Time", null),
                            new("partTime", "Employed Part-Time", null),
                            new("selfEmployed", "Self-Employed", null),
                            new("unemployed", "Unemployed", null),
                            new("retired", "Retired", null),
                            new("student", "Student", null)
                        },
                        Constraints: null,
                        VisibleWhen: null
                    ),
                    // Employer - visible when employed
                    new FormField(
                        Name: "employer",
                        Label: "Employer Name",
                        Type: FieldType.Text,
                        Required: true,
                        Placeholder: "Company name",
                        HelpText: null,
                        Options: null,
                        Constraints: null,
                        VisibleWhen: new VisibilityCondition(
                            Field: "employmentStatus",
                            Operator: ConditionOperator.Equals,
                            Value: "employed",
                            And: null,
                            Or: new List<VisibilityCondition>
                            {
                                new("employmentStatus", ConditionOperator.Equals, "employed", null, null),
                                new("employmentStatus", ConditionOperator.Equals, "partTime", null, null)
                            }
                        )
                    ),
                    // Annual salary - visible when employed or self-employed
                    new FormField(
                        Name: "annualSalary",
                        Label: "Annual Income",
                        Type: FieldType.Currency,
                        Required: true,
                        Placeholder: "50000",
                        HelpText: "Your gross annual income before taxes.",
                        Options: null,
                        Constraints: new FieldConstraints(MinLength: null, MaxLength: null, Min: 0, Max: 10000000, Pattern: null, PatternMessage: null),
                        VisibleWhen: new VisibilityCondition(
                            Field: null,
                            Operator: null,
                            Value: null,
                            And: null,
                            Or: new List<VisibilityCondition>
                            {
                                new("employmentStatus", ConditionOperator.Equals, "employed", null, null),
                                new("employmentStatus", ConditionOperator.Equals, "partTime", null, null),
                                new("employmentStatus", ConditionOperator.Equals, "selfEmployed", null, null)
                            }
                        )
                    )
                },
                VisibleWhen: null
            ),

            // ================================================================
            // Section 3: Insurance Type Selection
            // ================================================================
            new FormSection(
                Id: "insuranceType",
                Title: "Insurance Type",
                Description: "Select the type of insurance you need.",
                Fields: new List<FormField>
                {
                    new FormField(
                        Name: "insuranceType",
                        Label: "Type of Insurance",
                        Type: FieldType.Radio,
                        Required: true,
                        Placeholder: null,
                        HelpText: "Additional fields will appear based on your selection.",
                        Options: new List<SelectOption>
                        {
                            new("auto", "Auto Insurance", null),
                            new("home", "Home Insurance", null),
                            new("life", "Life Insurance", null)
                        },
                        Constraints: null,
                        VisibleWhen: null
                    )
                },
                VisibleWhen: null
            ),

            // ================================================================
            // Section 4: Auto Insurance Details (Conditional)
            // ================================================================
            new FormSection(
                Id: "autoDetails",
                Title: "Vehicle Information",
                Description: "Provide details about your vehicle.",
                Fields: new List<FormField>
                {
                    new FormField(
                        Name: "vehicleMake",
                        Label: "Vehicle Make",
                        Type: FieldType.Text,
                        Required: true,
                        Placeholder: "e.g., Toyota",
                        HelpText: null,
                        Options: null,
                        Constraints: null,
                        VisibleWhen: null
                    ),
                    new FormField(
                        Name: "vehicleModel",
                        Label: "Vehicle Model",
                        Type: FieldType.Text,
                        Required: true,
                        Placeholder: "e.g., Camry",
                        HelpText: null,
                        Options: null,
                        Constraints: null,
                        VisibleWhen: null
                    ),
                    new FormField(
                        Name: "vehicleYear",
                        Label: "Vehicle Year",
                        Type: FieldType.Number,
                        Required: true,
                        Placeholder: "2023",
                        HelpText: null,
                        Options: null,
                        Constraints: new FieldConstraints(MinLength: null, MaxLength: null, Min: 1900, Max: 2030, Pattern: null, PatternMessage: null),
                        VisibleWhen: null
                    ),
                    new FormField(
                        Name: "vinNumber",
                        Label: "VIN Number",
                        Type: FieldType.Text,
                        Required: true,
                        Placeholder: "17-character VIN",
                        HelpText: "Found on your registration or dashboard.",
                        Options: null,
                        Constraints: new FieldConstraints(MinLength: 17, MaxLength: 17, Min: null, Max: null, Pattern: @"^[A-HJ-NPR-Z0-9]{17}$", PatternMessage: "Enter a valid 17-character VIN"),
                        VisibleWhen: null
                    )
                },
                // Section only visible when insuranceType == "auto"
                VisibleWhen: new VisibilityCondition(
                    Field: "insuranceType",
                    Operator: ConditionOperator.Equals,
                    Value: "auto",
                    And: null,
                    Or: null
                )
            ),

            // ================================================================
            // Section 5: Home Insurance Details (Conditional)
            // ================================================================
            new FormSection(
                Id: "homeDetails",
                Title: "Property Information",
                Description: "Provide details about your property.",
                Fields: new List<FormField>
                {
                    new FormField(
                        Name: "propertyAddress",
                        Label: "Property Address",
                        Type: FieldType.Textarea,
                        Required: true,
                        Placeholder: "Full street address",
                        HelpText: null,
                        Options: null,
                        Constraints: null,
                        VisibleWhen: null
                    ),
                    new FormField(
                        Name: "propertyValue",
                        Label: "Estimated Property Value",
                        Type: FieldType.Currency,
                        Required: true,
                        Placeholder: "250000",
                        HelpText: null,
                        Options: null,
                        Constraints: new FieldConstraints(MinLength: null, MaxLength: null, Min: 50000, Max: 50000000, Pattern: null, PatternMessage: null),
                        VisibleWhen: null
                    ),
                    new FormField(
                        Name: "yearBuilt",
                        Label: "Year Built",
                        Type: FieldType.Number,
                        Required: true,
                        Placeholder: "1990",
                        HelpText: null,
                        Options: null,
                        Constraints: new FieldConstraints(MinLength: null, MaxLength: null, Min: 1800, Max: 2030, Pattern: null, PatternMessage: null),
                        VisibleWhen: null
                    ),
                    new FormField(
                        Name: "constructionType",
                        Label: "Construction Type",
                        Type: FieldType.Select,
                        Required: true,
                        Placeholder: null,
                        HelpText: null,
                        Options: new List<SelectOption>
                        {
                            new("frame", "Wood Frame", null),
                            new("masonry", "Masonry/Brick", null),
                            new("steel", "Steel Frame", null),
                            new("concrete", "Concrete", null)
                        },
                        Constraints: null,
                        VisibleWhen: null
                    )
                },
                // Section only visible when insuranceType == "home"
                VisibleWhen: new VisibilityCondition(
                    Field: "insuranceType",
                    Operator: ConditionOperator.Equals,
                    Value: "home",
                    And: null,
                    Or: null
                )
            ),

            // ================================================================
            // Section 6: Life Insurance Details (Conditional)
            // ================================================================
            new FormSection(
                Id: "lifeDetails",
                Title: "Life Insurance Details",
                Description: "Provide information for your life insurance policy.",
                Fields: new List<FormField>
                {
                    new FormField(
                        Name: "coverageAmount",
                        Label: "Coverage Amount",
                        Type: FieldType.Currency,
                        Required: true,
                        Placeholder: "500000",
                        HelpText: "The death benefit amount.",
                        Options: null,
                        Constraints: new FieldConstraints(MinLength: null, MaxLength: null, Min: 10000, Max: 10000000, Pattern: null, PatternMessage: null),
                        VisibleWhen: null
                    ),
                    new FormField(
                        Name: "beneficiary",
                        Label: "Primary Beneficiary",
                        Type: FieldType.Text,
                        Required: true,
                        Placeholder: "Full name of beneficiary",
                        HelpText: null,
                        Options: null,
                        Constraints: null,
                        VisibleWhen: null
                    ),
                    new FormField(
                        Name: "isSmoker",
                        Label: "Are you a smoker?",
                        Type: FieldType.Checkbox,
                        Required: false,
                        Placeholder: null,
                        HelpText: "Including e-cigarettes or cigars in the past 12 months.",
                        Options: null,
                        Constraints: null,
                        VisibleWhen: null
                    ),
                    new FormField(
                        Name: "preExistingConditions",
                        Label: "Pre-existing Conditions",
                        Type: FieldType.MultiSelect,
                        Required: false,
                        Placeholder: null,
                        HelpText: "Select any conditions that apply.",
                        Options: new List<SelectOption>
                        {
                            new("diabetes", "Diabetes", null),
                            new("heartDisease", "Heart Disease", null),
                            new("cancer", "Cancer (history)", null),
                            new("hypertension", "High Blood Pressure", null),
                            new("none", "None of the above", null)
                        },
                        Constraints: null,
                        VisibleWhen: null
                    )
                },
                // Section only visible when insuranceType == "life"
                VisibleWhen: new VisibilityCondition(
                    Field: "insuranceType",
                    Operator: ConditionOperator.Equals,
                    Value: "life",
                    And: null,
                    Or: null
                )
            ),

            // ================================================================
            // Section 7: Payment Information
            // ================================================================
            new FormSection(
                Id: "payment",
                Title: "Payment Information",
                Description: "How would you like to pay for your policy?",
                Fields: new List<FormField>
                {
                    new FormField(
                        Name: "paymentFrequency",
                        Label: "Payment Frequency",
                        Type: FieldType.Select,
                        Required: true,
                        Placeholder: null,
                        HelpText: null,
                        Options: new List<SelectOption>
                        {
                            new("monthly", "Monthly", null),
                            new("quarterly", "Quarterly", null),
                            new("semiAnnual", "Semi-Annual", null),
                            new("annual", "Annual", null)
                        },
                        Constraints: null,
                        VisibleWhen: null
                    ),
                    new FormField(
                        Name: "paymentMethod",
                        Label: "Payment Method",
                        Type: FieldType.Radio,
                        Required: true,
                        Placeholder: null,
                        HelpText: null,
                        Options: new List<SelectOption>
                        {
                            new("bankTransfer", "Bank Transfer (ACH)", null),
                            new("creditCard", "Credit Card", null),
                            new("check", "Check by Mail", null)
                        },
                        Constraints: null,
                        VisibleWhen: null
                    ),
                    // Bank fields - visible when paymentMethod == "bankTransfer"
                    new FormField(
                        Name: "bankName",
                        Label: "Bank Name",
                        Type: FieldType.Text,
                        Required: true,
                        Placeholder: "Your bank name",
                        HelpText: null,
                        Options: null,
                        Constraints: null,
                        VisibleWhen: new VisibilityCondition("paymentMethod", ConditionOperator.Equals, "bankTransfer", null, null)
                    ),
                    new FormField(
                        Name: "accountNumber",
                        Label: "Account Number",
                        Type: FieldType.Text,
                        Required: true,
                        Placeholder: "Account number",
                        HelpText: null,
                        Options: null,
                        Constraints: new FieldConstraints(MinLength: 8, MaxLength: 17, Min: null, Max: null, Pattern: @"^\d+$", PatternMessage: "Enter only numbers"),
                        VisibleWhen: new VisibilityCondition("paymentMethod", ConditionOperator.Equals, "bankTransfer", null, null)
                    ),
                    new FormField(
                        Name: "routingNumber",
                        Label: "Routing Number",
                        Type: FieldType.Text,
                        Required: true,
                        Placeholder: "9-digit routing number",
                        HelpText: null,
                        Options: null,
                        Constraints: new FieldConstraints(MinLength: 9, MaxLength: 9, Min: null, Max: null, Pattern: @"^\d{9}$", PatternMessage: "Enter a valid 9-digit routing number"),
                        VisibleWhen: new VisibilityCondition("paymentMethod", ConditionOperator.Equals, "bankTransfer", null, null)
                    ),
                    // Credit card fields - visible when paymentMethod == "creditCard"
                    new FormField(
                        Name: "cardNumber",
                        Label: "Card Number",
                        Type: FieldType.Text,
                        Required: true,
                        Placeholder: "1234 5678 9012 3456",
                        HelpText: null,
                        Options: null,
                        Constraints: new FieldConstraints(MinLength: 15, MaxLength: 19, Min: null, Max: null, Pattern: @"^[\d\s]+$", PatternMessage: "Enter a valid card number"),
                        VisibleWhen: new VisibilityCondition("paymentMethod", ConditionOperator.Equals, "creditCard", null, null)
                    ),
                    new FormField(
                        Name: "cardExpiry",
                        Label: "Expiration Date",
                        Type: FieldType.Text,
                        Required: true,
                        Placeholder: "MM/YY",
                        HelpText: null,
                        Options: null,
                        Constraints: new FieldConstraints(MinLength: 5, MaxLength: 5, Min: null, Max: null, Pattern: @"^\d{2}/\d{2}$", PatternMessage: "Enter in MM/YY format"),
                        VisibleWhen: new VisibilityCondition("paymentMethod", ConditionOperator.Equals, "creditCard", null, null)
                    ),
                    new FormField(
                        Name: "cardCvv",
                        Label: "CVV",
                        Type: FieldType.Text,
                        Required: true,
                        Placeholder: "123",
                        HelpText: "3 or 4 digit code on your card",
                        Options: null,
                        Constraints: new FieldConstraints(MinLength: 3, MaxLength: 4, Min: null, Max: null, Pattern: @"^\d{3,4}$", PatternMessage: "Enter a valid CVV"),
                        VisibleWhen: new VisibilityCondition("paymentMethod", ConditionOperator.Equals, "creditCard", null, null)
                    )
                },
                VisibleWhen: null
            )
        };
    }
}

// ============================================================================
// FluentValidation for Dynamic Form Submission
// ============================================================================

public class FormSubmitRequestValidator : AbstractValidator<FormSubmitRequest>
{
    public FormSubmitRequestValidator()
    {
        RuleFor(x => x.FormId).NotEmpty();
        RuleFor(x => x.Values).NotNull();
    }
}

/// <summary>
/// Server-side validation that evaluates conditional requirements.
/// Fields that are hidden don't need to be validated.
/// </summary>
public class InsuranceFormValidator : AbstractValidator<InsuranceFormData>
{
    public InsuranceFormValidator()
    {
        // Always required fields
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DateOfBirth).NotNull();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.EmploymentStatus).NotEmpty();
        RuleFor(x => x.InsuranceType).NotEmpty();
        RuleFor(x => x.PaymentFrequency).NotEmpty();
        RuleFor(x => x.PaymentMethod).NotEmpty();

        // Conditional: Employer when employed
        RuleFor(x => x.Employer)
            .NotEmpty()
            .When(x => x.EmploymentStatus is "employed" or "partTime")
            .WithMessage("Employer is required for employed applicants.");

        // Conditional: Salary when employed
        RuleFor(x => x.AnnualSalary)
            .NotNull()
            .GreaterThan(0)
            .When(x => x.EmploymentStatus is "employed" or "partTime" or "selfEmployed")
            .WithMessage("Annual income is required.");

        // Conditional: Auto insurance fields
        When(x => x.InsuranceType == "auto", () =>
        {
            RuleFor(x => x.VehicleMake).NotEmpty();
            RuleFor(x => x.VehicleModel).NotEmpty();
            RuleFor(x => x.VehicleYear).NotNull().InclusiveBetween(1900, 2030);
            RuleFor(x => x.VinNumber).NotEmpty().Length(17);
        });

        // Conditional: Home insurance fields
        When(x => x.InsuranceType == "home", () =>
        {
            RuleFor(x => x.PropertyAddress).NotEmpty();
            RuleFor(x => x.PropertyValue).NotNull().GreaterThan(50000);
            RuleFor(x => x.YearBuilt).NotNull().InclusiveBetween(1800, 2030);
            RuleFor(x => x.ConstructionType).NotEmpty();
        });

        // Conditional: Life insurance fields
        When(x => x.InsuranceType == "life", () =>
        {
            RuleFor(x => x.CoverageAmount).NotNull().GreaterThan(10000);
            RuleFor(x => x.Beneficiary).NotEmpty();
        });

        // Conditional: Bank transfer fields
        When(x => x.PaymentMethod == "bankTransfer", () =>
        {
            RuleFor(x => x.BankName).NotEmpty();
            RuleFor(x => x.AccountNumber).NotEmpty().Matches(@"^\d+$");
            RuleFor(x => x.RoutingNumber).NotEmpty().Length(9);
        });

        // Conditional: Credit card fields
        When(x => x.PaymentMethod == "creditCard", () =>
        {
            RuleFor(x => x.CardNumber).NotEmpty().CreditCard();
            RuleFor(x => x.CardExpiry).NotEmpty().Matches(@"^\d{2}/\d{2}$");
            RuleFor(x => x.CardCvv).NotEmpty().Matches(@"^\d{3,4}$");
        });
    }
}
