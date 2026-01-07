using Impulse.Core;

namespace Impulse.Core.Tests;

public static class ComponentPathConventionTests
{
    public static void RunAll()
    {
        TestRunner.Group("ComponentPathConvention", () =>
        {
            TestRunner.Run("GetPath returns ./Dashboard for DashboardProps in Features.Dashboard",
                DashboardProps_Returns_Dashboard);

            TestRunner.Run("GetPath returns ./Residents/Detail for ResidentDetailProps",
                ResidentDetailProps_Returns_Residents_Detail);

            TestRunner.Run("GetPath returns ./Residents/List for ResidentsListProps",
                ResidentsListProps_Returns_Residents_List);

            TestRunner.Run("GetPath returns ./Users/Profile for UserProfileProps",
                UserProfileProps_Returns_Users_Profile);

            TestRunner.Run("GetPath throws for type not in Features namespace",
                NonFeaturesType_Throws);

            TestRunner.Run("GetPath handles deeply nested features",
                DeeplyNested_ReturnsCorrectPath);

            TestRunner.Run("GetPath handles props without Props suffix",
                NoSuffix_ReturnsCorrectPath);
        });
    }

    private static void DashboardProps_Returns_Dashboard()
    {
        var path = ComponentPathConvention.GetPath<TestTypes.Features.Dashboard.DashboardProps>();
        Assert.Equal("./Dashboard", path);
    }

    private static void ResidentDetailProps_Returns_Residents_Detail()
    {
        var path = ComponentPathConvention.GetPath<TestTypes.Features.Residents.ResidentDetailProps>();
        Assert.Equal("./Residents/Detail", path);
    }

    private static void ResidentsListProps_Returns_Residents_List()
    {
        var path = ComponentPathConvention.GetPath<TestTypes.Features.Residents.ResidentsListProps>();
        Assert.Equal("./Residents/List", path);
    }

    private static void UserProfileProps_Returns_Users_Profile()
    {
        var path = ComponentPathConvention.GetPath<TestTypes.Features.Users.UserProfileProps>();
        Assert.Equal("./Users/Profile", path);
    }

    private static void NonFeaturesType_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            ComponentPathConvention.GetPath<TestTypes.NotInFeatures.SomeProps>();
        });
    }

    private static void DeeplyNested_ReturnsCorrectPath()
    {
        var path = ComponentPathConvention.GetPath<TestTypes.Features.Admin.Settings.SettingsProps>();
        Assert.Equal("./Admin/Settings", path);
    }

    private static void NoSuffix_ReturnsCorrectPath()
    {
        var path = ComponentPathConvention.GetPath<TestTypes.Features.Help.HelpModel>();
        Assert.Equal("./Help/Model", path);
    }
}
// Test fixture types are in TestFixtures.cs
