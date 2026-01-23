namespace eyewearshop_data.Entities;

public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = null!;
    public string? Description { get; set; }
    public short Status { get; set; } = 1;

    public ICollection<User> Users { get; set; } = new List<User>();
}
