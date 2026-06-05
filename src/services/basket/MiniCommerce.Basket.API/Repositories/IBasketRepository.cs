using MiniCommerce.Basket.API.Models;

namespace MiniCommerce.Basket.API.Repositories;

public interface IBasketRepository
{
    Task<ShoppingCart?> GetAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    Task<ShoppingCart> UpsertAsync(
        ShoppingCart shoppingCart,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string customerId,
        CancellationToken cancellationToken = default);
}
