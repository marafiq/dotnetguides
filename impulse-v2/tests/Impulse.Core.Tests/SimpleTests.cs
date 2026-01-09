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

    // ========================================
    // ImpulseEndpoint<TRequest, TResponse> Tests
    // ========================================

    private static void Test_ImpulseEndpoint_IsAbstractClass()
    {
        var type = Type.GetType("Impulse.Core.ImpulseEndpoint`2, Impulse.Core");
        Assert(type != null, "ImpulseEndpoint<,> should exist");
        Assert(type!.IsAbstract, "ImpulseEndpoint should be abstract");
        Assert(type.IsClass, "ImpulseEndpoint should be a class");
    }

    private static void Test_ImpulseEndpoint_HasHandleMethod()
    {
        var type = Type.GetType("Impulse.Core.ImpulseEndpoint`2, Impulse.Core");
        Assert(type != null, "ImpulseEndpoint<,> should exist");

        var handleMethod = type!.GetMethod("Handle");
        Assert(handleMethod != null, "ImpulseEndpoint should have Handle method");
        Assert(handleMethod!.IsAbstract, "Handle should be abstract");
    }

    private static void Test_ImpulseEndpoint_HandleReturnsTask()
    {
        var type = Type.GetType("Impulse.Core.ImpulseEndpoint`2, Impulse.Core");
        Assert(type != null, "ImpulseEndpoint<,> should exist");

        var handleMethod = type!.GetMethod("Handle");
        Assert(handleMethod != null, "Handle method should exist");

        var returnType = handleMethod!.ReturnType;
        Assert(returnType.IsGenericType, "Return should be generic Task");
        Assert(returnType.GetGenericTypeDefinition() == typeof(Task<>),
            "Handle should return Task<IImpulseResult>");
    }

    // ========================================
    // IImpulseResult Tests
    // ========================================

    private static void Test_IImpulseResult_Exists()
    {
        var type = Type.GetType("Impulse.Core.IImpulseResult, Impulse.Core");
        Assert(type != null, "IImpulseResult should exist");
        Assert(type!.IsInterface, "IImpulseResult should be an interface");
    }

    // ========================================
    // ImpulseResults Static Factory Tests
    // ========================================

    private static void Test_ImpulseResults_HasOkMethod()
    {
        var type = Type.GetType("Impulse.Core.ImpulseResults, Impulse.Core");
        Assert(type != null, "ImpulseResults should exist");

        var okMethod = type!.GetMethod("Ok");
        Assert(okMethod != null, "ImpulseResults should have Ok method");
        Assert(okMethod!.IsStatic, "Ok should be static");
    }

    private static void Test_ImpulseResults_HasNotFoundMethod()
    {
        var type = Type.GetType("Impulse.Core.ImpulseResults, Impulse.Core");
        Assert(type != null, "ImpulseResults should exist");

        var method = type!.GetMethod("NotFound");
        Assert(method != null, "ImpulseResults should have NotFound method");
        Assert(method!.IsStatic, "NotFound should be static");
    }

    private static void Test_ImpulseResults_HasValidationProblemMethod()
    {
        var type = Type.GetType("Impulse.Core.ImpulseResults, Impulse.Core");
        Assert(type != null, "ImpulseResults should exist");

        var method = type!.GetMethod("ValidationProblem");
        Assert(method != null, "ImpulseResults should have ValidationProblem method");
        Assert(method!.IsStatic, "ValidationProblem should be static");
    }

    // ========================================
    // ImpulseContext Tests
    // ========================================

    private static void Test_ImpulseContext_Exists()
    {
        var type = Type.GetType("Impulse.Core.ImpulseContext, Impulse.Core");
        Assert(type != null, "ImpulseContext should exist");
        Assert(type!.IsClass, "ImpulseContext should be a class");
    }

    private static void Test_ImpulseContext_HasIsImpulseRequestProperty()
    {
        var type = Type.GetType("Impulse.Core.ImpulseContext, Impulse.Core");
        Assert(type != null, "ImpulseContext should exist");

        var prop = type!.GetProperty("IsImpulseRequest");
        Assert(prop != null, "ImpulseContext should have IsImpulseRequest property");
        Assert(prop!.PropertyType == typeof(bool), "IsImpulseRequest should be bool");
    }

    // ========================================
    // Code Generator Tests - Zero Magic Strings
    // ========================================

    private static void Test_GeneratedMutations_ImportRoutePaths()
    {
        var mutationsPath = FindGeneratedFile("mutations.ts");
        Assert(mutationsPath != null, "mutations.ts should exist in generated folder");

        var content = File.ReadAllText(mutationsPath!);
        Assert(content.Contains("import { RoutePaths }") || content.Contains("import {RoutePaths}"),
            "mutations.ts must import RoutePaths - zero magic strings rule");
    }

    private static void Test_GeneratedMutations_UseRoutePaths_NotMagicStrings()
    {
        var mutationsPath = FindGeneratedFile("mutations.ts");
        Assert(mutationsPath != null, "mutations.ts should exist");

        var content = File.ReadAllText(mutationsPath!);

        // Should use RoutePaths.X, not hardcoded strings like '/residents'
        Assert(content.Contains("RoutePaths."),
            "mutations.ts must use RoutePaths.X constants");

        // Should NOT contain hardcoded route strings in impulseMutate calls
        // Look for pattern: impulseMutate<...>('/... which indicates magic string
        var hasMagicString = System.Text.RegularExpressions.Regex.IsMatch(
            content, @"impulseMutate<[^>]+>\(\s*'\/");
        Assert(!hasMagicString,
            "mutations.ts must NOT have hardcoded route strings - use RoutePaths instead");
    }

    private static void Test_GeneratedLoaders_ImportRoutePaths()
    {
        var loadersPath = FindGeneratedFile("loaders.ts");
        Assert(loadersPath != null, "loaders.ts should exist in generated folder");

        var content = File.ReadAllText(loadersPath!);
        Assert(content.Contains("import { RoutePaths }") || content.Contains("import {RoutePaths}"),
            "loaders.ts must import RoutePaths - zero magic strings rule");
    }

    private static void Test_GeneratedLoaders_UseRoutePaths_NotMagicStrings()
    {
        var loadersPath = FindGeneratedFile("loaders.ts");
        Assert(loadersPath != null, "loaders.ts should exist");

        var content = File.ReadAllText(loadersPath!);

        // Should use RoutePaths.X
        Assert(content.Contains("RoutePaths."),
            "loaders.ts must use RoutePaths.X constants");

        // Should NOT contain hardcoded route strings like let url = '/residents'
        var hasMagicString = System.Text.RegularExpressions.Regex.IsMatch(
            content, @"let url = '\/");
        Assert(!hasMagicString,
            "loaders.ts must NOT have hardcoded route strings - use RoutePaths instead");
    }

    private static string? FindGeneratedFile(string fileName)
    {
        // Look for generated file relative to test assembly
        var assemblyDir = Path.GetDirectoryName(typeof(SimpleTests).Assembly.Location);
        var searchPaths = new[]
        {
            Path.Combine(assemblyDir!, "..", "..", "..", "..", "..", "samples", "SampleApp", "generated", fileName),
            Path.Combine(assemblyDir!, "..", "..", "..", "..", "samples", "SampleApp", "generated", fileName),
        };

        foreach (var path in searchPaths)
        {
            var normalized = Path.GetFullPath(path);
            if (File.Exists(normalized))
                return normalized;
        }
        return null;
    }
}
