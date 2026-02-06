using System.Security.Claims;
using eyewearshop_data;
using eyewearshop_data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly EyewearShopDbContext _db;

    public OrderController(EyewearShopDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult> GetMyOrders(
        [FromQuery] string? orderType,
        [FromQuery] short? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = GetUserIdOrThrow();

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _db.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == userId);

        if (!string.IsNullOrWhiteSpace(orderType))
        {
            query = query.Where(o => o.OrderType == orderType.Trim().ToUpperInvariant());
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var total = await query.CountAsync(ct);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.OrderId,
                o.OrderNumber,
                o.OrderType,
                o.Status,
                o.SubTotal,
                o.ShippingFee,
                o.DiscountAmount,
                o.TotalAmount,
                o.CreatedAt,
                o.UpdatedAt,
                ItemCount = o.Items.Count,
                ShippingInfo = o.ShippingInfo == null ? null : new
                {
                    o.ShippingInfo.TrackingNumber,
                    o.ShippingInfo.Carrier,
                    o.ShippingInfo.ShippedAt,
                    o.ShippingInfo.DeliveredAt
                }
            })
            .ToListAsync(ct);

        return Ok(new
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = orders
        });
    }

    [HttpGet("{orderId:long}")]
    public async Task<ActionResult> GetOrderDetail([FromRoute] long orderId, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var order = await _db.Orders
            .AsNoTracking()
            .Where(o => o.OrderId == orderId && o.CustomerId == userId)
            .Select(o => new
            {
                o.OrderId,
                o.OrderNumber,
                o.OrderType,
                o.Status,
                o.SubTotal,
                o.ShippingFee,
                o.DiscountAmount,
                o.TotalAmount,
                o.CreatedAt,
                o.UpdatedAt,
                Prescription = o.OrderPrescription == null ? null : new
                {
                    o.OrderPrescription.SavedPrescriptionId,
                    o.OrderPrescription.RightSphere,
                    o.OrderPrescription.RightCylinder,
                    o.OrderPrescription.RightAxis,
                    o.OrderPrescription.RightAdd,
                    o.OrderPrescription.RightPD,
                    o.OrderPrescription.LeftSphere,
                    o.OrderPrescription.LeftCylinder,
                    o.OrderPrescription.LeftAxis,
                    o.OrderPrescription.LeftAdd,
                    o.OrderPrescription.LeftPD,
                    o.OrderPrescription.Notes,
                    o.OrderPrescription.PrescribedBy,
                    o.OrderPrescription.PrescriptionDate,
                    o.OrderPrescription.CreatedAt
                },
                ShippingInfo = o.ShippingInfo == null ? null : new
                {
                    o.ShippingInfo.RecipientName,
                    o.ShippingInfo.PhoneNumber,
                    o.ShippingInfo.AddressLine,
                    o.ShippingInfo.City,
                    o.ShippingInfo.District,
                    o.ShippingInfo.Ward,
                    o.ShippingInfo.ShippingMethod,
                    o.ShippingInfo.TrackingNumber,
                    o.ShippingInfo.Carrier,
                    o.ShippingInfo.ShippedAt,
                    o.ShippingInfo.EstimatedDeliveryDate,
                    o.ShippingInfo.DeliveredAt
                },
                Items = o.Items.Select(oi => new
                {
                    oi.OrderItemId,
                    oi.UnitPrice,
                    oi.Quantity,
                    oi.SubTotal,
                    oi.Description,
                    Variant = oi.Variant == null ? null : new
                    {
                        oi.Variant.VariantId,
                        oi.Variant.Color,
                        Product = oi.Variant.Product == null ? null : new
                        {
                            oi.Variant.Product.ProductId,
                            oi.Variant.Product.ProductName,
                            oi.Variant.Product.Sku,
                            oi.Variant.Product.ProductType
                        }
                    }
                }),
                Payments = o.Payments.Select(p => new
                {
                    p.PaymentId,
                    p.PaymentType,
                    p.PaymentMethod,
                    p.Amount,
                    p.Status,
                    p.CreatedAt
                })
            })
            .FirstOrDefaultAsync(ct);

        if (order == null) return NotFound();

        return Ok(order);
    }

    private long GetUserIdOrThrow()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
            throw new InvalidOperationException("Missing user id claim.");
        return userId;
    }
}