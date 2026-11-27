# FluentValidation integration

## Software Bill of Materials

For versions released through the SBOM-enabled workflow, download
`TrueMediator.FluentValidation.<version>.sbom.spdx.json` from the matching
[GitHub Release](https://github.com/Kamyab7/TrueMediator/releases).
This SPDX 2.2 JSON SBOM describes the package and its release build inputs across
supported frameworks, including build dependencies. A consuming application should
generate its own SBOM to inventory the dependency versions it deploys.

`TrueMediator.FluentValidation` provides an optional asynchronous validation behavior for requests with responses and void requests. It targets .NET 8, .NET 10, and .NET 11 and uses FluentValidation 12. Core TrueMediator retains its .NET Standard 2.0 support and does not depend on FluentValidation.

## Install and register

Install both packages in the application project that configures dispatch:

```sh
dotnet add package TrueMediator
dotnet add package TrueMediator.FluentValidation
```

Register validators explicitly and include the behavior in your existing direct registration lambda:

```csharp
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TrueMediator.Contracts;
using TrueMediator.FluentValidation;

services.AddScoped<IValidator<CreateOrder>, CreateOrderValidator>();
services.AddTrueMediator(options =>
{
    options.RegisterServicesFromAssembly(typeof(CreateOrder).Assembly);
    options.AddOpenBehavior(typeof(FluentValidationBehavior<,>));
});

public sealed record CreateOrder(string Reference) : IRequest<Guid>;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrder>
{
    public CreateOrderValidator()
    {
        RuleFor(request => request.Reference)
            .NotEmpty().WithErrorCode("ReferenceRequired");
    }
}
```

Implement `IRequestHandler<CreateOrder, Guid>` in the included assembly as usual. Validators are resolved from DI; installing the package does not discover validators or enable the behavior automatically. Add the behavior once. No new configuration helper or source-generator configuration syntax is required.

## Execution and errors

- Validators run sequentially in DI registration order, allowing them to share scoped dependencies such as a database context.
- Every validator is invoked through `ValidateAsync`, supporting synchronous and asynchronous rules. Cancellation is forwarded and checked before continuing.
- Failures from all validators are aggregated into FluentValidation's standard `ValidationException`. Each failure retains its property name, message, error code, severity, attempted value, custom state, and message placeholders. Each validator's own cascade configuration still applies.
- Validation failures prevent subsequent behaviors and the handler from running. No registered validators means the pipeline continues normally.
- Infrastructure exceptions and cancellation propagate unchanged; they are not converted into validation failures.
- Streaming requests and notifications are outside this package's initial scope.

Behaviors are ordered outermost first. Put logging outside validation to observe failures. Put authorization before validation when rules must only run for authorized callers, and put transaction behaviors after validation when invalid requests should not open a transaction. A behavior placed earlier can short-circuit validation or retry it by invoking its continuation again.

The package does not select RuleSets or translate failures into application-specific result types. Configure those concerns in your application or a custom behavior if needed.

## ASP.NET Core

HTTP mapping belongs in the web application. Catch `FluentValidation.ValidationException` and map its failures to an HTTP 400 validation problem response, grouping by property name:

```csharp
catch (ValidationException exception)
{
    var errors = exception.Errors
        .GroupBy(failure => failure.PropertyName)
        .ToDictionary(group => group.Key,
            group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());
    return Results.ValidationProblem(errors);
}
```

Only expose approved error details. Attempted values and arbitrary custom state should not be returned automatically. The Clean Architecture sample demonstrates this mapping without adding an HTTP dependency to the integration package.

## Trimming and Native AOT

The integration adds no runtime assembly scanning or dynamic generic construction. Explicit validator registration is the recommended baseline for trimming and Native AOT. CI packs the integration and executes a consumer with synchronous and asynchronous rules, response and void requests, and validation failures under Native AOT for .NET 8, .NET 10, and .NET 11.

This verifies the included validators, not every possible rule or dependency. Publish and run your application's actual validator graph with trimming/AOT warnings enabled before claiming compatibility. Assembly-scanning helpers from `FluentValidation.DependencyInjectionExtensions` are optional and require separate trimming review; they are not dependencies of this package.
