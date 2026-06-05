namespace MiniCommerce.Basket.API.Clients;

public sealed record CatalogProductResponse(
    string Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
