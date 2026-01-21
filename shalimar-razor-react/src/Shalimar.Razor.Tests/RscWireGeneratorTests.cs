using Xunit;

namespace Shalimar.Razor.Tests;

public class RscWireGeneratorTests
{
    [Fact]
    public void Generate_ServerComponent_ReturnsWireFormat()
    {
        // Arrange
        var component = new RazorComponent
        {
            Name = "UserProfile",
            FilePath = "/test/UserProfile.razor",
            FeatureFolder = "Users",
            Directive = ComponentDirective.Server,
            Props = new List<ComponentProp>
            {
                new() { Name = "Name", Type = "string", IsRequired = true }
            },
            TemplateContent = "<div>@Props.Name</div>"
        };

        var generator = new RscWireGenerator();

        // Act
        var wireFormat = generator.Generate(component, new { Name = "John" });

        // Assert
        Assert.Contains("\"$\"", wireFormat);
        Assert.Contains("\"UserProfile\"", wireFormat);
        Assert.Contains("\"Name\"", wireFormat);
        Assert.Contains("\"John\"", wireFormat);
    }

    [Fact]
    public void GenerateResponse_ReturnsCorrectContentType()
    {
        // Arrange
        var component = new RazorComponent
        {
            Name = "Test",
            FilePath = "/test/Test.razor",
            FeatureFolder = "Test",
            Directive = ComponentDirective.Server,
            Props = new List<ComponentProp>(),
            TemplateContent = "<div></div>"
        };

        var generator = new RscWireGenerator();

        // Act
        var response = generator.GenerateResponse(component);

        // Assert
        Assert.Equal("text/x-component", response.ContentType);
        Assert.Equal("Test", response.ComponentName);
    }
}
