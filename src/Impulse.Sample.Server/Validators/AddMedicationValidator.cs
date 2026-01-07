using FluentValidation;
using Impulse.Sample.Server.Models;

namespace Impulse.Sample.Server.Validators;

/// <summary>
/// Validator for AddMedicationRequest.
/// </summary>
public class AddMedicationValidator : AbstractValidator<AddMedicationRequest>
{
    public AddMedicationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Dosage)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Frequency)
            .IsInEnum();

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .GreaterThanOrEqualTo(DateTime.Today.AddYears(-1));
    }
}
