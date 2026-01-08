using System.Diagnostics;
using System.Reflection;

namespace Impulse.Core.Tests;

/// <summary>
/// Minimal test runner without external dependencies.
/// Replace with xunit once NuGet access is available.
/// </summary>
public static class SimpleTests
{
    public static int Main()
    {
        Console.WriteLine("Running Impulse.Core.Tests...\n");
        var passed = 0;
        var failed = 0;

        // Find all methods with Test_ prefix
        var testMethods = typeof(SimpleTests)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.Name.StartsWith("Test_"))
            .ToList();

        foreach (var method in testMethods)
        {
            try
            {
                method.Invoke(null, null);
                passed++;
                Console.WriteLine($"  ✓ {method.Name}");
            }
            catch (Exception ex)
            {
                failed++;
                var msg = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"  ✗ {method.Name}: {msg}");
            }
        }

        Console.WriteLine($"\nResults: {passed} passed, {failed} failed");
        return failed > 0 ? 1 : 0;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    // ========================================
    // TESTS - TDD: Write test first, then implement
    // ========================================

    private static void Test_ImpulseEndpointAttribute_Exists()
    {
        // The ImpulseEndpoint attribute should exist in Impulse.Core
        var type = Type.GetType("Impulse.Core.ImpulseEndpointAttribute, Impulse.Core");
        Assert(type != null, "ImpulseEndpointAttribute should exist in Impulse.Core");
    }

    private static void Test_ImpulseEndpointAttribute_HasRouteProperty()
    {
        var type = Type.GetType("Impulse.Core.ImpulseEndpointAttribute, Impulse.Core");
        Assert(type != null, "ImpulseEndpointAttribute should exist");

        var routeProperty = type!.GetProperty("Route");
        Assert(routeProperty != null, "ImpulseEndpointAttribute should have Route property");
        Assert(routeProperty!.PropertyType == typeof(string), "Route should be string type");
    }

    private static void Test_ImpulseEndpointAttribute_CanBeAppliedToClass()
    {
        var type = Type.GetType("Impulse.Core.ImpulseEndpointAttribute, Impulse.Core");
        Assert(type != null, "ImpulseEndpointAttribute should exist");

        var attrUsage = type!.GetCustomAttribute<AttributeUsageAttribute>();
        Assert(attrUsage != null, "ImpulseEndpointAttribute should have AttributeUsage");
        Assert((attrUsage!.ValidOn & AttributeTargets.Class) != 0,
            "ImpulseEndpointAttribute should be applicable to classes");
    }
}
