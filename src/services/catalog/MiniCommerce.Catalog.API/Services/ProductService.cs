using MiniCommerce.Catalog.API.DTOs;
using MiniCommerce.Catalog.API.Entities;
using MiniCommerce.Catalog.API.Repositories;
using MiniCommerce.Shared.Common;
using MongoDB.Bson;

namespace MiniCommerce.Catalog.API.Services;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository productRepository,
        ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<IReadOnlyList<ProductResponse>>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);

        var response = products
            .Select(MapToResponse)
            .ToList();

        return ServiceResult<IReadOnlyList<ProductResponse>>.Success(response);
    }

    public async Task<ServiceResult<ProductResponse>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var idValidationResult = ValidateProductId(id);

        if (idValidationResult is not null)
        {
            return ServiceResult<ProductResponse>.Fail(idValidationResult);
        }

        var product = await _productRepository.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            return ServiceResult<ProductResponse>.Fail(
                new Error("Product.NotFound", "Product was not found."));
        }

        return ServiceResult<ProductResponse>.Success(MapToResponse(product));
    }

    public async Task<ServiceResult<ProductResponse>> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateProductInput(
            request.Name,
            request.Description,
            request.Price,
            request.Stock);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<ProductResponse>.ValidationFail(validationErrors);
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            Stock = request.Stock,
            CreatedAt = DateTime.UtcNow
        };

        await _productRepository.CreateAsync(product, cancellationToken);

        _logger.LogInformation("Product created. ProductId: {ProductId}", product.Id);

        return ServiceResult<ProductResponse>.Success(MapToResponse(product));
    }

    public async Task<ServiceResult<ProductResponse>> UpdateAsync(
        string id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var idValidationResult = ValidateProductId(id);

        if (idValidationResult is not null)
        {
            return ServiceResult<ProductResponse>.Fail(idValidationResult);
        }

        var validationErrors = ValidateProductInput(
            request.Name,
            request.Description,
            request.Price,
            request.Stock);

        if (validationErrors.Count > 0)
        {
            return ServiceResult<ProductResponse>.ValidationFail(validationErrors);
        }

        var product = await _productRepository.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            return ServiceResult<ProductResponse>.Fail(
                new Error("Product.NotFound", "Product was not found."));
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description.Trim();
        product.Price = request.Price;
        product.Stock = request.Stock;

        var updated = await _productRepository.UpdateAsync(product, cancellationToken);

        if (!updated)
        {
            return ServiceResult<ProductResponse>.Fail(
                new Error("Product.UpdateFailed", "Product could not be updated."));
        }

        _logger.LogInformation("Product updated. ProductId: {ProductId}", product.Id);

        return ServiceResult<ProductResponse>.Success(MapToResponse(product));
    }

    public async Task<ServiceResult> DeleteAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var idValidationResult = ValidateProductId(id);

        if (idValidationResult is not null)
        {
            return ServiceResult.Fail(idValidationResult);
        }

        var deleted = await _productRepository.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return ServiceResult.Fail(
                new Error("Product.NotFound", "Product was not found."));
        }

        _logger.LogInformation("Product deleted. ProductId: {ProductId}", id);

        return ServiceResult.Success();
    }

    private static Error? ValidateProductId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return new Error("Product.InvalidId", "Product id is required.");
        }

        if (!ObjectId.TryParse(id, out _))
        {
            return new Error("Product.InvalidId", "Product id format is invalid.");
        }

        return null;
    }

    private static ProductResponse MapToResponse(Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Stock,
            product.CreatedAt,
            product.UpdatedAt);
    }

    private static Dictionary<string, string[]> ValidateProductInput(
        string name,
        string description,
        decimal price,
        int stock)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = ["Product name is required."];
        }
        else if (name.Length > 100)
        {
            errors["name"] = ["Product name cannot exceed 100 characters."];
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            errors["description"] = ["Product description is required."];
        }
        else if (description.Length > 500)
        {
            errors["description"] = ["Product description cannot exceed 500 characters."];
        }

        if (price <= 0)
        {
            errors["price"] = ["Product price must be greater than zero."];
        }

        if (stock < 0)
        {
            errors["stock"] = ["Product stock cannot be negative."];
        }

        return errors;
    }
}
