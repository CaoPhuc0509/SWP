using System.Security.Claims;
using eyewearshop_data;
using eyewearshop_data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/checkout")]
[Authorize]
public class CheckoutController : ControllerBase
{
    private readonly EyewearShopDbContext _db;

    public CheckoutController(EyewearShopDbContext db)
    {
        _db = db;
    }

    public record CheckoutRequest(long AddressId, long? PrescriptionId);

    [HttpGet("requirements")]
    public async Task<ActionResult> GetRequirements(CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var cart = await _db.Carts.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == userId, ct);
        if (cart == null)
            return Ok(new { RequiresPrescription = false, RequiresShippingAddress = true, ItemCount = 0 });

        var cartItems = await _db.CartItems
            .AsNoTracking()
            .Where(ci => ci.CartId == cart.CartId)
            .Include(ci => ci.Variant)
            .ThenInclude(v => v.Product)
            .ToListAsync(ct);

        var hasFrame = cartItems.Any(x => x.Variant.Product.ProductType == ProductTypes.Frame);
        var hasRxLens = cartItems.Any(x => x.Variant.Product.ProductType == ProductTypes.RxLens);
        var hasContactLens = cartItems.Any(x => x.Variant.Product.ProductType == ProductTypes.ContactLens);
        var hasCombo = cartItems.Any(x => x.Variant.Product.ProductType == ProductTypes.Combo);

        var requiresPrescription = hasRxLens || hasContactLens || hasCombo || (hasFrame && hasRxLens);

        return Ok(new
        {
            RequiresPrescription = requiresPrescription,
            RequiresShippingAddress = true,
            ItemCount = cartItems.Sum(x => x.Quantity)
        });
    }

    [HttpPost]
    public async Task<ActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var address = await _db.UserAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AddressId == request.AddressId && a.CustomerId == userId && a.Status == 1, ct);

        if (address == null) return BadRequest("Invalid address.");

        var cart = await _db.Carts.FirstOrDefaultAsync(c => c.CustomerId == userId, ct);
        if (cart == null) return BadRequest("Cart is empty.");

        var cartItems = await _db.CartItems
            .Where(ci => ci.CartId == cart.CartId)
            .Include(ci => ci.Variant)
            .ThenInclude(v => v.Product)
            .ToListAsync(ct);

        if (cartItems.Count == 0) return BadRequest("Cart is empty.");

        var hasFrame = cartItems.Any(x => x.Variant.Product.ProductType == ProductTypes.Frame);
        var hasRxLens = cartItems.Any(x => x.Variant.Product.ProductType == ProductTypes.RxLens);
        var hasContactLens = cartItems.Any(x => x.Variant.Product.ProductType == ProductTypes.ContactLens);
        var hasCombo = cartItems.Any(x => x.Variant.Product.ProductType == ProductTypes.Combo);

        var requiresPrescription = hasRxLens || hasContactLens || hasCombo || (hasFrame && hasRxLens);

        if (requiresPrescription)
        {
            if (!request.PrescriptionId.HasValue)
                return BadRequest("PrescriptionId is required for this cart.");
        }

        // Stock check (simple): allow preorder if stock insufficient but preorder_quantity available.
        foreach (var ci in cartItems)
        {
            var v = ci.Variant;
            if (v.Status != 1 || v.Product.Status != 1)
                return BadRequest($"Variant {v.VariantId} is not available.");

            var availableNow = v.StockQuantity;
            var availablePre = v.PreOrderQuantity;

            if (ci.Quantity > availableNow + availablePre)
                return BadRequest($"Variant {v.VariantId} has insufficient quantity.");
        }

        var now = DateTime.UtcNow;
        var orderNumber = GenerateOrderNumber(now);

        var subTotal = cartItems.Sum(ci => ci.Variant.Price * ci.Quantity);
        var shippingFee = 0m;
        var discountAmount = 0m;
        var total = subTotal + shippingFee - discountAmount;

        var order = new Order
        {
            CustomerId = userId,
            OrderNumber = orderNumber,
            Status = 0,
            PrescriptionId = request.PrescriptionId,
            SubTotal = subTotal,
            ShippingFee = shippingFee,
            DiscountAmount = discountAmount,
            TotalAmount = total,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        foreach (var ci in cartItems)
        {
            _db.OrderItems.Add(new OrderItem
            {
                OrderId = order.OrderId,
                VariantId = ci.VariantId,
                Quantity = ci.Quantity,
                Status = 1
            });
        }

        // Clear cart
        _db.CartItems.RemoveRange(cartItems);
        cart.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            order.OrderId,
            order.OrderNumber,
            order.TotalAmount,
            RequiresPrescription = requiresPrescription
        });
    }

    private long GetUserIdOrThrow()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
            throw new InvalidOperationException("Missing user id claim.");
        return userId;
    }

    private static string GenerateOrderNumber(DateTime nowUtc)
    {
        // Must be <= 23 chars for VietQR content; keep short.
        // Example: OD240123123456789 (18 chars)
        return $"OD{nowUtc:yyMMddHHmmssfff}";
    }
}
