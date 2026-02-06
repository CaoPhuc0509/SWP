namespace eyewearshop_data.Entities;

public class Promotion
{
    public long PromotionId { get; set; }
    public string PromotionName { get; set; } = null!;
    public string? Description { get; set; }
    
    public string PromotionType { get; set; } = null!; // PERCENTAGE, FIXED_AMOUNT, BUY_X_GET_Y
    public decimal? DiscountPercentage { get; set; }
    public decimal? DiscountAmount { get; set; }
    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    public decimal? MinimumPurchaseAmount { get; set; }
    public int? MaximumUsagePerCustomer { get; set; }
    public int? TotalUsageLimit { get; set; }
    public int CurrentUsageCount { get; set; } = 0;
    
    public string? PromoCode { get; set; } // Optional promo code
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public short Status { get; set; } = 1; // 1 = active, 0 = inactive
    
    public ICollection<PromotionProduct> Products { get; set; } = new List<PromotionProduct>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}