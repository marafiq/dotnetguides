using Shalimar.Razor;

namespace Shalimar.Razor.Tests;

/// <summary>
/// Comprehensive tests for RazorToTsxCompiler with 100 test cases
/// Standalone test runner without external dependencies
/// </summary>
public class TestRunner : IDisposable
{
    private readonly string _testDir;
    private readonly RazorToTsxCompiler _compiler;
    private int _passed = 0;
    private int _failed = 0;
    private readonly List<string> _failures = new();

    public TestRunner()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"shalimar-tests-{Guid.NewGuid()}");
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

    private void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception($"Assertion failed: {message}");
    }

    private void AssertContains(string? haystack, string needle)
    {
        if (haystack == null || !haystack.Contains(needle))
            throw new Exception($"Expected to contain '{needle}' but got:\n{haystack?.Substring(0, Math.Min(200, haystack?.Length ?? 0))}...");
    }

    private void AssertNotContains(string? haystack, string needle)
    {
        if (haystack != null && haystack.Contains(needle))
            throw new Exception($"Expected NOT to contain '{needle}'");
    }

    private void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Expected {expected} but got {actual}");
    }

    private void AssertTrue(bool value) => Assert(value, "Expected true");
    private void AssertNotNull(object? value) => Assert(value != null, "Expected not null");
    private void AssertSingle<T>(IEnumerable<T>? collection)
    {
        var count = collection?.Count() ?? 0;
        Assert(count == 1, $"Expected single item but got {count}");
    }
    private void AssertEndsWith(string? haystack, string needle)
    {
        Assert(haystack != null && haystack.EndsWith(needle), $"Expected to end with '{needle}'");
    }

    private void RunTest(string name, Action test)
    {
        try
        {
            test();
            _passed++;
            Console.WriteLine($"  ✓ {name}");
        }
        catch (Exception ex)
        {
            _failed++;
            _failures.Add($"{name}: {ex.Message}");
            Console.WriteLine($"  ✗ {name}");
            Console.WriteLine($"    Error: {ex.Message}");
        }
    }

    public void RunAllTests()
    {
        Console.WriteLine("\n=== Shalimar Razor-to-TSX Compiler Tests ===\n");

        Console.WriteLine("Type Conversion Tests (1-15):");
        RunTypeConversionTests();

        Console.WriteLine("\nDirective Extraction Tests (16-25):");
        RunDirectiveTests();

        Console.WriteLine("\nProps Extraction Tests (26-40):");
        RunPropsTests();

        Console.WriteLine("\nTemplate Transformation Tests (41-60):");
        RunTemplateTests();

        Console.WriteLine("\nIf Block Transformation Tests (61-70):");
        RunIfBlockTests();

        Console.WriteLine("\nForeach Block Transformation Tests (71-80):");
        RunForeachTests();

        Console.WriteLine("\nStyle Transformation Tests (81-90):");
        RunStyleTests();

        Console.WriteLine("\nFull Compilation Integration Tests (91-100):");
        RunIntegrationTests();

        Console.WriteLine($"\n=== Results: {_passed} passed, {_failed} failed out of {_passed + _failed} tests ===\n");

        if (_failures.Any())
        {
            Console.WriteLine("Failed tests:");
            foreach (var f in _failures) Console.WriteLine($"  - {f}");
        }
    }

    private void RunTypeConversionTests()
    {
        RunTest("001 string->string", () => {
            var result = CompileRazor("<div>@Props.Name</div>\n@code {\n    [Parameter] public string Name { get; set; }\n}");
            AssertContains(result.GeneratedCode, "Name: string");
        });

        RunTest("002 int->number", () => {
            var result = CompileRazor("<div>@Props.Count</div>\n@code {\n    [Parameter] public int Count { get; set; }\n}");
            AssertContains(result.GeneratedCode, "Count: number");
        });

        RunTest("003 long->number", () => {
            var result = CompileRazor("<div>@Props.Id</div>\n@code {\n    [Parameter] public long Id { get; set; }\n}");
            AssertContains(result.GeneratedCode, "Id: number");
        });

        RunTest("004 double->number", () => {
            var result = CompileRazor("<div>@Props.Value</div>\n@code {\n    [Parameter] public double Value { get; set; }\n}");
            AssertContains(result.GeneratedCode, "Value: number");
        });

        RunTest("005 float->number", () => {
            var result = CompileRazor("<div>@Props.Rate</div>\n@code {\n    [Parameter] public float Rate { get; set; }\n}");
            AssertContains(result.GeneratedCode, "Rate: number");
        });

        RunTest("006 decimal->number", () => {
            var result = CompileRazor("<div>@Props.Price</div>\n@code {\n    [Parameter] public decimal Price { get; set; }\n}");
            AssertContains(result.GeneratedCode, "Price: number");
        });

        RunTest("007 bool->boolean", () => {
            var result = CompileRazor("<div>@Props.IsActive</div>\n@code {\n    [Parameter] public bool IsActive { get; set; }\n}");
            AssertContains(result.GeneratedCode, "IsActive: boolean");
        });

        RunTest("008 DateTime->Date", () => {
            var result = CompileRazor("<div>@Props.Created</div>\n@code {\n    [Parameter] public DateTime Created { get; set; }\n}");
            AssertContains(result.GeneratedCode, "Created: Date");
        });

        RunTest("009 Guid->string", () => {
            var result = CompileRazor("<div>@Props.Id</div>\n@code {\n    [Parameter] public Guid Id { get; set; }\n}");
            AssertContains(result.GeneratedCode, "Id: string");
        });

        RunTest("010 List<string>->string[]", () => {
            var result = CompileRazor("<div></div>\n@code {\n    [Parameter] public List<string> Items { get; set; }\n}");
            AssertContains(result.GeneratedCode, "Items: string[]");
        });

        RunTest("011 List<int>->number[]", () => {
            var result = CompileRazor("<div></div>\n@code {\n    [Parameter] public List<int> Numbers { get; set; }\n}");
            AssertContains(result.GeneratedCode, "Numbers: number[]");
        });

        RunTest("012 string[]->string[]", () => {
            var result = CompileRazor("<div></div>\n@code {\n    [Parameter] public string[] Tags { get; set; }\n}");
            AssertContains(result.GeneratedCode, "Tags: string[]");
        });

        RunTest("013 string?->string|null", () => {
            var result = CompileRazor("<div></div>\n@code {\n    [Parameter] public string? OptionalName { get; set; }\n}");
            AssertContains(result.GeneratedCode, "OptionalName: string | null");
        });

        RunTest("014 int?->number|null", () => {
            var result = CompileRazor("<div></div>\n@code {\n    [Parameter] public int? OptionalCount { get; set; }\n}");
            AssertContains(result.GeneratedCode, "OptionalCount: number | null");
        });

        RunTest("015 CustomType preserved", () => {
            var result = CompileRazor("<div></div>\n@code {\n    [Parameter] public UserModel User { get; set; }\n}");
            AssertContains(result.GeneratedCode, "User: UserModel");
        });
    }

    private void RunDirectiveTests()
    {
        RunTest("016 @client directive detected", () => {
            var result = CompileRazor("@client\n<div>Hello</div>");
            AssertTrue(result.Success);
            AssertEqual(ComponentDirective.Client, result.Component?.Directive);
        });

        RunTest("017 @server directive detected", () => {
            var result = CompileRazor("@server\n<div>Hello</div>");
            AssertTrue(result.Success);
            AssertEqual(ComponentDirective.Server, result.Component?.Directive);
        });

        RunTest("018 no directive = default", () => {
            var result = CompileRazor("<div>Hello</div>");
            AssertTrue(result.Success);
            AssertEqual(ComponentDirective.Default, result.Component?.Directive);
        });

        RunTest("019 @client with whitespace", () => {
            var result = CompileRazor("@client\n\n<div>Hello</div>");
            AssertEqual(ComponentDirective.Client, result.Component?.Directive);
        });

        RunTest("020 @server with trailing space", () => {
            var result = CompileRazor("@server   \n<div>Hello</div>");
            AssertEqual(ComponentDirective.Server, result.Component?.Directive);
        });

        RunTest("021 directive in middle not detected", () => {
            var result = CompileRazor("<div>\n@client\nHello</div>");
            AssertEqual(ComponentDirective.Default, result.Component?.Directive);
        });

        RunTest("022 @client removed from output", () => {
            var result = CompileRazor("@client\n<div>Hello</div>");
            AssertNotContains(result.GeneratedCode, "@client");
        });

        RunTest("023 @server removed from output", () => {
            var result = CompileRazor("@server\n<div>Hello</div>");
            AssertNotContains(result.GeneratedCode, "@server");
        });

        RunTest("024 component name from filename", () => {
            var result = CompileRazor("@client\n<div>Test</div>", "MyButton");
            AssertContains(result.GeneratedCode, "export function MyButton");
        });

        RunTest("025 multiple directives first wins", () => {
            var result = CompileRazor("@client\n@server\n<div>Test</div>");
            AssertEqual(ComponentDirective.Client, result.Component?.Directive);
        });
    }

    private void RunPropsTests()
    {
        RunTest("026 single prop extracted", () => {
            var result = CompileRazor("<div>@Props.Name</div>\n@code {\n    [Parameter] public string Name { get; set; }\n}");
            AssertSingle(result.Component?.Props);
            AssertEqual("Name", result.Component?.Props[0].Name);
        });

        RunTest("027 multiple props extracted", () => {
            var result = CompileRazor("<div></div>\n@code {\n    [Parameter] public string Name { get; set; }\n    [Parameter] public int Age { get; set; }\n    [Parameter] public bool Active { get; set; }\n}");
            AssertEqual(3, result.Component?.Props.Count);
        });

        RunTest("028 default value extracted", () => {
            var result = CompileRazor("<div></div>\n@code {\n    [Parameter] public bool IsActive { get; set; } = true;\n}");
            var prop = result.Component?.Props.FirstOrDefault(p => p.Name == "IsActive");
            AssertNotNull(prop);
            AssertEqual("true", prop?.DefaultValue);
        });

        RunTest("029 default makes optional", () => {
            var result = CompileRazor("<div></div>\n@code {\n    [Parameter] public string Title { get; set; } = \"Default\";\n}");
            AssertContains(result.GeneratedCode, "Title?:");
        });

        RunTest("030 no default = required", () => {
            var result = CompileRazor("<div></div>\n@code {\n    [Parameter] public string Name { get; set; }\n}");
            AssertContains(result.GeneratedCode, "Name:");
            AssertNotContains(result.GeneratedCode, "Name?:");
        });

        RunTest("031 interface generated", () => {
            var result = CompileRazor("<div></div>\n@code {\n    [Parameter] public string Name { get; set; }\n}", "Button");
            AssertContains(result.GeneratedCode, "export interface ButtonProps");
        });

        RunTest("032 no props = no interface", () => {
            var result = CompileRazor("<div>Static content</div>", "Static");
            AssertNotContains(result.GeneratedCode, "interface StaticProps");
        });

        RunTest("033 props destructured", () => {
            var result = CompileRazor("<div></div>\n@code {\n    [Parameter] public string Name { get; set; }\n    [Parameter] public int Count { get; set; }\n}");
            AssertContains(result.GeneratedCode, "{ Name, Count }");
        });

        RunTest("034 generic list prop", () => {
            var result = CompileRazor("<div></div>\n@code {\n    [Parameter] public List<ProductModel> Products { get; set; }\n}");
            AssertContains(result.GeneratedCode, "Products: ProductModel[]");
        });

        RunTest("035 string default with quotes", () => {
            var result = CompileRazor("<div></div>\n@code {\n    [Parameter] public string Text { get; set; } = \"Hello\";\n}");
            AssertContains(result.GeneratedCode, "Text?:");
        });

        RunTest("036 numeric default value", () => {
            var result = CompileRazor("<div></div>\n@code {\n    [Parameter] public int Count { get; set; } = 10;\n}");
            var prop = result.Component?.Props.FirstOrDefault(p => p.Name == "Count");
            AssertEqual("10", prop?.DefaultValue);
        });

        RunTest("037 bool default false", () => {
            var result = CompileRazor("<div></div>\n@code {\n    [Parameter] public bool Disabled { get; set; } = false;\n}");
            AssertContains(result.GeneratedCode, "Disabled?:");
        });

        RunTest("038 non-parameter ignored", () => {
            var result = CompileRazor("<div></div>\n@code {\n    public string NotAParam { get; set; }\n    [Parameter] public string IsAParam { get; set; }\n}");
            AssertEqual(1, result.Component?.Props.Count);
            AssertEqual("IsAParam", result.Component?.Props[0].Name);
        });

        RunTest("039 private field ignored", () => {
            var result = CompileRazor("<div></div>\n@code {\n    private string _privateField;\n    [Parameter] public string PublicProp { get; set; }\n}");
            AssertEqual(1, result.Component?.Props.Count);
        });

        RunTest("040 complex default extracted", () => {
            var result = CompileRazor("<div></div>\n@code {\n    [Parameter] public string Format { get; set; } = \"yyyy-MM-dd\";\n}");
            var prop = result.Component?.Props.FirstOrDefault(p => p.Name == "Format");
            AssertContains(prop?.DefaultValue ?? "", "yyyy-MM-dd");
        });
    }

    private void RunTemplateTests()
    {
        RunTest("041 @Props.X -> {X}", () => {
            var result = CompileRazor("<div>@Props.Name</div>\n@code {\n    [Parameter] public string Name { get; set; }\n}");
            AssertContains(result.GeneratedCode, "{Name}");
        });

        RunTest("042 class -> className", () => {
            var result = CompileRazor("<div class=\"container\">Content</div>");
            AssertContains(result.GeneratedCode, "className=\"container\"");
        });

        RunTest("043 for -> htmlFor", () => {
            var result = CompileRazor("<label for=\"input1\">Label</label>");
            AssertContains(result.GeneratedCode, "htmlFor=\"input1\"");
        });

        RunTest("044 multiple class transforms", () => {
            var result = CompileRazor("<div class=\"a\">\n  <span class=\"b\">Text</span>\n</div>");
            AssertNotContains(result.GeneratedCode, "class=");
            AssertContains(result.GeneratedCode, "className=");
        });

        RunTest("045 attribute with expression", () => {
            var result = CompileRazor("<img src=\"@Props.ImageUrl\" alt=\"@Props.Title\" />\n@code {\n    [Parameter] public string ImageUrl { get; set; }\n    [Parameter] public string Title { get; set; }\n}");
            AssertContains(result.GeneratedCode, "src={ImageUrl}");
            AssertContains(result.GeneratedCode, "alt={Title}");
        });

        RunTest("046 @code block removed", () => {
            var result = CompileRazor("<div>Hello</div>\n@code {\n    [Parameter] public string Name { get; set; }\n    private void DoSomething() { }\n}");
            AssertNotContains(result.GeneratedCode, "@code");
            // Methods are now extracted and generated as const arrow functions
            AssertContains(result.GeneratedCode, "DoSomething");
        });

        RunTest("047 @using directive removed", () => {
            var result = CompileRazor("@using MyApp.Models\n<div>Hello</div>");
            AssertNotContains(result.GeneratedCode, "@using");
            AssertNotContains(result.GeneratedCode, "MyApp.Models");
        });

        RunTest("048 self-closing tag preserved", () => {
            var result = CompileRazor("<img src=\"test.jpg\" />");
            AssertContains(result.GeneratedCode, "<img");
            AssertContains(result.GeneratedCode, "/>");
        });

        RunTest("049 nested elements preserved", () => {
            var result = CompileRazor("<div>\n  <header>\n    <h1>Title</h1>\n  </header>\n  <main>Content</main>\n</div>");
            AssertContains(result.GeneratedCode, "<header>");
            AssertContains(result.GeneratedCode, "<h1>Title</h1>");
            AssertContains(result.GeneratedCode, "<main>Content</main>");
        });

        RunTest("050 multiple props in line", () => {
            var result = CompileRazor("<div>@Props.First - @Props.Second</div>\n@code {\n    [Parameter] public string First { get; set; }\n    [Parameter] public string Second { get; set; }\n}");
            AssertContains(result.GeneratedCode, "{First}");
            AssertContains(result.GeneratedCode, "{Second}");
        });

        RunTest("051 text content preserved", () => {
            var result = CompileRazor("<p>This is plain text content.</p>");
            AssertContains(result.GeneratedCode, "This is plain text content.");
        });

        RunTest("052 HTML entities preserved", () => {
            var result = CompileRazor("<p>&copy; 2024 &amp; beyond</p>");
            AssertContains(result.GeneratedCode, "&copy;");
            AssertContains(result.GeneratedCode, "&amp;");
        });

        RunTest("053 mixed text and expressions", () => {
            var result = CompileRazor("<p>Hello, @Props.Name! Welcome to @Props.Site.</p>\n@code {\n    [Parameter] public string Name { get; set; }\n    [Parameter] public string Site { get; set; }\n}");
            AssertContains(result.GeneratedCode, "Hello, {Name}! Welcome to {Site}.");
        });

        RunTest("054 empty element", () => {
            var result = CompileRazor("<div></div>");
            AssertContains(result.GeneratedCode, "<div></div>");
        });

        RunTest("055 whitespace preserved", () => {
            var result = CompileRazor("<pre>  indented  </pre>");
            AssertContains(result.GeneratedCode, "  indented  ");
        });

        RunTest("056 HTML comment handled", () => {
            var result = CompileRazor("<div><!-- HTML comment --></div>");
            AssertTrue(result.Success);
        });

        RunTest("057 multiline template", () => {
            var result = CompileRazor("<div>\n  Line 1\n  Line 2\n  Line 3\n</div>");
            AssertContains(result.GeneratedCode, "Line 1");
            AssertContains(result.GeneratedCode, "Line 2");
            AssertContains(result.GeneratedCode, "Line 3");
        });

        RunTest("058 data attributes preserved", () => {
            var result = CompileRazor("<div data-testid=\"test\" data-value=\"123\">Test</div>");
            AssertContains(result.GeneratedCode, "data-testid=\"test\"");
            AssertContains(result.GeneratedCode, "data-value=\"123\"");
        });

        RunTest("059 aria attributes preserved", () => {
            var result = CompileRazor("<button aria-label=\"Close\" aria-pressed=\"false\">X</button>");
            AssertContains(result.GeneratedCode, "aria-label=\"Close\"");
            AssertContains(result.GeneratedCode, "aria-pressed=\"false\"");
        });

        RunTest("060 boolean attributes preserved", () => {
            var result = CompileRazor("<input type=\"checkbox\" checked disabled />");
            AssertContains(result.GeneratedCode, "checked");
            AssertContains(result.GeneratedCode, "disabled");
        });
    }

    private void RunIfBlockTests()
    {
        RunTest("061 simple @if transformed", () => {
            var result = CompileRazor("@if (Props.ShowMessage)\n{\n    <p>Message</p>\n}\n@code {\n    [Parameter] public bool ShowMessage { get; set; }\n}");
            AssertContains(result.GeneratedCode, "{(ShowMessage) && (");
            AssertContains(result.GeneratedCode, "<p>Message</p>");
        });

        RunTest("062 @if with comparison", () => {
            var result = CompileRazor("@if (Props.Count > 0)\n{\n    <p>Has items</p>\n}\n@code {\n    [Parameter] public int Count { get; set; }\n}");
            AssertContains(result.GeneratedCode, "{(Count > 0) && (");
        });

        RunTest("063 @if with nested content", () => {
            var result = CompileRazor("@if (Props.Active)\n{\n    <div>\n        <span>Nested</span>\n    </div>\n}\n@code {\n    [Parameter] public bool Active { get; set; }\n}");
            AssertContains(result.GeneratedCode, "<div>");
            AssertContains(result.GeneratedCode, "<span>Nested</span>");
        });

        RunTest("064 multiple @if blocks", () => {
            var result = CompileRazor("@if (Props.A)\n{\n    <p>A</p>\n}\n@if (Props.B)\n{\n    <p>B</p>\n}\n@code {\n    [Parameter] public bool A { get; set; }\n    [Parameter] public bool B { get; set; }\n}");
            var count = System.Text.RegularExpressions.Regex.Matches(result.GeneratedCode ?? "", @"\{\([^)]+\) && \(").Count;
            AssertEqual(2, count);
        });

        RunTest("065 equality comparison", () => {
            var result = CompileRazor("@if (Props.Status == \"Active\")\n{\n    <span>Active</span>\n}\n@code {\n    [Parameter] public string Status { get; set; }\n}");
            // C# == is transformed to JavaScript === for type-safe comparison
            AssertContains(result.GeneratedCode, "Status === \"Active\"");
        });

        RunTest("066 not condition", () => {
            var result = CompileRazor("@if (!Props.Hidden)\n{\n    <div>Visible</div>\n}\n@code {\n    [Parameter] public bool Hidden { get; set; }\n}");
            AssertContains(result.GeneratedCode, "!Hidden");
        });

        RunTest("067 AND condition", () => {
            var result = CompileRazor("@if (Props.A && Props.B)\n{\n    <div>Both</div>\n}\n@code {\n    [Parameter] public bool A { get; set; }\n    [Parameter] public bool B { get; set; }\n}");
            AssertContains(result.GeneratedCode, "A && B");
        });

        RunTest("068 OR condition", () => {
            var result = CompileRazor("@if (Props.A || Props.B)\n{\n    <div>Either</div>\n}\n@code {\n    [Parameter] public bool A { get; set; }\n    [Parameter] public bool B { get; set; }\n}");
            AssertContains(result.GeneratedCode, "A || B");
        });

        RunTest("069 null check", () => {
            var result = CompileRazor("@if (Props.Name != null)\n{\n    <div>@Props.Name</div>\n}\n@code {\n    [Parameter] public string Name { get; set; }\n}");
            AssertContains(result.GeneratedCode, "Name != null");
        });

        RunTest("070 length check", () => {
            var result = CompileRazor("@if (Props.Items.Count > 0)\n{\n    <ul></ul>\n}\n@code {\n    [Parameter] public List<string> Items { get; set; }\n}");
            AssertContains(result.GeneratedCode, "Items.Count > 0");
        });
    }

    private void RunForeachTests()
    {
        RunTest("071 @foreach to .map()", () => {
            var result = CompileRazor("@foreach (var item in Items)\n{\n    <li>@item.Name</li>\n}\n@code {\n    [Parameter] public List<ItemModel> Items { get; set; }\n}");
            AssertContains(result.GeneratedCode, ".map((item, index) => (");
        });

        RunTest("072 item property transformed", () => {
            var result = CompileRazor("@foreach (var item in Items)\n{\n    <li>@item.Title</li>\n}\n@code {\n    [Parameter] public List<ItemModel> Items { get; set; }\n}");
            AssertContains(result.GeneratedCode, "{item.Title}");
        });

        RunTest("073 multiple properties", () => {
            var result = CompileRazor("@foreach (var item in Items)\n{\n    <div>@item.Name - @item.Price</div>\n}\n@code {\n    [Parameter] public List<ItemModel> Items { get; set; }\n}");
            AssertContains(result.GeneratedCode, "{item.Name}");
            AssertContains(result.GeneratedCode, "{item.Price}");
        });

        RunTest("074 nested elements in foreach", () => {
            var result = CompileRazor("@foreach (var item in Items)\n{\n    <div>\n        <h3>@item.Title</h3>\n        <p>@item.Description</p>\n    </div>\n}\n@code {\n    [Parameter] public List<ItemModel> Items { get; set; }\n}");
            AssertContains(result.GeneratedCode, "<h3>{item.Title}</h3>");
            AssertContains(result.GeneratedCode, "<p>{item.Description}</p>");
        });

        RunTest("075 foreach with attributes", () => {
            var result = CompileRazor("@foreach (var item in Items)\n{\n    <div class=\"item\">@item.Name</div>\n}\n@code {\n    [Parameter] public List<ItemModel> Items { get; set; }\n}");
            AssertContains(result.GeneratedCode, "className=\"item\"");
        });

        RunTest("076 product variable works", () => {
            var result = CompileRazor("@foreach (var product in Products)\n{\n    <div>@product.Name</div>\n}\n@code {\n    [Parameter] public List<ProductModel> Products { get; set; }\n}");
            AssertContains(result.GeneratedCode, "Products.map((product, index) => (");
            AssertContains(result.GeneratedCode, "{product.Name}");
        });

        RunTest("077 user variable works", () => {
            var result = CompileRazor("@foreach (var user in Users)\n{\n    <div>@user.Email</div>\n}\n@code {\n    [Parameter] public List<User> Users { get; set; }\n}");
            AssertContains(result.GeneratedCode, "Users.map((user, index) => (");
            AssertContains(result.GeneratedCode, "{user.Email}");
        });

        RunTest("078 index available", () => {
            var result = CompileRazor("@foreach (var item in Items)\n{\n    <li>@item.Name</li>\n}\n@code {\n    [Parameter] public List<ItemModel> Items { get; set; }\n}");
            AssertContains(result.GeneratedCode, ", index)");
        });

        RunTest("079 multiple foreach blocks", () => {
            var result = CompileRazor("<ul>\n@foreach (var a in AItems)\n{\n    <li>@a.Name</li>\n}\n</ul>\n<ul>\n@foreach (var b in BItems)\n{\n    <li>@b.Name</li>\n}\n</ul>\n@code {\n    [Parameter] public List<ItemModel> AItems { get; set; }\n    [Parameter] public List<ItemModel> BItems { get; set; }\n}");
            AssertContains(result.GeneratedCode, "AItems.map((a, index)");
            AssertContains(result.GeneratedCode, "BItems.map((b, index)");
        });

        RunTest("080 empty container in foreach", () => {
            var result = CompileRazor("@foreach (var item in Items)\n{\n    <span></span>\n}\n@code {\n    [Parameter] public List<string> Items { get; set; }\n}");
            AssertContains(result.GeneratedCode, "Items.map((item, index) => (");
        });
    }

    private void RunStyleTests()
    {
        RunTest("081 simple style to object", () => {
            var result = CompileRazor("<div style=\"color: red;\">Text</div>");
            AssertContains(result.GeneratedCode, "style={{color: 'red'}}");
        });

        RunTest("082 multiple style properties", () => {
            var result = CompileRazor("<div style=\"color: red; font-size: 14px;\">Text</div>");
            AssertContains(result.GeneratedCode, "color: 'red'");
            AssertContains(result.GeneratedCode, "fontSize: '14px'");
        });

        RunTest("083 kebab-case to camelCase", () => {
            var result = CompileRazor("<div style=\"background-color: blue;\">Text</div>");
            AssertContains(result.GeneratedCode, "backgroundColor: 'blue'");
            AssertNotContains(result.GeneratedCode, "background-color:");
        });

        RunTest("084 font-family converted", () => {
            var result = CompileRazor("<div style=\"font-family: Arial;\">Text</div>");
            AssertContains(result.GeneratedCode, "fontFamily: 'Arial'");
        });

        RunTest("085 border-radius converted", () => {
            var result = CompileRazor("<div style=\"border-radius: 8px;\">Text</div>");
            AssertContains(result.GeneratedCode, "borderRadius: '8px'");
        });

        RunTest("086 box-shadow converted", () => {
            var result = CompileRazor("<div style=\"box-shadow: 0 2px 4px rgba(0,0,0,0.1);\">Text</div>");
            AssertContains(result.GeneratedCode, "boxShadow:");
        });

        RunTest("087 z-index converted", () => {
            var result = CompileRazor("<div style=\"z-index: 100;\">Text</div>");
            AssertContains(result.GeneratedCode, "zIndex: '100'");
        });

        RunTest("088 text-align converted", () => {
            var result = CompileRazor("<div style=\"text-align: center;\">Text</div>");
            AssertContains(result.GeneratedCode, "textAlign: 'center'");
        });

        RunTest("089 display flex + justify-content", () => {
            var result = CompileRazor("<div style=\"display: flex; justify-content: space-between;\">Text</div>");
            AssertContains(result.GeneratedCode, "display: 'flex'");
            AssertContains(result.GeneratedCode, "justifyContent: 'space-between'");
        });

        RunTest("090 margin and padding", () => {
            var result = CompileRazor("<div style=\"margin: 10px; padding: 20px;\">Text</div>");
            AssertContains(result.GeneratedCode, "margin: '10px'");
            AssertContains(result.GeneratedCode, "padding: '20px'");
        });
    }

    private void RunIntegrationTests()
    {
        RunTest("091 simple component succeeds", () => {
            var result = CompileRazor("<div>Hello World</div>");
            AssertTrue(result.Success);
            AssertNotNull(result.OutputPath);
            AssertNotNull(result.GeneratedCode);
        });

        RunTest("092 React imported", () => {
            var result = CompileRazor("<div>Hello</div>");
            AssertContains(result.GeneratedCode, "import React from 'react';");
        });

        RunTest("093 export function", () => {
            var result = CompileRazor("<div>Hello</div>", "Greeting");
            AssertContains(result.GeneratedCode, "export function Greeting(");
        });

        RunTest("094 returns JSX", () => {
            var result = CompileRazor("<div>Hello</div>");
            AssertContains(result.GeneratedCode, "return (");
            AssertContains(result.GeneratedCode, "<div>Hello</div>");
        });

        RunTest("095 output file created", () => {
            var result = CompileRazor("<div>Test</div>", "OutputTest");
            AssertTrue(result.Success);
            AssertTrue(File.Exists(result.OutputPath));
        });

        RunTest("096 output is .tsx", () => {
            var result = CompileRazor("<div>Test</div>", "ExtensionTest");
            AssertEndsWith(result.OutputPath, ".tsx");
        });

        RunTest("097 complex component", () => {
            var result = CompileRazor(@"@client

<div class=""card"" style=""padding: 20px; border: 1px solid #ccc;"">
    <h2>@Props.Title</h2>
    <p>@Props.Description</p>

    @if (Props.ShowDetails)
    {
        <div class=""details"">
            @foreach (var item in Items)
            {
                <span>@item.Name</span>
            }
        </div>
    }

    <button disabled=""@Props.Disabled"">Submit</button>
</div>

@code {
    [Parameter] public string Title { get; set; }
    [Parameter] public string Description { get; set; }
    [Parameter] public bool ShowDetails { get; set; }
    [Parameter] public bool Disabled { get; set; } = false;
    [Parameter] public List<ItemModel> Items { get; set; }
}", "ComplexCard");

            AssertTrue(result.Success);
            AssertContains(result.GeneratedCode, "export interface ComplexCardProps");
            AssertContains(result.GeneratedCode, "className=\"card\"");
            AssertContains(result.GeneratedCode, "padding: '20px'");
            AssertContains(result.GeneratedCode, "{Title}");
            AssertContains(result.GeneratedCode, "(ShowDetails) && (");
            AssertContains(result.GeneratedCode, ".map((item, index)");
        });

        RunTest("098 component info populated", () => {
            var result = CompileRazor("@client\n<div>@Props.Name</div>\n@code {\n    [Parameter] public string Name { get; set; }\n}", "InfoTest");
            AssertNotNull(result.Component);
            AssertEqual("InfoTest", result.Component?.Name);
            AssertEqual(ComponentDirective.Client, result.Component?.Directive);
            AssertSingle(result.Component?.Props);
        });

        RunTest("099 ProductCard real-world", () => {
            var result = CompileRazor(@"@client

<div class=""product-card"" style=""border: 1px solid #e0e0e0; border-radius: 12px; padding: 20px;"">
    <img src=""@Props.ImageUrl"" alt=""@Props.Name"" style=""width: 100%; height: 200px; object-fit: cover;"" />

    <h3 style=""margin: 16px 0; font-size: 18px;"">@Props.Name</h3>
    <p style=""color: #666; font-size: 14px;"">@Props.Description</p>

    <div style=""display: flex; justify-content: space-between; align-items: center;"">
        <span style=""font-size: 24px; font-weight: bold; color: #0066cc;"">$@Props.Price</span>

        @if (Props.InStock)
        {
            <span style=""background: #e8f5e9; color: #2e7d32; padding: 4px 12px; border-radius: 20px;"">In Stock</span>
        }
    </div>

    <button style=""width: 100%; margin-top: 16px; padding: 12px; background: #0066cc; color: white; border: none; border-radius: 8px;"">
        Add to Cart
    </button>
</div>

@code {
    [Parameter] public string Name { get; set; }
    [Parameter] public string Description { get; set; }
    [Parameter] public string ImageUrl { get; set; }
    [Parameter] public decimal Price { get; set; }
    [Parameter] public bool InStock { get; set; } = true;
}", "ProductCard");

            AssertTrue(result.Success);
            AssertEqual(5, result.Component?.Props.Count);
            AssertContains(result.GeneratedCode, "Name: string");
            AssertContains(result.GeneratedCode, "Price: number");
            AssertContains(result.GeneratedCode, "InStock?: boolean");
        });

        RunTest("100 directory compilation", () => {
            File.WriteAllText(Path.Combine(_testDir, "A.razor"), "<div>A</div>");
            File.WriteAllText(Path.Combine(_testDir, "B.razor"), "<div>B</div>");
            File.WriteAllText(Path.Combine(_testDir, "C.razor"), "<div>C</div>");

            var results = _compiler.CompileDirectory(_testDir);

            // At least 3 files (may have more from other tests)
            Assert(results.Count >= 3, $"Expected at least 3 results, got {results.Count}");
            Assert(results.Count(r => r.Success) >= 3, "Expected at least 3 successful");
            AssertTrue(File.Exists(Path.Combine(_testDir, "A.tsx")));
            AssertTrue(File.Exists(Path.Combine(_testDir, "B.tsx")));
            AssertTrue(File.Exists(Path.Combine(_testDir, "C.tsx")));
        });
    }
}

// Entry point
class Program
{
    static int Main(string[] args)
    {
        if (args.Contains("--dsl") || args.Contains("-d"))
        {
            // Run fluent DSL tests
            var dslTests = new FluentDslTests();
            dslTests.RunAllTests();
        }
        else if (args.Contains("--enhanced") || args.Contains("-n"))
        {
            // Run enhanced DSL tests
            var enhancedTests = new EnhancedDslTests();
            enhancedTests.RunAllTests();
        }
        else if (args.Contains("--expr") || args.Contains("-e"))
        {
            // Run expression tests with TS validation
            using var exprTests = new ExpressionTests();
            exprTests.RunAllTests();
        }
        else if (args.Contains("--all") || args.Contains("-a"))
        {
            // Run all tests
            using var runner = new TestRunner();
            runner.RunAllTests();

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            using var exprTests = new ExpressionTests();
            exprTests.RunAllTests();

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            var dslTests = new FluentDslTests();
            dslTests.RunAllTests();

            Console.WriteLine("\n" + new string('=', 50) + "\n");

            var enhancedTests = new EnhancedDslTests();
            enhancedTests.RunAllTests();
        }
        else
        {
            // Default: run basic tests
            using var runner = new TestRunner();
            runner.RunAllTests();
        }

        return 0;
    }
}
