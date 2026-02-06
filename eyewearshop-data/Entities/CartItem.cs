namespace eyewearshop_data.Entities;

public class CartItem
{
    public long CartItemId { get; set; }
    public long CartId { get; set; }
    public long VariantId { get; set; }
    public int Quantity { get; set; }

    public Cart Cart { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
}
