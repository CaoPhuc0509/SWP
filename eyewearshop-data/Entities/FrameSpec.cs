namespace eyewearshop_data.Entities;

public class FrameSpec
{
    public long ProductId { get; set; }
    
    // Frame structure
    public string? RimType { get; set; } // Full Rim, Semi-Rimless, Rimless
    public string? Material { get; set; } // Acetate, Metal, Titanium, etc.
    public string? Shape { get; set; } // Rectangle, Round, Aviator, etc.
    public decimal? Weight { get; set; } // in grams
    
    // Frame dimensions (at product level - these define the frame model)
    public decimal? A { get; set; } // Frame width (eye size)
    public decimal? B { get; set; } // Frame height
    public decimal? Dbl { get; set; } // Bridge width (distance between lenses)
    public decimal? TempleLength { get; set; } // Temple/arm length
    public decimal? LensWidth { get; set; } // Overall lens width
    
    // Additional specifications
    public string? HingeType { get; set; }
    public bool? HasNosePads { get; set; }
    
    public short Status { get; set; } = 1;

    public Product Product { get; set; } = null!;
}
