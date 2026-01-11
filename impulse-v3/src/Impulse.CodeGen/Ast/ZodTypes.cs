namespace Impulse.CodeGen.Ast;

/// <summary>
/// Base class for Zod schema AST nodes.
/// </summary>
public abstract record ZodType;

/// <summary>
/// z.string() with optional validators.
/// </summary>
public record ZodString(IReadOnlyList<ZodValidator>? Validators = null) : ZodType
{
    public IReadOnlyList<ZodValidator> Validators { get; } = Validators ?? Array.Empty<ZodValidator>();
}

/// <summary>
/// z.number() with optional validators.
/// </summary>
public record ZodNumber(IReadOnlyList<ZodValidator>? Validators = null) : ZodType
{
    public IReadOnlyList<ZodValidator> Validators { get; } = Validators ?? Array.Empty<ZodValidator>();
}

/// <summary>
/// z.boolean()
/// </summary>
public record ZodBoolean : ZodType;

/// <summary>
/// z.array(element) with optional validators.
/// </summary>
public record ZodArray(ZodType Element, IReadOnlyList<ZodValidator>? Validators = null) : ZodType
{
    public IReadOnlyList<ZodValidator> Validators { get; } = Validators ?? Array.Empty<ZodValidator>();
}

/// <summary>
/// z.record(key, value)
/// </summary>
public record ZodRecord(ZodType KeyType, ZodType ValueType) : ZodType;

/// <summary>
/// z.object({...})
/// </summary>
public record ZodObject(IReadOnlyList<ZodProperty> Properties) : ZodType;

/// <summary>
/// Property within a Zod object.
/// </summary>
public record ZodProperty(string Name, ZodType Type, bool IsOptional = false);

/// <summary>
/// z.nativeEnum(EnumName)
/// </summary>
public record ZodNativeEnum(string EnumName) : ZodType;

/// <summary>
/// Reference to another schema by name.
/// </summary>
public record ZodRef(string SchemaName) : ZodType;

/// <summary>
/// .nullable() wrapper
/// </summary>
public record ZodNullable(ZodType Inner) : ZodType;

/// <summary>
/// .optional() wrapper
/// </summary>
public record ZodOptional(ZodType Inner) : ZodType;

/// <summary>
/// Named schema export.
/// </summary>
public record ZodSchema(string Name, ZodType Shape);

/// <summary>
/// File containing multiple schemas.
/// </summary>
public record ZodFile(IReadOnlyList<ZodSchema> Schemas);

// ========================================
// Validators
// ========================================

/// <summary>
/// Base class for Zod validators.
/// </summary>
public abstract record ZodValidator;

/// <summary>
/// .min(value, message?)
/// </summary>
public record ZodMin(int Value, string? Message = null) : ZodValidator;

/// <summary>
/// .max(value, message?)
/// </summary>
public record ZodMax(int Value, string? Message = null) : ZodValidator;

/// <summary>
/// .email(message?)
/// </summary>
public record ZodEmail(string? Message = null) : ZodValidator;

/// <summary>
/// .url(message?)
/// </summary>
public record ZodUrl(string? Message = null) : ZodValidator;

/// <summary>
/// .uuid(message?)
/// </summary>
public record ZodUuid(string? Message = null) : ZodValidator;

/// <summary>
/// .int(message?)
/// </summary>
public record ZodInt(string? Message = null) : ZodValidator;

/// <summary>
/// .positive(message?)
/// </summary>
public record ZodPositive(string? Message = null) : ZodValidator;

/// <summary>
/// .negative(message?)
/// </summary>
public record ZodNegative(string? Message = null) : ZodValidator;

/// <summary>
/// .trim()
/// </summary>
public record ZodTrim : ZodValidator;

/// <summary>
/// .datetime(message?)
/// </summary>
public record ZodDatetime(string? Message = null) : ZodValidator;

/// <summary>
/// .regex(pattern, message?)
/// </summary>
public record ZodRegex(string Pattern, string? Message = null) : ZodValidator;
