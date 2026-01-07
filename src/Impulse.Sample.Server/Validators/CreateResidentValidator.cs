using FluentValidation;
using Impulse.Sample.Server.Models;

namespace Impulse.Sample.Server.Validators;

/// <summary>
/// Validator for CreateResidentRequest.
/// </summary>
public class CreateResidentValidator : AbstractValidator<CreateResidentRequest>
{
    public CreateResidentValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Room)
            .NotEmpty()
            .Matches(@"^[A-Z]-\d{3}$")
            .WithMessage("Room must be in format 'X-000' (e.g., 'A-101')");

        RuleFor(x => x.AdmitDate)
            .NotEmpty()
            .LessThanOrEqualTo(DateTime.Today);
    }
}
