namespace Impulse.SourceGen.Ast;

/// <summary>
/// Base class for Zod schema AST nodes.
/// Enables building Zod validation schemas from C# types and FluentValidation rules.
/// </summary>
public abstract record ZodNode;

/// <summary>
/// Represents a complete Zod schema file.
/// </summary>
public record ZodFile(IReadOnlyList<ZodSchema> Schemas) : ZodNode;

/// <summary>
/// Represents a named Zod schema export.
/// Example: export const PersonSchema = z.object({ ... })
/// </summary>
public record ZodSchema(string Name, ZodType Shape) : ZodNode;

// ============================================
// Zod Types
// ============================================

/// <summary>
/// Base class for Zod type definitions.
/// </summary>
public abstract record ZodType;

/// <summary>
/// z.object({ ... }) - Object schema with properties.
/// </summary>
public record ZodObject(IReadOnlyList<ZodProperty> Properties) : ZodType;

/// <summary>
/// Property within a Zod object schema.
/// </summary>
public record ZodProperty(string Name, ZodType Type, bool Optional = false);

/// <summary>
/// z.string() with optional validators.
/// </summary>
public record ZodString(IReadOnlyList<ZodValidator> Validators) : ZodType
{
    public ZodString() : this(Array.Empty<ZodValidator>()) { }
}

/// <summary>
/// z.number() with optional validators.
/// </summary>
public record ZodNumber(IReadOnlyList<ZodValidator> Validators) : ZodType
{
    public ZodNumber() : this(Array.Empty<ZodValidator>()) { }
}

/// <summary>
/// z.boolean()
/// </summary>
public record ZodBoolean() : ZodType;

/// <summary>
/// z.date()
/// </summary>
public record ZodDate() : ZodType;

/// <summary>
/// z.array(elementType) with optional validators.
/// </summary>
public record ZodArray(ZodType Element, IReadOnlyList<ZodValidator> Validators) : ZodType
{
    public ZodArray(ZodType element) : this(element, Array.Empty<ZodValidator>()) { }
}

/// <summary>
/// z.record(keyType, valueType)
/// </summary>
public record ZodRecord(ZodType Key, ZodType Value) : ZodType;

/// <summary>
/// z.enum([...]) for string enums.
/// </summary>
public record ZodEnum(IReadOnlyList<string> Values) : ZodType;

/// <summary>
/// z.nativeEnum(EnumType) for TypeScript enums.
/// </summary>
public record ZodNativeEnum(string EnumName) : ZodType;

/// <summary>
/// z.union([type1, type2, ...])
/// </summary>
public record ZodUnion(IReadOnlyList<ZodType> Types) : ZodType;

/// <summary>
/// z.intersection(type1, type2)
/// </summary>
public record ZodIntersection(ZodType Left, ZodType Right) : ZodType;

/// <summary>
/// z.literal(value) for literal types.
/// </summary>
public record ZodLiteral(string Value) : ZodType;

/// <summary>
/// z.null()
/// </summary>
public record ZodNull() : ZodType;

/// <summary>
/// z.undefined()
/// </summary>
public record ZodUndefined() : ZodType;

/// <summary>
/// z.unknown()
/// </summary>
public record ZodUnknown() : ZodType;

/// <summary>
/// z.any() - use sparingly.
/// </summary>
public record ZodAny() : ZodType;

/// <summary>
/// z.tuple([type1, type2, ...])
/// </summary>
public record ZodTuple(IReadOnlyList<ZodType> Elements) : ZodType;

/// <summary>
/// Reference to another schema by name (for nested/recursive types).
/// </summary>
public record ZodRef(string SchemaName) : ZodType;

/// <summary>
/// .nullable() modifier - wraps another type.
/// </summary>
public record ZodNullable(ZodType Inner) : ZodType;

/// <summary>
/// .optional() modifier - wraps another type.
/// </summary>
public record ZodOptional(ZodType Inner) : ZodType;

/// <summary>
/// .default(value) modifier.
/// </summary>
public record ZodDefault(ZodType Inner, string DefaultValue) : ZodType;

// ============================================
// Validators (method chains)
// ============================================

/// <summary>
/// Base class for Zod validators (method chains like .min(), .max(), .email()).
/// </summary>
public abstract record ZodValidator;

/// <summary>
/// .min(value) for strings (minLength) or numbers.
/// </summary>
public record ZodMin(int Value, string? Message = null) : ZodValidator;

/// <summary>
/// .max(value) for strings (maxLength) or numbers.
/// </summary>
public record ZodMax(int Value, string? Message = null) : ZodValidator;

/// <summary>
/// .length(value) for exact string length.
/// </summary>
public record ZodLength(int Value, string? Message = null) : ZodValidator;

/// <summary>
/// .email() for email validation.
/// </summary>
public record ZodEmail(string? Message = null) : ZodValidator;

/// <summary>
/// .url() for URL validation.
/// </summary>
public record ZodUrl(string? Message = null) : ZodValidator;

/// <summary>
/// .uuid() for UUID validation.
/// </summary>
public record ZodUuid(string? Message = null) : ZodValidator;

/// <summary>
/// .regex(pattern) for pattern matching.
/// </summary>
public record ZodRegex(string Pattern, string? Message = null) : ZodValidator;

/// <summary>
/// .int() for integer validation on numbers.
/// </summary>
public record ZodInt(string? Message = null) : ZodValidator;

/// <summary>
/// .positive() for positive number validation.
/// </summary>
public record ZodPositive(string? Message = null) : ZodValidator;

/// <summary>
/// .negative() for negative number validation.
/// </summary>
public record ZodNegative(string? Message = null) : ZodValidator;

/// <summary>
/// .nonnegative() for non-negative number validation.
/// </summary>
public record ZodNonnegative(string? Message = null) : ZodValidator;

/// <summary>
/// .nonpositive() for non-positive number validation.
/// </summary>
public record ZodNonpositive(string? Message = null) : ZodValidator;

/// <summary>
/// .nonempty() for non-empty strings or arrays.
/// </summary>
public record ZodNonempty(string? Message = null) : ZodValidator;

/// <summary>
/// .datetime() for ISO datetime strings.
/// </summary>
public record ZodDatetime(string? Message = null) : ZodValidator;

/// <summary>
/// .trim() for trimming whitespace.
/// </summary>
public record ZodTrim() : ZodValidator;

/// <summary>
/// .toLowerCase() for lowercase transformation.
/// </summary>
public record ZodLowerCase() : ZodValidator;

/// <summary>
/// .toUpperCase() for uppercase transformation.
/// </summary>
public record ZodUpperCase() : ZodValidator;

/// <summary>
/// .startsWith(prefix) for string prefix validation.
/// </summary>
public record ZodStartsWith(string Prefix, string? Message = null) : ZodValidator;

/// <summary>
/// .endsWith(suffix) for string suffix validation.
/// </summary>
public record ZodEndsWith(string Suffix, string? Message = null) : ZodValidator;

/// <summary>
/// .includes(substring) for substring validation.
/// </summary>
public record ZodIncludes(string Substring, string? Message = null) : ZodValidator;

/// <summary>
/// .multipleOf(value) for number divisibility.
/// </summary>
public record ZodMultipleOf(double Value, string? Message = null) : ZodValidator;

/// <summary>
/// .finite() for finite number validation.
/// </summary>
public record ZodFinite(string? Message = null) : ZodValidator;

/// <summary>
/// .safe() for safe integer validation.
/// </summary>
public record ZodSafe(string? Message = null) : ZodValidator;
