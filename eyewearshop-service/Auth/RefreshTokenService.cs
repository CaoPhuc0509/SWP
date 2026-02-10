using System.Security.Cryptography;
using eyewearshop_data.Entities;
using eyewearshop_data.Interfaces;
using eyewearshop_service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace eyewearshop_service.Auth;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRepository<RefreshToken> _refreshTokenRepository;
    private readonly JwtSettings _settings;

    public RefreshTokenService(IRepository<RefreshToken> refreshTokenRepository, IOptions<JwtSettings> options)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _settings = options.Value;
    }

    public async Task<RefreshToken> IssueAsync(long userId, CancellationToken ct = default)
    {
        var tokenValue = GenerateToken();
        var now = DateTime.UtcNow;
        var rt = new RefreshToken
        {
            UserId = userId,
            Token = tokenValue,
            CreatedAt = now,
            ExpiresAt = now.AddDays(_settings.RefreshTokenExpirationDays)
        };

        await _refreshTokenRepository.AddAsync(rt, ct);
        await _refreshTokenRepository.SaveChangesAsync(ct);
        return rt;
    }

    public async Task<(RefreshToken? newToken, string? error)> RotateAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return (null, "Refresh token is required.");

        var existing = await _refreshTokenRepository
            .Query()
            .FirstOrDefaultAsync(x => x.Token == refreshToken, ct);

        if (existing == null) return (null, "Invalid refresh token.");
        if (existing.RevokedAt != null) return (null, "Refresh token has been revoked.");
        if (existing.ExpiresAt <= DateTime.UtcNow) return (null, "Refresh token has expired.");

        existing.RevokedAt = DateTime.UtcNow;

        var replacement = new RefreshToken
        {
            UserId = existing.UserId,
            Token = GenerateToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_settings.RefreshTokenExpirationDays)
        };

        await _refreshTokenRepository.AddAsync(replacement, ct);
        await _refreshTokenRepository.SaveChangesAsync(ct);

        return (replacement, null);
    }

    public async Task<bool> RevokeAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return false;

        var existing = await _refreshTokenRepository
            .Query()
            .FirstOrDefaultAsync(x => x.Token == refreshToken, ct);

        if (existing == null) return false;
        if (existing.RevokedAt != null) return true;

        existing.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.SaveChangesAsync(ct);
        return true;
    }

    private static string GenerateToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
