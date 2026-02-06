namespace eyewearshop_data.Entities;

public class ProductImage
{
    public long ImageId { get; set; }
    public long ProductId { get; set; }
    public long? VariantId { get; set; }
    public string Url { get; set; } = null!;
    public int SortOrder { get; set; } = 0;
    public bool IsPrimary { get; set; } = false;
    public short Status { get; set; } = 1;

    public Product Product { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
}
