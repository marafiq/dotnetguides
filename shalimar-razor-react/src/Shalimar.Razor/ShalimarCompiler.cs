namespace Shalimar.Razor;

/// <summary>
/// Main compiler that orchestrates Razor to TSX compilation
/// </summary>
public class ShalimarCompiler
{
    private readonly RazorParser _parser = new();
    private readonly TsxEmitter _emitter = new();
    private readonly ShalimarOptions _options;

    public ShalimarCompiler(ShalimarOptions? options = null)
    {
        _options = options ?? new ShalimarOptions();
    }

    /// <summary>
    /// Compile a single .razor file to .tsx
    /// </summary>
    public CompilationResult CompileFile(string razorPath)
    {
        try
        {
            var component = _parser.Parse(razorPath);
            var tsxPath = _emitter.EmitToFile(component);

            return new CompilationResult
            {
                Success = true,
                SourcePath = razorPath,
                OutputPath = tsxPath,
                Component = component
            };
        }
        catch (Exception ex)
        {
            return new CompilationResult
            {
                Success = false,
                SourcePath = razorPath,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Compile all .razor files in a directory
    /// </summary>
    public CompilationSummary CompileDirectory(string directory)
    {
        var summary = new CompilationSummary();
        var razorFiles = Directory.GetFiles(directory, "*.razor", SearchOption.AllDirectories);

        foreach (var razorFile in razorFiles)
        {
            var result = CompileFile(razorFile);
            summary.Results.Add(result);

            if (result.Success)
            {
                summary.SuccessCount++;
                Console.WriteLine($"  Compiled: {Path.GetFileName(razorFile)} -> {Path.GetFileName(result.OutputPath)}");
            }
            else
            {
                summary.FailureCount++;
                Console.WriteLine($"  Failed: {Path.GetFileName(razorFile)} - {result.Error}");
            }
        }

        return summary;
    }

    /// <summary>
    /// Watch directory for changes and recompile
    /// </summary>
    public IDisposable Watch(string directory, Action<CompilationResult>? onChange = null)
    {
        var watcher = new FileSystemWatcher(directory)
        {
            Filter = "*.razor",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
        };

        void HandleChange(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Deleted) return;

            // Debounce rapid changes
            Thread.Sleep(100);

            var result = CompileFile(e.FullPath);
            onChange?.Invoke(result);

            if (result.Success)
            {
                Console.WriteLine($"[Shalimar] Recompiled: {Path.GetFileName(e.FullPath)}");
            }
            else
            {
                Console.WriteLine($"[Shalimar] Error in {Path.GetFileName(e.FullPath)}: {result.Error}");
            }
        }

        watcher.Changed += HandleChange;
        watcher.Created += HandleChange;
        watcher.EnableRaisingEvents = true;

        return watcher;
    }

    /// <summary>
    /// Generate component manifest for Vite
    /// </summary>
    public void GenerateComponentManifest(string directory, string outputPath)
    {
        var components = _parser.ParseDirectory(directory).ToList();
        var manifest = new ComponentManifest
        {
            Components = components.Select(c => new ComponentEntry
            {
                Name = c.Name,
                Path = Path.GetRelativePath(directory, c.FilePath),
                TsxPath = Path.GetRelativePath(directory, Path.ChangeExtension(c.FilePath, ".tsx")),
                Directive = c.Directive.ToString().ToLower(),
                Feature = c.FeatureFolder
            }).ToList()
        };

        var json = System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(outputPath, json);
        Console.WriteLine($"[Shalimar] Generated component manifest: {outputPath}");
    }
}

/// <summary>
/// Compiler configuration options
/// </summary>
public class ShalimarOptions
{
    public string OutputDirectory { get; set; } = "";
    public bool GenerateSourceMaps { get; set; } = true;
    public bool Verbose { get; set; } = false;
}

/// <summary>
/// Result of compiling a single file
/// </summary>
public class CompilationResult
{
    public bool Success { get; init; }
    public required string SourcePath { get; init; }
    public string? OutputPath { get; init; }
    public string? Error { get; init; }
    public RazorComponent? Component { get; init; }
}

/// <summary>
/// Summary of compiling multiple files
/// </summary>
public class CompilationSummary
{
    public List<CompilationResult> Results { get; } = new();
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int TotalCount => Results.Count;
}

/// <summary>
/// Component manifest for Vite configuration
/// </summary>
public class ComponentManifest
{
    public List<ComponentEntry> Components { get; set; } = new();
}

/// <summary>
/// Entry in the component manifest
/// </summary>
public class ComponentEntry
{
    public required string Name { get; set; }
    public required string Path { get; set; }
    public required string TsxPath { get; set; }
    public required string Directive { get; set; }
    public required string Feature { get; set; }
}
