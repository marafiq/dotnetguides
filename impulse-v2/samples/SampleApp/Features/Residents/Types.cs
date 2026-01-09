namespace SampleApp.Features.Residents;

// Shared value objects for Residents feature
public record Address(
    string Street,
    string City,
    string State,
    string ZipCode);

public record EmergencyContact(
    string Name,
    string Relationship,
    string Phone,
    string? Email);

public record ResidentPreferences(
    string? DietaryRestrictions,
    string? MobilityAids,
    string? CommunicationPreferences,
    bool PrefersMorningCare,
    bool PrefersEveningCare);

public record ResidentSummary(
    int Id,
    string FirstName,
    string LastName,
    string? RoomNumber,
    string Status,
    int Age);
