namespace eyewearshop_data.Entities;

public class Brand
{
    public long BrandId { get; set; }
    public string BrandName { get; set; } = null!;
    public short Status { get; set; } = 1;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
