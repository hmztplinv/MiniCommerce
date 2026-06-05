namespace MiniCommerce.Basket.API.Clients;

public interface ICatalogClient
{
    Task<CatalogProductResponse?> GetProductByIdAsync(
        string productId,
        CancellationToken cancellationToken = default);
}
