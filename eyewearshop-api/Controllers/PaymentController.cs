using System.Security.Claims;
using eyewearshop_service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eyewearshop_service.Payments;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
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

        var payments = await _paymentService.GetOrderPaymentsAsync(userId, orderId, ct);
        if (payments.Count == 0)
        {
            return NotFound("Order not found.");
        }

        return Ok(payments);
    }

    /// <summary>
    /// Create a payment record for an order (simplified; no real gateway integration).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreatePayment([FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var (result, error, statusCode) = await _paymentService.CreatePaymentAsync(
            userId,
            request.OrderId,
            request.PaymentType,
            request.PaymentMethod,
            request.Amount,
            request.Note,
            ct);

        if (error != null)
        {
            return StatusCode(statusCode ?? 400, error);
        }

        return Ok(result);
    }

    /// <summary>
    /// Confirm a payment (simulates gateway confirmation).
    /// </summary>
    [HttpPut("{paymentId:long}/confirm")]
    public async Task<ActionResult> ConfirmPayment([FromRoute] long paymentId, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var (result, error, statusCode) = await _paymentService.ConfirmPaymentAsync(
            userId,
            paymentId,
            ct);

        if (error != null)
        {
            return StatusCode(statusCode ?? 400, error);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get a single payment detail (must belong to current user).
    /// </summary>
    [HttpGet("{paymentId:long}")]
    public async Task<ActionResult> GetPayment([FromRoute] long paymentId, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var payment = await _paymentService.GetPaymentAsync(userId, paymentId, ct);

        if (payment == null) return NotFound();

        return Ok(payment);
    }

    /// <summary>
    /// Return URL endpoint for VNPay (browser redirect).
    /// Frontend can call this endpoint with the full VNPay query string,
    /// or you can configure VNPay to hit this API directly.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("vnpay/return")]
    public async Task<ActionResult> VnPayReturn(CancellationToken ct)
    {
        var queryParams = Request.Query.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
        var (success, message) = await _paymentService.HandleVnPayReturnAsync(queryParams, ct);
        if (!success)
        {
            return BadRequest(message);
        }

        return Ok(new { message = "VNPay return processed" });
    }

    /// <summary>
    /// Webhook endpoint for MoMo IPN notifications.
    /// This should be configured as the IPN/notify URL in MoMo dashboard.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("momo/ipn")]
    public async Task<ActionResult> MomoIpn([FromBody] MomoIpnRequest request, CancellationToken ct)
    {
        var (success, message) = await _paymentService.HandleMomoIpnAsync(request, ct);
        if (!success)
        {
            // MoMo expects HTTP 200 with resultCode/message in body even on error,
            // but for simplicity we just return BadRequest here. You can adapt if needed.
            return BadRequest(message);
        }

        return Ok(new { message = "IPN processed" });
    }

    /// <summary>
    /// Get high-level payment status for an order, including latest gateway transaction.
    /// </summary>
    [HttpGet("order/{orderId:long}/status")]
    public async Task<ActionResult> GetOrderPaymentStatus([FromRoute] long orderId, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();
        var status = await _paymentService.GetOrderPaymentStatusAsync(userId, orderId, ct);
        if (status == null) return NotFound();
        return Ok(status);
    }

    private long GetUserIdOrThrow()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
            throw new InvalidOperationException("Missing user id claim.");
        return userId;
    }
}