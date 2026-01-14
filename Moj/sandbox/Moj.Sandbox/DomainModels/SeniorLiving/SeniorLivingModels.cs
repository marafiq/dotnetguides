namespace Moj.Sandbox.DomainModels.SeniorLiving;

/// <summary>
/// Contact information
/// </summary>
public record ContactInfo
{
    public string Phone { get; init; } = "";
    public string? AlternatePhone { get; init; }
    public string Email { get; init; } = "";
    public string? PreferredContactMethod { get; init; }
}

/// <summary>
/// Address
/// </summary>
public record Address
{
    public string Street { get; init; } = "";
    public string? Unit { get; init; }
    public string City { get; init; } = "";
    public string State { get; init; } = "";
    public string ZipCode { get; init; } = "";
    public string Country { get; init; } = "USA";
}

/// <summary>
/// Emergency contact for a resident
/// </summary>
public record EmergencyContact
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Relationship { get; init; } = "";
    public ContactInfo Contact { get; init; } = new();
    public Address? Address { get; init; }
    public bool IsPrimaryContact { get; init; }
    public bool HasPowerOfAttorney { get; init; }
    public bool IsHealthcareProxy { get; init; }
}

/// <summary>
/// Room in the facility
/// </summary>
public record Room
{
    public string Id { get; init; } = "";
    public string Number { get; init; } = "";
    public int Floor { get; init; }
    public string Building { get; init; } = "";
    public RoomType Type { get; init; }
    public int Capacity { get; init; } = 1;
    public List<string> Amenities { get; init; } = [];
    public decimal MonthlyRate { get; init; }
    public bool IsAvailable { get; init; }
    public string? CurrentResidentId { get; init; }
}

/// <summary>
/// Staff member
/// </summary>
public record StaffMember
{
    public string Id { get; init; } = "";
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public StaffRole Role { get; init; }
    public string? Credentials { get; init; }
    public ContactInfo Contact { get; init; } = new();
    public List<string> AssignedResidentIds { get; init; } = [];
    public string? PhotoUrl { get; init; }
    public bool IsOnDuty { get; init; }
}

/// <summary>
/// Medication prescription
/// </summary>
public record Medication
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string GenericName { get; init; } = "";
    public string Dosage { get; init; } = "";
    public string Route { get; init; } = "Oral";
    public MedicationFrequency Frequency { get; init; }
    public List<string> ScheduleTimes { get; init; } = [];
    public string Prescriber { get; init; } = "";
    public string Pharmacy { get; init; } = "";
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Instructions { get; init; }
    public List<string> SideEffects { get; init; } = [];
    public List<string> Interactions { get; init; } = [];
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Health condition
/// </summary>
public record HealthCondition
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? IcdCode { get; init; }
    public DateTime DiagnosedDate { get; init; }
    public string? DiagnosedBy { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Allergy information
/// </summary>
public record Allergy
{
    public string Id { get; init; } = "";
    public string Allergen { get; init; } = "";
    public string Type { get; init; } = ""; // Food, Drug, Environmental
    public string Severity { get; init; } = ""; // Mild, Moderate, Severe
    public string Reaction { get; init; } = "";
    public DateTime? IdentifiedDate { get; init; }
}

/// <summary>
/// Vital signs reading
/// </summary>
public record VitalSigns
{
    public string Id { get; init; } = "";
    public DateTime RecordedAt { get; init; }
    public string RecordedBy { get; init; } = "";
    public int? BloodPressureSystolic { get; init; }
    public int? BloodPressureDiastolic { get; init; }
    public int? HeartRate { get; init; }
    public decimal? Temperature { get; init; }
    public int? RespiratoryRate { get; init; }
    public int? OxygenSaturation { get; init; }
    public decimal? Weight { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Care plan goal
/// </summary>
public record CarePlanGoal
{
    public string Id { get; init; } = "";
    public string Description { get; init; } = "";
    public string Category { get; init; } = "";
    public DateTime TargetDate { get; init; }
    public string Status { get; init; } = "Active";
    public int ProgressPercent { get; init; }
    public List<string> Interventions { get; init; } = [];
    public string? Outcome { get; init; }
}

/// <summary>
/// Care plan for a resident
/// </summary>
public record CarePlan
{
    public string Id { get; init; } = "";
    public DateTime CreatedDate { get; init; }
    public DateTime LastReviewDate { get; init; }
    public DateTime NextReviewDate { get; init; }
    public string PrimaryCaregiverId { get; init; } = "";
    public List<CarePlanGoal> Goals { get; init; } = [];
    public string? SpecialInstructions { get; init; }
    public List<string> DailyRoutine { get; init; } = [];
    public MobilityLevel MobilityLevel { get; init; }
    public DietType DietType { get; init; }
    public List<string> DietaryRestrictions { get; init; } = [];
    public bool RequiresAssistanceWithAdls { get; init; }
    public List<string> AdlAssistanceNeeded { get; init; } = [];
}

/// <summary>
/// Health record for a resident
/// </summary>
public record HealthRecord
{
    public string Id { get; init; } = "";
    public string ResidentId { get; init; } = "";
    public List<HealthCondition> Conditions { get; init; } = [];
    public List<Allergy> Allergies { get; init; } = [];
    public List<Medication> Medications { get; init; } = [];
    public List<VitalSigns> VitalHistory { get; init; } = [];
    public VitalSigns? LatestVitals { get; init; }
    public string? BloodType { get; init; }
    public string? PrimaryPhysician { get; init; }
    public string? PhysicianPhone { get; init; }
    public DateTime? LastPhysicianVisit { get; init; }
    public DateTime? NextPhysicianVisit { get; init; }
    public List<string> Immunizations { get; init; } = [];
    public string? AdvanceDirectives { get; init; }
    public bool HasDnr { get; init; }
}

/// <summary>
/// Activity/event in the facility
/// </summary>
public record FacilityActivity
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public ActivityType Type { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string Location { get; init; } = "";
    public string? LeadStaffId { get; init; }
    public int MaxParticipants { get; init; }
    public List<string> RegisteredResidentIds { get; init; } = [];
    public bool IsRecurring { get; init; }
    public string? RecurrencePattern { get; init; }
}

/// <summary>
/// Alert or notification for a resident
/// </summary>
public record ResidentAlert
{
    public string Id { get; init; } = "";
    public string ResidentId { get; init; } = "";
    public string Type { get; init; } = "";
    public AlertSeverity Severity { get; init; }
    public string Message { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public DateTime? AcknowledgedAt { get; init; }
    public string? AcknowledgedBy { get; init; }
    public bool IsResolved { get; init; }
    public string? Resolution { get; init; }
}

/// <summary>
/// Main Resident entity
/// </summary>
public record Resident
{
    public string Id { get; init; } = "";
    public string FirstName { get; init; } = "";
    public string MiddleName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string? PreferredName { get; init; }
    public DateTime DateOfBirth { get; init; }
    public string Gender { get; init; } = "";
    public string? PhotoUrl { get; init; }

    // Status & Care
    public ResidentStatus Status { get; init; }
    public CareLevel CareLevel { get; init; }
    public DateTime AdmissionDate { get; init; }
    public DateTime? DischargeDate { get; init; }

    // Room
    public Room? Room { get; init; }

    // Contacts
    public List<EmergencyContact> EmergencyContacts { get; init; } = [];
    public ContactInfo? PersonalContact { get; init; }

    // Health
    public HealthRecord? HealthRecord { get; init; }
    public CarePlan? CarePlan { get; init; }

    // Assigned Staff
    public List<StaffMember> AssignedStaff { get; init; } = [];
    public string? PrimaryCaregiverId { get; init; }

    // Activities & Alerts
    public List<string> EnrolledActivityIds { get; init; } = [];
    public List<ResidentAlert> ActiveAlerts { get; init; } = [];

    // Preferences
    public List<string> Interests { get; init; } = [];
    public List<string> Languages { get; init; } = [];
    public string? ReligiousPreference { get; init; }

    // Insurance & Billing
    public string? InsuranceProvider { get; init; }
    public string? InsurancePolicyNumber { get; init; }
    public string? MedicaidId { get; init; }
    public string? MedicareId { get; init; }

    // Notes
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Application state for resident management
/// </summary>
public record ResidentAppState
{
    public List<Resident> Residents { get; init; } = [];
    public Resident? SelectedResident { get; init; }
    public List<Room> Rooms { get; init; } = [];
    public List<StaffMember> Staff { get; init; } = [];
    public List<FacilityActivity> Activities { get; init; } = [];
    public List<ResidentAlert> UnacknowledgedAlerts { get; init; } = [];

    // Current user (staff member)
    public StaffMember? CurrentUser { get; init; }

    // Loading states
    public bool IsLoading { get; init; }
    public string? Error { get; init; }

    // Filters
    public ResidentFilters Filters { get; init; } = new();

    // UI State
    public ResidentUiState Ui { get; init; } = new();
}

/// <summary>
/// Filters for resident list
/// </summary>
public record ResidentFilters
{
    public List<ResidentStatus> Statuses { get; init; } = [];
    public List<CareLevel> CareLevels { get; init; } = [];
    public string? Building { get; init; }
    public int? Floor { get; init; }
    public string? SearchQuery { get; init; }
    public string? AssignedStaffId { get; init; }
    public bool? HasActiveAlerts { get; init; }
    public DateTime? AdmittedAfter { get; init; }
    public DateTime? AdmittedBefore { get; init; }
}

/// <summary>
/// UI state for resident management
/// </summary>
public record ResidentUiState
{
    public bool SidebarOpen { get; init; } = true;
    public string ActiveTab { get; init; } = "overview";
    public string ViewMode { get; init; } = "list";
    public ResidentSortConfig Sort { get; init; } = new();
    public PaginationState Pagination { get; init; } = new();
    public bool ShowAlertPanel { get; init; }
    public bool ShowActivityCalendar { get; init; }
}

/// <summary>
/// Sort configuration
/// </summary>
public record ResidentSortConfig
{
    public string Field { get; init; } = "lastName";
    public string Direction { get; init; } = "asc";
}

/// <summary>
/// Pagination state
/// </summary>
public record PaginationState
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
}
