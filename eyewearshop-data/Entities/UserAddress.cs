namespace eyewearshop_data.Entities;

public class UserAddress
{
    public long AddressId { get; set; }
    public long CustomerId { get; set; }
    public string? RecipientName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Ward { get; set; }
    public string? Note { get; set; }

    /// <summary>GHN district ID used for shipping fee calculation.</summary>
    public int? GhnDistrictId { get; set; }

    /// <summary>GHN ward code used for shipping fee calculation.</summary>
    public string? GhnWardCode { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public short Status { get; set; } = 1;

    public User Customer { get; set; } = null!;
}
