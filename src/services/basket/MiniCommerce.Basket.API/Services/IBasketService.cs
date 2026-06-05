using MiniCommerce.Basket.API.DTOs;
using MiniCommerce.Shared.Common;

namespace MiniCommerce.Basket.API.Services;

public interface IBasketService
{
    Task<ServiceResult<BasketResponse>> GetBasketAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<BasketResponse>> AddItemAsync(
        string customerId,
        AddBasketItemRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<BasketResponse>> UpdateItemQuantityAsync(
        string customerId,
        string productId,
        UpdateBasketItemQuantityRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<BasketResponse>> RemoveItemAsync(
        string customerId,
        string productId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> ClearBasketAsync(
        string customerId,
        CancellationToken cancellationToken = default);
}
