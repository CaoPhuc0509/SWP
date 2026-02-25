using eyewearshop_data;
using eyewearshop_data.Entities;
using eyewearshop_service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/manager/users")]
[Authorize(Roles = "Manager")]
public class UserManagerController : ControllerBase
{
    private readonly EyewearShopDbContext _db;

    public UserManagerController(EyewearShopDbContext db)
    {
        _db = db;
    }
    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAllUsers(CancellationToken ct = default)
    {
        var users = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Select(u => new
            {
                u.UserId,
                u.Email,
                u.FullName,
                u.PhoneNumber,
                u.Gender,
                u.DateOfBirth,
                Role = u.Role.RoleName,
                u.Status,
                u.CreatedAt,
                u.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(users);
    }

    /// <summary>
    /// Get staff list
    /// </summary>
    [HttpGet("staff")]
    public async Task<ActionResult> GetStaff(CancellationToken ct = default)
    {
        var staff = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
           .Where(u => u.Role.RoleId == 2 || u.Role.RoleId == 3)
            .Select(u => new
            {
                u.UserId,
                u.Email,
                u.FullName,
                u.PhoneNumber,
                Role = u.Role.RoleName,
                u.Status,
                u.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(staff);
    }

    /// <summary>
    /// Get customers list
    /// </summary>
    [HttpGet("customers")]
    public async Task<ActionResult> GetCustomers(CancellationToken ct = default)
    {
        var customers = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.Role.RoleId == 1)
            .Select(u => new
            {
                u.UserId,
                u.Email,
                u.FullName,
                u.PhoneNumber,
                u.Status,
                u.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(customers);
    }

    /// <summary>
    /// create staff account
    /// </summary>
    [HttpPost("staff")]
    public async Task<ActionResult> CreateStaff(
     [FromBody] CreateStaffRequest request,
     CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Email is required");

        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Password is required");

        if (request.RoleId != 2 && request.RoleId != 3)
            return BadRequest("Invalid staff role");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _db.Users
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, ct);

        if (existingUser)
            return BadRequest("Email already exists");

        var role = await _db.Roles
            .FirstOrDefaultAsync(r => r.RoleId == request.RoleId, ct);

        if (role == null)
            return BadRequest("Invalid role");

        var now = DateTime.UtcNow;

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = PasswordHasher.Hash(request.Password),
            FullName = request.FullName?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,
            RoleId = request.RoleId,
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetUserDetail),
            new { userId = user.UserId },
            new
            {
                user.UserId,
                user.Email,
                user.FullName,
                user.PhoneNumber,
                Role = role.RoleName
            });
    }

    /// <summary>
    /// get user detail
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<ActionResult> GetUserDetail([FromRoute] long userId, CancellationToken ct)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId, ct);

        if (user == null)
            return NotFound();

        return Ok(new
        {
            user.UserId,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.Gender,
            user.DateOfBirth,
            Role = user.Role.RoleName,
            user.Status,
            user.CreatedAt,
            user.UpdatedAt
        });
    }

    /// <summary>
    /// update staff info
    /// </summary>
    [HttpPut("{userId}")]
    public async Task<ActionResult> UpdateStaff([FromRoute] long userId, [FromBody] UpdateStaffRequest request, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId, ct);

        if (user == null)
            return NotFound();

        if (!string.IsNullOrEmpty(request.Email))
            user.Email = request.Email;

        if (!string.IsNullOrEmpty(request.FullName))
            user.FullName = request.FullName;

        if (!string.IsNullOrEmpty(request.PhoneNumber))
            user.PhoneNumber = request.PhoneNumber;

        if (!string.IsNullOrEmpty(request.Gender))
            user.Gender = request.Gender;

        if (request.DateOfBirth.HasValue)
            user.DateOfBirth = request.DateOfBirth;

        if (request.RoleId > 0)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleId == request.RoleId, ct);
            if (role == null)
                return BadRequest("Invalid role");
            user.RoleId = request.RoleId;
        }

        user.Status = request.Status;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    /// <summary>
    /// update user account status (deactivate) - delete account 
    /// </summary>
    [HttpPatch("{userId}/deactivate")]
    public async Task<ActionResult> UpdateUserStatus([FromRoute] long userId, [FromBody] UpdateUserStatusRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user == null)
            return NotFound();

        user.Status = 0; // Set status to 0 for deactivated
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(new { Message = $"User status updated to {user.Status}" });
    }

    /// <summary>
    /// update user account status (activate)
    /// </summary>
    [HttpPatch("{userId}/activate")]
    public async Task<ActionResult> ActiveAccount([FromRoute] long userId, [FromBody] UpdateUserStatusRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user == null)
            return NotFound();

        user.Status = 1; // Set status to 1 for activated
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(new { Message = $"User status updated to {user.Status}" });
    }

    /// <summary>
    /// reset user password
    /// </summary>
    [HttpPost("{userId}/reset-password")]
    public async Task<ActionResult> ResetPassword(
     [FromRoute] long userId,
     [FromBody] ResetPasswordRequest request,
     CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest("New password is required");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UserId == userId, ct);

        if (user == null)
            return NotFound();

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new { Message = "Password reset successfully." });
    }
}

public record CreateStaffRequest(string Email, string Password, string? FullName, string? PhoneNumber, string? Gender, DateOnly? DateOfBirth, int RoleId);
public record UpdateStaffRequest(string? Email, string? FullName, string? PhoneNumber, string? Gender, DateOnly? DateOfBirth, int RoleId, short Status);
public record UpdateUserStatusRequest(short Status);
public record ResetPasswordRequest(string NewPassword);
