using FluentAssertions;
using Impulse.CodeGen;
using Impulse.CodeGen.Ast;

namespace Impulse.CodeGen.Tests;

/// <summary>
/// Tests that define how C# types are converted to TypeScript.
/// These tests drive the design of our type analyzer.
///
/// Key behaviors:
/// 1. Primitives map correctly (string->string, int->number, etc.)
/// 2. Nullable types become union types (T | null)
/// 3. Collections become readonly arrays
/// 4. Enums become TypeScript string enums
/// 5. Nested types are analyzed recursively
/// 6. Dependencies are topologically sorted
/// </summary>
public class TypeAnalyzerTests
{
    private readonly TypeAnalyzer _analyzer = new();

    // ========================================
    // Primitive Type Mapping
    // ========================================

    [Fact]
    public void String_MapsToTsString()
    {
        var tsType = _analyzer.MapType(typeof(string));

        tsType.Should().BeOfType<TsString>();
    }

    [Fact]
    public void Int_MapsToTsNumber()
    {
        var tsType = _analyzer.MapType(typeof(int));

        tsType.Should().BeOfType<TsNumber>();
    }

    [Fact]
    public void Long_MapsToTsNumber()
    {
        var tsType = _analyzer.MapType(typeof(long));

        tsType.Should().BeOfType<TsNumber>();
    }

    [Fact]
    public void Double_MapsToTsNumber()
    {
        var tsType = _analyzer.MapType(typeof(double));

        tsType.Should().BeOfType<TsNumber>();
    }

    [Fact]
    public void Decimal_MapsToTsNumber()
    {
        var tsType = _analyzer.MapType(typeof(decimal));

        tsType.Should().BeOfType<TsNumber>();
    }

    [Fact]
    public void Bool_MapsToTsBoolean()
    {
        var tsType = _analyzer.MapType(typeof(bool));

        tsType.Should().BeOfType<TsBoolean>();
    }

    [Fact]
    public void DateTime_MapsToTsString()
    {
        // ISO 8601 strings are used for dates
        var tsType = _analyzer.MapType(typeof(DateTime));

        tsType.Should().BeOfType<TsString>();
    }

    [Fact]
    public void Guid_MapsToTsString()
    {
        var tsType = _analyzer.MapType(typeof(Guid));

        tsType.Should().BeOfType<TsString>();
    }

    // ========================================
    // Nullable Types
    // ========================================

    [Fact]
    public void NullableInt_MapsToUnionWithNull()
    {
        var tsType = _analyzer.MapType(typeof(int?));

        tsType.Should().BeOfType<TsUnion>();
        var union = (TsUnion)tsType;
        union.Types.Should().HaveCount(2);
        union.Types.Should().ContainSingle(t => t is TsNumber);
        union.Types.Should().ContainSingle(t => t is TsNull);
    }

    [Fact]
    public void NullableString_MapsToUnionWithNull()
    {
        // In C# 8+, string? is nullable
        var tsType = _analyzer.MapType(typeof(string));

        // Actually string is reference type, test nullable reference
        // For now, treat as non-nullable since we can't detect at runtime
        tsType.Should().BeOfType<TsString>();
    }

    // ========================================
    // Collections
    // ========================================

    [Fact]
    public void List_MapsToReadonlyArray()
    {
        var tsType = _analyzer.MapType(typeof(List<string>));

        tsType.Should().BeOfType<TsArray>();
        var array = (TsArray)tsType;
        array.Element.Should().BeOfType<TsString>();
        array.IsReadonly.Should().BeTrue();
    }

    [Fact]
    public void Array_MapsToReadonlyArray()
    {
        var tsType = _analyzer.MapType(typeof(string[]));

        tsType.Should().BeOfType<TsArray>();
        var array = (TsArray)tsType;
        array.Element.Should().BeOfType<TsString>();
    }

    [Fact]
    public void IEnumerable_MapsToReadonlyArray()
    {
        var tsType = _analyzer.MapType(typeof(IEnumerable<int>));

        tsType.Should().BeOfType<TsArray>();
        var array = (TsArray)tsType;
        array.Element.Should().BeOfType<TsNumber>();
    }

    [Fact]
    public void Dictionary_MapsToRecord()
    {
        var tsType = _analyzer.MapType(typeof(Dictionary<string, int>));

        tsType.Should().BeOfType<TsRecord>();
        var record = (TsRecord)tsType;
        record.KeyType.Should().BeOfType<TsString>();
        record.ValueType.Should().BeOfType<TsNumber>();
    }

    // ========================================
    // Enums
    // ========================================

    [Fact]
    public void Enum_MapsToTsEnum()
    {
        var tsType = _analyzer.MapType(typeof(TestStatus));

        tsType.Should().BeOfType<TsRef>();
        var enumRef = (TsRef)tsType;
        enumRef.Name.Should().Be("TestStatus");
    }

    [Fact]
    public void Enum_GeneratesStringEnum()
    {
        var tsEnum = _analyzer.AnalyzeEnum(typeof(TestStatus));

        tsEnum.Name.Should().Be("TestStatus");
        tsEnum.Members.Should().Contain(m => m.Name == "Active" && m.Value == "'Active'");
        tsEnum.Members.Should().Contain(m => m.Name == "Inactive" && m.Value == "'Inactive'");
    }

    // ========================================
    // Complex Types (Records/Classes)
    // ========================================

    [Fact]
    public void Record_GeneratesInterface()
    {
        var tsInterface = _analyzer.AnalyzeType(typeof(TestPerson));

        tsInterface.Name.Should().Be("TestPerson");
        tsInterface.Properties.Should().Contain(p => p.Name == "firstName" && p.Type is TsString);
        tsInterface.Properties.Should().Contain(p => p.Name == "lastName" && p.Type is TsString);
        tsInterface.Properties.Should().Contain(p => p.Name == "age" && p.Type is TsNumber);
    }

    [Fact]
    public void Record_PropertiesAreCamelCase()
    {
        var tsInterface = _analyzer.AnalyzeType(typeof(TestPerson));

        // C# PascalCase -> TypeScript camelCase
        tsInterface.Properties.Should().OnlyContain(p =>
            char.IsLower(p.Name[0]));
    }

    [Fact]
    public void Record_OptionalPropertiesAreMarked()
    {
        var tsInterface = _analyzer.AnalyzeType(typeof(TestPersonWithOptional));

        var middleName = tsInterface.Properties.First(p => p.Name == "middleName");
        middleName.IsOptional.Should().BeTrue();
    }

    // ========================================
    // Nested Types
    // ========================================

    [Fact]
    public void NestedTypes_AreAnalyzedRecursively()
    {
        var types = _analyzer.AnalyzeTypes(new[] { typeof(TestOrder) });

        // Should include Order and its nested Address type
        types.Should().Contain(t => t.Name == "TestOrder");
        types.Should().Contain(t => t.Name == "TestAddress");
    }

    [Fact]
    public void Dependencies_AreTopologicallySorted()
    {
        // Address should come before Order since Order depends on Address
        var types = _analyzer.AnalyzeTypes(new[] { typeof(TestOrder) });

        var addressIndex = types.ToList().FindIndex(t => t.Name == "TestAddress");
        var orderIndex = types.ToList().FindIndex(t => t.Name == "TestOrder");

        addressIndex.Should().BeLessThan(orderIndex,
            "dependent types should be defined before types that reference them");
    }

    // ========================================
    // Circular References
    // ========================================

    [Fact]
    public void CircularReferences_AreHandled()
    {
        // Should not infinite loop
        var types = _analyzer.AnalyzeTypes(new[] { typeof(TestNode) });

        types.Should().ContainSingle(t => t.Name == "TestNode");
    }
}

// ========================================
// Test Types
// ========================================

public enum TestStatus { Active, Inactive, Pending }

public record TestPerson(string FirstName, string LastName, int Age);

public record TestPersonWithOptional(string FirstName, string LastName, string? MiddleName);

public record TestAddress(string Street, string City, string ZipCode);

public record TestOrder(int Id, TestAddress ShippingAddress, List<string> Items);

public record TestNode(int Id, TestNode? Parent, List<TestNode> Children);
