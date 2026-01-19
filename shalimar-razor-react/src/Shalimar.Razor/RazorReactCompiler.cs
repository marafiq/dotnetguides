using Microsoft.AspNetCore.Razor.Language;
using System.Text;

namespace Shalimar.Razor;

/// <summary>
/// Main entry point for compiling Razor files to React TSX components.
///
/// Architecture:
/// .razor → RazorProjectEngine → Validation + IR → TsxEmitter → React Components
///
/// We leverage Microsoft's battle-tested Razor parser for validation,
/// then transform the source using our emitter.
/// </summary>
public class RazorReactCompiler
{
    private readonly RazorProjectEngine _engine;

    public RazorReactCompiler()
    {
        // Create a minimal file system for the project engine
        var fileSystem = new VirtualRazorProjectFileSystem();

        // Configure the Razor project engine
        _engine = RazorProjectEngine.Create(
            RazorConfiguration.Default,
            fileSystem,
            builder =>
            {
                builder.SetRootNamespace("Shalimar.Components");
            });
    }

    /// <summary>
    /// Compile a Razor file to TSX.
    /// </summary>
    public CompilationResult Compile(string razorSource, string fileName = "Component.razor")
    {
        // Create a project item from the source
        var projectItem = new VirtualRazorProjectItem(
            basePath: "/",
            filePath: $"/{fileName}",
            content: razorSource);

        // Parse the Razor source - this validates the syntax
        var codeDocument = _engine.Process(projectItem);
        var syntaxTree = codeDocument.GetSyntaxTree();

        // Check for parse errors
        var diagnostics = syntaxTree.Diagnostics
            .Where(d => d.Severity == RazorDiagnosticSeverity.Error)
            .ToList();

        if (diagnostics.Any())
        {
            return CompilationResult.Failed(diagnostics.Select(d =>
                new CompilationError(d.Span.LineIndex + 1, d.GetMessage())).ToList());
        }

        // Emit TSX from the validated source
        var emitter = new TsxEmitter(fileName);
        var tsx = emitter.Emit(razorSource);

        return CompilationResult.Success(tsx, razorSource);
    }

    /// <summary>
    /// Compile a Razor file and return only the TSX output.
    /// </summary>
    public string CompileToString(string razorSource, string fileName = "Component.razor")
    {
        var result = Compile(razorSource, fileName);
        if (!result.IsSuccess)
        {
            throw new CompilationException(result.Errors);
        }
        return result.TsxOutput!;
    }

    /// <summary>
    /// Dump diagnostic info for debugging.
    /// </summary>
    public string DumpInfo(string razorSource, string fileName = "Component.razor")
    {
        var projectItem = new VirtualRazorProjectItem(
            basePath: "/",
            filePath: $"/{fileName}",
            content: razorSource);

        var codeDocument = _engine.Process(projectItem);
        var syntaxTree = codeDocument.GetSyntaxTree();

        var sb = new StringBuilder();
        sb.AppendLine($"File: {fileName}");
        sb.AppendLine($"Source length: {razorSource.Length} chars");
        sb.AppendLine($"Diagnostics: {syntaxTree.Diagnostics.Count()}");

        foreach (var diag in syntaxTree.Diagnostics)
        {
            sb.AppendLine($"  [{diag.Severity}] Line {diag.Span.LineIndex + 1}: {diag.GetMessage()}");
        }

        // Show the generated C# code (proves the parser works)
        var csharpDoc = codeDocument.GetCSharpDocument();
        if (csharpDoc != null)
        {
            sb.AppendLine();
            sb.AppendLine("Generated C# (first 500 chars):");
            sb.AppendLine(csharpDoc.GeneratedCode.Substring(0, Math.Min(500, csharpDoc.GeneratedCode.Length)));
        }

        return sb.ToString();
    }
}

/// <summary>
/// Result of a compilation operation.
/// </summary>
public class CompilationResult
{
    public bool IsSuccess { get; }
    public string? TsxOutput { get; }
    public string? SourceCode { get; }
    public IReadOnlyList<CompilationError> Errors { get; }

    private CompilationResult(bool success, string? tsx, string? source, IReadOnlyList<CompilationError> errors)
    {
        IsSuccess = success;
        TsxOutput = tsx;
        SourceCode = source;
        Errors = errors;
    }

    public static CompilationResult Success(string tsx, string source) =>
        new(true, tsx, source, Array.Empty<CompilationError>());

    public static CompilationResult Failed(IReadOnlyList<CompilationError> errors) =>
        new(false, null, null, errors);
}

/// <summary>
/// Represents a compilation error with location information.
/// </summary>
public record CompilationError(int Line, string Message);

/// <summary>
/// Exception thrown when compilation fails.
/// </summary>
public class CompilationException : Exception
{
    public IReadOnlyList<CompilationError> Errors { get; }

    public CompilationException(IReadOnlyList<CompilationError> errors)
        : base($"Compilation failed with {errors.Count} error(s): {errors.First().Message}")
    {
        Errors = errors;
    }
}

/// <summary>
/// Virtual file system for the Razor project engine.
/// </summary>
internal class VirtualRazorProjectFileSystem : RazorProjectFileSystem
{
    public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
    {
        return Enumerable.Empty<RazorProjectItem>();
    }

    [Obsolete]
    public override RazorProjectItem GetItem(string path)
    {
        return new VirtualRazorProjectItem("/", path, string.Empty);
    }

    public override RazorProjectItem GetItem(string path, string? fileKind)
    {
        return new VirtualRazorProjectItem("/", path, string.Empty);
    }
}

/// <summary>
/// Virtual project item for in-memory Razor source.
/// </summary>
internal class VirtualRazorProjectItem : RazorProjectItem
{
    private readonly string _content;

    public VirtualRazorProjectItem(string basePath, string filePath, string content)
    {
        BasePath = basePath;
        FilePath = filePath;
        _content = content;
    }

    public override string BasePath { get; }
    public override string FilePath { get; }
    public override string PhysicalPath => FilePath;
    public override bool Exists => true;

    public override Stream Read()
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(_content));
    }
}
