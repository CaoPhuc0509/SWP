namespace eyewearshop_data.Entities;

public class SunglassesSpec
{
    public long ProductId { get; set; }
    
    // Frame specifications (same as regular frames)
    public string? RimType { get; set; }
    public string? Material { get; set; }
    public decimal? A { get; set; } // Frame width
    public decimal? B { get; set; } // Frame height
    public decimal? Dbl { get; set; } // Bridge width
    public decimal? TempleLength { get; set; } // Temple/arm length
    public string? Shape { get; set; }
    public decimal? Weight { get; set; }
    
    // Lens specifications
    public string? LensMaterial { get; set; } // Polycarbonate, CR-39, etc.
    public string? LensType { get; set; } // Polarized, Gradient, Mirror, etc.
    public int? UvProtection { get; set; } // UV400, etc.
    public string? TintColor { get; set; } // General tint description
    
    public short Status { get; set; } = 1;
    
    public Product Product { get; set; } = null!;
}