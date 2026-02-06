namespace eyewearshop_data.Entities;

public class RxLensSpecFeature
{
    public long ProductId { get; set; }
    public long FeatureId { get; set; }

    public RxLensSpec RxLensSpec { get; set; } = null!;
    public Feature Feature { get; set; } = null!;
}

