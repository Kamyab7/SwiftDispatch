using Scalar.AspNetCore;
using TrueMediator.Sample.CleanArchitecture.Application;
using TrueMediator.Sample.CleanArchitecture.Infrastructure;
using TrueMediator.Sample.CleanArchitecture.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddOpenApi();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("TrueMediator Clean Architecture API"));
}

app.MapTodoItems();
app.Run();
