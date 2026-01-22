using Shalimar.Razor;

namespace Shalimar.Razor.Tests;

/// <summary>
/// Tests for Enhanced DSL - cleaner separation of state/props/actions/computed
/// </summary>
public class EnhancedDslTests
{
    public void RunAllTests()
    {
        Console.WriteLine("\n=== Enhanced DSL Tests ===\n");

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
        Test("N01 Simple component with state", () =>
        {
            var tsx = Component.Define("Counter")
                .Client()
                .State(s => s.Field("count", 0))
                .Generate();

            Assert(tsx.Contains("export function Counter()"), "Should export Counter function");
            Assert(tsx.Contains("CounterState"), "Should have state interface");
            Assert(tsx.Contains("counterStore"), "Should create store");
            Assert(tsx.Contains("@tanstack/store"), "Should import TanStack");
        });

        // Multiple state fields
        Test("N02 Multiple state fields grouped", () =>
        {
            var tsx = Component.Define("Form")
                .Client()
                .State(s => {
                    s.Field("email", "");
                    s.Field("password", "");
                    s.Field("isSubmitting", false);
                })
                .Generate();

            Assert(tsx.Contains("email: string"), "Should have email");
            Assert(tsx.Contains("password: string"), "Should have password");
            Assert(tsx.Contains("isSubmitting: boolean"), "Should have isSubmitting");
            Assert(tsx.Contains("setEmail"), "Should generate setEmail");
            Assert(tsx.Contains("setPassword"), "Should generate setPassword");
        });

        // Props builder
        Test("N03 Props with required and optional", () =>
        {
            var tsx = Component.Define("Greeting")
                .Props(p => {
                    p.Required<string>("name");
                    p.Optional<int>("age", 0);
                    p.Optional<bool>("showAge", true);
                })
                .Generate();

            Assert(tsx.Contains("GreetingProps"), "Should have props interface");
            Assert(tsx.Contains("name: string"), "Should have required name");
            Assert(tsx.Contains("age?: number"), "Should have optional age");
            Assert(tsx.Contains("showAge?: boolean"), "Should have optional showAge");
            Assert(tsx.Contains("age = 0"), "Should default age to 0");
        });

        // Actions builder
        Test("N04 Actions with mutations", () =>
        {
            var tsx = Component.Define("Counter")
                .Client()
                .State(s => s.Field("count", 0))
                .Actions(a => {
                    a.Define("increment", "count++");
                    a.Define("decrement", "count--");
                    a.Define("reset", "count = 0;");
                })
                .Generate();

            Assert(tsx.Contains("const increment"), "Should have increment action");
            Assert(tsx.Contains("const decrement"), "Should have decrement action");
            Assert(tsx.Contains("const reset"), "Should have reset action");
            Assert(tsx.Contains("s.count + 1"), "Should transform count++");
            Assert(tsx.Contains("s.count - 1"), "Should transform count--");
        });

        // Actions with parameters
        Test("N05 Actions with parameters", () =>
        {
            var tsx = Component.Define("Counter")
                .Client()
                .State(s => s.Field("count", 0))
                .Actions(a => {
                    a.Define("add", "n: number", "count += n;");
                    a.Define("subtract", "n: number", "count -= n;");
                })
                .Generate();

            Assert(tsx.Contains("const add = (n: number)"), "Should have add with param");
            Assert(tsx.Contains("s.count + n"), "Should transform += n");
        });

        // Computed values
        Test("N06 Computed values", () =>
        {
            var tsx = Component.Define("Display")
                .Client()
                .State(s => {
                    s.Field("count", 0);
                    s.Field("label", "Count");
                })
                .Computed(c => {
                    c.Define("displayText", "$\"{label}: {count}\"");
                })
                .Generate();

            Assert(tsx.Contains("const displayText"), "Should have computed displayText");
            Assert(tsx.Contains("`${label}: ${count}`"), "Should convert to template literal");
        });

        // Imports
        Test("N07 Imports", () =>
        {
            var tsx = Component.Define("Dashboard")
                .Import("./Button")
                .Import("./utils", "formatDate", "formatCurrency")
                .Render("<div>Dashboard</div>")
                .Generate();

            Assert(tsx.Contains("from './Button'"), "Should import Button");
            Assert(tsx.Contains("formatDate, formatCurrency"), "Should import named");
        });

        // Children prop
        Test("N08 Children prop", () =>
        {
            var tsx = Component.Define("Card")
                .Props(p => p.Children())
                .Render(@"
                    <div className=""card"">
                        {children}
                    </div>")
                .Generate();

            Assert(tsx.Contains("children?: React.ReactNode"), "Should type children");
            Assert(tsx.Contains("{children}"), "Should render children");
        });

        // Full counter example
        Test("N09 Full Counter example", () =>
        {
            var tsx = Component.Define("Counter")
                .Client()
                .State(s => s.Field("count", 0))
                .Props(p => {
                    p.Required<string>("title");
                    p.Optional<int>("step", 1);
                })
                .Actions(a => {
                    a.Define("increment", "count++");
                    a.Define("decrement", "count--");
                })
                .Render(@"
                    <div>
                        <h1>{title}</h1>
                        <span>Count: {count}</span>
                        <button onClick={increment}>+{step}</button>
                        <button onClick={decrement}>-{step}</button>
                    </div>")
                .Generate();

            Console.WriteLine("\n--- Generated Full Counter ---");
            Console.WriteLine(tsx);

            Assert(tsx.Contains("CounterState"), "Should have state interface");
            Assert(tsx.Contains("CounterProps"), "Should have props interface");
            Assert(tsx.Contains("export function Counter"), "Should export function");
            Assert(tsx.Contains("step = 1"), "Should default step");
        });

        // Full form example
        Test("N10 Full Login Form example", () =>
        {
            var tsx = Component.Define("LoginForm")
                .Client()
                .State(s => {
                    s.Field("email", "");
                    s.Field("password", "");
                    s.Field("isLoading", false);
                    s.Field("error", "");
                })
                .Actions(a => {
                    a.Define("handleSubmit", "e: React.FormEvent", @"
                        e.preventDefault();
                        isLoading = true;
                        // API call would go here
                        isLoading = false;
                    ", isAsync: true);
                })
                .Render(@"
                    <form onSubmit={handleSubmit}>
                        <input value={email} onChange={(e) => setEmail(e.target.value)} type=""email"" />
                        <input value={password} onChange={(e) => setPassword(e.target.value)} type=""password"" />
                        {error && <div className=""error"">{error}</div>}
                        <button disabled={isLoading}>
                            {isLoading ? 'Loading...' : 'Login'}
                        </button>
                    </form>")
                .Generate();

            Console.WriteLine("\n--- Generated Login Form ---");
            Console.WriteLine(tsx);

            Assert(tsx.Contains("LoginFormState"), "Should have state");
            Assert(tsx.Contains("async (e: React.FormEvent)"), "Should be async with param");
        });

        // Async action
        Test("N11 Async action", () =>
        {
            var tsx = Component.Define("DataLoader")
                .Client()
                .State(s => {
                    s.Field("data", "");
                    s.Field("loading", false);
                })
                .Actions(a => {
                    a.Define("fetchData", @"
                        loading = true;
                        // await fetch...
                        loading = false;
                    ", isAsync: true);
                })
                .Generate();

            Assert(tsx.Contains("const fetchData = async ()"), "Should be async");
        });

        // Validate TypeScript
        Test("N12 TypeScript validation", () =>
        {
            var tsx = Component.Define("ValidComponent")
                .Client()
                .State(s => s.Field("value", 0))
                .Props(p => p.Required<string>("label"))
                .Actions(a => a.Define("increment", "value++"))
                .Render(@"
                    <div>
                        <span>{label}: {value}</span>
                        <button onClick={increment}>+1</button>
                    </div>")
                .Generate();

            var (isValid, error) = TsxValidator.ValidateTsx(tsx, "ValidComponent");
            if (!isValid)
                throw new Exception($"TypeScript validation failed: {error}");
        });

        Console.WriteLine($"\n=== Enhanced DSL Results: {passed} passed, {failed} failed ===\n");

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
