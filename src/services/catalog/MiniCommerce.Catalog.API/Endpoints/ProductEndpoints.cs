using MiniCommerce.Catalog.API.DTOs;
using MiniCommerce.Catalog.API.Extensions;
using MiniCommerce.Catalog.API.Services;

namespace MiniCommerce.Catalog.API.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/products")
            .WithTags("Products");

        group.MapGet("/", async (
            IProductService productService,
            CancellationToken cancellationToken) =>
        {
            var result = await productService.GetAllAsync(cancellationToken);

            return result.ToApiResult();
        });

        group.MapGet("/{id}", async (
            string id,
            IProductService productService,
            CancellationToken cancellationToken) =>
        {
            var result = await productService.GetByIdAsync(id, cancellationToken);

            return result.ToApiResult();
        });

        group.MapPost("/", async (
            CreateProductRequest request,
            IProductService productService,
            CancellationToken cancellationToken) =>
        {
            var result = await productService.CreateAsync(request, cancellationToken);

            return result.ToApiResult(product =>
                Results.Created($"/api/products/{product.Id}", product));
        });

        group.MapPut("/{id}", async (
            string id,
            UpdateProductRequest request,
            IProductService productService,
            CancellationToken cancellationToken) =>
        {
            var result = await productService.UpdateAsync(id, request, cancellationToken);

            return result.ToApiResult();
        });

        group.MapDelete("/{id}", async (
            string id,
            IProductService productService,
            CancellationToken cancellationToken) =>
        {
            var result = await productService.DeleteAsync(id, cancellationToken);

            return result.ToApiResult();
        });

        return app;
    }
}
