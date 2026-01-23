namespace eyewearshop_data.Entities;

public class ContactLensSpec
{
    public long ProductId { get; set; }
    public decimal? MinSphere { get; set; }
    public decimal? MaxSphere { get; set; }
    public decimal? MinCylinder { get; set; }
    public decimal? MaxCylinder { get; set; }
    public decimal? BaseCurve { get; set; }
    public decimal? Diameter { get; set; }
    public short Status { get; set; } = 1;

    public Product Product { get; set; } = null!;
}
