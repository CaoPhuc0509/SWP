using eyewearshop_data.Entities;

namespace eyewearshop_service.Interfaces;

public interface IRefreshTokenService
{
    Task<RefreshToken> IssueAsync(long userId, CancellationToken ct = default);
    Task<(RefreshToken? newToken, string? error)> RotateAsync(string refreshToken, CancellationToken ct = default);
    Task<bool> RevokeAsync(string refreshToken, CancellationToken ct = default);
}

