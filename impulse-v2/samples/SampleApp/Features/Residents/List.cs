using Impulse.Core;

namespace SampleApp.Features.Residents;

// ========================================
// List Residents - GET /residents
// Request/Response colocated with endpoint
// ========================================

public record ListResidentsRequest(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    string? SearchTerm = null);

public record ListResidentsResponse(
    List<ResidentSummary> Residents,
    int TotalCount,
    int Page,
    int PageSize);

[ImpulseEndpoint("/residents")]
public class ListResidentsEndpoint : ImpulseEndpoint<ListResidentsResponse>
{
    public override Task<IImpulseResult> Handle(CancellationToken ct = default)
    {
        // Simulated data - in real app this would query database
        var residents = new List<ResidentSummary>
        {
            new(1, "John", "Smith", "101A", "Active", 78),
            new(2, "Mary", "Johnson", "102B", "Active", 82),
            new(3, "Robert", "Williams", "103A", "Active", 75),
            new(4, "Dorothy", "Brown", "104B", "Active", 88),
            new(5, "James", "Davis", "105A", "Active", 71),
        };

        return Task.FromResult<IImpulseResult>(
            ImpulseResults.Ok(new ListResidentsResponse(residents, 5, 1, 20)));
    }
}
