using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Impulse;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Impulse.Core.Tests;

/// <summary>
/// Integration tests that define how Impulse endpoints work with ASP.NET Core.
/// These tests drive the design of our ASP.NET integration layer.
///
/// Key behaviors:
/// 1. Endpoints are automatically discovered and registered
/// 2. Content negotiation: JSON for X-Impulse header, HTML otherwise
/// 3. Route parameters are bound to Request types
/// 4. Validation errors return ProblemDetails (RFC 7807)
/// </summary>
public class IntegrationTests : IAsyncLifetime
{
    private IHost _host = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            // This is the API we're designing
                            endpoints.MapImpulseEndpoints(typeof(IntegrationTests).Assembly);
                        });
                    });
            })
            .StartAsync();

        _client = _host.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _host.StopAsync();
        _host.Dispose();
    }

    // ========================================
    // Endpoint Registration
    // ========================================

    [Fact]
    public async Task MapEndpoints_RegistersGetEndpoints()
    {
        // Add X-Impulse header to get JSON response
        _client.DefaultRequestHeaders.Add("X-Impulse", "1");

        var response = await _client.GetAsync("/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MapEndpoints_RegistersPostEndpoints()
    {
        _client.DefaultRequestHeaders.Add("X-Impulse", "1");
        var request = new CreateResidentRequest("John", "Doe", new DateTime(1950, 1, 1));

        var response = await _client.PostAsJsonAsync("/residents", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ========================================
    // Content Negotiation
    // ========================================

    [Fact]
    public async Task WithXImpulseHeader_ReturnsJson()
    {
        _client.DefaultRequestHeaders.Add("X-Impulse", "1");

        var response = await _client.GetAsync("/dashboard");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        // Response is wrapped with props, component, version
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("props", out _).Should().BeTrue();
    }

    [Fact]
    public async Task WithoutXImpulseHeader_ReturnsHtml()
    {
        // No X-Impulse header - browser request
        var response = await _client.GetAsync("/dashboard");

        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("<!DOCTYPE html>");
        html.Should().Contain("data-impulse="); // Hydration data embedded
    }

    // ========================================
    // Route Parameter Binding
    // ========================================

    [Fact]
    public async Task RouteParameters_AreBoundToRequest()
    {
        _client.DefaultRequestHeaders.Add("X-Impulse", "1");

        var response = await _client.GetAsync("/residents/42");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var props = doc.RootElement.GetProperty("props");
        props.GetProperty("id").GetInt32().Should().Be(42);
    }

    [Fact]
    public async Task InvalidRouteParameter_ReturnsNotFound()
    {
        _client.DefaultRequestHeaders.Add("X-Impulse", "1");

        var response = await _client.GetAsync("/residents/-1");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ========================================
    // Validation Error Handling
    // ========================================

    [Fact]
    public async Task ValidationError_Returns422WithProblemDetails()
    {
        _client.DefaultRequestHeaders.Add("X-Impulse", "1");
        var request = new CreateResidentRequest("", "", DateTime.Today); // Invalid

        var response = await _client.PostAsJsonAsync("/residents", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ValidationError_ContainsFieldErrors()
    {
        _client.DefaultRequestHeaders.Add("X-Impulse", "1");
        var request = new CreateResidentRequest("", "", DateTime.Today);

        var response = await _client.PostAsJsonAsync("/residents", request);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        // RFC 7807 ProblemDetails structure
        doc.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.TryGetProperty("firstName", out _).Should().BeTrue();
    }

    // ========================================
    // POST with Route Parameters
    // ========================================

    [Fact]
    public async Task PostWithRouteParams_BindsBothRouteAndBody()
    {
        _client.DefaultRequestHeaders.Add("X-Impulse", "1");
        var body = new AddMedicationBody("Aspirin", "100mg");

        var response = await _client.PostAsJsonAsync("/residents/42/medications", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ========================================
    // Version Checking
    // ========================================

    [Fact]
    public async Task VersionMismatch_SetsReloadHeader()
    {
        _client.DefaultRequestHeaders.Add("X-Impulse", "1");
        _client.DefaultRequestHeaders.Add("X-Impulse-Version", "old-version");

        var response = await _client.GetAsync("/dashboard");

        // When client version doesn't match server, signal reload needed
        response.Headers.Contains("X-Impulse-Reload").Should().BeTrue();
    }
}

// ========================================
// Additional Test Endpoints for Integration
// ========================================

[Endpoint("/residents/{residentId}/medications", HttpMethod.Post)]
public class AddMedicationEndpoint : Endpoint<AddMedicationRequest, AddMedicationResponse>
{
    public override Task<IResult<AddMedicationResponse>> HandleAsync(
        AddMedicationRequest request,
        CancellationToken ct = default)
    {
        var response = new AddMedicationResponse(1, request.ResidentId, request.Name);
        return Task.FromResult<IResult<AddMedicationResponse>>(Result.Ok(response));
    }
}

// Request combines route param (residentId) and body
public record AddMedicationRequest(int ResidentId, string Name, string Dosage);
public record AddMedicationBody(string Name, string Dosage); // Body-only portion
public record AddMedicationResponse(int Id, int ResidentId, string Name);
