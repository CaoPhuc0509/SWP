namespace eyewearshop_data.Entities;

public class ProductVariant
{
    public long VariantId { get; set; }
    public long ProductId { get; set; }
    
    // Variant-specific attributes
    public string? Color { get; set; } // For frames, sunglasses, and other colored products
    
    // For RxLens: Refractive index varies by variant
    public decimal? RefractiveIndex { get; set; } // e.g., 1.50, 1.59, 1.67, 1.74
    
    // For Contact Lenses: Base curve and diameter can vary slightly by variant
    public decimal? BaseCurve { get; set; } // Override product base curve if needed
    public decimal? Diameter { get; set; } // Override product diameter if needed
    
    // Pricing and inventory
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int PreOrderQuantity { get; set; }
    public DateTime? ExpectedDateRestock { get; set; }
    
    // Variant SKU (optional - for tracking specific variant)
    public string? VariantSku { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public short Status { get; set; } = 1;

    public Product Product { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
