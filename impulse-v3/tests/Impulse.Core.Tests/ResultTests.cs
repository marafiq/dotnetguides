using FluentAssertions;
using Impulse;

namespace Impulse.Core.Tests;

/// <summary>
/// Tests that define Result behavior.
/// These tests drive the design of our Result types.
///
/// Key behaviors:
/// 1. Results carry data and HTTP semantics
/// 2. Results are immutable records (pattern matchable)
/// 3. Validation errors follow RFC 7807 ProblemDetails
/// 4. Results can be constructed via static factory
/// </summary>
public class ResultTests
{
    // ========================================
    // Ok Result Behavior
    // ========================================

    [Fact]
    public void Ok_CarriesData()
    {
        // The fundamental behavior: Ok results carry data
        var data = new TestData("hello", 42);

        var result = Result.Ok(data);

        result.Should().BeOfType<OkResult<TestData>>();
        result.Data.Should().Be(data);
    }

    [Fact]
    public void Ok_HasStatusCode200()
    {
        var result = Result.Ok(new TestData("x", 1));

        result.StatusCode.Should().Be(200);
    }

    [Fact]
    public void Ok_IsPatternMatchable()
    {
        // Pattern matching is essential for handling results elegantly
        IResult<TestData> result = Result.Ok(new TestData("test", 1));

        var matched = result switch
        {
            OkResult<TestData> ok => ok.Data.Name,
            _ => "not ok"
        };

        matched.Should().Be("test");
    }

    // ========================================
    // NotFound Result Behavior
    // ========================================

    [Fact]
    public void NotFound_HasStatusCode404()
    {
        var result = Result.NotFound();

        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public void NotFound_CanCarryMessage()
    {
        // Sometimes we want to explain what wasn't found
        var result = Result.NotFound("Resident with ID 42 not found");

        result.Message.Should().Be("Resident with ID 42 not found");
    }

    [Fact]
    public void NotFound_MessageIsOptional()
    {
        var result = Result.NotFound();

        result.Message.Should().BeNull();
    }

    // ========================================
    // ValidationProblem Result Behavior
    // ========================================

    [Fact]
    public void ValidationProblem_HasStatusCode422()
    {
        // 422 Unprocessable Entity is the correct status for validation failures
        var result = Result.ValidationProblem(new Dictionary<string, string[]>());

        result.StatusCode.Should().Be(422);
    }

    [Fact]
    public void ValidationProblem_ContainsFieldErrors()
    {
        // Field errors map field names to error messages
        var errors = new Dictionary<string, string[]>
        {
            ["firstName"] = ["First name is required"],
            ["email"] = ["Invalid email format", "Email already exists"]
        };

        var result = Result.ValidationProblem(errors);

        result.Errors.Should().ContainKey("firstName");
        result.Errors["firstName"].Should().Contain("First name is required");
        result.Errors["email"].Should().HaveCount(2);
    }

    [Fact]
    public void ValidationProblem_CanBeBuiltFluently()
    {
        // Fluent API makes it easier to add validation errors incrementally
        var result = Result.ValidationProblem()
            .WithError("firstName", "First name is required")
            .WithError("lastName", "Last name is required")
            .WithError("email", "Invalid email format")
            .WithError("email", "Email already exists"); // Multiple errors per field

        result.Errors.Should().HaveCount(3); // 3 fields
        result.Errors["email"].Should().HaveCount(2); // 2 errors on email
    }

    // ========================================
    // Created Result Behavior
    // ========================================

    [Fact]
    public void Created_HasStatusCode201()
    {
        var result = Result.Created("/residents/42", new TestData("new", 42));

        result.StatusCode.Should().Be(201);
    }

    [Fact]
    public void Created_HasLocation()
    {
        // Location header tells client where the new resource is
        var result = Result.Created("/residents/42", new TestData("new", 42));

        result.Location.Should().Be("/residents/42");
    }

    [Fact]
    public void Created_CarriesData()
    {
        var data = new TestData("new", 42);
        var result = Result.Created("/residents/42", data);

        result.Data.Should().Be(data);
    }

    // ========================================
    // Redirect Result Behavior
    // ========================================

    [Fact]
    public void Redirect_HasStatusCode302()
    {
        var result = Result.Redirect("/dashboard");

        result.StatusCode.Should().Be(302);
    }

    [Fact]
    public void Redirect_HasUrl()
    {
        var result = Result.Redirect("/residents/42");

        result.Url.Should().Be("/residents/42");
    }

    // ========================================
    // Result as return type (covariance)
    // ========================================

    [Fact]
    public void Results_AreCovariant()
    {
        // Endpoints return IResult<T>, but can return any result type
        IResult<TestData> okResult = Result.Ok(new TestData("x", 1));

        // This should compile and work - important for endpoint return types
        okResult.Should().BeAssignableTo<IResult<TestData>>();
    }
}

// Test data record for testing
public record TestData(string Name, int Value);
