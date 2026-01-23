namespace eyewearshop_data.Entities;

public class Category
{
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public short Status { get; set; } = 1;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
