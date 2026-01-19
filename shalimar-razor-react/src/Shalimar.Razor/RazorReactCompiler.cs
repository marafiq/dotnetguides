using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using System.Text;

namespace Shalimar.Razor;

/// <summary>
/// Main entry point for compiling Razor files to React TSX components.
///
/// Architecture:
/// .razor → RazorProjectEngine → SyntaxTree → TsxEmitter → React Components
///
/// We leverage Microsoft's battle-tested Razor parser (50,000+ lines)
/// and only write the TSX emitter (~500 lines).
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
                // Configure for component-style Razor (Blazor-like)
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

        // Parse the Razor source
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

        // Emit TSX
        var emitter = new TsxEmitter(fileName);
        var tsx = emitter.Emit(syntaxTree);

        return CompilationResult.Success(tsx, syntaxTree);
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
    /// Dump the syntax tree for debugging/visualization.
    /// </summary>
    public string DumpSyntaxTree(string razorSource, string fileName = "Component.razor")
    {
        var projectItem = new VirtualRazorProjectItem(
            basePath: "/",
            filePath: $"/{fileName}",
            content: razorSource);

        var codeDocument = _engine.Process(projectItem);
        var syntaxTree = codeDocument.GetSyntaxTree();

        var dumper = new SyntaxTreeDumper();
        return dumper.Dump(syntaxTree.Root);
    }
}

/// <summary>
/// Result of a compilation operation.
/// </summary>
public class CompilationResult
{
    public bool IsSuccess { get; }
    public string? TsxOutput { get; }
    public RazorSyntaxTree? SyntaxTree { get; }
    public IReadOnlyList<CompilationError> Errors { get; }

    private CompilationResult(bool success, string? tsx, RazorSyntaxTree? tree, IReadOnlyList<CompilationError> errors)
    {
        IsSuccess = success;
        TsxOutput = tsx;
        SyntaxTree = tree;
        Errors = errors;
    }

    public static CompilationResult Success(string tsx, RazorSyntaxTree tree) =>
        new(true, tsx, tree, Array.Empty<CompilationError>());

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
    public override string FileKind => "component";

    public override Stream Read()
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(_content));
    }
}

/// <summary>
/// Utility for dumping syntax trees for debugging.
/// </summary>
internal class SyntaxTreeDumper
{
    private readonly StringBuilder _sb = new();
    private int _indent = 0;

    public string Dump(SyntaxNode root)
    {
        Visit(root);
        return _sb.ToString();
    }

    private void Visit(SyntaxNode node)
    {
        _sb.AppendLine($"{new string(' ', _indent * 2)}{node.GetType().Name} [{node.SpanStart}..{node.EndPosition}]");

        // Show content for leaf nodes
        if (node is SyntaxToken token && !string.IsNullOrWhiteSpace(token.Content))
        {
            var content = token.Content.Length > 50
                ? token.Content.Substring(0, 47) + "..."
                : token.Content;
            content = content.Replace("\n", "\\n").Replace("\r", "\\r");
            _sb.AppendLine($"{new string(' ', (_indent + 1) * 2)}Content: \"{content}\"");
        }

        _indent++;
        foreach (var child in node.ChildNodes())
        {
            Visit(child);
        }
        _indent--;
    }
}
