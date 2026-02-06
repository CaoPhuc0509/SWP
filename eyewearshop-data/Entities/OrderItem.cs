namespace eyewearshop_data.Entities;

public class OrderItem
{
    public long OrderItemId { get; set; }
    public long OrderId { get; set; }
    public long? VariantId { get; set; }
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; } // Price at time of order (for historical tracking)
    public int Quantity { get; set; } = 1;
    public decimal SubTotal { get; set; } // UnitPrice * Quantity
    public short Status { get; set; } = 1;

    public Order Order { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
    public ICollection<ReturnRequestItem> ReturnRequestItems { get; set; } = new List<ReturnRequestItem>();
}
