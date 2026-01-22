using Shalimar.Razor;

Console.WriteLine("[Shalimar] Razor to TSX Compiler (.NET)");
Console.WriteLine();

if (args.Length == 0)
{
    Console.WriteLine("Usage: shalimar <source-directory>");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  shalimar ./Features");
    Console.WriteLine("  shalimar /path/to/razor/components");
    return 1;
}

var sourceDir = args[0];

if (!Directory.Exists(sourceDir))
{
    Console.WriteLine($"Error: Directory not found: {sourceDir}");
    return 1;
}

Console.WriteLine($"Compiling Razor files in: {Path.GetFullPath(sourceDir)}");
Console.WriteLine();

var compiler = new RazorToTsxCompiler(sourceDir);
var results = compiler.CompileDirectory(sourceDir);

Console.WriteLine();
Console.WriteLine($"Compilation complete: {results.Count(r => r.Success)} succeeded, {results.Count(r => !r.Success)} failed");

return results.All(r => r.Success) ? 0 : 1;
