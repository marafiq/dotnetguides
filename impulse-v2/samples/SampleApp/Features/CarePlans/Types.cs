namespace SampleApp.Features.CarePlans;

// Shared value objects for CarePlans feature
public enum CareGoalStatus { Active, Completed, OnHold, Cancelled }
public enum AssessmentType { Initial, Quarterly, Annual, ChangeInCondition }
public enum InterventionFrequency { AsNeeded, Daily, BID, TID, QID, Weekly }

public record Assessment(
    int Id,
    AssessmentType Type,
    DateTime AssessmentDate,
    string AssessorName,
    Dictionary<string, string> Findings,
    List<string> Recommendations);

public record CareGoal(
    int Id,
    string Category,
    string Description,
    string TargetOutcome,
    DateTime TargetDate,
    CareGoalStatus Status,
    List<Intervention> Interventions);

public record Intervention(
    int Id,
    string Description,
    InterventionFrequency Frequency,
    string? ResponsibleRole,
    string? SpecialInstructions,
    bool RequiresDocumentation);
