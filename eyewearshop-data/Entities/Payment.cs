namespace eyewearshop_data.Entities;

public class Payment
{
    public long PaymentId { get; set; }
    public long? OrderId { get; set; }
    public long? CustomerId { get; set; }
    public string? PaymentType { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public short Status { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Order? Order { get; set; }
    public User? Customer { get; set; }
}
