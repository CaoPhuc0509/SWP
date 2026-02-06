namespace eyewearshop_data.Entities;

public class ContactLensSpec
{
    public long ProductId { get; set; }
    
    // Lens physical specifications (at product level - defines the lens model)
    public decimal? BaseCurve { get; set; } // Base curve in mm (e.g., 8.4, 8.6)
    public decimal? Diameter { get; set; } // Diameter in mm (e.g., 14.0, 14.2)
    
    // Prescription ranges (at product level - defines what prescriptions this lens can accommodate)
    public decimal? MinSphere { get; set; }
    public decimal? MaxSphere { get; set; }
    public decimal? MinCylinder { get; set; }
    public decimal? MaxCylinder { get; set; }
    public decimal? MinAxis { get; set; }
    public decimal? MaxAxis { get; set; }
    
    // Lens type and material (at product level)
    public string? LensType { get; set; } // Daily, Monthly, Extended Wear, etc.
    public string? Material { get; set; } // Silicone Hydrogel, Hydrogel, etc.
    public int? WaterContent { get; set; } // Percentage
    public int? OxygenPermeability { get; set; } // Dk/t value
    
    // Usage specifications
    public int? ReplacementSchedule { get; set; } // Days (1, 30, 90, etc.)
    public bool? IsToric { get; set; } // For astigmatism correction
    public bool? IsMultifocal { get; set; }
    
    public short Status { get; set; } = 1;

    public Product Product { get; set; } = null!;
}
