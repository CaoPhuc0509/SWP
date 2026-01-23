namespace eyewearshop_data.Entities;

public class RefreshToken
{
    public long RefreshTokenId { get; set; }
    public long UserId { get; set; }
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;

    public User User { get; set; } = null!;
}
