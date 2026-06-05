namespace MiniCommerce.Basket.API.Models;

public sealed class ShoppingCart
{
    public string CustomerId { get; set; } = string.Empty;

    public List<ShoppingCartItem> Items { get; set; } = [];

    public decimal TotalPrice => Items.Sum(item => item.TotalPrice);
}
