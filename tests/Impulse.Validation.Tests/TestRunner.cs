namespace Impulse.Validation.Tests;

/// <summary>
/// Minimal test framework - no external dependencies.
/// </summary>
public static class TestRunner
{
    private static int _passed;
    private static int _failed;
    private static readonly List<string> _failures = [];

    public static void Run(string name, Action test)
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
            Console.WriteLine($"    {ex.Message}");
        }
    }

    public static void Group(string name, Action tests)
    {
        Console.WriteLine($"\n{name}:");
        tests();
    }

    public static int Report()
    {
        Console.WriteLine($"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine($"Results: {_passed} passed, {_failed} failed");

        if (_failures.Count > 0)
        {
            Console.WriteLine("\nFailures:");
            foreach (var failure in _failures)
            {
                Console.WriteLine($"  • {failure}");
            }
        }

        return _failed > 0 ? 1 : 0;
    }
}

/// <summary>
/// Assertion helpers.
/// </summary>
public static class Assert
{
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new AssertionException($"Expected '{expected}' but got '{actual}'");
        }
    }

    public static void Contains(string expected, string actual)
    {
        if (!actual.Contains(expected, StringComparison.Ordinal))
        {
            throw new AssertionException($"Expected string to contain '{expected}' but it did not.\nActual: {Truncate(actual, 200)}");
        }
    }

    public static void NotNull<T>(T? value)
    {
        if (value is null)
        {
            throw new AssertionException("Expected non-null value but got null");
        }
    }

    public static void True(bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new AssertionException(message ?? "Expected true but got false");
        }
    }

    public static void False(bool condition, string? message = null)
    {
        if (condition)
        {
            throw new AssertionException(message ?? "Expected false but got true");
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}

public sealed class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}
