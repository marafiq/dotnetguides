namespace Impulse.IntegrationTests.Features.DynamicForms;

/// <summary>
/// Dynamic form configuration with server-defined conditional visibility rules.
/// The server sends form schema including which fields are visible based on conditions.
/// </summary>

// ============================================================================
// Main Props - Server sends this to render the dynamic form
// ============================================================================

public record DynamicFormProps(
    string FormTitle,
    string FormDescription,
    IReadOnlyList<FormSection> Sections,
    FormValues Values,
    IReadOnlyDictionary<string, string[]> ValidationErrors,
    bool IsSubmitting
);

// ============================================================================
// Form Structure Types
// ============================================================================

/// <summary>
/// A section groups related fields together.
/// </summary>
public record FormSection(
    string Id,
    string Title,
    string? Description,
    IReadOnlyList<FormField> Fields,
    VisibilityCondition? VisibleWhen
);

/// <summary>
/// Individual form field with conditional visibility.
/// </summary>
public record FormField(
    string Name,
    string Label,
    FieldType Type,
    bool Required,
    string? Placeholder,
    string? HelpText,
    IReadOnlyList<SelectOption>? Options,
    FieldConstraints? Constraints,
    VisibilityCondition? VisibleWhen
);

/// <summary>
/// Select/radio/checkbox options.
/// </summary>
public record SelectOption(
    string Value,
    string Label,
    bool? Disabled
);

/// <summary>
/// Field validation constraints sent to client for immediate feedback.
/// </summary>
public record FieldConstraints(
    int? MinLength,
    int? MaxLength,
    decimal? Min,
    decimal? Max,
    string? Pattern,
    string? PatternMessage
);

// ============================================================================
// Conditional Visibility - Server defines when fields/sections appear
// ============================================================================

/// <summary>
/// Visibility condition evaluated on the client based on form values.
/// Supports simple equality and compound AND/OR conditions.
/// </summary>
public record VisibilityCondition(
    string? Field,
    ConditionOperator? Operator,
    object? Value,
    IReadOnlyList<VisibilityCondition>? And,
    IReadOnlyList<VisibilityCondition>? Or
);

public enum ConditionOperator
{
    Equals,
    NotEquals,
    Contains,
    NotContains,
    GreaterThan,
    LessThan,
    IsEmpty,
    IsNotEmpty
}

public enum FieldType
{
    Text,
    Email,
    Phone,
    Number,
    Currency,
    Date,
    DateTime,
    Select,
    MultiSelect,
    Radio,
    Checkbox,
    Textarea,
    File,
    Hidden
}

// ============================================================================
// Form Values - Flexible key-value storage for dynamic fields
// ============================================================================

/// <summary>
/// Form values with strongly-typed accessors for common patterns.
/// Uses Dictionary for flexibility with dynamic field names.
/// </summary>
public record FormValues(
    IReadOnlyDictionary<string, string> StringValues,
    IReadOnlyDictionary<string, decimal> NumberValues,
    IReadOnlyDictionary<string, bool> BooleanValues,
    IReadOnlyDictionary<string, string[]> MultiSelectValues,
    IReadOnlyDictionary<string, DateTime> DateValues
);

// ============================================================================
// Mutation Types - Form submission and field updates
// ============================================================================

public record FormSubmitRequest(
    string FormId,
    FormValues Values
);

public record FormSubmitResponse(
    bool Success,
    IReadOnlyDictionary<string, string[]>? ValidationErrors,
    string? RedirectUrl,
    string? Message
);

public record FieldUpdateRequest(
    string FormId,
    string FieldName,
    object? Value
);

public record FieldUpdateResponse(
    bool Success,
    IReadOnlyList<string> FieldsToShow,
    IReadOnlyList<string> FieldsToHide,
    IReadOnlyDictionary<string, string[]>? FieldErrors
);

// ============================================================================
// Example: Insurance Application Form
// Demonstrates complex conditional logic
// ============================================================================

public record InsuranceApplicationProps(
    string ApplicantName,
    InsuranceFormData FormData,
    InsuranceFormSchema Schema,
    IReadOnlyDictionary<string, string[]> Errors
);

public record InsuranceFormData(
    // Basic Info
    string? FirstName,
    string? LastName,
    DateTime? DateOfBirth,
    string? Email,
    string? Phone,

    // Employment - shows salary fields
    string? EmploymentStatus,
    string? Employer,
    decimal? AnnualSalary,

    // Insurance Type - shows type-specific fields
    string? InsuranceType,

    // Auto Insurance fields (visible when InsuranceType == "Auto")
    string? VehicleMake,
    string? VehicleModel,
    int? VehicleYear,
    string? VinNumber,

    // Home Insurance fields (visible when InsuranceType == "Home")
    string? PropertyAddress,
    decimal? PropertyValue,
    int? YearBuilt,
    string? ConstructionType,

    // Life Insurance fields (visible when InsuranceType == "Life")
    decimal? CoverageAmount,
    string? Beneficiary,
    bool? IsSmoker,
    IReadOnlyList<string>? PreExistingConditions,

    // Payment
    string? PaymentFrequency,
    string? PaymentMethod,

    // Bank details (visible when PaymentMethod == "BankTransfer")
    string? BankName,
    string? AccountNumber,
    string? RoutingNumber,

    // Card details (visible when PaymentMethod == "CreditCard")
    string? CardNumber,
    string? CardExpiry,
    string? CardCvv
);

public record InsuranceFormSchema(
    IReadOnlyList<FormSection> Sections,
    IReadOnlyDictionary<string, VisibilityCondition> ConditionalFields
);
