namespace eyewearshop_data.Entities;

public class User
{
    public long UserId { get; set; }
    public int RoleId { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public short Status { get; set; } = 1;

    public Role Role { get; set; } = null!;
}
