using System.Security.Claims;
using eyewearshop_data;
using eyewearshop_data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly EyewearShopDbContext _db;

    public CartController(EyewearShopDbContext db)
    {
        _db = db;
    }

    public record AddCartItemRequest(long VariantId, int Quantity);
    public record UpdateCartItemRequest(int Quantity);

    [HttpGet]
    public async Task<ActionResult> GetMyCart(CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var cart = await EnsureCartAsync(userId, ct);

        var items = await _db.CartItems
            .AsNoTracking()
            .Where(ci => ci.CartId == cart.CartId)
            .Include(ci => ci.Variant)
            .ThenInclude(v => v.Product)
            .Select(ci => new
            {
                ci.CartItemId,
                ci.Quantity,
                Variant = new
                {
                    ci.Variant.VariantId,
                    ci.Variant.Color,
                    ci.Variant.Price,
                    ci.Variant.StockQuantity,
                    ci.Variant.PreOrderQuantity,
                    Product = new
                    {
                        ci.Variant.Product.ProductId,
                        ci.Variant.Product.ProductName,
                        ci.Variant.Product.Sku,
                        ci.Variant.Product.ProductType
                    }
                },
                LineTotal = ci.Variant.Price * ci.Quantity
            })
            .ToListAsync(ct);

        var subTotal = items.Sum(x => (decimal)x.LineTotal);

        return Ok(new
        {
            cart.CartId,
            CartUpdatedAt = cart.UpdatedAt,
            Items = items,
            Summary = new
            {
                SubTotal = subTotal,
                ItemCount = items.Sum(x => (int)x.Quantity)
            }
        });
    }

    [HttpPost("items")]
    public async Task<ActionResult> AddItem([FromBody] AddCartItemRequest request, CancellationToken ct)
    {
        if (request.Quantity <= 0) return BadRequest("Quantity must be greater than 0.");

        var userId = GetUserIdOrThrow();
        var cart = await EnsureCartAsync(userId, ct);

        var variant = await _db.ProductVariants
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.VariantId == request.VariantId && v.Status == 1, ct);

        if (variant == null) return NotFound("Variant not found.");

        var item = await _db.CartItems
            .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.VariantId == request.VariantId, ct);

        if (item == null)
        {
            item = new CartItem
            {
                CartId = cart.CartId,
                VariantId = request.VariantId,
                Quantity = request.Quantity
            };
            _db.CartItems.Add(item);
        }
        else
        {
            item.Quantity += request.Quantity; // Increment behavior
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new { item.CartItemId, item.CartId, item.VariantId, item.Quantity });
    }

    [HttpPut("items/{cartItemId:long}")]
    public async Task<ActionResult> UpdateItem([FromRoute] long cartItemId, [FromBody] UpdateCartItemRequest request, CancellationToken ct)
    {
        if (request.Quantity <= 0) return BadRequest("Quantity must be greater than 0.");

        var userId = GetUserIdOrThrow();

        var cart = await EnsureCartAsync(userId, ct);

        var item = await _db.CartItems
            .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId && ci.CartId == cart.CartId, ct);

        if (item == null) return NotFound();

        item.Quantity = request.Quantity;
        cart.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(new { item.CartItemId, item.CartId, item.VariantId, item.Quantity });
    }

    [HttpDelete("items/{cartItemId:long}")]
    public async Task<ActionResult> RemoveItem([FromRoute] long cartItemId, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();
        var cart = await EnsureCartAsync(userId, ct);

        var item = await _db.CartItems
            .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId && ci.CartId == cart.CartId, ct);

        if (item == null) return NotFound();

        _db.CartItems.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private long GetUserIdOrThrow()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
            throw new InvalidOperationException("Missing user id claim.");

        return userId;
    }

    private async Task<Cart> EnsureCartAsync(long userId, CancellationToken ct)
    {
        var cart = await _db.Carts
            .FirstOrDefaultAsync(c => c.CustomerId == userId, ct);

        if (cart != null) return cart;

        var now = DateTime.UtcNow;
        cart = new Cart
        {
            CustomerId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Carts.Add(cart);
        await _db.SaveChangesAsync(ct);
        return cart;
    }
}
