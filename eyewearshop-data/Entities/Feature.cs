namespace eyewearshop_data.Entities;

public class Feature
{
    public long FeatureId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public short Status { get; set; } = 1;

    public ICollection<RxLensSpecFeature> RxLensSpecFeatures { get; set; } = new List<RxLensSpecFeature>();
}
