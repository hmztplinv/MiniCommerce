namespace MiniCommerce.Catalog.API.DTOs;

public sealed record ProductResponse(
    string Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int Stock);

public sealed record UpdateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int Stock);
