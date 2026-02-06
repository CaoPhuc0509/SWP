namespace eyewearshop_data.Entities;

public class ComboItem
{
    public long ComboItemId { get; set; }
    public long ComboId { get; set; }
    public long ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public bool IsRequired { get; set; } = true; // If false, customer can choose alternative
    
    public Combo Combo { get; set; } = null!;
    public Product Product { get; set; } = null!;
}