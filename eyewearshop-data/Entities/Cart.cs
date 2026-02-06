namespace eyewearshop_data.Entities;

public class Cart
{
    public long CartId { get; set; }
    public long CustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User Customer { get; set; } = null!;
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
