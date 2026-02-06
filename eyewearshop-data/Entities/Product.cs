namespace eyewearshop_data.Entities;

public class Product
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string Sku { get; set; } = null!;
    public string? Description { get; set; }

    public long? CategoryId { get; set; }
    public long? BrandId { get; set; }

    public string ProductType { get; set; } = null!;
    public decimal? BasePrice { get; set; }
    public string? Specifications { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public short Status { get; set; } = 1;

    public Category? Category { get; set; }
    public Brand? Brand { get; set; }

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    
    // Product type-specific specifications
    public SunglassesSpec? SunglassesSpec { get; set; }
    public FrameSpec? FrameSpec { get; set; }
    public RxLensSpec? RxLensSpec { get; set; }
    public ContactLensSpec? ContactLensSpec { get; set; }
    
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
