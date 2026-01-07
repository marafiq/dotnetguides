namespace Impulse.Core;

/// <summary>
/// Convention-based component path derivation from Props types.
/// Derives component paths from the Props type's namespace and name.
/// </summary>
/// <remarks>
/// Examples:
/// - Features.Dashboard.DashboardProps → ./Dashboard
/// - Features.Residents.ResidentDetailProps → ./Residents/Detail
/// - Features.Residents.ResidentsListProps → ./Residents/List
/// </remarks>
public static class ComponentPathConvention
{
    private const string FeaturesMarker = "Features";
    private const string PropsSuffix = "Props";

    /// <summary>
    /// Gets the component path for a Props type using convention.
    /// </summary>
    /// <typeparam name="TProps">The props type.</typeparam>
    /// <returns>The derived component path (e.g., "./Dashboard").</returns>
    public static string GetPath<TProps>() => GetPath(typeof(TProps));

    /// <summary>
    /// Gets the component path for a Props type using convention.
    /// </summary>
    /// <param name="propsType">The props type.</param>
    /// <returns>The derived component path (e.g., "./Dashboard").</returns>
    public static string GetPath(Type propsType)
    {
        ArgumentNullException.ThrowIfNull(propsType);

        var fullName = propsType.FullName
            ?? throw new InvalidOperationException($"Type {propsType.Name} has no FullName");

        // Find the Features marker in the namespace
        var featuresIndex = fullName.IndexOf(FeaturesMarker, StringComparison.Ordinal);
        if (featuresIndex < 0)
        {
            throw new InvalidOperationException(
                $"Type {fullName} must be in a 'Features' namespace to use convention-based paths. " +
                $"Expected pattern: *.Features.<Feature>.<TypeName>Props");
        }

        // Get the path after "Features."
        var pathStart = featuresIndex + FeaturesMarker.Length + 1;
        if (pathStart >= fullName.Length)
        {
            throw new InvalidOperationException(
                $"Type {fullName} must have a feature namespace after 'Features'");
        }

        var pathPart = fullName[pathStart..];

        // Split into namespace parts and type name
        var parts = pathPart.Split('.');
        if (parts.Length < 2)
        {
            throw new InvalidOperationException(
                $"Type {fullName} must have at least one namespace level and type name after 'Features'");
        }

        // Get namespace path (all but last)
        var namespaceParts = parts[..^1];
        var typeName = parts[^1];

        // Remove Props suffix from type name
        if (typeName.EndsWith(PropsSuffix, StringComparison.Ordinal))
        {
            typeName = typeName[..^PropsSuffix.Length];
        }

        // Build the component path
        var componentPath = DeriveComponentPath(namespaceParts, typeName);

        return $"./{componentPath}";
    }

    private static string DeriveComponentPath(string[] namespaceParts, string typeName)
    {
        if (namespaceParts.Length == 0)
        {
            return typeName;
        }

        var basePath = string.Join("/", namespaceParts);
        var folderName = namespaceParts[^1];

        // If the type name matches the folder name exactly, use just the path
        // e.g., Features.Dashboard.DashboardProps → Dashboard
        if (typeName.Equals(folderName, StringComparison.OrdinalIgnoreCase))
        {
            return basePath;
        }

        // Check if type name starts with the folder name (exact or singular form)
        // e.g., Features.Residents.ResidentsListProps → Residents/List
        //       Features.Residents.ResidentDetailProps → Residents/Detail
        var prefixToStrip = GetMatchingPrefix(typeName, folderName);
        if (prefixToStrip is not null)
        {
            var suffix = typeName[prefixToStrip.Length..];
            if (!string.IsNullOrEmpty(suffix))
            {
                return $"{basePath}/{suffix}";
            }
            // Suffix is empty - type name matches folder name (singular form)
            // e.g., DynamicForms folder, DynamicFormProps type → DynamicForms
            return basePath;
        }

        // Default: namespace/TypeName
        return $"{basePath}/{typeName}";
    }

    /// <summary>
    /// Gets the prefix to strip from the type name if it matches the folder name.
    /// Handles both exact matches and singular/plural variants.
    /// </summary>
    private static string? GetMatchingPrefix(string typeName, string folderName)
    {
        // Exact match (case-insensitive)
        if (typeName.StartsWith(folderName, StringComparison.OrdinalIgnoreCase))
        {
            return folderName;
        }

        // If folder is plural (ends with 's'), check singular form
        // e.g., "Residents" folder, "ResidentDetail" type → strip "Resident"
        if (folderName.Length > 1 && folderName.EndsWith('s'))
        {
            var singular = folderName[..^1]; // Remove trailing 's'
            if (typeName.StartsWith(singular, StringComparison.OrdinalIgnoreCase))
            {
                return singular;
            }
        }

        // If folder is singular, check plural form
        // e.g., "User" folder, "UsersListProps" type → strip "Users"
        var plural = folderName + "s";
        if (typeName.StartsWith(plural, StringComparison.OrdinalIgnoreCase))
        {
            return plural;
        }

        return null;
    }
}
