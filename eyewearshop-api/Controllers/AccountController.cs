using System.Security.Claims;
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

    public AccountController(EyewearShopDbContext db)
    {
        _db = db;
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
            address.Note,
            address.UpdatedAt
        });
    }

    /// <summary>
    /// Delete a saved shipping address (soft delete if used by past orders, otherwise hard delete).
    /// </summary>
    [HttpDelete("addresses/{addressId:long}")]
    public async Task<ActionResult> DeleteAddress([FromRoute] long addressId, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var address = await _db.UserAddresses
            .FirstOrDefaultAsync(a => a.AddressId == addressId && a.CustomerId == userId && a.Status == 1, ct);

        if (address == null) return NotFound();

        // Check if address is used in any orders
        var hasOrders = await _db.Orders
            .Include(o => o.ShippingInfo)
            .AnyAsync(o => o.CustomerId == userId && 
                o.ShippingInfo != null && 
                o.ShippingInfo.AddressLine == address.AddressLine &&
                o.ShippingInfo.PhoneNumber == address.PhoneNumber, ct);

        if (hasOrders)
        {
            // Soft delete
            address.Status = 0;
            address.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Hard delete
            _db.UserAddresses.Remove(address);
        }

        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    public record CreateAddressRequest(
        string RecipientName,
        string PhoneNumber,
        string AddressLine,
        string? City,
        string? District,
        string? Note);

    public record UpdateAddressRequest(
        string? RecipientName,
        string? PhoneNumber,
        string? AddressLine,
        string? City,
        string? District,
        string? Note);

    private long GetUserIdOrThrow()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
            throw new InvalidOperationException("Missing user id claim.");
        return userId;
    }
}