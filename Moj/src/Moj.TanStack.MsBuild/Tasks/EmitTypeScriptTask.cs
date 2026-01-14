using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Moj.TanStack.MsBuild.Tasks;

/// <summary>
/// MSBuild task that emits TypeScript files from generated C# code
/// </summary>
public sealed class EmitTypeScriptTask : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// The project directory
    /// </summary>
    [Required]
    public string? ProjectDirectory { get; set; }

    /// <summary>
    /// Output directory relative to project directory
    /// </summary>
    public string OutputDirectory { get; set; } = "Generated/ts";

    /// <summary>
    /// The generated C# files to scan for TypeScript
    /// </summary>
    [Required]
    public ITaskItem[]? GeneratedFiles { get; set; }

    /// <summary>
    /// List of generated TypeScript files
    /// </summary>
    [Output]
    public ITaskItem[]? TypeScriptFiles { get; set; }

    public override bool Execute()
    {
        if (string.IsNullOrEmpty(ProjectDirectory))
        {
            Log.LogError("ProjectDirectory is required");
            return false;
        }

        if (GeneratedFiles == null || GeneratedFiles.Length == 0)
        {
            Log.LogMessage(MessageImportance.Normal, "No generated files to process");
            TypeScriptFiles = Array.Empty<ITaskItem>();
            return true;
        }

        var outputDir = Path.Combine(ProjectDirectory, OutputDirectory);
        Directory.CreateDirectory(outputDir);

        var outputFiles = new List<ITaskItem>();

        foreach (var file in GeneratedFiles)
        {
            try
            {
                var filePath = file.GetMetadata("FullPath");
                if (!File.Exists(filePath))
                {
                    Log.LogWarning($"File not found: {filePath}");
                    continue;
                }

                var content = File.ReadAllText(filePath);
                var tsFiles = ExtractTypeScript(content, outputDir);

                foreach (var tsFile in tsFiles)
                {
                    outputFiles.Add(new TaskItem(tsFile));
                    Log.LogMessage(MessageImportance.Normal, $"Generated TypeScript: {tsFile}");
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Error processing {file.ItemSpec}: {ex.Message}");
            }
        }

        TypeScriptFiles = outputFiles.ToArray();
        Log.LogMessage(MessageImportance.High, $"Generated {TypeScriptFiles.Length} TypeScript file(s)");

        return true;
    }

    private List<string> ExtractTypeScript(string content, string outputDir)
    {
        var files = new List<string>();

        // Find /* TS ... */ blocks
        var startMarker = "/* TS";
        var endMarker = "*/";

        int startIndex = 0;
        while ((startIndex = content.IndexOf(startMarker, startIndex)) >= 0)
        {
            var contentStart = startIndex + startMarker.Length;
            var endIndex = content.IndexOf(endMarker, contentStart);

            if (endIndex < 0)
            {
                Log.LogWarning("Unclosed TypeScript block found");
                break;
            }

            var tsContent = content.Substring(contentStart, endIndex - contentStart).Trim();

            // Extract file name from the generated class
            var fileName = ExtractFileName(content, startIndex) ?? $"generated_{files.Count}.ts";
            var outputPath = Path.Combine(outputDir, fileName);

            // Write TypeScript file
            File.WriteAllText(outputPath, tsContent);
            files.Add(outputPath);

            startIndex = endIndex + endMarker.Length;
        }

        return files;
    }

    private static string? ExtractFileName(string content, int position)
    {
        // Look backwards for FileName = "..."
        var searchStart = Math.Max(0, position - 500);
        var searchContent = content.Substring(searchStart, position - searchStart);

        var fileNameMarker = "FileName = \"";
        var markerIndex = searchContent.LastIndexOf(fileNameMarker);

        if (markerIndex >= 0)
        {
            var nameStart = markerIndex + fileNameMarker.Length;
            var nameEnd = searchContent.IndexOf("\"", nameStart);

            if (nameEnd > nameStart)
            {
                return searchContent.Substring(nameStart, nameEnd - nameStart);
            }
        }

        return null;
    }
}
