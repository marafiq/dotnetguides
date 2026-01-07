using Impulse.Validation;

namespace Impulse.Validation.Tests;

/// <summary>
/// TDD Level 5: Tests for FluentValidation → Zod generation.
/// </summary>
public static class ZodSchemaGeneratorTests
{
    public static void RunAll()
    {
        TestRunner.Group("ZodSchemaGenerator - Basic Schema Generation", () =>
        {
            TestRunner.Run("Generate creates z.object schema",
                Generate_CreatesZodObjectSchema);

            TestRunner.Run("Generate includes import statement",
                Generate_IncludesImportStatement);

            TestRunner.Run("Generate creates type inference export",
                Generate_GeneratesTypeInference);

            TestRunner.Run("Property names use camelCase",
                Generate_UsesCamelCasePropertyNames);
        });

        TestRunner.Group("ZodSchemaGenerator - String Validations", () =>
        {
            TestRunner.Run("NotEmpty adds .min(1)",
                Generate_NotEmpty_AddsMin1);

            TestRunner.Run("MaxLength adds .max(n)",
                Generate_MaxLength_AddsMax);

            TestRunner.Run("MinLength adds .min(n)",
                Generate_MinLength_AddsMin);

            TestRunner.Run("Email adds .email()",
                Generate_Email_AddsEmail);

            TestRunner.Run("Regex adds .regex() with pattern",
                Generate_Regex_AddsRegex);

            TestRunner.Run("Url adds .url()",
                Generate_Url_AddsUrl);
        });

        TestRunner.Group("ZodSchemaGenerator - Number Validations", () =>
        {
            TestRunner.Run("GreaterThan adds .gt(n)",
                Generate_GreaterThan_AddsGt);

            TestRunner.Run("LessThan adds .lt(n)",
                Generate_LessThan_AddsLt);

            TestRunner.Run("GreaterThanOrEqual adds .gte(n)",
                Generate_GreaterThanOrEqual_AddsGte);

            TestRunner.Run("LessThanOrEqual adds .lte(n)",
                Generate_LessThanOrEqual_AddsLte);
        });

        TestRunner.Group("ZodSchemaGenerator - Type Mapping", () =>
        {
            TestRunner.Run("string maps to z.string()",
                Generate_String_MapsToZString);

            TestRunner.Run("int maps to z.number()",
                Generate_Int_MapsToZNumber);

            TestRunner.Run("bool maps to z.boolean()",
                Generate_Bool_MapsToZBoolean);

            TestRunner.Run("DateTime maps to z.string()",
                Generate_DateTime_MapsToZString);

            TestRunner.Run("Guid maps to z.string().uuid()",
                Generate_Guid_MapsToZStringUuid);

            TestRunner.Run("enum maps to z.enum()",
                Generate_Enum_MapsToZEnum);

            TestRunner.Run("array maps to z.array()",
                Generate_Array_MapsToZArray);
        });

        TestRunner.Group("ZodSchemaGenerator - Nullable and Optional", () =>
        {
            TestRunner.Run("nullable property adds .nullable()",
                Generate_Nullable_AddsNullable);

            TestRunner.Run("optional property adds .optional()",
                Generate_Optional_AddsOptional);

            TestRunner.Run("nullable and optional both applied",
                Generate_NullableAndOptional_BothApplied);
        });

        TestRunner.Group("ZodSchemaGenerator - Multiple Rules", () =>
        {
            TestRunner.Run("Multiple rules chain correctly",
                Generate_MultipleRules_ChainCorrectly);

            TestRunner.Run("Multiple properties generate correctly",
                Generate_MultipleProperties_GenerateCorrectly);

            TestRunner.Run("Multiple validators generate separate schemas",
                Generate_MultipleValidators_GenerateSeparateSchemas);
        });
    }

    // ═══════════════════════════════════════════════════════
    // Basic Schema Generation
    // ═══════════════════════════════════════════════════════

    private static void Generate_CreatesZodObjectSchema()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateBasicMetadata("TestRequest", "Name", typeof(string));

        var result = generator.Generate([metadata]);

        Assert.Contains("export const testRequestSchema = z.object({", result);
        Assert.Contains("});", result);
    }

    private static void Generate_IncludesImportStatement()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateBasicMetadata("TestRequest", "Name", typeof(string));

        var result = generator.Generate([metadata]);

        Assert.Contains("import { z } from 'zod';", result);
    }

    private static void Generate_GeneratesTypeInference()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateBasicMetadata("TestRequest", "Name", typeof(string));

        var result = generator.Generate([metadata]);

        Assert.Contains("export type TestRequest = z.infer<typeof testRequestSchema>;", result);
    }

    private static void Generate_UsesCamelCasePropertyNames()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateBasicMetadata("TestRequest", "UserName", typeof(string));

        var result = generator.Generate([metadata]);

        Assert.Contains("userName:", result);
    }

    // ═══════════════════════════════════════════════════════
    // String Validations
    // ═══════════════════════════════════════════════════════

    private static void Generate_NotEmpty_AddsMin1()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateMetadataWithRule("TestRequest", "Name", typeof(string),
            ValidationRuleType.NotEmpty);

        var result = generator.Generate([metadata]);

        Assert.Contains(".min(1)", result);
    }

    private static void Generate_MaxLength_AddsMax()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateMetadataWithRule("TestRequest", "Name", typeof(string),
            ValidationRuleType.MaxLength, ("max", 100));

        var result = generator.Generate([metadata]);

        Assert.Contains(".max(100)", result);
    }

    private static void Generate_MinLength_AddsMin()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateMetadataWithRule("TestRequest", "Name", typeof(string),
            ValidationRuleType.MinLength, ("min", 5));

        var result = generator.Generate([metadata]);

        Assert.Contains(".min(5)", result);
    }

    private static void Generate_Email_AddsEmail()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateMetadataWithRule("TestRequest", "Email", typeof(string),
            ValidationRuleType.Email);

        var result = generator.Generate([metadata]);

        Assert.Contains(".email()", result);
    }

    private static void Generate_Regex_AddsRegex()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateMetadataWithRule("TestRequest", "Code", typeof(string),
            ValidationRuleType.Regex, ("pattern", @"^[A-Z]{3}$"));

        var result = generator.Generate([metadata]);

        Assert.Contains(".regex(/^[A-Z]{3}$/)", result);
    }

    private static void Generate_Url_AddsUrl()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateMetadataWithRule("TestRequest", "Website", typeof(string),
            ValidationRuleType.Url);

        var result = generator.Generate([metadata]);

        Assert.Contains(".url()", result);
    }

    // ═══════════════════════════════════════════════════════
    // Number Validations
    // ═══════════════════════════════════════════════════════

    private static void Generate_GreaterThan_AddsGt()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateMetadataWithRule("TestRequest", "Age", typeof(int),
            ValidationRuleType.GreaterThan, ("value", 0));

        var result = generator.Generate([metadata]);

        Assert.Contains(".gt(0)", result);
    }

    private static void Generate_LessThan_AddsLt()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateMetadataWithRule("TestRequest", "Age", typeof(int),
            ValidationRuleType.LessThan, ("value", 150));

        var result = generator.Generate([metadata]);

        Assert.Contains(".lt(150)", result);
    }

    private static void Generate_GreaterThanOrEqual_AddsGte()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateMetadataWithRule("TestRequest", "Age", typeof(int),
            ValidationRuleType.GreaterThanOrEqual, ("value", 18));

        var result = generator.Generate([metadata]);

        Assert.Contains(".gte(18)", result);
    }

    private static void Generate_LessThanOrEqual_AddsLte()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateMetadataWithRule("TestRequest", "Age", typeof(int),
            ValidationRuleType.LessThanOrEqual, ("value", 65));

        var result = generator.Generate([metadata]);

        Assert.Contains(".lte(65)", result);
    }

    // ═══════════════════════════════════════════════════════
    // Type Mapping
    // ═══════════════════════════════════════════════════════

    private static void Generate_String_MapsToZString()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateBasicMetadata("TestRequest", "Name", typeof(string));

        var result = generator.Generate([metadata]);

        Assert.Contains("z.string()", result);
    }

    private static void Generate_Int_MapsToZNumber()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateBasicMetadata("TestRequest", "Age", typeof(int));

        var result = generator.Generate([metadata]);

        Assert.Contains("z.number()", result);
    }

    private static void Generate_Bool_MapsToZBoolean()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateBasicMetadata("TestRequest", "IsActive", typeof(bool));

        var result = generator.Generate([metadata]);

        Assert.Contains("z.boolean()", result);
    }

    private static void Generate_DateTime_MapsToZString()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateBasicMetadata("TestRequest", "CreatedAt", typeof(DateTime));

        var result = generator.Generate([metadata]);

        Assert.Contains("z.string()", result);
    }

    private static void Generate_Guid_MapsToZStringUuid()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateBasicMetadata("TestRequest", "Id", typeof(Guid));

        var result = generator.Generate([metadata]);

        Assert.Contains("z.string().uuid()", result);
    }

    private static void Generate_Enum_MapsToZEnum()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateBasicMetadata("TestRequest", "Status", typeof(TestStatus));

        var result = generator.Generate([metadata]);

        Assert.Contains("z.enum([", result);
        Assert.Contains("'pending'", result);
        Assert.Contains("'active'", result);
        Assert.Contains("'completed'", result);
    }

    private static void Generate_Array_MapsToZArray()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = CreateBasicMetadata("TestRequest", "Tags", typeof(string[]));

        var result = generator.Generate([metadata]);

        Assert.Contains("z.array(z.string())", result);
    }

    // ═══════════════════════════════════════════════════════
    // Nullable and Optional
    // ═══════════════════════════════════════════════════════

    private static void Generate_Nullable_AddsNullable()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = new ValidatorMetadata
        {
            TypeName = "TestRequest",
            ValidatedType = typeof(TestRequest)
        };
        metadata.Properties.Add(new PropertyValidation
        {
            Name = "NickName",
            PropertyType = typeof(string),
            IsNullable = true,
            IsOptional = false
        });

        var result = generator.Generate([metadata]);

        Assert.Contains(".nullable()", result);
    }

    private static void Generate_Optional_AddsOptional()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = new ValidatorMetadata
        {
            TypeName = "TestRequest",
            ValidatedType = typeof(TestRequest)
        };
        metadata.Properties.Add(new PropertyValidation
        {
            Name = "MiddleName",
            PropertyType = typeof(string),
            IsNullable = false,
            IsOptional = true
        });

        var result = generator.Generate([metadata]);

        Assert.Contains(".optional()", result);
    }

    private static void Generate_NullableAndOptional_BothApplied()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = new ValidatorMetadata
        {
            TypeName = "TestRequest",
            ValidatedType = typeof(TestRequest)
        };
        metadata.Properties.Add(new PropertyValidation
        {
            Name = "Suffix",
            PropertyType = typeof(string),
            IsNullable = true,
            IsOptional = true
        });

        var result = generator.Generate([metadata]);

        Assert.Contains(".nullable()", result);
        Assert.Contains(".optional()", result);
    }

    // ═══════════════════════════════════════════════════════
    // Multiple Rules
    // ═══════════════════════════════════════════════════════

    private static void Generate_MultipleRules_ChainCorrectly()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = new ValidatorMetadata
        {
            TypeName = "TestRequest",
            ValidatedType = typeof(TestRequest)
        };

        var prop = new PropertyValidation
        {
            Name = "Email",
            PropertyType = typeof(string),
            IsNullable = false,
            IsOptional = false
        };
        prop.Rules.Add(new ValidationRule { Type = ValidationRuleType.NotEmpty });
        prop.Rules.Add(new ValidationRule { Type = ValidationRuleType.Email });
        var maxRule = new ValidationRule { Type = ValidationRuleType.MaxLength };
        maxRule.Parameters["max"] = 255;
        prop.Rules.Add(maxRule);

        metadata.Properties.Add(prop);

        var result = generator.Generate([metadata]);

        Assert.Contains(".min(1)", result);
        Assert.Contains(".email()", result);
        Assert.Contains(".max(255)", result);
    }

    private static void Generate_MultipleProperties_GenerateCorrectly()
    {
        var generator = new ZodSchemaGenerator();
        var metadata = new ValidatorMetadata
        {
            TypeName = "CreateUserRequest",
            ValidatedType = typeof(TestRequest)
        };
        metadata.Properties.Add(new PropertyValidation
        {
            Name = "FirstName",
            PropertyType = typeof(string),
            IsNullable = false,
            IsOptional = false
        });
        metadata.Properties.Add(new PropertyValidation
        {
            Name = "LastName",
            PropertyType = typeof(string),
            IsNullable = false,
            IsOptional = false
        });
        metadata.Properties.Add(new PropertyValidation
        {
            Name = "Age",
            PropertyType = typeof(int),
            IsNullable = false,
            IsOptional = false
        });

        var result = generator.Generate([metadata]);

        Assert.Contains("firstName: z.string(),", result);
        Assert.Contains("lastName: z.string(),", result);
        Assert.Contains("age: z.number(),", result);
    }

    private static void Generate_MultipleValidators_GenerateSeparateSchemas()
    {
        var generator = new ZodSchemaGenerator();

        var metadata1 = CreateBasicMetadata("CreateUserRequest", "Name", typeof(string));
        var metadata2 = CreateBasicMetadata("UpdateUserRequest", "Email", typeof(string));

        var result = generator.Generate([metadata1, metadata2]);

        Assert.Contains("export const createUserRequestSchema", result);
        Assert.Contains("export const updateUserRequestSchema", result);
        Assert.Contains("export type CreateUserRequest", result);
        Assert.Contains("export type UpdateUserRequest", result);
    }

    // ═══════════════════════════════════════════════════════
    // Test Helpers
    // ═══════════════════════════════════════════════════════

    private static ValidatorMetadata CreateBasicMetadata(string typeName, string propertyName, Type propertyType)
    {
        var metadata = new ValidatorMetadata
        {
            TypeName = typeName,
            ValidatedType = typeof(TestRequest)
        };
        metadata.Properties.Add(new PropertyValidation
        {
            Name = propertyName,
            PropertyType = propertyType,
            IsNullable = false,
            IsOptional = false
        });
        return metadata;
    }

    private static ValidatorMetadata CreateMetadataWithRule(
        string typeName,
        string propertyName,
        Type propertyType,
        ValidationRuleType ruleType,
        params (string key, object value)[] parameters)
    {
        var metadata = new ValidatorMetadata
        {
            TypeName = typeName,
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

    // Test fixtures
    private record TestRequest(string Name, int Age);

    private enum TestStatus
    {
        Pending,
        Active,
        Completed
    }
}
