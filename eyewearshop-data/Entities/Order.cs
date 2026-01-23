namespace eyewearshop_data.Entities;

public class Order
{
    public long OrderId { get; set; }
    public long CustomerId { get; set; }

    public string OrderNumber { get; set; } = null!;
    public short Status { get; set; } = 0;

    public long? PrescriptionId { get; set; }

    public decimal SubTotal { get; set; } = 0;
    public decimal ShippingFee { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    public decimal TotalAmount { get; set; } = 0;

    public long? AssignedSaleStaffId { get; set; }
    public long? AssignedOpStaffId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User Customer { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
