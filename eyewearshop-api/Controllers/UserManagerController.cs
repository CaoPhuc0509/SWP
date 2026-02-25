using eyewearshop_data;
using eyewearshop_data.Entities;
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
            .Where(u => u.Role.RoleName != "Customer")
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
            .Where(u => u.Role.RoleName == "Customer")
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
    public async Task<ActionResult> CreateStaff([FromBody] CreateStaffRequest request, CancellationToken ct)
    {
        var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (existingUser != null)
            return BadRequest("Email already exists");

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleId == request.RoleId, ct);
        if (role == null)
            return BadRequest("Invalid role");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,
            RoleId = request.RoleId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = 1
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetUserDetail), new { userId = user.UserId }, new
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
    /// update user account status (deactivate/activate) - delete account 
    /// </summary>
    [HttpPatch("{userId}/status")]
    public async Task<ActionResult> UpdateUserStatus([FromRoute] long userId, [FromBody] UpdateUserStatusRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user == null)
            return NotFound();

        user.Status = request.Status;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(new { Message = $"User status updated to {request.Status}" });
    }

    /// <summary>
    /// reset user password
    /// </summary>
    [HttpPost("{userId}/reset-password")]
    public async Task<ActionResult> ResetPassword([FromRoute] long userId, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user == null)
            return NotFound();

        user.PasswordHash = HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    private string HashPassword(string password)
    {
        // Simple hash using SHA256
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return System.Convert.ToBase64String(hashedBytes);
        }
    }
}

public record CreateStaffRequest(string Email, string Password, string? FullName, string? PhoneNumber, string? Gender, DateOnly? DateOfBirth, int RoleId);
public record UpdateStaffRequest(string? Email, string? FullName, string? PhoneNumber, string? Gender, DateOnly? DateOfBirth, int RoleId, short Status);
public record UpdateUserStatusRequest(short Status);
public record ResetPasswordRequest(string NewPassword);
