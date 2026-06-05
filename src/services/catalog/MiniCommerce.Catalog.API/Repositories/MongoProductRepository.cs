using Microsoft.Extensions.Options;
using MiniCommerce.Catalog.API.Entities;
using MiniCommerce.Catalog.API.Options;
using MongoDB.Driver;

namespace MiniCommerce.Catalog.API.Repositories;

public sealed class MongoProductRepository : IProductRepository
{
    private readonly IMongoCollection<Product> _products;

    public MongoProductRepository(
        IMongoDatabase database,
        IOptions<MongoDbOptions> options)
    {
        _products = database.GetCollection<Product>(
            options.Value.ProductsCollectionName);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _products
            .Find(_ => true)
            .SortByDescending(product => product.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return await _products
            .Find(product => product.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(Product product, CancellationToken cancellationToken)
    {
        await _products.InsertOneAsync(product, cancellationToken: cancellationToken);
    }

    public async Task<bool> UpdateAsync(Product product, CancellationToken cancellationToken)
    {
        product.UpdatedAt = DateTime.UtcNow;

        var result = await _products.ReplaceOneAsync(
            existingProduct => existingProduct.Id == product.Id,
            product,
            cancellationToken: cancellationToken);

        return result.ModifiedCount == 1;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _products.DeleteOneAsync(
            product => product.Id == id,
            cancellationToken);

        return result.DeletedCount == 1;
    }
}
