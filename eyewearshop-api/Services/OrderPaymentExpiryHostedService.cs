using eyewearshop_data;
using eyewearshop_data.Entities;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_api.Services;

/// <summary>
/// Periodically soft-deletes orders that are awaiting payment and remain unpaid after a timeout.
/// Business rule:
/// - If OrderStatus == AwaitingPayment (8) and PaymentStatus == Unpaid for 30 minutes => OrderStatus becomes Deleted (9)
/// </summary>
public sealed class OrderPaymentExpiryHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public OrderPaymentExpiryHostedService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireOrdersAsync(stoppingToken);
            }
            catch
            {
                // Intentionally swallow exceptions to keep the background loop alive.
                // Consider adding structured logging if needed.
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ExpireOrdersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EyewearShopDbContext>();

        var cutoff = DateTime.UtcNow.AddMinutes(-30);

        var candidates = await db.Orders
            .Where(o =>
                o.Status == OrderStatuses.AwaitingPayment &&
                o.PaymentStatus == PaymentStatuses.Unpaid &&
                o.CreatedAt <= cutoff)
            .ToListAsync(ct);

        if (candidates.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var order in candidates)
        {
            order.Status = OrderStatuses.Deleted;
            order.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
    }
}

