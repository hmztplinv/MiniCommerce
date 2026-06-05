using MiniCommerce.Basket.API.Clients;
using MiniCommerce.Basket.API.DTOs;
using MiniCommerce.Basket.API.Models;
using MiniCommerce.Basket.API.Repositories;
using MiniCommerce.Shared.Common;

namespace MiniCommerce.Basket.API.Services;

public sealed class BasketService : IBasketService
{
    private readonly IBasketRepository _basketRepository;
    private readonly ICatalogClient _catalogClient;

    public BasketService(
        IBasketRepository basketRepository,
        ICatalogClient catalogClient)
    {
        _basketRepository = basketRepository;
        _catalogClient = catalogClient;
    }

    public async Task<ServiceResult<BasketResponse>> GetBasketAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return ServiceResult<BasketResponse>.ValidationFail(
                new Dictionary<string, string[]>
                {
                    ["customerId"] = ["Customer id is required."]
                });
        }

        var shoppingCart = await _basketRepository.GetAsync(
            customerId,
            cancellationToken);

        shoppingCart ??= new ShoppingCart
        {
            CustomerId = customerId
        };

        return ServiceResult<BasketResponse>.Success(MapToResponse(shoppingCart));
    }

    public async Task<ServiceResult<BasketResponse>> AddItemAsync(
        string customerId,
        AddBasketItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateAddItemRequest(customerId, request);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<BasketResponse>.ValidationFail(validationErrors);
        }

        var product = await _catalogClient.GetProductByIdAsync(
            request.ProductId,
            cancellationToken);

        if (product is null)
        {
            return ServiceResult<BasketResponse>.Fail(
                new Error("Basket.ProductNotFound", "Product was not found."));
        }

        if (product.Stock <= 0)
        {
            return ServiceResult<BasketResponse>.Fail(
                new Error("Basket.ProductOutOfStock", "Product is out of stock."));
        }

        var shoppingCart = await _basketRepository.GetAsync(
            customerId,
            cancellationToken);

        shoppingCart ??= new ShoppingCart
        {
            CustomerId = customerId
        };

        var existingItem = shoppingCart.Items
            .FirstOrDefault(item => item.ProductId == product.Id);

        var requestedTotalQuantity = request.Quantity + (existingItem?.Quantity ?? 0);

        if (requestedTotalQuantity > product.Stock)
        {
            return ServiceResult<BasketResponse>.Fail(
                new Error("Basket.InsufficientStock", "Requested quantity exceeds available stock."));
        }

        if (existingItem is null)
        {
            shoppingCart.Items.Add(new ShoppingCartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = request.Quantity
            });
        }
        else
        {
            existingItem.ProductName = product.Name;
            existingItem.UnitPrice = product.Price;
            existingItem.Quantity = requestedTotalQuantity;
        }

        var updatedShoppingCart = await _basketRepository.UpsertAsync(
            shoppingCart,
            cancellationToken);

        return ServiceResult<BasketResponse>.Success(MapToResponse(updatedShoppingCart));
    }


    public async Task<ServiceResult<BasketResponse>> UpdateItemQuantityAsync(
        string customerId,
        string productId,
        UpdateBasketItemQuantityRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = ValidateUpdateItemQuantityRequest(
            customerId,
            productId,
            request);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<BasketResponse>.ValidationFail(validationErrors);
        }

        var shoppingCart = await _basketRepository.GetAsync(
            customerId,
            cancellationToken);

        if (shoppingCart is null)
        {
            return ServiceResult<BasketResponse>.Fail(
                new Error("Basket.NotFound", "Basket was not found."));
        }

        var existingItem = shoppingCart.Items
            .FirstOrDefault(item => item.ProductId == productId);

        if (existingItem is null)
        {
            return ServiceResult<BasketResponse>.Fail(
                new Error("Basket.ItemNotFound", "Basket item was not found."));
        }

        var product = await _catalogClient.GetProductByIdAsync(
            productId,
            cancellationToken);

        if (product is null)
        {
            return ServiceResult<BasketResponse>.Fail(
                new Error("Basket.ProductNotFound", "Product was not found."));
        }

        if (request.Quantity > product.Stock)
        {
            return ServiceResult<BasketResponse>.Fail(
                new Error("Basket.InsufficientStock", "Requested quantity exceeds available stock."));
        }

        existingItem.ProductName = product.Name;
        existingItem.UnitPrice = product.Price;
        existingItem.Quantity = request.Quantity;

        var updatedShoppingCart = await _basketRepository.UpsertAsync(
            shoppingCart,
            cancellationToken);

        return ServiceResult<BasketResponse>.Success(MapToResponse(updatedShoppingCart));
    }

    public async Task<ServiceResult> ClearBasketAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return ServiceResult.ValidationFail(
                new Dictionary<string, string[]>
                {
                    ["customerId"] = ["Customer id is required."]
                });
        }

        await _basketRepository.DeleteAsync(
            customerId,
            cancellationToken);

        return ServiceResult.Success();
    }

    private static Dictionary<string, string[]> ValidateAddItemRequest(
        string customerId,
        AddBasketItemRequest request)
    {
        var validationErrors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(customerId))
        {
            validationErrors["customerId"] = ["Customer id is required."];
        }

        if (string.IsNullOrWhiteSpace(request.ProductId))
        {
            validationErrors[nameof(request.ProductId)] = ["Product id is required."];
        }

        if (request.Quantity <= 0)
        {
            validationErrors[nameof(request.Quantity)] = ["Quantity must be greater than zero."];
        }

        return validationErrors;
    }

    private static Dictionary<string, string[]> ValidateUpdateItemQuantityRequest(
        string customerId,
        string productId,
        UpdateBasketItemQuantityRequest request)
    {
        var validationErrors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(customerId))
        {
            validationErrors["customerId"] = ["Customer id is required."];
        }

        if (string.IsNullOrWhiteSpace(productId))
        {
            validationErrors["productId"] = ["Product id is required."];
        }

        if (request.Quantity <= 0)
        {
            validationErrors[nameof(request.Quantity)] = ["Quantity must be greater than zero."];
        }

        return validationErrors;
    }

    private static BasketResponse MapToResponse(ShoppingCart shoppingCart)
    {
        var items = shoppingCart.Items
            .Select(item => new BasketItemResponse(
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity,
                item.TotalPrice))
            .ToList();

        return new BasketResponse(
            shoppingCart.CustomerId,
            items,
            shoppingCart.TotalPrice);
    }
}
