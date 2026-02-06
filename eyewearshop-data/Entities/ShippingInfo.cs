namespace eyewearshop_data.Entities;

public class ShippingInfo
{
    public long ShippingInfoId { get; set; }
    public long OrderId { get; set; }
    
    public string RecipientName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string AddressLine { get; set; } = null!;
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Ward { get; set; }
    public string? PostalCode { get; set; }
    public string? Note { get; set; }
    
    // Shipping details
    public string? ShippingMethod { get; set; } // Standard, Express, etc.
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; } // Shipping company
    public DateTime? ShippedAt { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public DateTime? DeliveredAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public Order Order { get; set; } = null!;
}