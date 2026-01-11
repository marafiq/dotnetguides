using CareHome.Models;
using Impulse;

namespace CareHome.Endpoints;

/// <summary>
/// List all residents.
/// </summary>
[Endpoint("/residents", HttpMethod.Get)]
public class ListResidentsEndpoint : Endpoint<ListResidentsResponse>
{
    public override Task<IResult<ListResidentsResponse>> HandleAsync(CancellationToken ct)
    {
        // In a real app, this would query a database
        var residents = new List<ResidentSummary>
        {
            new(1, "John Smith", "101A", CareLevel.Independent, 78),
            new(2, "Mary Johnson", "102B", CareLevel.Assisted, 85),
            new(3, "Robert Williams", "103A", CareLevel.FullCare, 92)
        };

        return Task.FromResult(Result.Ok(new ListResidentsResponse(residents, residents.Count)));
    }
}

/// <summary>
/// Response for listing residents.
/// </summary>
public record ListResidentsResponse(
    List<ResidentSummary> Residents,
    int TotalCount);

/// <summary>
/// Get a single resident by ID.
/// </summary>
[Endpoint("/residents/{id}", HttpMethod.Get)]
public class GetResidentEndpoint : Endpoint<GetResidentRequest, Resident>
{
    public override Task<IResult<Resident>> HandleAsync(GetResidentRequest request, CancellationToken ct)
    {
        // In a real app, this would query a database
        if (request.Id == 1)
        {
            var resident = new Resident(
                1, "John", "Smith",
                new DateTime(1946, 3, 15),
                "101A",
                CareLevel.Independent,
                new DateTime(2023, 1, 10),
                new List<DietaryRequirement> { DietaryRequirement.LowSodium },
                new EmergencyContact("Jane Smith", "Daughter", "555-0123", "jane@email.com"));

            return Task.FromResult(Result.Ok(resident));
        }

        return Task.FromResult(Result.NotFound<Resident>($"Resident {request.Id} not found"));
    }
}

/// <summary>
/// Request to get a resident.
/// </summary>
public record GetResidentRequest(int Id);

/// <summary>
/// Create a new resident.
/// </summary>
[Endpoint("/residents", HttpMethod.Post)]
public class CreateResidentEndpoint : Endpoint<CreateResidentRequest, CreateResidentResponse>
{
    public override Task<IResult<CreateResidentResponse>> HandleAsync(
        CreateResidentRequest request, CancellationToken ct)
    {
        // Validate
        var errors = new ValidationBuilder();

        if (string.IsNullOrWhiteSpace(request.FirstName))
            errors.Add("firstName", "First name is required");

        if (string.IsNullOrWhiteSpace(request.LastName))
            errors.Add("lastName", "Last name is required");

        if (request.DateOfBirth > DateTime.Today.AddYears(-18))
            errors.Add("dateOfBirth", "Resident must be at least 18 years old");

        if (errors.HasErrors)
            return Task.FromResult(errors.ToResult<CreateResidentResponse>());

        // In a real app, this would save to a database
        var newId = Random.Shared.Next(100, 999);

        return Task.FromResult(Result.Created(
            new CreateResidentResponse(newId, "Resident created successfully"),
            $"/residents/{newId}"));
    }
}

/// <summary>
/// Update an existing resident.
/// </summary>
[Endpoint("/residents/{id}", HttpMethod.Put)]
public class UpdateResidentEndpoint : Endpoint<UpdateResidentRequest, UpdateResidentResponse>
{
    public override Task<IResult<UpdateResidentResponse>> HandleAsync(
        UpdateResidentRequest request, CancellationToken ct)
    {
        // Validate
        var errors = new ValidationBuilder();

        if (string.IsNullOrWhiteSpace(request.FirstName))
            errors.Add("firstName", "First name is required");

        if (string.IsNullOrWhiteSpace(request.LastName))
            errors.Add("lastName", "Last name is required");

        if (errors.HasErrors)
            return Task.FromResult(errors.ToResult<UpdateResidentResponse>());

        // In a real app, this would update the database
        return Task.FromResult(Result.Ok(
            new UpdateResidentResponse(true, "Resident updated successfully")));
    }
}
