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
    Task ChangeStatusAsync(Guid orderId, short newStatus, string role);

}

