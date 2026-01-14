namespace Moj.Sandbox.DomainModels.SeniorLiving;

/// <summary>
/// Level of care required for a resident
/// </summary>
public enum CareLevel
{
    Independent,
    AssistedLiving,
    MemoryCare,
    SkilledNursing,
    Hospice
}

/// <summary>
/// Status of a resident
/// </summary>
public enum ResidentStatus
{
    Active,
    Temporary,
    OnLeave,
    Hospitalized,
    Discharged,
    Deceased
}

/// <summary>
/// Type of room
/// </summary>
public enum RoomType
{
    Studio,
    OneBedroom,
    TwoBedroom,
    Suite,
    Shared,
    PrivateMemoryCare
}

/// <summary>
/// Medication schedule frequency
/// </summary>
public enum MedicationFrequency
{
    AsNeeded,
    Daily,
    TwiceDaily,
    ThreeTimesDaily,
    FourTimesDaily,
    Weekly,
    Monthly
}

/// <summary>
/// Alert severity level
/// </summary>
public enum AlertSeverity
{
    Info,
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Type of activity
/// </summary>
public enum ActivityType
{
    Social,
    Physical,
    Cognitive,
    Creative,
    Spiritual,
    Medical,
    Dining,
    Therapy
}

/// <summary>
/// Staff role
/// </summary>
public enum StaffRole
{
    Caregiver,
    Nurse,
    Physician,
    ActivityDirector,
    Administrator,
    Dietitian,
    PhysicalTherapist,
    SocialWorker
}

/// <summary>
/// Mobility level
/// </summary>
public enum MobilityLevel
{
    Independent,
    CaneOrWalker,
    Wheelchair,
    Bedridden,
    RequiresAssistance
}

/// <summary>
/// Diet type
/// </summary>
public enum DietType
{
    Regular,
    Diabetic,
    LowSodium,
    Pureed,
    Mechanical,
    GlutenFree,
    Vegetarian,
    Kosher
}
