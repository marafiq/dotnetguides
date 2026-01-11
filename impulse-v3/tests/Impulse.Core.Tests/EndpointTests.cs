using FluentAssertions;
using Impulse;

namespace Impulse.Core.Tests;

/// <summary>
/// Tests that define Endpoint behavior.
/// These tests drive the design of our endpoint abstractions.
///
/// Key behaviors:
/// 1. Endpoints are typed (Request -> Response)
/// 2. Endpoints are discoverable via [Endpoint] attribute
/// 3. Endpoints handle async operations with cancellation
/// 4. Route parameters are part of the Request type
/// </summary>
public class EndpointTests
{
    // ========================================
    // Endpoint with Request
    // ========================================

    [Fact]
    public async Task Endpoint_WithRequest_ReceivesTypedRequest()
    {
        // Arrange
        var endpoint = new CreateResidentEndpoint();
        var request = new CreateResidentRequest("John", "Doe", new DateTime(1950, 1, 1));

        // Act
        var result = await endpoint.HandleAsync(request, CancellationToken.None);

        // Assert - endpoint receives the request and can use it
        result.Should().BeOfType<OkResult<CreateResidentResponse>>();
        var okResult = (OkResult<CreateResidentResponse>)result;
        okResult.Data.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task Endpoint_WithRequest_CanReturnValidationErrors()
    {
        // Validation is a core concern - endpoints must be able to reject invalid data
        var endpoint = new CreateResidentEndpoint();
        var request = new CreateResidentRequest("", "", DateTime.Today); // Invalid

        var result = await endpoint.HandleAsync(request, CancellationToken.None);

        result.Should().BeOfType<ValidationProblemResult>();
        var validationResult = (ValidationProblemResult)result;
        validationResult.Errors.Should().ContainKey("firstName");
    }

    [Fact]
    public async Task Endpoint_WithRequest_SupportsCancellation()
    {
        // Long-running endpoints must respect cancellation
        var endpoint = new SlowEndpoint();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => endpoint.HandleAsync(new EmptyRequest(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ========================================
    // Endpoint without Request (GET)
    // ========================================

    [Fact]
    public async Task Endpoint_WithoutRequest_ReturnsData()
    {
        // GET endpoints typically don't have a request body
        var endpoint = new GetDashboardEndpoint();

        var result = await endpoint.HandleAsync(CancellationToken.None);

        result.Should().BeOfType<OkResult<DashboardResponse>>();
    }

    // ========================================
    // Endpoint Attribute
    // ========================================

    [Fact]
    public void EndpointAttribute_DefinesTouteAndMethod()
    {
        var attr = typeof(CreateResidentEndpoint)
            .GetCustomAttributes(typeof(EndpointAttribute), false)
            .Cast<EndpointAttribute>()
            .FirstOrDefault();

        attr.Should().NotBeNull();
        attr!.Route.Should().Be("/residents");
        attr.Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public void EndpointAttribute_DefaultMethodIsGet()
    {
        var attr = typeof(GetDashboardEndpoint)
            .GetCustomAttributes(typeof(EndpointAttribute), false)
            .Cast<EndpointAttribute>()
            .FirstOrDefault();

        attr.Should().NotBeNull();
        attr!.Method.Should().Be(HttpMethod.Get);
    }

    [Fact]
    public void EndpointAttribute_SupportsRouteParameters()
    {
        var attr = typeof(GetResidentEndpoint)
            .GetCustomAttributes(typeof(EndpointAttribute), false)
            .Cast<EndpointAttribute>()
            .FirstOrDefault();

        attr.Should().NotBeNull();
        attr!.Route.Should().Be("/residents/{id}");
    }

    // ========================================
    // Endpoint Discovery
    // ========================================

    [Fact]
    public void Endpoints_AreDiscoverableFromAssembly()
    {
        // The framework needs to find all endpoints automatically
        var endpoints = EndpointDiscovery.FindEndpoints(typeof(EndpointTests).Assembly);

        endpoints.Should().Contain(e => e.Type == typeof(CreateResidentEndpoint));
        endpoints.Should().Contain(e => e.Type == typeof(GetDashboardEndpoint));
        endpoints.Should().Contain(e => e.Type == typeof(GetResidentEndpoint));
    }

    [Fact]
    public void EndpointDiscovery_ExtractsRequestAndResponseTypes()
    {
        var endpoints = EndpointDiscovery.FindEndpoints(typeof(EndpointTests).Assembly);

        var createResident = endpoints.First(e => e.Type == typeof(CreateResidentEndpoint));
        createResident.RequestType.Should().Be(typeof(CreateResidentRequest));
        createResident.ResponseType.Should().Be(typeof(CreateResidentResponse));

        var getDashboard = endpoints.First(e => e.Type == typeof(GetDashboardEndpoint));
        getDashboard.RequestType.Should().BeNull(); // No request for GET
        getDashboard.ResponseType.Should().Be(typeof(DashboardResponse));
    }
}

// ========================================
// Test Endpoints - Define expected API
// ========================================

[Endpoint("/residents", HttpMethod.Post)]
public class CreateResidentEndpoint : Endpoint<CreateResidentRequest, CreateResidentResponse>
{
    public override Task<IResult<CreateResidentResponse>> HandleAsync(
        CreateResidentRequest request,
        CancellationToken ct = default)
    {
        // Validation logic
        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            return Task.FromResult<IResult<CreateResidentResponse>>(
                Result.ValidationProblem()
                    .WithError("firstName", "First name is required"));
        }

        // Success path
        var response = new CreateResidentResponse(42, request.FirstName, request.LastName);
        return Task.FromResult<IResult<CreateResidentResponse>>(Result.Ok(response));
    }
}

[Endpoint("/dashboard")]
public class GetDashboardEndpoint : Endpoint<DashboardResponse>
{
    public override Task<IResult<DashboardResponse>> HandleAsync(CancellationToken ct = default)
    {
        var response = new DashboardResponse(10, 8);
        return Task.FromResult<IResult<DashboardResponse>>(Result.Ok(response));
    }
}

[Endpoint("/residents/{id}")]
public class GetResidentEndpoint : Endpoint<GetResidentRequest, ResidentResponse>
{
    public override Task<IResult<ResidentResponse>> HandleAsync(
        GetResidentRequest request,
        CancellationToken ct = default)
    {
        if (request.Id <= 0)
        {
            return Task.FromResult<IResult<ResidentResponse>>(
                Result.NotFound($"Resident {request.Id} not found"));
        }

        var response = new ResidentResponse(request.Id, "John", "Doe");
        return Task.FromResult<IResult<ResidentResponse>>(Result.Ok(response));
    }
}

public class SlowEndpoint : Endpoint<EmptyRequest, EmptyResponse>
{
    public override async Task<IResult<EmptyResponse>> HandleAsync(
        EmptyRequest request,
        CancellationToken ct = default)
    {
        await Task.Delay(10000, ct); // Will throw if cancelled
        return Result.Ok(new EmptyResponse());
    }
}

// ========================================
// Test Request/Response Types
// ========================================

public record CreateResidentRequest(string FirstName, string LastName, DateTime DateOfBirth);
public record CreateResidentResponse(int Id, string FirstName, string LastName);
public record GetResidentRequest(int Id);
public record ResidentResponse(int Id, string FirstName, string LastName);
public record DashboardResponse(int TotalResidents, int ActiveResidents);
public record EmptyRequest();
public record EmptyResponse();
