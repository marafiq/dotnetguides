using Impulse.Core;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Impulse.Core.Tests;

public class ImpulseResultTests
{
    [Fact]
    public void Ok_ReturnsOkResult()
    {
        var data = new { Id = 1, Name = "Test" };
        var result = Impulse.Ok(data);

        var okResult = Assert.IsType<Ok<object>>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void Created_ReturnsCreatedResult()
    {
        var data = new { Id = 1, Name = "Test" };
        var result = Impulse.Created(data);

        Assert.IsType<Created<object>>(result);
    }

    [Fact]
    public void Created_WithLocation_ReturnsCreatedResultWithLocation()
    {
        var data = new { Id = 1, Name = "Test" };
        var result = Impulse.Created("/items/1", data);

        var createdResult = Assert.IsType<Created<object>>(result);
        Assert.Equal("/items/1", createdResult.Location);
    }

    [Fact]
    public void NoContent_ReturnsNoContentResult()
    {
        var result = Impulse.NoContent();

        Assert.IsType<NoContent>(result);
    }

    [Fact]
    public void Accepted_WithoutJobId_ReturnsAcceptedResult()
    {
        var result = Impulse.Accepted();

        Assert.IsType<Accepted>(result);
    }

    [Fact]
    public void Accepted_WithJobId_ReturnsAcceptedResultWithBody()
    {
        var result = Impulse.Accepted("job-123");

        Assert.IsType<Accepted<object>>(result);
    }

    [Fact]
    public void ValidationError_ReturnsBadRequestWithErrors()
    {
        var errors = new Dictionary<string, List<string>>
        {
            ["name"] = ["Name is required"],
            ["email"] = ["Email is invalid"]
        };

        var result = Impulse.ValidationError(errors);

        var badRequestResult = Assert.IsType<BadRequest<ImpulseValidationErrors>>(result);
        Assert.NotNull(badRequestResult.Value);
        Assert.Equal(2, badRequestResult.Value.Errors.Count);
    }

    [Fact]
    public void ValidationError_SingleField_ReturnsBadRequestWithSingleError()
    {
        var result = Impulse.ValidationError("name", "Name is required");

        var badRequestResult = Assert.IsType<BadRequest<ImpulseValidationErrors>>(result);
        Assert.NotNull(badRequestResult.Value);
        Assert.Single(badRequestResult.Value.Errors);
        Assert.Contains("Name is required", badRequestResult.Value.Errors["name"]);
    }

    [Fact]
    public void NotFound_ReturnsNotFoundResult()
    {
        var result = Impulse.NotFound();

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public void NotFound_WithMessage_ReturnsNotFoundResultWithMessage()
    {
        var result = Impulse.NotFound("Item not found");

        Assert.IsType<NotFound<object>>(result);
    }
}
