using FluentValidation;

namespace NexusWorkspace.Application.Tasks;

public sealed class CreateWorkTaskRequestValidator : AbstractValidator<CreateWorkTaskRequest>
{
    public CreateWorkTaskRequestValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("La tarea debe pertenecer a un proyecto.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El título de la tarea es obligatorio.")
            .MaximumLength(300).WithMessage("El título no puede superar los 300 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(20000).WithMessage("La descripción es demasiado larga.");
    }
}
