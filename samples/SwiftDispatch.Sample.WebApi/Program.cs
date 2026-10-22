using SwiftDispatch.Contracts;
using SwiftDispatch.Sample.WebApi.Configuration;
using SwiftDispatch.Sample.WebApi.Requests;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSwiftDispatch();

var app = builder.Build();

app.MapGet("/greetings/{name}", async (string name, IMediator mediator, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(name))
    {
        return Results.BadRequest("Name is required.");
    }

    string greeting = await mediator.Send(new Hello(name), cancellationToken);
    return Results.Ok(new { greeting });
});

app.Run();
