namespace eyewearshop_service.Interfaces;

public interface IPaymentService
{
    Task<IReadOnlyList<object>> GetOrderPaymentsAsync(long customerId, long orderId, CancellationToken ct = default);

    Task<(object? result, string? error, int? statusCode)> CreatePaymentAsync(
        long customerId,
        long orderId,
        string paymentType,
        string? paymentMethod,
        decimal amount,
        string? note,
        CancellationToken ct = default);

    Task<(object? result, string? error, int? statusCode)> ConfirmPaymentAsync(
        long customerId,
        long paymentId,
        CancellationToken ct = default);

    Task<object?> GetPaymentAsync(long customerId, long paymentId, CancellationToken ct = default);

    /// <summary>
    /// Used by MoMo IPN/webhook to update payment transaction and create a confirmed payment.
    /// </summary>
    Task<(bool success, string message)> HandleMomoIpnAsync(eyewearshop_service.Payments.MomoIpnRequest request, CancellationToken ct = default);

    /// <summary>
    /// Summarised payment status for an order, including latest gateway transaction.
    /// </summary>
    Task<object?> GetOrderPaymentStatusAsync(long customerId, long orderId, CancellationToken ct = default);

    /// <summary>
    /// Handle VNPay return URL (browser redirect) and update payment transaction/payment status.
    /// </summary>
    Task<(bool success, string message)> HandleVnPayReturnAsync(IDictionary<string, string> queryParams, CancellationToken ct = default);
}

