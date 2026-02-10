using System.Security.Claims;
using eyewearshop_data;
using eyewearshop_service.Auth;
using eyewearshop_service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtTokenService _jwt;
    private readonly IRefreshTokenService _refreshTokens;

    public AuthController(IAuthService authService, IJwtTokenService jwt, IRefreshTokenService refreshTokens)
    {
        _authService = authService;
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
        var (user, roleName, error, statusCode) = await _authService.RegisterCustomerAsync(
            request.Email,
            request.Password,
            request.FullName,
            request.PhoneNumber,
            ct);

        if (error != null)
        {
            return StatusCode(statusCode ?? 400, error);
        }

        var accessToken = _jwt.CreateAccessToken(user, roleName);
        var refreshToken = await _refreshTokens.IssueAsync(user.UserId, ct);

        return Ok(new AuthResponse(accessToken, refreshToken.Token, user.UserId, user.Email, roleName));
    }

    /// <summary>
    /// Login with email/password and return access/refresh tokens.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var (user, roleName, error, statusCode) = await _authService.LoginAsync(
            request.Email,
            request.Password,
            ct);

        if (error != null)
        {
            return StatusCode(statusCode ?? 401, error);
        }

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

        var (user, userError) = await _authService.GetUserByIdAsync(newToken.UserId, ct);
        if (user == null) return Unauthorized(userError ?? "User not found.");

        var accessToken = _jwt.CreateAccessToken(user, user.Role!.RoleName);

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

        var (user, error) = await _authService.GetUserByIdAsync(userId, ct);

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
