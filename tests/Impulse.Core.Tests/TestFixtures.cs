// Test fixture types for ComponentPathConvention tests
// These need to be in specific namespace patterns to test the convention

namespace TestTypes.Features.Dashboard
{
    public record DashboardProps(int TotalCount);
}

namespace TestTypes.Features.Residents
{
    public record ResidentDetailProps(int Id, string Name);
    public record ResidentsListProps(IReadOnlyList<ResidentDetailProps> Items);
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
