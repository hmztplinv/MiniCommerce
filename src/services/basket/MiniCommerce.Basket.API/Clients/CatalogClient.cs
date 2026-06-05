using System.Net;
using System.Net.Http.Json;

namespace MiniCommerce.Basket.API.Clients;

public sealed class CatalogClient : ICatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogClient> _logger;

    public CatalogClient(
        HttpClient httpClient,
        ILogger<CatalogClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CatalogProductResponse?> GetProductByIdAsync(
        string productId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"/api/products/{Uri.EscapeDataString(productId)}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogWarning(
                "Catalog API returned unsuccessful response. StatusCode: {StatusCode}, Body: {Body}",
                response.StatusCode,
                responseBody);

            response.EnsureSuccessStatusCode();
        }

        return await response.Content.ReadFromJsonAsync<CatalogProductResponse>(
            cancellationToken);
    }
}
