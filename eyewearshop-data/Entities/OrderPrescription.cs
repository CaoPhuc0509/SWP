namespace eyewearshop_data.Entities;

/// <summary>
/// Snapshot of the prescription used for a specific order.
/// This must NOT be affected if the customer later edits/deletes their saved Prescription.
/// </summary>
public class OrderPrescription
{
    public long OrderId { get; set; }
    public long CustomerId { get; set; }

    // Optional reference to the user's saved prescription id at checkout time (NO FK on purpose)
    public long? SavedPrescriptionId { get; set; }

    // Right eye (OD)
    public decimal? RightSphere { get; set; }
    public decimal? RightCylinder { get; set; }
    public decimal? RightAxis { get; set; }
    public decimal? RightAdd { get; set; }
    public decimal? RightPD { get; set; }

    // Left eye (OS)
    public decimal? LeftSphere { get; set; }
    public decimal? LeftCylinder { get; set; }
    public decimal? LeftAxis { get; set; }
    public decimal? LeftAdd { get; set; }
    public decimal? LeftPD { get; set; }

    public string? Notes { get; set; }
    public DateTime? PrescriptionDate { get; set; }
    public string? PrescribedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public Order Order { get; set; } = null!;
    public User Customer { get; set; } = null!;
}

