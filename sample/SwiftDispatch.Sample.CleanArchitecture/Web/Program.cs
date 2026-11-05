using Scalar.AspNetCore;
using SwiftDispatch.Sample.CleanArchitecture.Application;
using SwiftDispatch.Sample.CleanArchitecture.Infrastructure;
using SwiftDispatch.Sample.CleanArchitecture.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddOpenApi();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("SwiftDispatch Clean Architecture API"));
}

app.MapTodoItems();
app.Run();
