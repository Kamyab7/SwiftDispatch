using SwiftDispatch.Contracts;
using SwiftDispatch.Sample.CleanArchitecture.Application.Common.Exceptions;
using SwiftDispatch.Sample.CleanArchitecture.Application.TodoItems;
using SwiftDispatch.Sample.CleanArchitecture.Application.TodoItems.Commands.CreateTodo;
using SwiftDispatch.Sample.CleanArchitecture.Application.TodoItems.Commands.CompleteTodo;
using SwiftDispatch.Sample.CleanArchitecture.Application.TodoItems.Queries.GetTodo;

namespace SwiftDispatch.Sample.CleanArchitecture.Web.Endpoints;

public static class TodoItems
{
    public static IEndpointRouteBuilder MapTodoItems(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/todos").WithTags("Todo items");

        group.MapPost("", async (CreateTodoBody body, IMediator mediator, CancellationToken cancellationToken) =>
        {
            try
            {
                TodoResponse todo = await mediator.Send(new CreateTodo(body.Title ?? string.Empty), cancellationToken);
                return Results.Created($"/todos/{todo.Id}", todo);
            }
            catch (RequestValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
        })
        .WithName("CreateTodo")
        .WithSummary("Create a todo item")
        .Produces<TodoResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            TodoResponse? todo = await mediator.Send(new GetTodo(id), cancellationToken);
            return todo is null ? Results.NotFound() : Results.Ok(todo);
        })
        .WithName("GetTodo")
        .WithSummary("Get a todo item by ID")
        .Produces<TodoResponse>()
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/complete", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            TodoResponse? todo = await mediator.Send(new CompleteTodo(id), cancellationToken);
            return todo is null ? Results.NotFound() : Results.Ok(todo);
        })
        .WithName("CompleteTodo")
        .WithSummary("Mark a todo item as completed")
        .Produces<TodoResponse>()
        .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
