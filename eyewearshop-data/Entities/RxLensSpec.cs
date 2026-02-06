namespace eyewearshop_data.Entities;

public class RxLensSpec
{
    public long ProductId { get; set; }
    
    // Lens design and material (at product level)
    public string? DesignType { get; set; } // Single Vision, Progressive, Bifocal, etc.
    public string? Material { get; set; } // CR-39, Polycarbonate, High Index, etc.
    public decimal? LensWidth { get; set; } // Standard lens width
    
    // Prescription ranges (at product level - defines what prescriptions this lens can accommodate)
    public decimal? MinSphere { get; set; }
    public decimal? MaxSphere { get; set; }
    public decimal? MinCylinder { get; set; }
    public decimal? MaxCylinder { get; set; }
    public decimal? MinAxis { get; set; } // Usually 0-180
    public decimal? MaxAxis { get; set; }
    public decimal? MinAdd { get; set; } // For progressive/bifocal
    public decimal? MaxAdd { get; set; }
    
    // Coating options (can be multiple)
    public bool? HasAntiReflective { get; set; }
    public bool? HasBlueLightFilter { get; set; }
    public bool? HasUVProtection { get; set; }
    public bool? HasScratchResistant { get; set; }
    
    public short Status { get; set; } = 1;

    public Product Product { get; set; } = null!;
    public ICollection<RxLensSpecFeature> RxLensSpecFeatures { get; set; } = new List<RxLensSpecFeature>();
}
