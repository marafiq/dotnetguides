using Xunit;

namespace Shalimar.Razor.Tests;

public class TsxEmitterTests
{
    private readonly TsxEmitter _emitter = new();
    private readonly RazorParser _parser = new();

    [Fact]
    public void Emit_SimpleComponent_GeneratesValidTsx()
    {
        // Arrange
        var component = new RazorComponent
        {
            Name = "Button",
            FilePath = "/test/Button.razor",
            FeatureFolder = "Common",
            Directive = ComponentDirective.Client,
            Props = new List<ComponentProp>
            {
                new() { Name = "Label", Type = "string", IsRequired = true },
                new() { Name = "Disabled", Type = "bool", IsRequired = false }
            },
            TemplateContent = "<button class=\"btn\" disabled=\"@Props.Disabled\">@Props.Label</button>"
        };

        // Act
        var tsx = _emitter.Emit(component);

        // Assert
        Assert.Contains("import React from 'react'", tsx);
        Assert.Contains("export interface ButtonProps", tsx);
        Assert.Contains("Label: string", tsx);
        Assert.Contains("Disabled?: boolean", tsx);
        Assert.Contains("export function Button", tsx);
        Assert.Contains("className=\"btn\"", tsx);
        Assert.Contains("{Label}", tsx);
    }

    [Fact]
    public void Emit_ServerComponent_SetsCorrectDirective()
    {
        // Arrange
        var component = new RazorComponent
        {
            Name = "UserCard",
            FilePath = "/test/UserCard.razor",
            FeatureFolder = "Users",
            Directive = ComponentDirective.Server,
            Props = new List<ComponentProp>
            {
                new() { Name = "Name", Type = "string", IsRequired = true }
            },
            TemplateContent = "<div>@Props.Name</div>"
        };

        // Act
        var tsx = _emitter.Emit(component);

        // Assert
        Assert.Contains("export function UserCard", tsx);
        Assert.Contains("{Name}", tsx);
    }

    [Fact]
    public void Emit_ComponentWithChildren_ImportsChildComponents()
    {
        // Arrange
        var component = new RazorComponent
        {
            Name = "Card",
            FilePath = "/test/Card.razor",
            FeatureFolder = "Common",
            Directive = ComponentDirective.Client,
            Props = new List<ComponentProp>(),
            Children = new List<ComponentChild>
            {
                new() { Name = "Avatar", TagName = "Avatar", IsSelfClosing = true }
            },
            TemplateContent = "<div><Avatar /></div>"
        };

        // Act
        var tsx = _emitter.Emit(component);

        // Assert
        Assert.Contains("import { Avatar } from './Avatar'", tsx);
    }

    [Fact]
    public void ConvertToTypeScriptType_ConvertsCommonTypes()
    {
        var component = new RazorComponent
        {
            Name = "Test",
            FilePath = "/test/Test.razor",
            FeatureFolder = "Test",
            Props = new List<ComponentProp>
            {
                new() { Name = "Count", Type = "int", IsRequired = true },
                new() { Name = "Price", Type = "decimal", IsRequired = true },
                new() { Name = "Items", Type = "List<string>", IsRequired = true },
                new() { Name = "Created", Type = "DateTime", IsRequired = false }
            },
            TemplateContent = "<div></div>"
        };

        var tsx = _emitter.Emit(component);

        Assert.Contains("Count: number", tsx);
        Assert.Contains("Price: number", tsx);
        Assert.Contains("Items: string[]", tsx);
        Assert.Contains("Created?: Date", tsx);
    }
}
