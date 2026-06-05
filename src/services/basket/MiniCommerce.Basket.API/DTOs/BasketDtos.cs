namespace MiniCommerce.Basket.API.DTOs;

public sealed record BasketItemResponse(
    string ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice);

public sealed record BasketResponse(
    string CustomerId,
    IReadOnlyList<BasketItemResponse> Items,
    decimal TotalPrice);

public sealed record AddBasketItemRequest(
    string ProductId,
    int Quantity);

public sealed record UpdateBasketItemQuantityRequest(
    int Quantity);
