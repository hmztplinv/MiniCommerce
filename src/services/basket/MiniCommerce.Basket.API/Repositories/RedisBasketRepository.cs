using System.Text.Json;
using MiniCommerce.Basket.API.Models;
using StackExchange.Redis;

namespace MiniCommerce.Basket.API.Repositories;

public sealed class RedisBasketRepository : IBasketRepository
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisBasketRepository(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<ShoppingCart?> GetAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var database = _connectionMultiplexer.GetDatabase();
        var basketJson = await database.StringGetAsync(GetBasketKey(customerId));

        if (!basketJson.HasValue)
        {
            return null;
        }

        return JsonSerializer.Deserialize<ShoppingCart>(
            basketJson!,
            JsonSerializerOptions);
    }

    public async Task<ShoppingCart> UpsertAsync(
        ShoppingCart shoppingCart,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var database = _connectionMultiplexer.GetDatabase();

        var basketJson = JsonSerializer.Serialize(
            shoppingCart,
            JsonSerializerOptions);

        await database.StringSetAsync(
            GetBasketKey(shoppingCart.CustomerId),
            basketJson);

        return shoppingCart;
    }

    public async Task<bool> DeleteAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var database = _connectionMultiplexer.GetDatabase();

        return await database.KeyDeleteAsync(GetBasketKey(customerId));
    }

    private static string GetBasketKey(string customerId)
    {
        return $"basket:{customerId}";
    }
}
