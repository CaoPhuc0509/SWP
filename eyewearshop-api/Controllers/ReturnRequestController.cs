using System.Security.Claims;
using eyewearshop_data;
using eyewearshop_data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/return-requests")]
[Authorize]
public class ReturnRequestController : ControllerBase
{
    private readonly EyewearShopDbContext _db;

    public ReturnRequestController(EyewearShopDbContext db)
    {
        _db = db;
    }

    public record CreateReturnRequestRequest(
        long OrderId,
        string RequestType, // EXCHANGE, RETURN, WARRANTY
        string Reason,
        string? Description,
        List<ReturnRequestItemDto> Items);

    public record ReturnRequestItemDto(long OrderItemId, int Quantity);

    [HttpGet]
    public async Task<ActionResult> GetMyReturnRequests(
        [FromQuery] string? requestType,
        [FromQuery] short? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = GetUserIdOrThrow();

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _db.ReturnRequests
            .AsNoTracking()
            .Where(rr => rr.CustomerId == userId);

        if (!string.IsNullOrWhiteSpace(requestType))
        {
            query = query.Where(rr => rr.RequestType == requestType.Trim().ToUpperInvariant());
        }

        if (status.HasValue)
        {
            query = query.Where(rr => rr.Status == status.Value);
        }

        var total = await query.CountAsync(ct);

        var requests = await query
            .OrderByDescending(rr => rr.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(rr => new
            {
                rr.ReturnRequestId,
                rr.RequestNumber,
                rr.RequestType,
                rr.Status,
                rr.Reason,
                rr.Description,
                rr.CreatedAt,
                rr.UpdatedAt,
                Order = new
                {
                    rr.Order.OrderId,
                    rr.Order.OrderNumber,
                    rr.Order.OrderType
                },
                ExchangeOrder = rr.ExchangeOrder == null ? null : new
                {
                    rr.ExchangeOrder.OrderId,
                    rr.ExchangeOrder.OrderNumber
                }
            })
            .ToListAsync(ct);

        return Ok(new
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = requests
        });
    }

    [HttpGet("{returnRequestId:long}")]
    public async Task<ActionResult> GetReturnRequestDetail([FromRoute] long returnRequestId, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var request = await _db.ReturnRequests
            .AsNoTracking()
            .Where(rr => rr.ReturnRequestId == returnRequestId && rr.CustomerId == userId)
            .Select(rr => new
            {
                rr.ReturnRequestId,
                rr.RequestNumber,
                rr.RequestType,
                rr.Status,
                rr.Reason,
                rr.Description,
                rr.StaffNotes,
                rr.CreatedAt,
                rr.UpdatedAt,
                Order = new
                {
                    rr.Order.OrderId,
                    rr.Order.OrderNumber,
                    rr.Order.OrderType,
                    rr.Order.TotalAmount
                },
                ExchangeOrder = rr.ExchangeOrder == null ? null : new
                {
                    rr.ExchangeOrder.OrderId,
                    rr.ExchangeOrder.OrderNumber
                },
                Items = rr.Items.Select(rri => new
                {
                    rri.ReturnRequestItemId,
                    rri.Quantity,
                    OrderItem = new
                    {
                        rri.OrderItem.OrderItemId,
                        rri.OrderItem.UnitPrice,
                        rri.OrderItem.Quantity,
                        Variant = rri.OrderItem.Variant == null ? null : new
                        {
                            rri.OrderItem.Variant.VariantId,
                            rri.OrderItem.Variant.Color,
                            Product = rri.OrderItem.Variant.Product == null ? null : new
                            {
                                rri.OrderItem.Variant.Product.ProductId,
                                rri.OrderItem.Variant.Product.ProductName,
                                rri.OrderItem.Variant.Product.Sku
                            }
                        }
                    }
                })
            })
            .FirstOrDefaultAsync(ct);

        if (request == null) return NotFound();

        return Ok(request);
    }

    [HttpPost]
    public async Task<ActionResult> CreateReturnRequest([FromBody] CreateReturnRequestRequest request, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        // Validate request type
        if (!new[] { ReturnRequestTypes.Exchange, ReturnRequestTypes.Return, ReturnRequestTypes.Warranty }
            .Contains(request.RequestType.ToUpperInvariant()))
        {
            return BadRequest("Invalid request type. Must be EXCHANGE, RETURN, or WARRANTY.");
        }

        // Verify order belongs to customer
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.OrderId == request.OrderId && o.CustomerId == userId, ct);

        if (order == null) return NotFound("Order not found.");

        // Validate order status - can only return if delivered or completed
        if (order.Status != OrderStatuses.Delivered && order.Status != OrderStatuses.Completed)
        {
            return BadRequest("Order must be delivered or completed to create a return request.");
        }

        // Validate items belong to the order
        var orderItemIds = request.Items.Select(i => i.OrderItemId).ToList();
        var orderItems = await _db.OrderItems
            .Where(oi => oi.OrderId == request.OrderId && orderItemIds.Contains(oi.OrderItemId))
            .ToListAsync(ct);

        if (orderItems.Count != request.Items.Count)
        {
            return BadRequest("Some order items not found.");
        }

        // Validate quantities
        foreach (var item in request.Items)
        {
            var orderItem = orderItems.First(oi => oi.OrderItemId == item.OrderItemId);
            if (item.Quantity > orderItem.Quantity)
            {
                return BadRequest($"Quantity for item {item.OrderItemId} exceeds ordered quantity.");
            }
        }

        var now = DateTime.UtcNow;
        var requestNumber = GenerateReturnRequestNumber(now);

        var returnRequest = new ReturnRequest
        {
            OrderId = request.OrderId,
            CustomerId = userId,
            RequestType = request.RequestType.ToUpperInvariant(),
            RequestNumber = requestNumber,
            Status = OrderStatuses.ReturnRequested,
            Reason = request.Reason,
            Description = request.Description,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.ReturnRequests.Add(returnRequest);
        await _db.SaveChangesAsync(ct);

        // Add return request items
        foreach (var item in request.Items)
        {
            _db.ReturnRequestItems.Add(new ReturnRequestItem
            {
                ReturnRequestId = returnRequest.ReturnRequestId,
                OrderItemId = item.OrderItemId,
                Quantity = item.Quantity
            });
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            returnRequest.ReturnRequestId,
            returnRequest.RequestNumber,
            returnRequest.RequestType,
            returnRequest.Status
        });
    }

    private long GetUserIdOrThrow()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
            throw new InvalidOperationException("Missing user id claim.");
        return userId;
    }

    private static string GenerateReturnRequestNumber(DateTime nowUtc)
    {
        return $"RR{nowUtc:yyMMddHHmmssfff}";
    }
}