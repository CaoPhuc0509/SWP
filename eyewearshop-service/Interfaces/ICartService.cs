namespace eyewearshop_service.Interfaces;

public interface ICartService
{
    Task<object> GetCartSummaryAsync(CancellationToken ct = default);

    Task<(object? result, string? error, int? statusCode)> AddItemAsync(long variantId, int quantity, CancellationToken ct = default);

    Task<(object? result, string? error, int? statusCode)> UpdateItemAsync(long variantId, int quantity, CancellationToken ct = default);

    Task<(bool success, string? error, int? statusCode)> RemoveItemAsync(long variantId);

    void ClearCart();
}

