namespace eyewearshop_data.Entities;

public class ReturnRequestItem
{
    public long ReturnRequestItemId { get; set; }
    public long ReturnRequestId { get; set; }
    public long OrderItemId { get; set; }
    public int Quantity { get; set; }
    
    public ReturnRequest ReturnRequest { get; set; } = null!;
    public OrderItem OrderItem { get; set; } = null!;
}