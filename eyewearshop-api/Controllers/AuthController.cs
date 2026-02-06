using System.Security.Claims;
using eyewearshop_data;
using eyewearshop_data.Entities;
using eyewearshop_service;
using eyewearshop_service.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly EyewearShopDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IRefreshTokenService _refreshTokens;

    public AuthController(EyewearShopDbContext db, IJwtTokenService jwt, IRefreshTokenService refreshTokens)
    {
        _db = db;
        _jwt = jwt;
        _refreshTokens = refreshTokens;
    }

    public record RegisterRequest(string Email, string Password, string? FullName, string? PhoneNumber);
    public record LoginRequest(string Email, string Password);
    public record RefreshRequest(string RefreshToken);
    public record LogoutRequest(string RefreshToken);

    public record AuthResponse(string AccessToken, string RefreshToken, long UserId, string Email, string Role);

    /// <summary>
    /// Register a new customer account and return access/refresh tokens.
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Email and password are required.");

        var exists = await _db.Users.AnyAsync(u => u.Email.ToLower() == email, ct);
        if (exists) return Conflict("Email already registered.");

        var customerRole = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == RoleNames.Customer, ct);
        if (customerRole == null)
            return StatusCode(500, "Default role 'Customer' is missing in roles table.");

        var now = DateTime.UtcNow;
        var user = new User
        {
            Email = email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            RoleId = customerRole.RoleId,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var accessToken = _jwt.CreateAccessToken(user, customerRole.RoleName);
        var refreshToken = await _refreshTokens.IssueAsync(user.UserId, ct);

        return Ok(new AuthResponse(accessToken, refreshToken.Token, user.UserId, user.Email, customerRole.RoleName));
    }

    /// <summary>
    /// Login with email/password and return access/refresh tokens.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);

        if (user == null) return Unauthorized("Invalid credentials.");
        if (user.Status != 1) return Unauthorized("User is not active.");

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials.");

        var roleName = user.Role.RoleName;
        var accessToken = _jwt.CreateAccessToken(user, roleName);
        var refreshToken = await _refreshTokens.IssueAsync(user.UserId, ct);

        return Ok(new AuthResponse(accessToken, refreshToken.Token, user.UserId, user.Email, roleName));
    }

    /// <summary>
    /// Rotate a refresh token and issue a new access token.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<object>> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var (newToken, error) = await _refreshTokens.RotateAsync(request.RefreshToken, ct);
        if (newToken == null) return Unauthorized(error ?? "Invalid refresh token.");

        var user = await _db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == newToken.UserId, ct);

        if (user == null) return Unauthorized("User not found.");
        if (user.Status != 1) return Unauthorized("User is not active.");

        var accessToken = _jwt.CreateAccessToken(user, user.Role.RoleName);

        return Ok(new
        {
            AccessToken = accessToken,
            RefreshToken = newToken.Token
        });
    }

    /// <summary>
    /// Revoke a refresh token (logout).
    /// </summary>
    [HttpPost("logout")]
    public async Task<ActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        var ok = await _refreshTokens.RevokeAsync(request.RefreshToken, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Get the current authenticated user's profile (basic fields + role).
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<object>> Me(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _db.Users.Include(u => u.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId, ct);

        if (user == null) return NotFound();

        return Ok(new
        {
            user.UserId,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            Role = user.Role.RoleName
        });
    }

    /// <summary>
    /// Admin-only test endpoint.
    /// </summary>
    [Authorize(Roles = RoleNames.Admin)]
    [HttpGet("admin-only")]
    public ActionResult<object> AdminOnly()
    {
        return Ok(new { Message = "You are an admin." });
    }
}
