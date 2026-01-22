using Shalimar.Razor;

namespace Shalimar.Razor.Tests;

/// <summary>
/// TDD-style tests for each supported Razor expression.
/// Each test: 1) Compiles Razor 2) Validates TSX with tsc 3) Checks output pattern
/// </summary>
public class ExpressionTests : IDisposable
{
    private readonly string _testDir;
    private readonly RazorToTsxCompiler _compiler;
    private int _passed = 0;
    private int _failed = 0;
    private readonly List<string> _failures = new();

    public ExpressionTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"expr-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _compiler = new RazorToTsxCompiler(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    private CompilationResult CompileRazor(string razorContent, string componentName = "TestComponent")
    {
        var filePath = Path.Combine(_testDir, $"{componentName}.razor");
        File.WriteAllText(filePath, razorContent);
        return _compiler.Compile(filePath);
    }

    private void RunTest(string name, string razorInput, Action<CompilationResult> assertions, bool validateTs = true)
    {
        try
        {
            Console.WriteLine($"\n--- Test: {name} ---");
            Console.WriteLine($"Input Razor:\n{razorInput.Trim()}");

            var result = CompileRazor(razorInput, name.Replace(" ", ""));

            if (!result.Success)
                throw new Exception($"Compilation failed: {result.Error}");

            Console.WriteLine($"\nGenerated TSX:\n{result.GeneratedCode}");

            // Run custom assertions
            assertions(result);

            // Validate TypeScript
            if (validateTs)
            {
                var (isValid, tsError) = TsxValidator.ValidateTsx(result.GeneratedCode!, name.Replace(" ", ""));
                if (!isValid)
                    throw new Exception($"TypeScript validation failed:\n{tsError}");
                Console.WriteLine("TypeScript: VALID");
            }

            _passed++;
            Console.WriteLine($"✓ PASSED: {name}");
        }
        catch (Exception ex)
        {
            _failed++;
            _failures.Add($"{name}: {ex.Message}");
            Console.WriteLine($"✗ FAILED: {name}");
            Console.WriteLine($"  Error: {ex.Message}");
        }
    }

    public void RunAllTests()
    {
        Console.WriteLine("=== Expression Tests with TypeScript Validation ===\n");

        // BASIC EXPRESSIONS
        Console.WriteLine("\n### Basic Expressions ###");

        RunTest("E01 Simple prop reference", @"
<div>Hello @Props.Name</div>
@code {
    [Parameter] public string Name { get; set; }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("{Name}"), "Should contain {Name}");
            });

        RunTest("E02 Multiple prop references", @"
<div>@Props.First and @Props.Second</div>
@code {
    [Parameter] public string First { get; set; }
    [Parameter] public string Second { get; set; }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("{First}"), "Should contain {First}");
                Assert(result.GeneratedCode!.Contains("{Second}"), "Should contain {Second}");
            });

        RunTest("E03 Prop in attribute", @"
<img src=""@Props.ImageUrl"" alt=""@Props.Alt"" />
@code {
    [Parameter] public string ImageUrl { get; set; }
    [Parameter] public string Alt { get; set; }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("src={ImageUrl}"), "Should have src={ImageUrl}");
                Assert(result.GeneratedCode!.Contains("alt={Alt}"), "Should have alt={Alt}");
            });

        // CONDITIONALS
        Console.WriteLine("\n### Conditionals ###");

        RunTest("E04 Simple if boolean", @"
@if (Props.IsVisible)
{
    <span>Visible</span>
}
@code {
    [Parameter] public bool IsVisible { get; set; }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("IsVisible") && result.GeneratedCode!.Contains("&&"),
                    "Should have conditional &&");
            });

        RunTest("E05 If with comparison", @"
@if (Props.Count > 0)
{
    <span>Has items</span>
}
@code {
    [Parameter] public int Count { get; set; }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("Count > 0"), "Should preserve comparison");
            });

        RunTest("E06 If with negation", @"
@if (!Props.IsHidden)
{
    <span>Shown</span>
}
@code {
    [Parameter] public bool IsHidden { get; set; }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("!IsHidden"), "Should preserve negation");
            });

        // LOOPS
        Console.WriteLine("\n### Loops ###");

        RunTest("E07 Simple foreach", @"
<ul>
@foreach (var item in Items)
{
    <li>@item.Name</li>
}
</ul>
@code {
    [Parameter] public List<ItemModel> Items { get; set; }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("Items.map"), "Should use .map()");
                Assert(result.GeneratedCode!.Contains("{item.Name}"), "Should reference item.Name");
            });

        RunTest("E08 Foreach with multiple properties", @"
@foreach (var product in Products)
{
    <div>
        <span>@product.Name</span>
        <span>@product.Price</span>
    </div>
}
@code {
    [Parameter] public List<ProductModel> Products { get; set; }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("Products.map"), "Should use Products.map");
                Assert(result.GeneratedCode!.Contains("{product.Name}"), "Should have {product.Name}");
                Assert(result.GeneratedCode!.Contains("{product.Price}"), "Should have {product.Price}");
            });

        // ATTRIBUTES
        Console.WriteLine("\n### Attribute Transforms ###");

        RunTest("E09 class to className", @"
<div class=""container"">
    <span class=""label"">Text</span>
</div>",
            result => {
                Assert(!result.GeneratedCode!.Contains("class="), "Should not have class=");
                Assert(result.GeneratedCode!.Contains("className="), "Should have className=");
            });

        RunTest("E10 for to htmlFor", @"
<label for=""email"">Email:</label>
<input id=""email"" />",
            result => {
                Assert(!result.GeneratedCode!.Contains(" for="), "Should not have for=");
                Assert(result.GeneratedCode!.Contains("htmlFor="), "Should have htmlFor=");
            });

        // STYLES
        Console.WriteLine("\n### Style Transforms ###");

        RunTest("E11 Simple style", @"
<div style=""color: red;"">Red text</div>",
            result => {
                Assert(result.GeneratedCode!.Contains("style={{"), "Should have style={{");
                Assert(result.GeneratedCode!.Contains("color:"), "Should have color property");
            });

        RunTest("E12 MultiStyleKebabCase", @"
<div style=""background-color: blue; font-size: 14px;"">Styled</div>",
            result => {
                Assert(result.GeneratedCode!.Contains("backgroundColor"), "Should camelCase backgroundColor");
                Assert(result.GeneratedCode!.Contains("fontSize"), "Should camelCase fontSize");
            });

        // COMPONENT OUTPUT
        Console.WriteLine("\n### Component Structure ###");

        RunTest("E13 Props interface generated", @"
<div>@Props.Name</div>
@code {
    [Parameter] public string Name { get; set; }
    [Parameter] public int Age { get; set; }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("interface"), "Should have interface");
                Assert(result.GeneratedCode!.Contains("Name: string"), "Should type Name as string");
                Assert(result.GeneratedCode!.Contains("Age: number"), "Should type Age as number");
            });

        RunTest("E14 Export function", @"
<div>Hello</div>",
            result => {
                Assert(result.GeneratedCode!.Contains("export function"), "Should export function");
            });

        RunTest("E15 React import", @"
<div>Hello</div>",
            result => {
                Assert(result.GeneratedCode!.Contains("import React"), "Should import React");
            });

        // PRINT SUMMARY
        Console.WriteLine($"\n=== Results: {_passed} passed, {_failed} failed ===\n");

        if (_failures.Any())
        {
            Console.WriteLine("Failed tests:");
            foreach (var f in _failures)
                Console.WriteLine($"  - {f}");
        }
    }

    private void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception($"Assertion failed: {message}");
    }
}
