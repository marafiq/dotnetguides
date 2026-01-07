namespace Impulse.Sample.Server.Models;

/// <summary>
/// Application-wide context provided to all components.
/// </summary>
public record AppContext(
    CurrentUser User,
    IReadOnlyList<string> Permissions,
    Tenant Tenant
);

/// <summary>
/// Current authenticated user.
/// </summary>
public record CurrentUser(
    int Id,
    string Name,
    string Role
);

/// <summary>
/// Current tenant (for multi-tenant applications).
/// </summary>
public record Tenant(
    int Id,
    string Name
);

/// <summary>
/// Permission constants.
/// </summary>
public static class Permissions
{
    public static class Residents
    {
        public const string Read = "residents:read";
        public const string Write = "residents:write";
        public const string Delete = "residents:delete";
    }

    public static class Medications
    {
        public const string Read = "medications:read";
        public const string Write = "medications:write";
    }

    public static class Documents
    {
        public const string Read = "documents:read";
        public const string Write = "documents:write";
    }

    /// <summary>
    /// Gets all permissions organized by category for code generation.
    /// </summary>
    public static Dictionary<string, Dictionary<string, string>> GetAll() => new()
    {
        ["Residents"] = new()
        {
            ["Read"] = Residents.Read,
            ["Write"] = Residents.Write,
            ["Delete"] = Residents.Delete
        },
        ["Medications"] = new()
        {
            ["Read"] = Medications.Read,
            ["Write"] = Medications.Write
        },
        ["Documents"] = new()
        {
            ["Read"] = Documents.Read,
            ["Write"] = Documents.Write
        }
    };
}
