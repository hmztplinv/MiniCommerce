using MiniCommerce.Basket.API.DTOs;
using MiniCommerce.Basket.API.Extensions;
using MiniCommerce.Basket.API.Services;
using MiniCommerce.Shared.Common;

namespace MiniCommerce.Basket.API.Endpoints;

public static class BasketEndpoints
{
    public static IEndpointRouteBuilder MapBasketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/basket")
            .WithTags("Basket");

        group.MapGet("/", async (
            HttpContext httpContext,
            IBasketService basketService,
            CancellationToken cancellationToken) =>
        {
            var customerId = httpContext.GetCustomerId();

            if (customerId is null)
            {
                return CustomerIdMissingResult();
            }

            var result = await basketService.GetBasketAsync(
                customerId,
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapPost("/items", async (
            AddBasketItemRequest request,
            HttpContext httpContext,
            IBasketService basketService,
            CancellationToken cancellationToken) =>
        {
            var customerId = httpContext.GetCustomerId();

            if (customerId is null)
            {
                return CustomerIdMissingResult();
            }

            var result = await basketService.AddItemAsync(
                customerId,
                request,
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapPut("/items/{productId}", async (
            string productId,
            UpdateBasketItemQuantityRequest request,
            HttpContext httpContext,
            IBasketService basketService,
            CancellationToken cancellationToken) =>
        {
            var customerId = httpContext.GetCustomerId();

            if (customerId is null)
            {
                return CustomerIdMissingResult();
            }

            var result = await basketService.UpdateItemQuantityAsync(
                customerId,
                productId,
                request,
                cancellationToken);

            return result.ToHttpResult();
        });

        group.MapDelete("/", async (
            HttpContext httpContext,
            IBasketService basketService,
            CancellationToken cancellationToken) =>
        {
            var customerId = httpContext.GetCustomerId();

            if (customerId is null)
            {
                return CustomerIdMissingResult();
            }

            var result = await basketService.ClearBasketAsync(
                customerId,
                cancellationToken);

            return result.ToHttpResult();
        });

        return app;
    }

    private static IResult CustomerIdMissingResult()
    {
        return ServiceResult.ValidationFail(
            new Dictionary<string, string[]>
            {
                [HttpContextExtensions.CustomerIdHeaderName] =
                [
                    "X-Customer-Id header is required."
                ]
            }).ToHttpResult();
    }
}
