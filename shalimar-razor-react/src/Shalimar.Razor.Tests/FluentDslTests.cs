using Shalimar.Razor;

namespace Shalimar.Razor.Tests;

/// <summary>
/// Tests for Fluent C# DSL - no Razor, just pure C# component definitions
/// </summary>
public class FluentDslTests
{
    public void RunAllTests()
    {
        Console.WriteLine("\n=== Fluent DSL Tests ===\n");

        int passed = 0, failed = 0;
        var failures = new List<string>();

        void Test(string name, Action test)
        {
            try
            {
                test();
                passed++;
                Console.WriteLine($"  ✓ {name}");
            }
            catch (Exception ex)
            {
                failed++;
                failures.Add($"{name}: {ex.Message}");
                Console.WriteLine($"  ✗ {name}");
                Console.WriteLine($"    {ex.Message}");
            }
        }

        // Basic component
        Test("D01 Create simple component", () =>
        {
            var tsx = ComponentBuilder.Create("Hello")
                .ToTsx("<div>Hello World</div>");

            Assert(tsx.Contains("export function Hello()"), "Should export Hello function");
            Assert(tsx.Contains("import React"), "Should import React");
        });

        // With props
        Test("D02 Component with props", () =>
        {
            var tsx = ComponentBuilder.Create("Greeting")
                .Prop<string>("name")
                .Prop<int>("age", 0)
                .ToTsx("<div>Hello {name}, age {age}</div>");

            Assert(tsx.Contains("GreetingProps"), "Should have props interface");
            Assert(tsx.Contains("name: string"), "Should have name prop");
            Assert(tsx.Contains("age?: number"), "Should have optional age prop");
        });

        // With TanStack Store
        Test("D03 Component with Store state", () =>
        {
            var tsx = ComponentBuilder.Create("Counter")
                .Client()
                .Store("count", 0)
                .ToTsx("<div>Count: {count}</div>");

            Assert(tsx.Contains("@tanstack/store"), "Should import TanStack Store");
            Assert(tsx.Contains("CounterState"), "Should have state interface");
            Assert(tsx.Contains("count: number"), "Should have count in state");
            Assert(tsx.Contains("counterStore"), "Should create store instance");
            Assert(tsx.Contains("useStore"), "Should use useStore hook");
        });

        // Multiple store vars
        Test("D04 Multiple store variables", () =>
        {
            var tsx = ComponentBuilder.Create("Form")
                .Client()
                .Store("email", "")
                .Store("password", "")
                .Store("isSubmitting", false)
                .ToTsx();

            Assert(tsx.Contains("email: string"), "Should have email");
            Assert(tsx.Contains("password: string"), "Should have password");
            Assert(tsx.Contains("isSubmitting: boolean"), "Should have isSubmitting");
            Assert(tsx.Contains("setEmail"), "Should generate setEmail");
            Assert(tsx.Contains("setPassword"), "Should generate setPassword");
        });

        // With actions
        Test("D05 Component with actions", () =>
        {
            var tsx = ComponentBuilder.Create("Counter")
                .Client()
                .Store("count", 0)
                .Action("increment", "count++")
                .Action("decrement", "count--")
                .Action("reset", "count = 0;")
                .ToTsx();

            Assert(tsx.Contains("const increment"), "Should have increment");
            Assert(tsx.Contains("const decrement"), "Should have decrement");
            Assert(tsx.Contains("const reset"), "Should have reset");
            // Check mutations are transformed
            Assert(tsx.Contains("s.count + 1"), "Should transform count++");
            Assert(tsx.Contains("s.count - 1"), "Should transform count--");
        });

        // With imports
        Test("D06 Component with imports", () =>
        {
            var tsx = ComponentBuilder.Create("Dashboard")
                .Import("./Button")
                .Import("./utils", "formatDate", "formatCurrency")
                .ToTsx();

            Assert(tsx.Contains("from './Button'"), "Should import Button");
            Assert(tsx.Contains("formatDate, formatCurrency"), "Should import named");
        });

        // Full counter example
        Test("D07 Full Counter example", () =>
        {
            var tsx = ComponentBuilder.Create("Counter")
                .Client()
                .Store("count", 0)
                .Prop<string>("title")
                .Prop<int>("step", 1)
                .Action("increment", "count++")
                .Action("decrement", "count--")
                .ToTsx(@"
    <div>
      <h1>{title}</h1>
      <span>Count: {count}</span>
      <button onClick={increment}>+{step}</button>
      <button onClick={decrement}>-{step}</button>
    </div>");

            Console.WriteLine("\n--- Generated Counter TSX ---");
            Console.WriteLine(tsx);

            Assert(tsx.Contains("CounterProps"), "Should have props interface");
            Assert(tsx.Contains("CounterState"), "Should have state interface");
            Assert(tsx.Contains("export function Counter"), "Should export function");
        });

        // Full form example
        Test("D08 Full Login Form example", () =>
        {
            var tsx = ComponentBuilder.Create("LoginForm")
                .Client()
                .Store("email", "")
                .Store("password", "")
                .Store("isLoading", false)
                .Action("handleSubmit", @"
                    isLoading = true;
                    // API call would go here
                    isLoading = false;
                ", isAsync: true)
                .ToTsx(@"
    <form onSubmit={handleSubmit}>
      <input value={email} onChange={(e) => setEmail(e.target.value)} />
      <input type=""password"" value={password} onChange={(e) => setPassword(e.target.value)} />
      <button disabled={isLoading}>
        {isLoading ? 'Loading...' : 'Login'}
      </button>
    </form>");

            Console.WriteLine("\n--- Generated LoginForm TSX ---");
            Console.WriteLine(tsx);

            Assert(tsx.Contains("LoginFormState"), "Should have state interface");
            Assert(tsx.Contains("const handleSubmit = async"), "Should be async");
        });

        // ComponentInfo can be reused
        Test("D09 Build ComponentInfo for reuse", () =>
        {
            var component = ComponentBuilder.Create("Reusable")
                .Store("value", 0)
                .Prop<string>("label")
                .Build();

            Assert(component.Name == "Reusable", "Should have name");
            Assert(component.StoreVars.Count == 1, "Should have 1 store var");
            Assert(component.Props.Count == 1, "Should have 1 prop");

            // Can emit multiple times or transform
            var emitter = new ComponentEmitter();
            var tsx1 = emitter.Emit(component, "<div>Template 1</div>");
            var tsx2 = emitter.Emit(component, "<span>Template 2</span>");

            Assert(tsx1.Contains("Template 1"), "Should have template 1");
            Assert(tsx2.Contains("Template 2"), "Should have template 2");
        });

        // Type safety
        Test("D10 Type safety with generics", () =>
        {
            var tsx = ComponentBuilder.Create("TypedComponent")
                .Prop<string>("text")
                .Prop<int>("count")
                .Prop<bool>("enabled")
                .Prop<List<string>>("items")
                .Store("selected", "")
                .Store("index", 0)
                .Store("active", true)
                .ToTsx();

            Assert(tsx.Contains("text: string"), "String prop");
            Assert(tsx.Contains("count: number"), "Int prop");
            Assert(tsx.Contains("enabled: boolean"), "Bool prop");
            Assert(tsx.Contains("items: string[]"), "List prop");
            Assert(tsx.Contains("selected: string"), "String store");
            Assert(tsx.Contains("index: number"), "Int store");
            Assert(tsx.Contains("active: boolean"), "Bool store");
        });

        Console.WriteLine($"\n=== DSL Results: {passed} passed, {failed} failed ===\n");

        if (failures.Any())
        {
            Console.WriteLine("Failed:");
            foreach (var f in failures)
                Console.WriteLine($"  - {f}");
        }
    }

    private void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception($"Assertion failed: {message}");
    }
}
