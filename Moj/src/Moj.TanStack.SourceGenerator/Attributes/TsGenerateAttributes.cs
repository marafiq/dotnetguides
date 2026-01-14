namespace Moj.TanStack.SourceGenerator.Attributes;

/// <summary>
/// Marks a class or method for TypeScript generation
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class TsGenerateAttribute : Attribute
{
    /// <summary>
    /// The output file name (without extension)
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Output directory relative to project root
    /// </summary>
    public string OutputDir { get; set; } = "Generated/ts";
}

/// <summary>
/// Marks a class as a TanStack Store definition
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class TsStoreAttribute : Attribute
{
    public string? Name { get; set; }
}

/// <summary>
/// Marks a class as TanStack Router routes definition
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class TsRouterAttribute : Attribute
{
    public string? Name { get; set; }
}

/// <summary>
/// Marks a class as TanStack Query definitions
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class TsQueryAttribute : Attribute
{
    public string? Name { get; set; }
}

/// <summary>
/// Specifies imports for the generated file
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class TsImportAttribute : Attribute
{
    public string Module { get; }
    public string[] Names { get; }
    public string? DefaultImport { get; set; }
    public bool TypeOnly { get; set; }

    public TsImportAttribute(string module, params string[] names)
    {
        Module = module;
        Names = names;
    }
}

/// <summary>
/// Marks a property or method to be exported in the generated TS
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
public sealed class TsExportAttribute : Attribute
{
    public string? Name { get; set; }
    public bool IsDefault { get; set; }
}
