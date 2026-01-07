using FluentValidation;
using System.Reflection;

namespace Impulse.Validation;

/// <summary>
/// Discovers and generates Zod schemas from all FluentValidation validators in an assembly.
/// </summary>
public sealed class ValidationCodeGenerator
{
    private readonly FluentValidationAnalyzer _analyzer = new();
    private readonly ZodSchemaGenerator _zodGenerator = new();

    /// <summary>
    /// Generates validation.g.ts from all validators in the given assembly.
    /// </summary>
    public string Generate(Assembly assembly)
    {
        var validators = DiscoverValidators(assembly);
        var metadata = validators.Select(v => AnalyzeValidator(v)).Where(m => m is not null).ToList();

        return _zodGenerator.Generate(metadata!);
    }

    /// <summary>
    /// Generates validation.g.ts from specific validator types.
    /// </summary>
    public string Generate(params Type[] validatorTypes)
    {
        var metadata = validatorTypes
            .Select(t => CreateAndAnalyzeValidator(t))
            .Where(m => m is not null)
            .ToList();

        return _zodGenerator.Generate(metadata!);
    }

    private static IEnumerable<object> DiscoverValidators(Assembly assembly)
    {
        var validatorInterface = typeof(IValidator<>);

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface) continue;

            var interfaces = type.GetInterfaces();
            var isValidator = interfaces.Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == validatorInterface);

            if (isValidator)
            {
                var instance = Activator.CreateInstance(type);
                if (instance is not null)
                {
                    yield return instance;
                }
            }
        }
    }

    private ValidatorMetadata? AnalyzeValidator(object validatorInstance)
    {
        var validatorType = validatorInstance.GetType();
        var interfaces = validatorType.GetInterfaces();

        var validatorInterface = interfaces.FirstOrDefault(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));

        if (validatorInterface is null) return null;

        var validatedType = validatorInterface.GetGenericArguments()[0];

        // Use reflection to call Analyze<T>
        var analyzeMethod = typeof(FluentValidationAnalyzer)
            .GetMethod(nameof(FluentValidationAnalyzer.Analyze))!
            .MakeGenericMethod(validatedType);

        return analyzeMethod.Invoke(_analyzer, [validatorInstance]) as ValidatorMetadata;
    }

    private ValidatorMetadata? CreateAndAnalyzeValidator(Type validatorType)
    {
        try
        {
            var instance = Activator.CreateInstance(validatorType);
            if (instance is null) return null;
            return AnalyzeValidator(instance);
        }
        catch
        {
            return null;
        }
    }
}
