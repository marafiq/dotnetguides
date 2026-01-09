using System.Reflection;
using Impulse.SourceGen.Ast;

namespace Impulse.SourceGen.Analyzers;

/// <summary>
/// Analyzes FluentValidation validators and converts them to Zod schema AST.
/// Extracts validation rules and transforms them into equivalent Zod validators.
/// </summary>
public class ValidatorAnalyzer
{
    private readonly TypeAnalyzer _typeAnalyzer = new();

    /// <summary>
    /// Analyze a FluentValidation validator type and extract Zod schema with rules.
    /// </summary>
    public ZodSchema? AnalyzeValidator(Type validatorType)
    {
        // Check if this is a FluentValidation validator
        var abstractValidatorBase = FindAbstractValidatorBase(validatorType);
        if (abstractValidatorBase == null) return null;

        // Get the validated type (T in AbstractValidator<T>)
        var validatedType = abstractValidatorBase.GetGenericArguments()[0];
        var schemaName = $"{validatedType.Name}Schema";

        // Create instance to analyze rules
        var validator = CreateValidatorInstance(validatorType);
        if (validator == null) return null;

        // Extract rules using reflection on the validator's internal state
        var properties = ExtractValidationRules(validator, validatorType, validatedType);

        return new ZodSchema(schemaName, new ZodObject(properties));
    }

    /// <summary>
    /// Analyze all validators in an assembly.
    /// </summary>
    public ZodFile AnalyzeAssembly(Assembly assembly)
    {
        var schemas = new List<ZodSchema>();

        foreach (var type in assembly.GetExportedTypes())
        {
            if (FindAbstractValidatorBase(type) != null)
            {
                var schema = AnalyzeValidator(type);
                if (schema != null)
                {
                    schemas.Add(schema);
                }
            }
        }

        return new ZodFile(schemas);
    }

    private static Type? FindAbstractValidatorBase(Type type)
    {
        var current = type.BaseType;
        while (current != null)
        {
            if (current.IsGenericType &&
                current.Name.StartsWith("AbstractValidator"))
            {
                return current;
            }
            current = current.BaseType;
        }
        return null;
    }

    private static object? CreateValidatorInstance(Type validatorType)
    {
        try
        {
            // Try parameterless constructor
            var ctor = validatorType.GetConstructor(Type.EmptyTypes);
            if (ctor != null)
            {
                return ctor.Invoke(null);
            }
        }
        catch
        {
            // Validator might have dependencies, skip
        }
        return null;
    }

    private List<ZodProperty> ExtractValidationRules(object validator, Type validatorType, Type validatedType)
    {
        var properties = new List<ZodProperty>();

        // Get all properties of the validated type
        foreach (var prop in validatedType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propName = ToCamelCase(prop.Name);
            var zodType = _typeAnalyzer.MapToZodType(prop.PropertyType);

            // Try to extract validators for this property from the FluentValidation instance
            var validators = ExtractPropertyValidators(validator, validatorType, prop.Name);

            // Apply extracted validators to the Zod type
            zodType = ApplyValidators(zodType, validators);

            var isOptional = IsNullable(prop.PropertyType) ||
                             validators.Any(v => v is OptionalRule);

            properties.Add(new ZodProperty(propName, zodType, isOptional));
        }

        return properties;
    }

    private List<ValidationRule> ExtractPropertyValidators(object validator, Type validatorType, string propertyName)
    {
        var rules = new List<ValidationRule>();

        try
        {
            // FluentValidation stores rules internally - we try to access them via reflection
            // This is a simplified approach; real implementation would use FluentValidation's API

            // Look for a Rules or _rules field
            var rulesField = validatorType.GetField("_rules",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (rulesField?.GetValue(validator) is not System.Collections.IEnumerable rulesCollection)
                return rules;

            foreach (var rule in rulesCollection)
            {
                var ruleType = rule.GetType();

                // Check if this rule is for our property
                var memberProp = ruleType.GetProperty("Member");
                var member = memberProp?.GetValue(rule);
                if (member is not MemberInfo memberInfo) continue;
                if (memberInfo.Name != propertyName) continue;

                // Extract validators from the rule
                var componentsProp = ruleType.GetProperty("Components");
                if (componentsProp?.GetValue(rule) is not System.Collections.IEnumerable components)
                    continue;

                foreach (var component in components)
                {
                    var extracted = ExtractValidatorRule(component);
                    if (extracted != null)
                    {
                        rules.Add(extracted);
                    }
                }
            }
        }
        catch
        {
            // Reflection failed, return empty rules
        }

        return rules;
    }

    private ValidationRule? ExtractValidatorRule(object component)
    {
        var typeName = component.GetType().Name;

        return typeName switch
        {
            "NotEmptyValidator" or "NotEmptyValidator`2" => new NotEmptyRule(),
            "NotNullValidator" or "NotNullValidator`2" => new NotNullRule(),
            "MaximumLengthValidator" or "MaximumLengthValidator`1" =>
                new MaxLengthRule(GetIntProperty(component, "Max")),
            "MinimumLengthValidator" or "MinimumLengthValidator`1" =>
                new MinLengthRule(GetIntProperty(component, "Min")),
            "LengthValidator" or "LengthValidator`1" =>
                new LengthRule(GetIntProperty(component, "Min"), GetIntProperty(component, "Max")),
            "EmailValidator" or "EmailValidator`1" => new EmailRule(),
            "RegularExpressionValidator" or "RegularExpressionValidator`1" =>
                new RegexRule(GetStringProperty(component, "Expression") ?? ".*"),
            "LessThanValidator" or "LessThanValidator`2" =>
                new LessThanRule(GetDoubleProperty(component, "ValueToCompare")),
            "LessThanOrEqualValidator" or "LessThanOrEqualValidator`2" =>
                new LessThanOrEqualRule(GetDoubleProperty(component, "ValueToCompare")),
            "GreaterThanValidator" or "GreaterThanValidator`2" =>
                new GreaterThanRule(GetDoubleProperty(component, "ValueToCompare")),
            "GreaterThanOrEqualValidator" or "GreaterThanOrEqualValidator`2" =>
                new GreaterThanOrEqualRule(GetDoubleProperty(component, "ValueToCompare")),
            "InclusiveBetweenValidator" or "InclusiveBetweenValidator`2" =>
                new BetweenRule(GetDoubleProperty(component, "From"), GetDoubleProperty(component, "To")),
            _ => null
        };
    }

    private static int GetIntProperty(object obj, string name)
    {
        var prop = obj.GetType().GetProperty(name);
        return prop?.GetValue(obj) is int value ? value : 0;
    }

    private static double GetDoubleProperty(object obj, string name)
    {
        var prop = obj.GetType().GetProperty(name);
        var value = prop?.GetValue(obj);
        return value switch
        {
            int i => i,
            long l => l,
            float f => f,
            double d => d,
            decimal dec => (double)dec,
            _ => 0
        };
    }

    private static string? GetStringProperty(object obj, string name)
    {
        var prop = obj.GetType().GetProperty(name);
        return prop?.GetValue(obj) as string;
    }

    private ZodType ApplyValidators(ZodType baseType, List<ValidationRule> rules)
    {
        var validators = new List<ZodValidator>();

        foreach (var rule in rules)
        {
            switch (rule)
            {
                case NotEmptyRule:
                    validators.Add(new ZodNonempty());
                    break;
                case MinLengthRule minLen:
                    validators.Add(new ZodMin(minLen.Length));
                    break;
                case MaxLengthRule maxLen:
                    validators.Add(new ZodMax(maxLen.Length));
                    break;
                case LengthRule len:
                    if (len.Min == len.Max)
                        validators.Add(new ZodLength(len.Min));
                    else
                    {
                        validators.Add(new ZodMin(len.Min));
                        validators.Add(new ZodMax(len.Max));
                    }
                    break;
                case EmailRule:
                    validators.Add(new ZodEmail());
                    break;
                case RegexRule regex:
                    validators.Add(new ZodRegex(regex.Pattern));
                    break;
                case LessThanRule lt:
                    validators.Add(new ZodMax((int)lt.Value - 1));
                    break;
                case LessThanOrEqualRule lte:
                    validators.Add(new ZodMax((int)lte.Value));
                    break;
                case GreaterThanRule gt:
                    validators.Add(new ZodMin((int)gt.Value + 1));
                    break;
                case GreaterThanOrEqualRule gte:
                    validators.Add(new ZodMin((int)gte.Value));
                    break;
                case BetweenRule between:
                    validators.Add(new ZodMin((int)between.From));
                    validators.Add(new ZodMax((int)between.To));
                    break;
            }
        }

        // Apply validators to the appropriate Zod type
        return baseType switch
        {
            ZodString str when validators.Count > 0 =>
                new ZodString(str.Validators.Concat(validators).ToList()),
            ZodNumber num when validators.Count > 0 =>
                new ZodNumber(num.Validators.Concat(validators).ToList()),
            ZodArray arr when validators.Count > 0 =>
                new ZodArray(arr.Element, arr.Validators.Concat(validators).ToList()),
            _ => baseType
        };
    }

    private static bool IsNullable(Type type)
    {
        return Nullable.GetUnderlyingType(type) != null ||
               (!type.IsValueType && type != typeof(string));
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}

// Internal rule representations
internal abstract record ValidationRule;
internal record NotEmptyRule : ValidationRule;
internal record NotNullRule : ValidationRule;
internal record OptionalRule : ValidationRule;
internal record MinLengthRule(int Length) : ValidationRule;
internal record MaxLengthRule(int Length) : ValidationRule;
internal record LengthRule(int Min, int Max) : ValidationRule;
internal record EmailRule : ValidationRule;
internal record RegexRule(string Pattern) : ValidationRule;
internal record LessThanRule(double Value) : ValidationRule;
internal record LessThanOrEqualRule(double Value) : ValidationRule;
internal record GreaterThanRule(double Value) : ValidationRule;
internal record GreaterThanOrEqualRule(double Value) : ValidationRule;
internal record BetweenRule(double From, double To) : ValidationRule;
