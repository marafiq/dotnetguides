using Impulse.Sample.Server.Models;

namespace Impulse.Sample.Server.Handlers;

/// <summary>
/// Handlers for document-related endpoints.
/// </summary>
public static class DocumentHandlers
{
    // In-memory data store (for demo purposes)
    private static readonly Dictionary<int, List<Document>> _documentsByResident = new()
    {
        [1] = [
            new(1, "Admission Form", "PDF", new DateTime(2024, 3, 15), 245_000),
            new(2, "Medical History", "PDF", new DateTime(2024, 3, 15), 512_000),
            new(3, "Insurance Card", "Image", new DateTime(2024, 3, 16), 125_000),
        ],
        [2] = [
            new(4, "Admission Form", "PDF", new DateTime(2024, 5, 20), 198_000),
        ],
    };

    public static DocumentsProps GetByResidentId(int residentId)
    {
        var documents = _documentsByResident.GetValueOrDefault(residentId, []);
        return new DocumentsProps(documents);
    }
}
