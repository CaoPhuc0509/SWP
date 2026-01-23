namespace eyewearshop_data.Entities;

public class FrameSpec
{
    public long ProductId { get; set; }
    public string? RimType { get; set; }
    public string? Material { get; set; }
    public decimal? A { get; set; }
    public decimal? B { get; set; }
    public decimal? Dbl { get; set; }
    public string? Shape { get; set; }
    public decimal? Weight { get; set; }
    public short Status { get; set; } = 1;

    public Product Product { get; set; } = null!;
}
