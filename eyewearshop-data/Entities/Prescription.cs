namespace eyewearshop_data.Entities;

public class Prescription
{
    public long PrescriptionId { get; set; }
    public long CustomerId { get; set; }
    
    // Right eye (OD - Oculus Dexter)
    public decimal? RightSphere { get; set; }
    public decimal? RightCylinder { get; set; }
    public decimal? RightAxis { get; set; }
    public decimal? RightAdd { get; set; } // Addition for reading
    public decimal? RightPD { get; set; } // Pupillary Distance
    
    // Left eye (OS - Oculus Sinister)
    public decimal? LeftSphere { get; set; }
    public decimal? LeftCylinder { get; set; }
    public decimal? LeftAxis { get; set; }
    public decimal? LeftAdd { get; set; }
    public decimal? LeftPD { get; set; }
    
    // Additional information
    public string? Notes { get; set; }
    public DateTime? PrescriptionDate { get; set; }
    public string? PrescribedBy { get; set; } // Doctor name or clinic
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public short Status { get; set; } = 1; // 1 = active, 0 = inactive
    
    public User Customer { get; set; } = null!;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}