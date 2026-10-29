using FluentValidation;

namespace SwiftDispatch.Sample.CleanArchitecture.Application.TodoItems.Commands.CreateTodo;

public sealed class CreateTodoValidator : AbstractValidator<CreateTodo>
{
    public CreateTodoValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .WithMessage("Title is required.");
    }
}
