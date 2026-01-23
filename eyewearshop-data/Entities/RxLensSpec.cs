namespace eyewearshop_data.Entities;

public class RxLensSpec
{
    public long ProductId { get; set; }
    public string? DesignType { get; set; }
    public string? Material { get; set; }
    public decimal? LensWidth { get; set; }
    public decimal? MinSphere { get; set; }
    public decimal? MaxSphere { get; set; }
    public decimal? MinCylinder { get; set; }
    public decimal? MaxCylinder { get; set; }
    public long? FeatureId { get; set; }
    public short Status { get; set; } = 1;

    public Product Product { get; set; } = null!;
    public Feature? Feature { get; set; }
}
