using Microsoft.Extensions.Options;
using MiniCommerce.Basket.API.Clients;
using MiniCommerce.Basket.API.HealthChecks;
using MiniCommerce.Basket.API.Options;
using MiniCommerce.Basket.API.Repositories;
using MiniCommerce.Basket.API.Services;
using StackExchange.Redis;

namespace MiniCommerce.Basket.API.Extensions;

public static class BasketInfrastructureExtensions
{
    public static IServiceCollection AddBasketInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "Redis connection string is required.")
            .ValidateOnStart();

        services.AddOptions<CatalogOptions>()
            .Bind(configuration.GetSection(CatalogOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.BaseUrl),
                "Catalog base URL is required.")
            .ValidateOnStart();

        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var redisOptions = serviceProvider
                .GetRequiredService<IOptions<RedisOptions>>()
                .Value;

            return ConnectionMultiplexer.Connect(redisOptions.ConnectionString);
        });

        services.AddHttpClient<ICatalogClient, CatalogClient>((serviceProvider, httpClient) =>
        {
            var catalogOptions = serviceProvider
                .GetRequiredService<IOptions<CatalogOptions>>()
                .Value;

            httpClient.BaseAddress = new Uri(catalogOptions.BaseUrl);
        });

        services.AddScoped<IBasketRepository, RedisBasketRepository>();
        services.AddScoped<IBasketService, BasketService>();

        services.AddHealthChecks()
            .AddCheck<RedisHealthCheck>("redis");

        return services;
    }
}
