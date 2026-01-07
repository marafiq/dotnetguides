using Impulse.CodeGen;

namespace Impulse.CodeGen.Tests;

public static class TypeScriptGeneratorTests
{
    public static void RunAll()
    {
        TestRunner.Group("TypeScriptGenerator - Primitive Types", () =>
        {
            TestRunner.Run("Maps int to number",
                () => Assert.Equal("number", Gen().GetTypeScriptType(typeof(int))));

            TestRunner.Run("Maps long to number",
                () => Assert.Equal("number", Gen().GetTypeScriptType(typeof(long))));

            TestRunner.Run("Maps double to number",
                () => Assert.Equal("number", Gen().GetTypeScriptType(typeof(double))));

            TestRunner.Run("Maps decimal to number",
                () => Assert.Equal("number", Gen().GetTypeScriptType(typeof(decimal))));

            TestRunner.Run("Maps string to string",
                () => Assert.Equal("string", Gen().GetTypeScriptType(typeof(string))));

            TestRunner.Run("Maps Guid to string",
                () => Assert.Equal("string", Gen().GetTypeScriptType(typeof(Guid))));

            TestRunner.Run("Maps bool to boolean",
                () => Assert.Equal("boolean", Gen().GetTypeScriptType(typeof(bool))));

            TestRunner.Run("Maps DateTime to string",
                () => Assert.Equal("string", Gen().GetTypeScriptType(typeof(DateTime))));

            TestRunner.Run("Maps DateOnly to string",
                () => Assert.Equal("string", Gen().GetTypeScriptType(typeof(DateOnly))));

            TestRunner.Run("Maps TimeOnly to string",
                () => Assert.Equal("string", Gen().GetTypeScriptType(typeof(TimeOnly))));

            TestRunner.Run("Maps DateTimeOffset to string",
                () => Assert.Equal("string", Gen().GetTypeScriptType(typeof(DateTimeOffset))));
        });

        TestRunner.Group("TypeScriptGenerator - Nullable Types", () =>
        {
            TestRunner.Run("Maps int? to number | null",
                () => Assert.Equal("number | null", Gen().GetTypeScriptType(typeof(int?))));

            TestRunner.Run("Maps bool? to boolean | null",
                () => Assert.Equal("boolean | null", Gen().GetTypeScriptType(typeof(bool?))));

            TestRunner.Run("Maps DateTime? to string | null",
                () => Assert.Equal("string | null", Gen().GetTypeScriptType(typeof(DateTime?))));
        });

        TestRunner.Group("TypeScriptGenerator - Collections", () =>
        {
            TestRunner.Run("Maps int[] to readonly number[]",
                () => Assert.Equal("readonly number[]", Gen().GetTypeScriptType(typeof(int[]))));

            TestRunner.Run("Maps string[] to readonly string[]",
                () => Assert.Equal("readonly string[]", Gen().GetTypeScriptType(typeof(string[]))));

            TestRunner.Run("Maps List<int> to readonly number[]",
                () => Assert.Equal("readonly number[]", Gen().GetTypeScriptType(typeof(List<int>))));

            TestRunner.Run("Maps IReadOnlyList<string> to readonly string[]",
                () => Assert.Equal("readonly string[]", Gen().GetTypeScriptType(typeof(IReadOnlyList<string>))));

            TestRunner.Run("Maps IEnumerable<int> to readonly number[]",
                () => Assert.Equal("readonly number[]", Gen().GetTypeScriptType(typeof(IEnumerable<int>))));

            TestRunner.Run("Maps Dictionary<string, int> to Record",
                () => Assert.Equal("Record<string, number>", Gen().GetTypeScriptType(typeof(Dictionary<string, int>))));

            TestRunner.Run("Maps IDictionary<string, bool> to Record",
                () => Assert.Equal("Record<string, boolean>", Gen().GetTypeScriptType(typeof(IDictionary<string, bool>))));

            TestRunner.Run("Maps IReadOnlyDictionary to Record",
                () => Assert.Equal("Record<string, string>", Gen().GetTypeScriptType(typeof(IReadOnlyDictionary<string, string>))));
        });

        TestRunner.Group("TypeScriptGenerator - Interface Generation", () =>
        {
            TestRunner.Run("Generates interface for record",
                () =>
                {
                    var result = Gen().Generate([typeof(SimpleRecord)]);
                    Assert.Contains("export interface SimpleRecord", result);
                    Assert.Contains("id: number", result);
                    Assert.Contains("name: string", result);
                });

            TestRunner.Run("Uses camelCase for property names",
                () =>
                {
                    var result = Gen().Generate([typeof(CamelCaseRecord)]);
                    Assert.Contains("firstName: string", result);
                    Assert.Contains("lastName: string", result);
                    Assert.Contains("dateOfBirth: string", result);
                });

            TestRunner.Run("Handles optional properties",
                () =>
                {
                    var result = Gen().Generate([typeof(OptionalRecord)]);
                    Assert.Contains("required: string", result);
                    Assert.Contains("optional?:", result);
                });

            TestRunner.Run("Generates dependent types transitively",
                () =>
                {
                    var result = Gen().Generate([typeof(ParentRecord)]);
                    Assert.Contains("export interface ParentRecord", result);
                    Assert.Contains("export interface ChildRecord", result);
                });
        });

        TestRunner.Group("TypeScriptGenerator - Enum Generation", () =>
        {
            TestRunner.Run("Generates union type for enum",
                () =>
                {
                    var result = Gen().Generate([typeof(Status)]);
                    Assert.Contains("export type Status", result);
                    Assert.Contains("'pending'", result);
                    Assert.Contains("'approved'", result);
                    Assert.Contains("'rejected'", result);
                });
        });

        TestRunner.Group("TypeScriptGenerator - Complex Props", () =>
        {
            TestRunner.Run("Generates complete props interface",
                () =>
                {
                    var result = Gen().Generate([typeof(ResidentDetailProps)]);
                    Assert.Contains("export interface ResidentDetailProps", result);
                    Assert.Contains("id: number", result);
                    Assert.Contains("name: string", result);
                    Assert.Contains("status: Status", result);
                    Assert.Contains("readonly Medication[]", result);
                });
        });
    }

    private static TypeScriptGenerator Gen() => new();
}

// Test fixture types
public record SimpleRecord(int Id, string Name);
public record CamelCaseRecord(string FirstName, string LastName, DateTime DateOfBirth);
public record OptionalRecord(string Required, string? Optional);
public record ParentRecord(int Id, ChildRecord Child);
public record ChildRecord(string Value);
public enum Status { Pending, Approved, Rejected }
public record Medication(int Id, string Name, string Dosage);
public record ResidentDetailProps(int Id, string Name, Status Status, IReadOnlyList<Medication> Medications);
