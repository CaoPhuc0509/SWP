using eyewearshop_data.Entities;

namespace eyewearshop_service.Payments;

public record PaymentRedirectResult(
    string Gateway,
    string RequestId,
    string PayUrl,
    decimal Amount,
    string Currency);

public record PaymentCallbackResult(
    string Gateway,
    string OrderId,
    string RequestId,
    string? GatewayTransactionId,
    bool IsSuccess,
    string? ErrorCode,
    string? Message);

/// <summary>
/// Abstraction over different payment gateways (MoMo, VietQR, etc.).
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Gateway identifier, e.g. MOMO, VIETQR.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Create a payment session for an order and return redirect URL/info.
    /// </summary>
    Task<PaymentRedirectResult> CreatePaymentAsync(Order order, decimal amount, CancellationToken ct = default);
}

