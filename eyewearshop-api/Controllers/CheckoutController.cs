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

    public record CheckoutRequest(
        long AddressId, 
        long? PrescriptionId,
        string? PromoCode = null,
        string? ShippingMethod = null);

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

        // Determine order type
        string orderType;
        if (hasFrame && hasRxLens && request.PrescriptionId.HasValue)
        {
            orderType = OrderTypes.Prescription;
        }
        else if (cartItems.Any(ci => ci.Quantity > ci.Variant.StockQuantity && ci.Variant.PreOrderQuantity > 0))
        {
            orderType = OrderTypes.PreOrder;
        }
        else
        {
            orderType = OrderTypes.Available;
        }

        // Check promotion
        long? promotionId = null;
        decimal discountAmount = 0m;
        if (!string.IsNullOrWhiteSpace(request.PromoCode))
        {
            var promotion = await _db.Promotions
                .FirstOrDefaultAsync(p => p.PromoCode == request.PromoCode.Trim() 
                    && p.Status == 1 
                    && p.StartDate <= now 
                    && p.EndDate >= now, ct);
            
            if (promotion != null && promotion.CurrentUsageCount < (promotion.TotalUsageLimit ?? int.MaxValue))
            {
                promotionId = promotion.PromotionId;
                // Calculate discount (simplified - can be enhanced)
                if (promotion.DiscountPercentage.HasValue)
                {
                    discountAmount = subTotal * promotion.DiscountPercentage.Value / 100;
                }
                else if (promotion.DiscountAmount.HasValue)
                {
                    discountAmount = promotion.DiscountAmount.Value;
                }
            }
        }

        var shippingFee = CalculateShippingFee(request.ShippingMethod);
        var total = subTotal + shippingFee - discountAmount;

        var order = new Order
        {
            CustomerId = userId,
            OrderNumber = orderNumber,
            OrderType = orderType,
            Status = OrderStatuses.Pending,
            PrescriptionId = request.PrescriptionId,
            PromotionId = promotionId,
            SubTotal = subTotal,
            ShippingFee = shippingFee,
            DiscountAmount = discountAmount,
            TotalAmount = total,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        // Create shipping info
        var shippingInfo = new ShippingInfo
        {
            OrderId = order.OrderId,
            RecipientName = address.RecipientName ?? "",
            PhoneNumber = address.PhoneNumber ?? "",
            AddressLine = address.AddressLine ?? "",
            City = address.City,
            District = address.District,
            Ward = null, // Can be added to UserAddress if needed
            Note = address.Note,
            ShippingMethod = request.ShippingMethod ?? "Standard",
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.ShippingInfos.Add(shippingInfo);

        // Update promotion usage
        if (promotionId.HasValue)
        {
            var promotion = await _db.Promotions.FindAsync(new object[] { promotionId.Value }, ct);
            if (promotion != null)
            {
                promotion.CurrentUsageCount++;
                promotion.UpdatedAt = now;
            }
        }

        foreach (var ci in cartItems)
        {
            var unitPrice = ci.Variant.Price;
            _db.OrderItems.Add(new OrderItem
            {
                OrderId = order.OrderId,
                VariantId = ci.VariantId,
                UnitPrice = unitPrice,
                Quantity = ci.Quantity,
                SubTotal = unitPrice * ci.Quantity,
                Status = 1
            });

            // Update stock
            if (ci.Quantity <= ci.Variant.StockQuantity)
            {
                ci.Variant.StockQuantity -= ci.Quantity;
            }
            else
            {
                var stockUsed = ci.Variant.StockQuantity;
                ci.Variant.StockQuantity = 0;
                ci.Variant.PreOrderQuantity -= (ci.Quantity - stockUsed);
            }
            ci.Variant.UpdatedAt = now;
        }

        // Clear cart
        _db.CartItems.RemoveRange(cartItems);
        cart.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            order.OrderId,
            order.OrderNumber,
            order.OrderType,
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

    private static decimal CalculateShippingFee(string? shippingMethod)
    {
        // Simplified shipping fee calculation
        return shippingMethod?.ToUpper() switch
        {
            "EXPRESS" => 50000m,
            "STANDARD" => 30000m,
            _ => 30000m
        };
    }
}
