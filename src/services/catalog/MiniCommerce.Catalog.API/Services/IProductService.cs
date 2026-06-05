using MiniCommerce.Catalog.API.DTOs;
using MiniCommerce.Shared.Common;

namespace MiniCommerce.Catalog.API.Services;

public interface IProductService
{
    Task<ServiceResult<IReadOnlyList<ProductResponse>>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<ServiceResult<ProductResponse>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProductResponse>> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<ProductResponse>> UpdateAsync(
        string id,
        UpdateProductRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult> DeleteAsync(
        string id,
        CancellationToken cancellationToken);
}
