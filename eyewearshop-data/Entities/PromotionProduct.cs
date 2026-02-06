namespace eyewearshop_data.Entities;

public class PromotionProduct
{
    public long PromotionProductId { get; set; }
    public long PromotionId { get; set; }
    public long? ProductId { get; set; } // null means applies to all products
    public long? CategoryId { get; set; } // null means applies to all categories
    
    public Promotion Promotion { get; set; } = null!;
    public Product? Product { get; set; }
    public Category? Category { get; set; }
}