using System.Linq.Expressions;
using System.Reflection;
using FluentValidation;
using FluentValidation.Validators;

namespace Impulse.Validation;

/// <summary>
/// Analyzes FluentValidation validators to extract metadata for Zod generation.
/// </summary>
public sealed class FluentValidationAnalyzer
{
    /// <summary>
    /// Analyzes a validator and extracts metadata.
    /// </summary>
    public ValidatorMetadata Analyze<T>(IValidator<T> validator)
    {
        var metadata = new ValidatorMetadata
        {
            TypeName = typeof(T).Name,
            ValidatedType = typeof(T)
        };

        var descriptor = validator.CreateDescriptor();

        foreach (var member in descriptor.GetMembersWithValidators())
        {
            var propertyInfo = typeof(T).GetProperty(member.Key);
            if (propertyInfo is null) continue;

            var propValidation = new PropertyValidation
            {
                Name = member.Key,
                PropertyType = propertyInfo.PropertyType,
                IsNullable = IsNullable(propertyInfo),
                IsOptional = false
            };

            foreach (var (propValidator, _) in member)
            {
                var validationRule = AnalyzeRule(propValidator);
                if (validationRule is not null)
                {
                    propValidation.Rules.Add(validationRule);
                }
            }

            metadata.Properties.Add(propValidation);
        }

        return metadata;
    }

    private static ValidationRule? AnalyzeRule(IPropertyValidator validator)
    {
        return validator switch
        {
            INotEmptyValidator => new ValidationRule { Type = ValidationRuleType.NotEmpty },
            INotNullValidator => new ValidationRule { Type = ValidationRuleType.NotNull },
            ILengthValidator lengthValidator => AnalyzeLengthValidator(lengthValidator),
            IEmailValidator => new ValidationRule { Type = ValidationRuleType.Email },
            IComparisonValidator comparisonValidator => AnalyzeComparisonValidator(comparisonValidator),
            IRegularExpressionValidator regexValidator => AnalyzeRegexValidator(regexValidator),
            _ => null // Custom/unsupported validators are server-only
        };
    }

    private static ValidationRule? AnalyzeLengthValidator(ILengthValidator validator)
    {
        if (validator.Max > 0 && validator.Min > 0)
        {
            // Has both min and max
            var rule = new ValidationRule { Type = ValidationRuleType.MinLength };
            rule.Parameters["min"] = validator.Min;
            return rule;
        }

        if (validator.Max > 0)
        {
            var rule = new ValidationRule { Type = ValidationRuleType.MaxLength };
            rule.Parameters["max"] = validator.Max;
            return rule;
        }

        if (validator.Min > 0)
        {
            var rule = new ValidationRule { Type = ValidationRuleType.MinLength };
            rule.Parameters["min"] = validator.Min;
            return rule;
        }

        return null;
    }

    private static ValidationRule? AnalyzeComparisonValidator(IComparisonValidator validator)
    {
        var comparison = validator.Comparison;
        var valueToCompare = validator.ValueToCompare;

        return comparison switch
        {
            Comparison.GreaterThan => CreateComparisonRule(ValidationRuleType.GreaterThan, valueToCompare),
            Comparison.LessThan => CreateComparisonRule(ValidationRuleType.LessThan, valueToCompare),
            Comparison.GreaterThanOrEqual => CreateComparisonRule(ValidationRuleType.GreaterThanOrEqual, valueToCompare),
            Comparison.LessThanOrEqual => CreateComparisonRule(ValidationRuleType.LessThanOrEqual, valueToCompare),
            _ => null
        };
    }

    private static ValidationRule CreateComparisonRule(ValidationRuleType type, object? value)
    {
        var rule = new ValidationRule { Type = type };
        if (value is not null)
        {
            rule.Parameters["value"] = value;
        }
        return rule;
    }

    private static ValidationRule? AnalyzeRegexValidator(IRegularExpressionValidator validator)
    {
        var rule = new ValidationRule { Type = ValidationRuleType.Regex };
        rule.Parameters["pattern"] = validator.Expression;
        return rule;
    }

    private static bool IsNullable(PropertyInfo property)
    {
        if (Nullable.GetUnderlyingType(property.PropertyType) is not null)
            return true;

        var nullableContext = property.DeclaringType?
            .GetCustomAttribute<System.Runtime.CompilerServices.NullableContextAttribute>();
        var nullableAttribute = property
            .GetCustomAttribute<System.Runtime.CompilerServices.NullableAttribute>();

        if (nullableAttribute?.NullableFlags.Length > 0)
            return nullableAttribute.NullableFlags[0] == 2;

        return nullableContext?.Flag == 2;
    }
}
