namespace eyewearshop_service.Interfaces;

public interface IOrderService
{
    Task<object> GetMyOrdersAsync(
        long customerId,
        string? orderType,
        short? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<object?> GetOrderDetailAsync(
        long customerId,
        long orderId,
        CancellationToken ct = default);
    Task ChangeStatusAsync(long orderId, short newStatus, string role);

    /// <summary>
    /// Customer soft-delete for orders that are awaiting payment and still unpaid.
    /// </summary>
    Task<(bool success, string? error, int? statusCode)> DeleteAwaitingPaymentOrderAsync(
        long customerId,
        long orderId,
        CancellationToken ct = default);

}

