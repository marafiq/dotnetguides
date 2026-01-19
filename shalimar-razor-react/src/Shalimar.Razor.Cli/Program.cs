using Shalimar.Razor;

namespace Shalimar.Razor.Cli;

/// <summary>
/// CLI tool for compiling Razor files to React TSX components.
///
/// Usage:
///   shalimar compile <file.razor> [-o output/]
///   shalimar compile <directory> [-o output/]
///   shalimar watch <directory> [-o output/]
///   shalimar tree <file.razor>  # Dump syntax tree
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var command = args[0].ToLower();

        return command switch
        {
            "compile" => CompileCommand(args.Skip(1).ToArray()),
            "tree" => TreeCommand(args.Skip(1).ToArray()),
            "watch" => WatchCommand(args.Skip(1).ToArray()),
            "demo" => DemoCommand(),
            "--help" or "-h" => PrintUsageAndReturn0(),
            _ => UnknownCommand(command)
        };
    }

    static int PrintUsageAndReturn0()
    {
        PrintUsage();
        return 0;
    }

    static void PrintUsage()
    {
        Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════════╗
║                    SHALIMAR RAZOR COMPILER                     ║
║           .razor → RazorProjectEngine → TSX → React           ║
╚═══════════════════════════════════════════════════════════════╝

USAGE:
    shalimar <command> [options]

COMMANDS:
    compile <file|directory>    Compile .razor files to .tsx
        -o, --output <dir>      Output directory (default: same as input)

    tree <file.razor>           Dump the Razor syntax tree (for debugging)

    watch <directory>           Watch for changes and recompile
        -o, --output <dir>      Output directory

    demo                        Run a demo compilation

EXAMPLES:
    shalimar compile MyComponent.razor
    shalimar compile ./Components -o ./output
    shalimar tree MyComponent.razor
    shalimar watch ./Components -o ./dist
    shalimar demo

TRANSFORMATION:
    .razor (Blazor-style)  →  .tsx (React)

    class=""card""          →  className=""card""
    @Props.Name            →  {props.Name}
    @if (cond) { }         →  {cond && ( )}
    @foreach (var x in xs) →  {xs.map(x => ( ))}
    @onclick=""@Handler""    →  onClick={Handler}
");
    }

    static int CompileCommand(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: No input file specified.");
            Console.WriteLine("Usage: shalimar compile <file.razor> [-o output/]");
            return 1;
        }

        var inputPath = args[0];
        var outputDir = GetOption(args, "-o") ?? GetOption(args, "--output");

        if (File.Exists(inputPath))
        {
            return CompileFile(inputPath, outputDir);
        }
        else if (Directory.Exists(inputPath))
        {
            return CompileDirectory(inputPath, outputDir);
        }
        else
        {
            Console.WriteLine($"Error: Path not found: {inputPath}");
            return 1;
        }
    }

    static int CompileFile(string inputPath, string? outputDir)
    {
        try
        {
            var fileName = Path.GetFileName(inputPath);
            var source = File.ReadAllText(inputPath);

            Console.WriteLine($"Compiling: {fileName}");
            Console.WriteLine(new string('─', 60));

            var compiler = new RazorReactCompiler();
            var result = compiler.Compile(source, fileName);

            if (!result.IsSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Compilation failed:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  Line {error.Line}: {error.Message}");
                }
                Console.ResetColor();
                return 1;
            }

            // Determine output path
            var outputFileName = Path.GetFileNameWithoutExtension(fileName) + ".tsx";
            var outputPath = outputDir != null
                ? Path.Combine(outputDir, outputFileName)
                : Path.Combine(Path.GetDirectoryName(inputPath) ?? ".", outputFileName);

            // Ensure output directory exists
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            File.WriteAllText(outputPath, result.TsxOutput);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Generated: {outputPath}");
            Console.ResetColor();

            // Print a preview
            Console.WriteLine();
            Console.WriteLine("Output preview:");
            Console.WriteLine(new string('─', 60));
            var lines = result.TsxOutput!.Split('\n').Take(30);
            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }
            if (result.TsxOutput!.Split('\n').Length > 30)
            {
                Console.WriteLine("... (truncated)");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    static int CompileDirectory(string inputDir, string? outputDir)
    {
        var razorFiles = Directory.GetFiles(inputDir, "*.razor", SearchOption.AllDirectories);

        if (razorFiles.Length == 0)
        {
            Console.WriteLine($"No .razor files found in {inputDir}");
            return 0;
        }

        Console.WriteLine($"Found {razorFiles.Length} .razor file(s)");
        Console.WriteLine();

        var successCount = 0;
        var failCount = 0;

        foreach (var file in razorFiles)
        {
            var result = CompileFile(file, outputDir ?? Path.GetDirectoryName(file));
            if (result == 0) successCount++;
            else failCount++;
            Console.WriteLine();
        }

        Console.WriteLine(new string('═', 60));
        Console.WriteLine($"Compilation complete: {successCount} succeeded, {failCount} failed");

        return failCount > 0 ? 1 : 0;
    }

    static int TreeCommand(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: No input file specified.");
            Console.WriteLine("Usage: shalimar tree <file.razor>");
            return 1;
        }

        var inputPath = args[0];

        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"Error: File not found: {inputPath}");
            return 1;
        }

        try
        {
            var source = File.ReadAllText(inputPath);
            var compiler = new RazorReactCompiler();
            var tree = compiler.DumpInfo(source, Path.GetFileName(inputPath));

            Console.WriteLine($"Info for: {inputPath}");
            Console.WriteLine(new string('═', 60));
            Console.WriteLine(tree);

            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    static int WatchCommand(string[] args)
    {
        Console.WriteLine("Watch mode not implemented in POC.");
        Console.WriteLine("Would watch for .razor file changes and recompile.");
        return 0;
    }

    static int DemoCommand()
    {
        Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════════╗
║                    SHALIMAR RAZOR DEMO                         ║
╚═══════════════════════════════════════════════════════════════╝
");

        var demoRazor = @"@inherits SliceComponent<ResidentCardProps>

<div class=""card resident-card"">
    <header class=""card-header"">
        <h1>@Props.Name</h1>
        <span class=""room-number"">Room @Props.RoomNumber</span>
    </header>

    @if (Props.IsHighRisk)
    {
        <div class=""alert alert-warning"">
            <Icon name=""warning"" />
            High Fall Risk
        </div>
    }

    <section class=""medications"">
        <h2>Current Medications</h2>
        <ul>
            @foreach (var med in Props.Medications)
            {
                <MedRow medication=""@med"" />
            }
        </ul>
    </section>

    <footer class=""card-actions"">
        <button class=""btn btn-primary"" @onclick=""@OnViewDetails"">
            View Details
        </button>
    </footer>
</div>";

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("INPUT: ResidentCard.razor");
        Console.ResetColor();
        Console.WriteLine(new string('─', 60));
        Console.WriteLine(demoRazor);
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("PARSING with Microsoft.AspNetCore.Razor.Language...");
        Console.ResetColor();
        Console.WriteLine();

        var compiler = new RazorReactCompiler();

        // Show parse info
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("PARSE INFO:");
        Console.ResetColor();
        Console.WriteLine(new string('─', 60));
        var info = compiler.DumpInfo(demoRazor, "ResidentCard.razor");
        var infoLines = info.Split('\n').Take(25);
        foreach (var line in infoLines)
        {
            Console.WriteLine(line);
        }
        Console.WriteLine("... (truncated)");
        Console.WriteLine();

        // Compile to TSX
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("OUTPUT: ResidentCard.tsx");
        Console.ResetColor();
        Console.WriteLine(new string('─', 60));

        try
        {
            var result = compiler.Compile(demoRazor, "ResidentCard.razor");
            if (result.IsSuccess)
            {
                Console.WriteLine(result.TsxOutput);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"Error at line {error.Line}: {error.Message}");
                }
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Compilation error: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.WriteLine(new string('═', 60));
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(@"
The PROOF:
  • Microsoft's Razor parser: 50,000+ lines of battle-tested code
  • Our emitter: ~500 lines
  • Result: React components from C# Razor syntax

That's leverage.
");
        Console.ResetColor();

        return 0;
    }

    static int UnknownCommand(string command)
    {
        Console.WriteLine($"Unknown command: {command}");
        PrintUsage();
        return 1;
    }

    static string? GetOption(string[] args, string option)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == option)
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
