using Microsoft.Extensions.Options;
using MiniCommerce.Catalog.API.Options;
using MiniCommerce.Catalog.API.Repositories;
using MiniCommerce.Catalog.API.Services;
using MongoDB.Driver;

namespace MiniCommerce.Catalog.API.Extensions;

public static class CatalogInfrastructureExtensions
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<MongoDbOptions>()
            .Bind(configuration.GetSection(MongoDbOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "MongoDb:ConnectionString is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DatabaseName),
                "MongoDb:DatabaseName is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ProductsCollectionName),
                "MongoDb:ProductsCollectionName is required.")
            .ValidateOnStart();

        services.AddSingleton<IMongoClient>(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<MongoDbOptions>>()
                .Value;

            return new MongoClient(options.ConnectionString);
        });

        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<MongoDbOptions>>()
                .Value;

            var mongoClient = serviceProvider.GetRequiredService<IMongoClient>();

            return mongoClient.GetDatabase(options.DatabaseName);
        });

        services.AddScoped<IProductRepository, MongoProductRepository>();
        services.AddScoped<IProductService, ProductService>();

        return services;
    }
}
