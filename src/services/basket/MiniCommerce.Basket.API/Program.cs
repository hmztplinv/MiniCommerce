using MiniCommerce.Basket.API.Clients;
using MiniCommerce.Basket.API.Endpoints;
using MiniCommerce.Basket.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBasketInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        Service = "MiniCommerce.Basket.API",
        Status = "Running"
    });
});

app.MapHealthChecks("/health");

app.MapBasketEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/debug/catalog/products/{productId}", async (
        string productId,
        ICatalogClient catalogClient,
        CancellationToken cancellationToken) =>
    {
        var product = await catalogClient.GetProductByIdAsync(
            productId,
            cancellationToken);

        if (product is null)
        {
            return Results.NotFound(new
            {
                Message = "Product not found in Catalog API."
            });
        }

        return Results.Ok(product);
    });
}

app.Run();
