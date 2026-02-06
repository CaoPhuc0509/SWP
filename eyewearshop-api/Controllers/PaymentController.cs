using System.Security.Claims;
using eyewearshop_data;
using eyewearshop_data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly EyewearShopDbContext _db;

    public PaymentController(EyewearShopDbContext db)
    {
        _db = db;
    }

    public record CreatePaymentRequest(
        long OrderId,
        string PaymentType, // CASH, CARD, BANK_TRANSFER, E_WALLET, etc.
        string PaymentMethod, // VISA, MASTERCARD, VIETQR, MOMO, etc.
        decimal Amount,
        string? Note);

    /// <summary>
    /// List payments for a specific order (must belong to current user).
    /// </summary>
    [HttpGet("order/{orderId:long}")]
    public async Task<ActionResult> GetOrderPayments([FromRoute] long orderId, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        // Verify order belongs to customer
        var orderExists = await _db.Orders
            .AnyAsync(o => o.OrderId == orderId && o.CustomerId == userId, ct);

        if (!orderExists) return NotFound("Order not found.");

        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.PaymentId,
                p.PaymentType,
                p.PaymentMethod,
                p.Amount,
                p.Status,
                p.Note,
                p.CreatedAt,
                p.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(payments);
    }

    /// <summary>
    /// Create a payment record for an order (simplified; no real gateway integration).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreatePayment([FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        if (request.Amount <= 0)
            return BadRequest("Payment amount must be greater than 0.");

        // Verify order belongs to customer
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.OrderId == request.OrderId && o.CustomerId == userId, ct);

        if (order == null) return NotFound("Order not found.");

        // Check if order is in a valid state for payment
        if (order.Status == OrderStatuses.Cancelled)
            return BadRequest("Cannot make payment for a cancelled order.");

        // Calculate total paid amount
        var totalPaid = await _db.Payments
            .Where(p => p.OrderId == request.OrderId && p.Status == 1) // Status 1 = successful
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        // Check if payment amount exceeds remaining balance
        var remainingBalance = order.TotalAmount - totalPaid;
        if (request.Amount > remainingBalance)
            return BadRequest($"Payment amount ({request.Amount}) exceeds remaining balance ({remainingBalance}).");

        var now = DateTime.UtcNow;
        var payment = new Payment
        {
            OrderId = request.OrderId,
            CustomerId = userId,
            PaymentType = request.PaymentType.Trim(),
            PaymentMethod = request.PaymentMethod?.Trim(),
            Amount = request.Amount,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            Status = 0, // 0 = pending, 1 = completed, 2 = failed
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct);

        // If payment is full or over, you might want to update order status
        // This is simplified - in production, you'd integrate with payment gateways
        var newTotalPaid = totalPaid + request.Amount;
        if (newTotalPaid >= order.TotalAmount)
        {
            // Payment is complete (or overpaid)
            // In a real system, you'd wait for payment gateway confirmation
            // For now, we'll just mark the payment as pending
        }

        return Ok(new
        {
            payment.PaymentId,
            payment.OrderId,
            payment.PaymentType,
            payment.PaymentMethod,
            payment.Amount,
            payment.Status,
            payment.CreatedAt
        });
    }

    /// <summary>
    /// Confirm a payment (simulates gateway confirmation).
    /// </summary>
    [HttpPut("{paymentId:long}/confirm")]
    public async Task<ActionResult> ConfirmPayment([FromRoute] long paymentId, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var payment = await _db.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.CustomerId == userId, ct);

        if (payment == null) return NotFound("Payment not found.");

        if (payment.Status == 1) // Already confirmed
            return BadRequest("Payment is already confirmed.");

        // In a real system, this would be called by a webhook from the payment gateway
        // For now, we'll allow manual confirmation
        payment.Status = 1; // Completed
        payment.UpdatedAt = DateTime.UtcNow;

        // Check if order is fully paid
        var totalPaid = await _db.Payments
            .Where(p => p.OrderId == payment.OrderId && p.Status == 1)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        var order = payment.Order!;
        if (totalPaid >= order.TotalAmount && order.Status == OrderStatuses.Pending)
        {
            // Order is fully paid, but status update should be done by staff
            // For now, we'll leave it as pending for staff validation
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            payment.PaymentId,
            payment.Status,
            payment.UpdatedAt
        });
    }

    /// <summary>
    /// Get a single payment detail (must belong to current user).
    /// </summary>
    [HttpGet("{paymentId:long}")]
    public async Task<ActionResult> GetPayment([FromRoute] long paymentId, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var payment = await _db.Payments
            .AsNoTracking()
            .Where(p => p.PaymentId == paymentId && p.CustomerId == userId)
            .Select(p => new
            {
                p.PaymentId,
                p.OrderId,
                p.PaymentType,
                p.PaymentMethod,
                p.Amount,
                p.Status,
                p.Note,
                p.CreatedAt,
                p.UpdatedAt,
                Order = new
                {
                    p.Order!.OrderNumber,
                    p.Order.TotalAmount
                }
            })
            .FirstOrDefaultAsync(ct);

        if (payment == null) return NotFound();

        return Ok(payment);
    }

    private long GetUserIdOrThrow()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
            throw new InvalidOperationException("Missing user id claim.");
        return userId;
    }
}