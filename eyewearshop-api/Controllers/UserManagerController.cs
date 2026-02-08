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
    /// Get staff list
    /// </summary>
    [HttpGet("staff")]
    public async Task<ActionResult> GetStaff([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var query = _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.Role.RoleName != "Customer");

        var total = await query.CountAsync(ct);
        var staff = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        return Ok(new { Page = page, PageSize = pageSize, Total = total, Items = staff });
    }

    /// <summary>
    /// Get customers list
    /// </summary>
    [HttpGet("customers")]
    public async Task<ActionResult> GetCustomers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var total = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.Role.RoleName == "Customer")
            .CountAsync(ct);

        var customers = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.Role.RoleName == "Customer")
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        return Ok(new { Page = page, PageSize = pageSize, Total = total, Items = customers });
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

        if (!string.IsNullOrEmpty(request.FullName))
            user.FullName = request.FullName;

        if (!string.IsNullOrEmpty(request.PhoneNumber))
            user.PhoneNumber = request.PhoneNumber;

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
    /// deactivate user account
    /// </summary>
    [HttpDelete("{userId}")]
    public async Task<ActionResult> DeactivateUser([FromRoute] long userId, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user == null)
            return NotFound();

        user.Status = 0;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok();
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

public record CreateStaffRequest(string Email, string Password, string? FullName, string? PhoneNumber, int RoleId);
public record UpdateStaffRequest(string? FullName, string? PhoneNumber, int RoleId, short Status);
public record ResetPasswordRequest(string NewPassword);
