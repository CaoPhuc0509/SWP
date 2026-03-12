using System.Security.Claims;
using eyewearshop_api.Services;
using eyewearshop_data;
using eyewearshop_data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly EyewearShopDbContext _db;
    private readonly IR2StorageService _r2;

    public AccountController(EyewearShopDbContext db, IR2StorageService r2)
    {
        _db = db;
        _r2 = r2;
    }

    public record UpdateProfileRequest(
        string? FullName,
        string? PhoneNumber,
        string? Gender,
        DateOnly? DateOfBirth);

    /// <summary>
    /// Get the current authenticated user's profile (including role).
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult> GetProfile(CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.UserId == userId)
            .Select(u => new
            {
                u.UserId,
                u.Email,
                u.FullName,
                u.PhoneNumber,
                u.Gender,
                u.DateOfBirth,
                u.AvatarUrl,
                u.CreatedAt,
                u.UpdatedAt,
                Role = u.Role.RoleName
            })
            .FirstOrDefaultAsync(ct);

        if (user == null) return NotFound();

        return Ok(user);
    }

    /// <summary>
    /// Update the current authenticated user's profile fields.
    /// </summary>
    [HttpPut("profile")]
    public async Task<ActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UserId == userId, ct);

        if (user == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName.Trim();

        if (request.PhoneNumber != null)
            user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        if (request.Gender != null)
            user.Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim();

        user.DateOfBirth = request.DateOfBirth;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            user.UserId,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.Gender,
            user.DateOfBirth,
            user.UpdatedAt
        });
    }

    /// <summary>
    /// Upload or replace the current user's avatar. Accepts multipart/form-data with field "file" (image/*, max 5 MB).
    /// </summary>
    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> UploadAvatar(IFormFile file, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        string url;
        try
        {
            url = await _r2.UploadAsync(file, "avatars", ct);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user == null) return NotFound();

        // Delete old avatar from R2 if one existed
        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            var oldKey = _r2.ExtractKeyFromUrl(user.AvatarUrl);
            if (oldKey != null)
                await _r2.DeleteAsync(oldKey, ct);
        }

        user.AvatarUrl = url;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new { avatarUrl = url });
    }

    /// <summary>
    /// List active saved shipping addresses of the current user.
    /// </summary>
    [HttpGet("addresses")]
    public async Task<ActionResult> GetAddresses(CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var addresses = await _db.UserAddresses
            .AsNoTracking()
            .Where(a => a.CustomerId == userId && a.Status == 1)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.AddressId,
                a.RecipientName,
                a.PhoneNumber,
                a.AddressLine,
                a.City,
                a.District,
                a.Ward,
                a.GhnDistrictId,
                a.GhnWardCode,
                a.Note,
                a.CreatedAt,
                a.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(addresses);
    }

    /// <summary>
    /// Get a single saved shipping address by id (must belong to current user).
    /// </summary>
    [HttpGet("addresses/{addressId:long}")]
    public async Task<ActionResult> GetAddress([FromRoute] long addressId, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var address = await _db.UserAddresses
            .AsNoTracking()
            .Where(a => a.AddressId == addressId && a.CustomerId == userId && a.Status == 1)
            .Select(a => new
            {
                a.AddressId,
                a.RecipientName,
                a.PhoneNumber,
                a.AddressLine,
                a.City,
                a.District,
                a.Ward,
                a.GhnDistrictId,
                a.GhnWardCode,
                a.Note,
                a.CreatedAt,
                a.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (address == null) return NotFound();

        return Ok(address);
    }

    /// <summary>
    /// Create a new saved shipping address for the current user.
    /// </summary>
    [HttpPost("addresses")]
    public async Task<ActionResult> CreateAddress([FromBody] CreateAddressRequest request, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        if (string.IsNullOrWhiteSpace(request.RecipientName))
            return BadRequest("Recipient name is required.");

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            return BadRequest("Phone number is required.");

        if (string.IsNullOrWhiteSpace(request.AddressLine))
            return BadRequest("Address line is required.");

        var now = DateTime.UtcNow;
        var address = new UserAddress
        {
            CustomerId = userId,
            RecipientName = request.RecipientName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            AddressLine = request.AddressLine.Trim(),
            City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            District = string.IsNullOrWhiteSpace(request.District) ? null : request.District.Trim(),
            Ward = string.IsNullOrWhiteSpace(request.Ward) ? null : request.Ward.Trim(),
            GhnDistrictId = request.GhnDistrictId,
            GhnWardCode = string.IsNullOrWhiteSpace(request.GhnWardCode) ? null : request.GhnWardCode.Trim(),
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            Status = 1
        };

        _db.UserAddresses.Add(address);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            address.AddressId,
            address.RecipientName,
            address.PhoneNumber,
            address.AddressLine,
            address.City,
            address.District,
            address.Ward,
            address.GhnDistrictId,
            address.GhnWardCode,
            address.Note,
            address.CreatedAt
        });
    }

    /// <summary>
    /// Update an existing saved shipping address (must belong to current user).
    /// </summary>
    [HttpPut("addresses/{addressId:long}")]
    public async Task<ActionResult> UpdateAddress(
        [FromRoute] long addressId,
        [FromBody] UpdateAddressRequest request,
        CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var address = await _db.UserAddresses
            .FirstOrDefaultAsync(a => a.AddressId == addressId && a.CustomerId == userId && a.Status == 1, ct);

        if (address == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.RecipientName))
            address.RecipientName = request.RecipientName.Trim();

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            address.PhoneNumber = request.PhoneNumber.Trim();

        if (!string.IsNullOrWhiteSpace(request.AddressLine))
            address.AddressLine = request.AddressLine.Trim();

        address.City = string.IsNullOrWhiteSpace(request.City) ? null : request.City?.Trim();
        address.District = string.IsNullOrWhiteSpace(request.District) ? null : request.District?.Trim();
        address.Ward = string.IsNullOrWhiteSpace(request.Ward) ? null : request.Ward?.Trim();
        address.GhnDistrictId = request.GhnDistrictId;
        address.GhnWardCode = string.IsNullOrWhiteSpace(request.GhnWardCode) ? null : request.GhnWardCode?.Trim();
        address.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note?.Trim();
        address.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            address.AddressId,
            address.RecipientName,
            address.PhoneNumber,
            address.AddressLine,
            address.City,
            address.District,
            address.Ward,
            address.GhnDistrictId,
            address.GhnWardCode,
            address.Note,
            address.UpdatedAt
        });
    }

    /// <summary>
    /// Delete a saved shipping address (soft delete).
    /// </summary>
    [HttpDelete("addresses/{addressId:long}")]
    public async Task<ActionResult> DeleteAddress([FromRoute] long addressId, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var address = await _db.UserAddresses
            .FirstOrDefaultAsync(a => a.AddressId == addressId && a.CustomerId == userId && a.Status == 1, ct);

        if (address == null) return NotFound();

        address.Status = 0;
        address.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    public record CreateAddressRequest(
        string RecipientName,
        string PhoneNumber,
        string AddressLine,
        string? City,
        string? District,
        string? Ward,
        int? GhnDistrictId,
        string? GhnWardCode,
        string? Note);

    public record UpdateAddressRequest(
        string? RecipientName,
        string? PhoneNumber,
        string? AddressLine,
        string? City,
        string? District,
        string? Ward,
        int? GhnDistrictId,
        string? GhnWardCode,
        string? Note);

    private long GetUserIdOrThrow()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
            throw new InvalidOperationException("Missing user id claim.");
        return userId;
    }
}