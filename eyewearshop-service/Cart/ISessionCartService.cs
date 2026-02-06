namespace eyewearshop_service.Cart;

public interface ISessionCartService
{
    List<CartItemDto> GetCart();
    void SaveCart(List<CartItemDto> cart);
    void ClearCart();
    void AddItem(long variantId, int quantity);
    bool UpdateItem(long variantId, int quantity);
    bool RemoveItem(long variantId);
}