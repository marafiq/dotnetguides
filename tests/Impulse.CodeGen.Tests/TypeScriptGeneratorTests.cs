using Impulse.CodeGen;

namespace Impulse.CodeGen.Tests;

public class TypeScriptGeneratorTests
{
    [Fact]
    public void GetTypeScriptType_MapsIntToNumber()
    {
        var generator = new TypeScriptGenerator();
        var result = generator.GetTypeScriptType(typeof(int));
        Assert.Equal("number", result);
    }

    [Fact]
    public void GetTypeScriptType_MapsLongToNumber()
    {
        var generator = new TypeScriptGenerator();
        var result = generator.GetTypeScriptType(typeof(long));
        Assert.Equal("number", result);
    }

    [Fact]
    public void GetTypeScriptType_MapsDoubleToNumber()
    {
        var generator = new TypeScriptGenerator();
        var result = generator.GetTypeScriptType(typeof(double));
        Assert.Equal("number", result);
    }

    [Fact]
    public void GetTypeScriptType_MapsStringToString()
    {
        var generator = new TypeScriptGenerator();
        var result = generator.GetTypeScriptType(typeof(string));
        Assert.Equal("string", result);
    }

    [Fact]
    public void GetTypeScriptType_MapsGuidToString()
    {
        var generator = new TypeScriptGenerator();
        var result = generator.GetTypeScriptType(typeof(Guid));
        Assert.Equal("string", result);
    }

    [Fact]
    public void GetTypeScriptType_MapsBoolToBoolean()
    {
        var generator = new TypeScriptGenerator();
        var result = generator.GetTypeScriptType(typeof(bool));
        Assert.Equal("boolean", result);
    }

    [Fact]
    public void GetTypeScriptType_MapsDateTimeToString()
    {
        var generator = new TypeScriptGenerator();
        var result = generator.GetTypeScriptType(typeof(DateTime));
        Assert.Equal("string", result);
    }

    [Fact]
    public void GetTypeScriptType_MapsDateOnlyToString()
    {
        var generator = new TypeScriptGenerator();
        var result = generator.GetTypeScriptType(typeof(DateOnly));
        Assert.Equal("string", result);
    }

    [Fact]
    public void GetTypeScriptType_MapsNullableIntToNumberOrNull()
    {
        var generator = new TypeScriptGenerator();
        var result = generator.GetTypeScriptType(typeof(int?));
        Assert.Equal("number | null", result);
    }

    [Fact]
    public void GetTypeScriptType_MapsArrayToReadonlyArray()
    {
        var generator = new TypeScriptGenerator();
        var result = generator.GetTypeScriptType(typeof(int[]));
        Assert.Equal("readonly number[]", result);
    }

    [Fact]
    public void GetTypeScriptType_MapsListToReadonlyArray()
    {
        var generator = new TypeScriptGenerator();
        var result = generator.GetTypeScriptType(typeof(List<string>));
        Assert.Equal("readonly string[]", result);
    }

    [Fact]
    public void GetTypeScriptType_MapsIReadOnlyListToReadonlyArray()
    {
        var generator = new TypeScriptGenerator();
        var result = generator.GetTypeScriptType(typeof(IReadOnlyList<int>));
        Assert.Equal("readonly number[]", result);
    }

    [Fact]
    public void GetTypeScriptType_MapsDictionaryToRecord()
    {
        var generator = new TypeScriptGenerator();
        var result = generator.GetTypeScriptType(typeof(Dictionary<string, int>));
        Assert.Equal("Record<string, number>", result);
    }

    [Fact]
    public void Generate_CreatesInterfaceForRecord()
    {
        var generator = new TypeScriptGenerator();
        var result = generator.Generate([typeof(SimpleRecord)]);

        Assert.Contains("export interface SimpleRecord", result);
        Assert.Contains("id: number", result);
        Assert.Contains("name: string", result);
    }

    [Fact]
    public void Generate_HandlesEnums()
    {
        var generator = new TypeScriptGenerator();
        var result = generator.Generate([typeof(TestEnum)]);

        Assert.Contains("export type TestEnum", result);
        Assert.Contains("'first'", result);
        Assert.Contains("'second'", result);
    }

    [Fact]
    public void Generate_HandlesNestedTypes()
    {
        var generator = new TypeScriptGenerator();
        var result = generator.Generate([typeof(ParentRecord)]);

        Assert.Contains("export interface ParentRecord", result);
        Assert.Contains("export interface ChildRecord", result);
    }

    private record SimpleRecord(int Id, string Name);
    private record ParentRecord(int Id, ChildRecord Child);
    private record ChildRecord(string Value);
    private enum TestEnum { First, Second }
}
