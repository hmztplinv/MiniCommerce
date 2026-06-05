using MiniCommerce.Catalog.API.Endpoints;
using MiniCommerce.Catalog.API.Exceptions;
using MiniCommerce.Catalog.API.Extensions;
using MiniCommerce.Catalog.API.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services
    .AddHealthChecks()
    .AddCheck<MongoDbHealthCheck>("mongodb");

builder.Services.AddCatalogInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapGet("/debug/throw", () =>
    {
        throw new InvalidOperationException("This is a test exception from Catalog.API.");
    });
}

app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Ok(new
{
    Service = "MiniCommerce.Catalog.API",
    Status = "Running"
}));

app.MapProductEndpoints();

app.Run();
