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

        // EVENT HANDLERS
        Console.WriteLine("\n### Event Handlers ###");

        RunTest("E16 onclick handler", @"
<button @onclick=""handleClick"">Click me</button>
@code {
    void handleClick() { }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("onClick={handleClick}"), "Should have onClick={handleClick}");
            });

        RunTest("E17 onclick inline arrow", @"
<button @onclick=""() => count++"">+1</button>
@code {
    [Store] int count = 0;
}",
            result => {
                Assert(result.GeneratedCode!.Contains("onClick={() =>"), "Should have onClick arrow function");
            });

        RunTest("E18 onchange handler", @"
<input @onchange=""handleChange"" />
@code {
    void handleChange() { }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("onChange={handleChange}"), "Should have onChange");
            });

        RunTest("E19 onsubmit handler", @"
<form @onsubmit=""handleSubmit"">
    <button type=""submit"">Submit</button>
</form>
@code {
    void handleSubmit() { }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("onSubmit={handleSubmit}"), "Should have onSubmit");
            });

        RunTest("E20 onkeydown handler", @"
<input @onkeydown=""handleKeyDown"" />
@code {
    void handleKeyDown() { }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("onKeyDown={handleKeyDown}"), "Should have onKeyDown");
            });

        // ELSE / ELSE IF
        Console.WriteLine("\n### Conditionals - else/else if ###");

        RunTest("E21 if else", @"
@if (Props.IsLoggedIn)
{
    <span>Welcome!</span>
}
else
{
    <span>Please login</span>
}
@code {
    [Parameter] public bool IsLoggedIn { get; set; }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("?") && result.GeneratedCode!.Contains(":"),
                    "Should have ternary operator");
            });

        RunTest("E22 if else if else", @"
@if (Props.Status == ""loading"")
{
    <span>Loading...</span>
}
else if (Props.Status == ""error"")
{
    <span>Error!</span>
}
else
{
    <span>Ready</span>
}
@code {
    [Parameter] public string Status { get; set; }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("Status === \"loading\""), "Should have first condition");
                Assert(result.GeneratedCode!.Contains("Status === \"error\""), "Should have else if condition");
            });

        // EXPLICIT EXPRESSIONS
        Console.WriteLine("\n### Explicit Expressions @(...) ###");

        RunTest("E23 explicit expression add", @"
<div>Sum: @(Props.A + Props.B)</div>
@code {
    [Parameter] public int A { get; set; }
    [Parameter] public int B { get; set; }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("{A + B}"), "Should have {A + B}");
            });

        RunTest("E24 explicit ternary", @"
<div>@(Props.IsActive ? ""Active"" : ""Inactive"")</div>
@code {
    [Parameter] public bool IsActive { get; set; }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("{IsActive ?"), "Should have ternary");
            });

        RunTest("E25 string interpolation", @"
<div>@($""Hello {Props.Name}!"")</div>
@code {
    [Parameter] public string Name { get; set; }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("`Hello ${Name}!`"), "Should convert to template literal");
            });

        // TANSTACK STORE
        Console.WriteLine("\n### TanStack Store ###");

        RunTest("E26 Store state var", @"
@client
<div>Count: @count</div>
<button @onclick=""increment"">+1</button>
@code {
    [Store] int count = 0;

    void increment() {
        count++;
    }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("@tanstack/store") || result.GeneratedCode!.Contains("useState"),
                    "Should import store or useState");
                Assert(result.GeneratedCode!.Contains("count"), "Should have count state");
            });

        RunTest("E27 Multiple store vars", @"
@client
<div>@name - @age</div>
@code {
    [Store] string name = """";
    [Store] int age = 0;
}",
            result => {
                Assert(result.GeneratedCode!.Contains("name"), "Should have name");
                Assert(result.GeneratedCode!.Contains("age"), "Should have age");
            });

        // TWO-WAY BINDING
        Console.WriteLine("\n### Two-Way Binding ###");

        RunTest("E28 bind input text", @"
@client
<input @bind=""name"" />
@code {
    [Store] string name = """";
}",
            result => {
                Assert(result.GeneratedCode!.Contains("value={name}"), "Should have value={name}");
                Assert(result.GeneratedCode!.Contains("onChange"), "Should have onChange");
            });

        RunTest("E29 bind select", @"
@client
<select @bind=""selected"">
    <option value=""a"">A</option>
    <option value=""b"">B</option>
</select>
@code {
    [Store] string selected = ""a"";
}",
            result => {
                Assert(result.GeneratedCode!.Contains("value={selected}"), "Should bind select value");
            });

        RunTest("E30 bind checkbox", @"
@client
<input type=""checkbox"" @bind=""isChecked"" />
@code {
    [Store] bool isChecked = false;
}",
            result => {
                Assert(result.GeneratedCode!.Contains("checked"), "Should use checked for checkbox");
            });

        // IMPORTS (Skip TS validation - testing import transformation, not module existence)
        Console.WriteLine("\n### Imports ###");

        RunTest("E31 using component", @"
@using ""./ProductCard""

<div>
    <ProductCard />
</div>",
            result => {
                Assert(result.GeneratedCode!.Contains("import") && result.GeneratedCode!.Contains("ProductCard"),
                    "Should import ProductCard");
                Assert(result.GeneratedCode!.Contains("from './ProductCard'"), "Should have correct import path");
            }, validateTs: false);

        RunTest("E32 using with alias", @"
@using ""./Button"" as PrimaryButton

<div>
    <PrimaryButton>Click</PrimaryButton>
</div>",
            result => {
                Assert(result.GeneratedCode!.Contains("PrimaryButton"), "Should have alias");
                Assert(result.GeneratedCode!.Contains("from './Button'"), "Should have correct path");
            }, validateTs: false);

        RunTest("E33 using named imports", @"
@using { formatDate, formatCurrency } from ""./utils""

<div>Test</div>",
            result => {
                Assert(result.GeneratedCode!.Contains("formatDate"), "Should import formatDate");
                Assert(result.GeneratedCode!.Contains("formatCurrency"), "Should import formatCurrency");
                Assert(result.GeneratedCode!.Contains("from './utils'"), "Should have correct path");
            }, validateTs: false);

        // CHILDREN / SLOTS
        Console.WriteLine("\n### Children/Slots ###");

        RunTest("E34 children slot", @"
<div class=""card"">
    @children
</div>
@code {
    [Parameter] public RenderFragment children { get; set; }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("{children}"), "Should render children");
                Assert(result.GeneratedCode!.Contains("React.ReactNode") ||
                       result.GeneratedCode!.Contains("ReactNode"),
                    "Should type children as ReactNode");
            });

        // COMBINED PATTERNS
        Console.WriteLine("\n### Combined Patterns ###");

        RunTest("E35 form with bind and events", @"
@client
<form @onsubmit=""handleSubmit"">
    <input @bind=""email"" type=""email"" />
    <input @bind=""password"" type=""password"" />
    <button type=""submit"">Login</button>
</form>
@code {
    [Store] string email = """";
    [Store] string password = """";

    void handleSubmit() {
        // submit logic
    }
}",
            result => {
                Assert(result.GeneratedCode!.Contains("onSubmit"), "Should have onSubmit");
                Assert(result.GeneratedCode!.Contains("email"), "Should have email");
                Assert(result.GeneratedCode!.Contains("password"), "Should have password");
            });

        RunTest("E36 list with conditional", @"
<ul>
@foreach (var item in Items)
{
    @if (item.IsVisible)
    {
        <li>@item.Name</li>
    }
}
</ul>
@code {
    [Parameter] public List<ItemModel> Items { get; set; }
}",
            result => {
                Assert(result.GeneratedCode!.Contains(".map"), "Should use map");
                Assert(result.GeneratedCode!.Contains("&&") || result.GeneratedCode!.Contains("?"),
                    "Should have conditional");
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
