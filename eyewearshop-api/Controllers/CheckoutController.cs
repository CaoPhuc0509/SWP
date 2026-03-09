using System.Security.Claims;
using eyewearshop_data;
using eyewearshop_data.Entities;
using eyewearshop_service.Cart;
using eyewearshop_service.Validation;
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
    private readonly ISessionCartService _cartService;

    public CheckoutController(EyewearShopDbContext db, ISessionCartService cartService)
    {
        _db = db;
        _cartService = cartService;
    }

    public record CheckoutRequest(
        long AddressId, 
        long? PrescriptionId,
        string? PromoCode = null,
        string? ShippingMethod = null);

    /// <summary>
    /// Business rule: customer must provide a prescription ONLY when the cart contains BOTH a Frame and an RxLens.
    /// </summary>
    private static bool CartContainsFrameAndRxLens(IEnumerable<string> productTypes)
    {
        var hasFrame = false;
        var hasRxLens = false;

        foreach (var pt in productTypes)
        {
            if (pt == ProductTypes.Frame) hasFrame = true;
            else if (pt == ProductTypes.RxLens) hasRxLens = true;

            if (hasFrame && hasRxLens) return true;
        }

        return false;
    }

    /// <summary>
    /// Check what the current cart requires before checkout (e.g., prescription required or not).
    /// Business rule: prescription is required ONLY if cart contains both Frame + RxLens.
    /// </summary>
    [HttpGet("requirements")]
    public async Task<ActionResult> GetRequirements(CancellationToken ct)
    {
        // Ensure session is loaded before accessing session-backed cart (important for some hosting setups)
        await HttpContext.Session.LoadAsync(ct);

        var cartItems = _cartService.GetCart();
        
        if (cartItems.Count == 0)
            return Ok(new { RequiresPrescription = false, RequiresShippingAddress = true, ItemCount = 0 });

        var variantIds = cartItems.Select(ci => ci.VariantId).ToList();
        
        var variants = await _db.ProductVariants
            .AsNoTracking()
            .Where(v => variantIds.Contains(v.VariantId) && v.Status == 1)
            .Include(v => v.Product)
            .ToListAsync(ct);

        // Filter out variants with null Product or null ProductType, and get distinct product types
        var productTypes = variants
            .Where(v => v.Product != null && !string.IsNullOrEmpty(v.Product.ProductType))
            .Select(v => v.Product!.ProductType)
            .Distinct()
            .ToList();

        var requiresPrescription = CartContainsFrameAndRxLens(productTypes);

        return Ok(new
        {
            RequiresPrescription = requiresPrescription,
            RequiresShippingAddress = true,
            ItemCount = cartItems.Sum(x => x.Quantity)
        });
    }

    /// <summary>
    /// Create an order from the current session cart, validate stock and (if needed) prescription compatibility,
    /// then snapshot prescription into the order and clear the cart.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var address = await _db.UserAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AddressId == request.AddressId && a.CustomerId == userId && a.Status == 1, ct);

        if (address == null) return BadRequest("Invalid address.");

        var cartItems = _cartService.GetCart();
        if (cartItems.Count == 0) return BadRequest("Cart is empty.");

        var variantIds = cartItems.Select(ci => ci.VariantId).ToList();
        var variants = await _db.ProductVariants
            .Where(v => variantIds.Contains(v.VariantId))
            .Include(v => v.Product)
            .ThenInclude(p => p.RxLensSpec)
            .Include(v => v.Product)
            .ThenInclude(p => p.FrameSpec)
            .ToListAsync(ct);

        if (variants.Count != variantIds.Count)
            return BadRequest("Some variants are no longer available.");

        // Build cart items with product information
        var cartItemsWithProducts = cartItems
            .Join(variants, ci => ci.VariantId, v => v.VariantId, (ci, v) => new { CartItem = ci, Variant = v })
            .ToList();

        var requiresPrescription = CartContainsFrameAndRxLens(cartItemsWithProducts.Select(x => x.Variant.Product.ProductType));
        var hasFrame = cartItemsWithProducts.Any(x => x.Variant.Product.ProductType == ProductTypes.Frame);
        var hasRxLens = cartItemsWithProducts.Any(x => x.Variant.Product.ProductType == ProductTypes.RxLens);

        Prescription? prescription = null;
        if (requiresPrescription)
        {
            if (!request.PrescriptionId.HasValue)
                return BadRequest("PrescriptionId is required for this cart.");

            prescription = await _db.Prescriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PrescriptionId == request.PrescriptionId.Value
                                          && p.CustomerId == userId
                                          && p.Status == 1, ct);

            if (prescription == null)
                return BadRequest("Invalid PrescriptionId.");

            // Compatibility checks (Prescription <-> RxLens, Frame <-> RxLens)
            var rxLensSpecs = cartItemsWithProducts
                .Where(x => x.Variant.Product.ProductType == ProductTypes.RxLens)
                .Select(x => x.Variant.Product.RxLensSpec)
                .Where(s => s != null)
                .DistinctBy(s => s!.ProductId)
                .Select(s => s!)
                .ToList();

            if (rxLensSpecs.Count == 0)
                return BadRequest("Selected Rx lens has no specification configured.");

            var frameSpecs = cartItemsWithProducts
                .Where(x => x.Variant.Product.ProductType == ProductTypes.Frame)
                .Select(x => x.Variant.Product.FrameSpec)
                .Where(s => s != null)
                .DistinctBy(s => s!.ProductId)
                .Select(s => s!)
                .ToList();

            // 1) Prescription vs each RxLensSpec
            var issues = new List<string>();
            foreach (var lensSpec in rxLensSpecs)
            {
                issues.AddRange(RxCompatibility.ValidatePrescriptionAgainstLens(prescription, lensSpec));
            }

            // 2) Frame vs RxLensSpec (only when a frame exists)
            foreach (var frameSpec in frameSpecs)
            {
                foreach (var lensSpec in rxLensSpecs)
                {
                    issues.AddRange(RxCompatibility.ValidateFrameLensCompatibility(frameSpec, lensSpec));
                }
            }

            if (issues.Count > 0)
                return BadRequest(new { Message = "Frame / RxLens / Prescription are incompatible.", Issues = issues });
        }

        // Stock check
        foreach (var item in cartItemsWithProducts)
        {
            var v = item.Variant;
            if (v.Status != 1 || v.Product.Status != 1)
                return BadRequest($"Variant {v.VariantId} is not available.");

            var availableNow = v.StockQuantity;
            var availablePre = v.PreOrderQuantity;

            if (item.CartItem.Quantity > availableNow + availablePre)
                return BadRequest($"Variant {v.VariantId} has insufficient quantity.");
        }

        var now = DateTime.UtcNow;
        var orderNumber = GenerateOrderNumber(now);

        var subTotal = cartItemsWithProducts.Sum(item => item.Variant.Price * item.CartItem.Quantity);

        // Determine order type
        string orderType;
        if (hasFrame && hasRxLens && request.PrescriptionId.HasValue)
        {
            orderType = OrderTypes.Prescription;
        }
        else if (cartItemsWithProducts.Any(item => item.CartItem.Quantity > item.Variant.StockQuantity && item.Variant.PreOrderQuantity > 0))
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
            Status = OrderStatuses.AwaitingPayment,
            PaymentStatus = PaymentStatuses.Unpaid,
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

        // Snapshot prescription for this order (do NOT link to editable Prescription entity)
        if (requiresPrescription && prescription != null)
        {
            _db.OrderPrescriptions.Add(new OrderPrescription
            {
                OrderId = order.OrderId,
                CustomerId = userId,
                SavedPrescriptionId = prescription.PrescriptionId,

                RightSphere = prescription.RightSphere,
                RightCylinder = prescription.RightCylinder,
                RightAxis = prescription.RightAxis,
                RightAdd = prescription.RightAdd,
                RightPD = prescription.RightPD,

                LeftSphere = prescription.LeftSphere,
                LeftCylinder = prescription.LeftCylinder,
                LeftAxis = prescription.LeftAxis,
                LeftAdd = prescription.LeftAdd,
                LeftPD = prescription.LeftPD,

                Notes = prescription.Notes,
                PrescriptionDate = prescription.PrescriptionDate,
                PrescribedBy = prescription.PrescribedBy,
                CreatedAt = now
            });
        }

        // Create shipping info
        var shippingInfo = new ShippingInfo
        {
            OrderId = order.OrderId,
            RecipientName = address.RecipientName ?? "",
            PhoneNumber = address.PhoneNumber ?? "",
            AddressLine = address.AddressLine ?? "",
            City = address.City,
            District = address.District,
            Ward = null,
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

        // Create order items and update stock
        foreach (var item in cartItemsWithProducts)
        {
            var unitPrice = item.Variant.Price;
            _db.OrderItems.Add(new OrderItem
            {
                OrderId = order.OrderId,
                VariantId = item.Variant.VariantId,
                UnitPrice = unitPrice,
                Quantity = item.CartItem.Quantity,
                SubTotal = unitPrice * item.CartItem.Quantity,
                Status = 1
            });

            // Update stock
            var variant = item.Variant;
            if (item.CartItem.Quantity <= variant.StockQuantity)
            {
                variant.StockQuantity -= item.CartItem.Quantity;
            }
            else
            {
                var stockUsed = variant.StockQuantity;
                variant.StockQuantity = 0;
                variant.PreOrderQuantity -= (item.CartItem.Quantity - stockUsed);
            }
            variant.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);

        // Clear cart after successful checkout
        _cartService.ClearCart();

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
        return $"OD{nowUtc:yyMMddHHmmssfff}";
    }

    private static decimal CalculateShippingFee(string? shippingMethod)
    {
        return shippingMethod?.ToUpper() switch
        {
            "EXPRESS" => 50000m,
            "STANDARD" => 30000m,
            _ => 30000m
        };
    }
}