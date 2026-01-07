using Impulse.Core;

namespace Impulse.Core.Tests;

public class ImpulseTypeRegistryTests
{
    [Fact]
    public void RegisterComponent_AddsToRegistry()
    {
        var registry = new ImpulseTypeRegistry();
        var metadata = new ImpulseComponentMetadata
        {
            PropsType = typeof(TestProps),
            ComponentPath = "./Test"
        };

        registry.RegisterComponent("/test", metadata);

        Assert.Single(registry.Components);
        Assert.Contains(typeof(TestProps), registry.PropsTypes);
    }

    [Fact]
    public void RegisterComponent_AddsDeferredPropsTypes()
    {
        var registry = new ImpulseTypeRegistry();
        var metadata = new ImpulseComponentMetadata
        {
            PropsType = typeof(TestProps),
            ComponentPath = "./Test"
        };
        metadata.Deferred["extra"] = new DeferredComponentInfo
        {
            PropsType = typeof(DeferredProps),
            UrlTemplate = "/test/extra"
        };

        registry.RegisterComponent("/test", metadata);

        Assert.Contains(typeof(TestProps), registry.PropsTypes);
        Assert.Contains(typeof(DeferredProps), registry.PropsTypes);
    }

    [Fact]
    public void RegisterMutation_AddsToRegistry()
    {
        var registry = new ImpulseTypeRegistry();
        var metadata = new ImpulseMutationMetadata
        {
            RequestType = typeof(TestRequest),
            ResponseType = typeof(TestResponse)
        };

        registry.RegisterMutation("/test", metadata);

        Assert.Single(registry.Mutations);
        Assert.Contains(typeof(TestRequest), registry.RequestTypes);
        Assert.Contains(typeof(TestResponse), registry.ResponseTypes);
    }

    [Fact]
    public void AllTypes_ReturnsDistinctTypes()
    {
        var registry = new ImpulseTypeRegistry();

        var componentMetadata = new ImpulseComponentMetadata
        {
            PropsType = typeof(TestProps),
            ComponentPath = "./Test"
        };
        registry.RegisterComponent("/test", componentMetadata);

        var mutationMetadata = new ImpulseMutationMetadata
        {
            RequestType = typeof(TestRequest),
            ResponseType = typeof(TestProps) // Same as props type
        };
        registry.RegisterMutation("/test/create", mutationMetadata);

        var allTypes = registry.AllTypes.ToList();

        Assert.Equal(3, allTypes.Count); // TestProps, TestRequest, TestProps (distinct)
        Assert.Contains(typeof(TestProps), allTypes);
        Assert.Contains(typeof(TestRequest), allTypes);
    }

    private record TestProps(int Id, string Name);
    private record DeferredProps(string Data);
    private record TestRequest(string Name);
    private record TestResponse(int Id);
}
