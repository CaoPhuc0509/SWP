namespace eyewearshop_data.Entities;

public class ProductVariant
{
    public long VariantId { get; set; }
    public long ProductId { get; set; }
    public string? Color { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int PreOrderQuantity { get; set; }
    public DateTime? ExpectedDateRestock { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public short Status { get; set; } = 1;

    public Product Product { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
