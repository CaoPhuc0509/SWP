namespace eyewearshop_data.Entities;

public class ReturnRequest
{
    public long ReturnRequestId { get; set; }
    public long OrderId { get; set; }
    public long CustomerId { get; set; }
    
    public string RequestType { get; set; } = null!; // EXCHANGE, RETURN, WARRANTY
    public string RequestNumber { get; set; } = null!; // Unique return request number
    public short Status { get; set; } = 0; // Using OrderStatuses constants
    
    public string? Reason { get; set; }
    public string? Description { get; set; }
    public string? StaffNotes { get; set; }
    
    // For exchanges
    public long? ExchangeOrderId { get; set; } // New order created for exchange
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public Order Order { get; set; } = null!;
    public User Customer { get; set; } = null!;
    public Order? ExchangeOrder { get; set; }
    public ICollection<ReturnRequestItem> Items { get; set; } = new List<ReturnRequestItem>();
}