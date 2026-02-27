namespace eyewearshop_data.Entities;

public class Order
{
    public long OrderId { get; set; }
    public long CustomerId { get; set; }

    public string OrderNumber { get; set; } = null!;
    public string OrderType { get; set; } = null!; // AVAILABLE, PRE_ORDER, PRESCRIPTION
    public short Status { get; set; } = 0; // Using OrderStatuses constants
    public short PaymentStatus { get; set; } = PaymentStatuses.Unpaid; // Using PaymentStatuses constants

    public long? PromotionId { get; set; }

    public decimal SubTotal { get; set; } = 0;
    public decimal ShippingFee { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    public decimal TotalAmount { get; set; } = 0;

    public long? AssignedSaleStaffId { get; set; }
    public long? AssignedOpStaffId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User Customer { get; set; } = null!;
    public OrderPrescription? OrderPrescription { get; set; }
    public Promotion? Promotion { get; set; }
    public ShippingInfo? ShippingInfo { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    public ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();
    public ICollection<ReturnRequest> ExchangeOrders { get; set; } = new List<ReturnRequest>(); // Orders created from exchanges
}
