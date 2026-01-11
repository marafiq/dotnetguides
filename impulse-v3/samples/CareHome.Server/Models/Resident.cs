namespace CareHome.Models;

/// <summary>
/// Care status for a resident.
/// </summary>
public enum CareLevel
{
    Independent,
    Assisted,
    FullCare,
    Memory
}

/// <summary>
/// Dietary requirement types.
/// </summary>
public enum DietaryRequirement
{
    Regular,
    Diabetic,
    LowSodium,
    Vegetarian,
    Vegan,
    GlutenFree,
    Pureed
}

/// <summary>
/// Resident of the care home.
/// </summary>
public record Resident(
    int Id,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string? RoomNumber,
    CareLevel CareLevel,
    DateTime AdmissionDate,
    List<DietaryRequirement> DietaryRequirements,
    EmergencyContact? EmergencyContact);

/// <summary>
/// Emergency contact for a resident.
/// </summary>
public record EmergencyContact(
    string Name,
    string Relationship,
    string Phone,
    string? Email);

/// <summary>
/// Summary view of a resident for lists.
/// </summary>
public record ResidentSummary(
    int Id,
    string FullName,
    string? RoomNumber,
    CareLevel CareLevel,
    int Age);

/// <summary>
/// Request to create a new resident.
/// </summary>
public record CreateResidentRequest(
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string? RoomNumber,
    CareLevel CareLevel,
    List<DietaryRequirement> DietaryRequirements);

/// <summary>
/// Response after creating a resident.
/// </summary>
public record CreateResidentResponse(
    int Id,
    string Message);

/// <summary>
/// Request to update a resident.
/// </summary>
public record UpdateResidentRequest(
    int Id,
    string FirstName,
    string LastName,
    string? RoomNumber,
    CareLevel CareLevel,
    List<DietaryRequirement> DietaryRequirements);

/// <summary>
/// Response after updating a resident.
/// </summary>
public record UpdateResidentResponse(
    bool Success,
    string Message);
