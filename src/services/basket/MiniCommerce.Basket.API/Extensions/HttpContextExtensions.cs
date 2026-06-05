namespace MiniCommerce.Basket.API.Extensions;

public static class HttpContextExtensions
{
    public const string CustomerIdHeaderName = "X-Customer-Id";

    public static string? GetCustomerId(this HttpContext httpContext)
    {
        if (!httpContext.Request.Headers.TryGetValue(CustomerIdHeaderName, out var customerIdHeader))
        {
            return null;
        }

        var customerId = customerIdHeader.ToString();

        if (string.IsNullOrWhiteSpace(customerId))
        {
            return null;
        }

        return customerId.Trim();
    }
}
