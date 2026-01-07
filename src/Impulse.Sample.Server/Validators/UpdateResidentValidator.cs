using FluentValidation;
using Impulse.Sample.Server.Models;

namespace Impulse.Sample.Server.Validators;

/// <summary>
/// Validator for UpdateResidentRequest.
/// </summary>
public class UpdateResidentValidator : AbstractValidator<UpdateResidentRequest>
{
    public UpdateResidentValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Room)
            .NotEmpty()
            .Matches(@"^[A-Z]-\d{3}$")
            .WithMessage("Room must be in format 'X-000' (e.g., 'A-101')");

        RuleForEach(x => x.Allergies)
            .NotEmpty()
            .MaximumLength(50);
    }
}
