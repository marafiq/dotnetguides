using Impulse.Validation;

namespace Impulse.Validation.Tests;

public class ZodSchemaGeneratorTests
{
    [Fact]
    public void Generate_CreatesZodObjectSchema()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = new ValidatorMetadata
        {
            TypeName = "TestRequest",
            ValidatedType = typeof(TestRequest)
        };
        metadata.Properties.Add(new PropertyValidation
        {
            Name = "Name",
            PropertyType = typeof(string),
            IsNullable = false,
            IsOptional = false
        });

        var result = generator.Generate([metadata]);

        Assert.Contains("import { z } from 'zod'", result);
        Assert.Contains("export const testRequestSchema = z.object({", result);
        Assert.Contains("name: z.string()", result);
    }

    [Fact]
    public void Generate_AddsMinValidation()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateMetadataWithRule("Name", typeof(string), ValidationRuleType.NotEmpty);

        var result = generator.Generate([metadata]);

        Assert.Contains(".min(1)", result);
    }

    [Fact]
    public void Generate_AddsMaxLengthValidation()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateMetadataWithRule("Name", typeof(string), ValidationRuleType.MaxLength, ("max", 100));

        var result = generator.Generate([metadata]);

        Assert.Contains(".max(100)", result);
    }

    [Fact]
    public void Generate_AddsEmailValidation()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateMetadataWithRule("Email", typeof(string), ValidationRuleType.Email);

        var result = generator.Generate([metadata]);

        Assert.Contains(".email()", result);
    }

    [Fact]
    public void Generate_HandlesNullableProperty()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = new ValidatorMetadata
        {
            TypeName = "TestRequest",
            ValidatedType = typeof(TestRequest)
        };
        metadata.Properties.Add(new PropertyValidation
        {
            Name = "OptionalField",
            PropertyType = typeof(string),
            IsNullable = true,
            IsOptional = false
        });

        var result = generator.Generate([metadata]);

        Assert.Contains(".nullable()", result);
    }

    [Fact]
    public void Generate_HandlesOptionalProperty()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = new ValidatorMetadata
        {
            TypeName = "TestRequest",
            ValidatedType = typeof(TestRequest)
        };
        metadata.Properties.Add(new PropertyValidation
        {
            Name = "OptionalField",
            PropertyType = typeof(string),
            IsNullable = false,
            IsOptional = true
        });

        var result = generator.Generate([metadata]);

        Assert.Contains(".optional()", result);
    }

    [Fact]
    public void Generate_HandlesNumericTypes()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = new ValidatorMetadata
        {
            TypeName = "TestRequest",
            ValidatedType = typeof(TestRequest)
        };
        metadata.Properties.Add(new PropertyValidation
        {
            Name = "Age",
            PropertyType = typeof(int),
            IsNullable = false,
            IsOptional = false
        });

        var result = generator.Generate([metadata]);

        Assert.Contains("z.number()", result);
    }

    [Fact]
    public void Generate_AddsGreaterThanValidation()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateMetadataWithRule("Age", typeof(int), ValidationRuleType.GreaterThan, ("value", 0));

        var result = generator.Generate([metadata]);

        Assert.Contains(".gt(0)", result);
    }

    [Fact]
    public void Generate_AddsRegexValidation()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateMetadataWithRule("Code", typeof(string), ValidationRuleType.Regex, ("pattern", @"^[A-Z]{3}$"));

        var result = generator.Generate([metadata]);

        Assert.Contains(".regex(", result);
    }

    [Fact]
    public void Generate_GeneratesTypeInference()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = new ValidatorMetadata
        {
            TypeName = "TestRequest",
            ValidatedType = typeof(TestRequest)
        };

        var result = generator.Generate([metadata]);

        Assert.Contains("export type TestRequest = z.infer<typeof testRequestSchema>", result);
    }

    private static ValidatorMetadata CreateMetadataWithRule(
        string propertyName,
        Type propertyType,
        ValidationRuleType ruleType,
        params (string key, object value)[] parameters)
    {
        var metadata = new ValidatorMetadata
        {
            TypeName = "TestRequest",
            ValidatedType = typeof(TestRequest)
        };

        var rule = new ValidationRule { Type = ruleType };
        foreach (var (key, value) in parameters)
        {
            rule.Parameters[key] = value;
        }

        var prop = new PropertyValidation
        {
            Name = propertyName,
            PropertyType = propertyType,
            IsNullable = false,
            IsOptional = false
        };
        prop.Rules.Add(rule);

        metadata.Properties.Add(prop);
        return metadata;
    }

    private record TestRequest(string Name, int Age);
}
