using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace eyewearshop_service.Cart;

public class SessionCartService : ISessionCartService
{
    private const string CartSessionKey = "ShoppingCart";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionCartService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public List<CartItemDto> GetCart()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null) return new List<CartItemDto>();

        byte[]? cartBytes = null;
        if (session.TryGetValue(CartSessionKey, out cartBytes) && cartBytes != null)
        {
            var cartJson = Encoding.UTF8.GetString(cartBytes);
            try
            {
                return JsonSerializer.Deserialize<List<CartItemDto>>(cartJson) ?? new List<CartItemDto>();
            }
            catch
            {
                return new List<CartItemDto>();
            }
        }

        return new List<CartItemDto>();
    }

    public void SaveCart(List<CartItemDto> cart)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null) return;

        var cartJson = JsonSerializer.Serialize(cart);
        var cartBytes = Encoding.UTF8.GetBytes(cartJson);
        session.Set(CartSessionKey, cartBytes);
    }

    public void ClearCart()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        session?.Remove(CartSessionKey);
    }

    public void AddItem(long variantId, int quantity)
    {
        var cart = GetCart();
        var existingItem = cart.FirstOrDefault(x => x.VariantId == variantId);

        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            cart.Add(new CartItemDto
            {
                VariantId = variantId,
                Quantity = quantity
            });
        }

        SaveCart(cart);
    }

    public bool UpdateItem(long variantId, int quantity)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(x => x.VariantId == variantId);

        if (item == null) return false;

        item.Quantity = quantity;
        SaveCart(cart);
        return true;
    }

    public bool RemoveItem(long variantId)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(x => x.VariantId == variantId);

        if (item == null) return false;

        cart.Remove(item);
        SaveCart(cart);
        return true;
    }
}

public class CartItemDto
{
    public long VariantId { get; set; }
    public int Quantity { get; set; }
}