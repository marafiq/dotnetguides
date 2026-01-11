using FluentAssertions;
using Impulse.CodeGen;
using Impulse.CodeGen.Ast;

namespace Impulse.CodeGen.Tests;

/// <summary>
/// Tests that define how Zod schemas are generated.
/// These tests drive the design of our Zod emitter.
///
/// Key behaviors:
/// 1. Object schemas use z.object({})
/// 2. String/number/boolean map to z.string/number/boolean
/// 3. Arrays use z.array()
/// 4. Optional properties use .optional()
/// 5. Nullable properties use .nullable()
/// 6. Validation rules are included (min, max, email, etc.)
/// </summary>
public class ZodEmitterTests
{
    private readonly ZodEmitter _emitter = new();

    // ========================================
    // Basic Types
    // ========================================

    [Fact]
    public void ZodString_EmitsCorrectly()
    {
        var schema = new ZodString();

        var code = _emitter.EmitType(schema);

        code.Should().Be("z.string()");
    }

    [Fact]
    public void ZodNumber_EmitsCorrectly()
    {
        var schema = new ZodNumber();

        var code = _emitter.EmitType(schema);

        code.Should().Be("z.number()");
    }

    [Fact]
    public void ZodBoolean_EmitsCorrectly()
    {
        var schema = new ZodBoolean();

        var code = _emitter.EmitType(schema);

        code.Should().Be("z.boolean()");
    }

    // ========================================
    // Object Schemas
    // ========================================

    [Fact]
    public void ZodObject_EmitsProperties()
    {
        var schema = new ZodObject(new[]
        {
            new ZodProperty("name", new ZodString()),
            new ZodProperty("age", new ZodNumber())
        });

        var code = _emitter.EmitType(schema);

        code.Should().Contain("z.object({");
        code.Should().Contain("name: z.string()");
        code.Should().Contain("age: z.number()");
    }

    [Fact]
    public void ZodObject_OptionalProperties()
    {
        var schema = new ZodObject(new[]
        {
            new ZodProperty("name", new ZodString()),
            new ZodProperty("nickname", new ZodString(), IsOptional: true)
        });

        var code = _emitter.EmitType(schema);

        code.Should().Contain("nickname: z.string().optional()");
    }

    // ========================================
    // Arrays and Records
    // ========================================

    [Fact]
    public void ZodArray_EmitsCorrectly()
    {
        var schema = new ZodArray(new ZodString());

        var code = _emitter.EmitType(schema);

        code.Should().Be("z.array(z.string())");
    }

    [Fact]
    public void ZodRecord_EmitsCorrectly()
    {
        var schema = new ZodRecord(new ZodString(), new ZodNumber());

        var code = _emitter.EmitType(schema);

        code.Should().Be("z.record(z.string(), z.number())");
    }

    // ========================================
    // Enums
    // ========================================

    [Fact]
    public void ZodNativeEnum_EmitsCorrectly()
    {
        var schema = new ZodNativeEnum("Status");

        var code = _emitter.EmitType(schema);

        code.Should().Be("z.nativeEnum(Status)");
    }

    // ========================================
    // Nullable and Optional
    // ========================================

    [Fact]
    public void ZodNullable_WrapsType()
    {
        var schema = new ZodNullable(new ZodString());

        var code = _emitter.EmitType(schema);

        code.Should().Be("z.string().nullable()");
    }

    [Fact]
    public void ZodOptional_WrapsType()
    {
        var schema = new ZodOptional(new ZodString());

        var code = _emitter.EmitType(schema);

        code.Should().Be("z.string().optional()");
    }

    // ========================================
    // Validators
    // ========================================

    [Fact]
    public void ZodString_WithMinLength()
    {
        var schema = new ZodString(new[] { new ZodMin(2, "Must be at least 2 characters") });

        var code = _emitter.EmitType(schema);

        code.Should().Be("z.string().min(2, 'Must be at least 2 characters')");
    }

    [Fact]
    public void ZodString_WithMaxLength()
    {
        var schema = new ZodString(new[] { new ZodMax(100) });

        var code = _emitter.EmitType(schema);

        code.Should().Be("z.string().max(100)");
    }

    [Fact]
    public void ZodString_WithEmail()
    {
        var schema = new ZodString(new[] { new ZodEmail() });

        var code = _emitter.EmitType(schema);

        code.Should().Be("z.string().email()");
    }

    [Fact]
    public void ZodString_WithMultipleValidators()
    {
        var schema = new ZodString(new ZodValidator[]
        {
            new ZodMin(2),
            new ZodMax(50),
            new ZodTrim()
        });

        var code = _emitter.EmitType(schema);

        code.Should().Be("z.string().min(2).max(50).trim()");
    }

    [Fact]
    public void ZodNumber_WithInt()
    {
        var schema = new ZodNumber(new[] { new ZodInt() });

        var code = _emitter.EmitType(schema);

        code.Should().Be("z.number().int()");
    }

    [Fact]
    public void ZodNumber_WithPositive()
    {
        var schema = new ZodNumber(new[] { new ZodPositive() });

        var code = _emitter.EmitType(schema);

        code.Should().Be("z.number().positive()");
    }

    [Fact]
    public void ZodNumber_WithMinMax()
    {
        var schema = new ZodNumber(new ZodValidator[]
        {
            new ZodMin(0),
            new ZodMax(100)
        });

        var code = _emitter.EmitType(schema);

        code.Should().Be("z.number().min(0).max(100)");
    }

    [Fact]
    public void ZodArray_WithMinLength()
    {
        var schema = new ZodArray(new ZodString(), new[] { new ZodMin(1, "At least one item required") });

        var code = _emitter.EmitType(schema);

        code.Should().Be("z.array(z.string()).min(1, 'At least one item required')");
    }

    // ========================================
    // Full Schema Export
    // ========================================

    [Fact]
    public void Schema_EmitsExport()
    {
        var schema = new ZodSchema("CreatePersonRequest", new ZodObject(new[]
        {
            new ZodProperty("firstName", new ZodString(new[] { new ZodMin(2) })),
            new ZodProperty("lastName", new ZodString(new[] { new ZodMin(2) })),
            new ZodProperty("age", new ZodNumber(new[] { new ZodInt(), new ZodMin(0) }))
        }));

        var code = _emitter.EmitSchema(schema);

        code.Should().StartWith("export const CreatePersonRequestSchema = z.object({");
        code.Should().Contain("firstName: z.string().min(2)");
    }

    // ========================================
    // File Generation
    // ========================================

    [Fact]
    public void File_IncludesImports()
    {
        var file = new ZodFile(new[]
        {
            new ZodSchema("TestSchema", new ZodObject(Array.Empty<ZodProperty>()))
        });

        var code = _emitter.EmitFile(file);

        code.Should().Contain("import { z } from 'zod';");
    }

    [Fact]
    public void File_IncludesEnumImports()
    {
        var file = new ZodFile(new[]
        {
            new ZodSchema("TestSchema", new ZodObject(new[]
            {
                new ZodProperty("status", new ZodNativeEnum("Status"))
            }))
        });

        var code = _emitter.EmitFile(file);

        code.Should().Contain("import { Status } from './types';");
    }
}
