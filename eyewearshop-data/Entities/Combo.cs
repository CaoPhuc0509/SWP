namespace eyewearshop_data.Entities;

public class Combo
{
    public long ComboId { get; set; }
    public string ComboName { get; set; } = null!;
    public string? Description { get; set; }
    
    public decimal ComboPrice { get; set; }
    public decimal? OriginalPrice { get; set; } // Sum of individual items
    
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public short Status { get; set; } = 1; // 1 = active, 0 = inactive
    
    public ICollection<ComboItem> Items { get; set; } = new List<ComboItem>();
}