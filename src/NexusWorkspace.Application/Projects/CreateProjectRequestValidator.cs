using FluentValidation;

namespace NexusWorkspace.Application.Projects;

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del proyecto es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no puede superar los 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(8000).WithMessage("La descripción es demasiado larga.");

        RuleFor(x => x.Color)
            .Matches("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$")
            .When(x => !string.IsNullOrWhiteSpace(x.Color))
            .WithMessage("El color debe ser un valor hexadecimal, p. ej. #4A43D9.");

        RuleFor(x => x.DueDateUtc)
            .GreaterThanOrEqualTo(x => x.StartDateUtc!.Value)
            .When(x => x.StartDateUtc.HasValue && x.DueDateUtc.HasValue)
            .WithMessage("La fecha prevista no puede ser anterior a la fecha de inicio.");
    }
}
