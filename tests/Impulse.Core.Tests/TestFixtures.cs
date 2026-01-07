// Test fixture types for ComponentPathConvention tests
// These need to be in specific namespace patterns to test the convention

namespace TestTypes.Features.Dashboard
{
    public record DashboardProps(int TotalCount);
    public record DashboardStatsProps(int ActiveCount, int InactiveCount);
    public record NotificationsProps(IReadOnlyList<string> Messages);
}

namespace TestTypes.Features.Residents
{
    public record ResidentDetailProps(int Id, string Name);
    public record ResidentsListProps(IReadOnlyList<ResidentDetailProps> Items);
    public record ResidentHistoryProps(int ResidentId, IReadOnlyList<string> Events);
}

namespace TestTypes.Features.Users
{
    public record UserProfileProps(string UserId, string Email);
}

namespace TestTypes.Features.Admin.Settings
{
    public record SettingsProps(bool DarkMode);
}

namespace TestTypes.Features.Help
{
    public record HelpModel(string Title, string Content);
}

namespace TestTypes.NotInFeatures
{
    public record SomeProps(string Data);
}

// Mutation request/response types for testing
namespace TestTypes
{
    public record CreateResidentRequest(string Name, string Email);
    public record CreateResidentResponse(int Id, string Name);
    public record UpdateResidentRequest(int Id, string Name, string Email);
    public record UpdateResidentResponse(int Id, bool Success);
}
