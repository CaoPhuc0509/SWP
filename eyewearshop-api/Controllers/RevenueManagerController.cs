using eyewearshop_data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/manager/revenue")]
[Authorize(Roles = "Manager")]
public class RevenueManagerController : ControllerBase
{
    private readonly EyewearShopDbContext _db;

    public RevenueManagerController(EyewearShopDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Revenue by day
    /// </summary>
    [HttpGet("daily")]
    public async Task<ActionResult> GetDailyRevenue([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken ct = default)
    {
        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;

        var data = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalRevenue = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count(),
                AvgOrderValue = g.Average(o => o.TotalAmount)
            })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        return Ok(data);
    }

    /// <summary>
    /// Revenue by month
    /// </summary>
    [HttpGet("monthly")]
    public async Task<ActionResult> GetMonthlyRevenue([FromQuery] int? year, CancellationToken ct = default)
    {
        year ??= DateTime.UtcNow.Year;

        var data = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt.Year == year)
            .GroupBy(o => o.CreatedAt.Month)
            .Select(g => new
            {
                Month = g.Key,
                Year = year,
                TotalRevenue = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count(),
                AvgOrderValue = g.Average(o => o.TotalAmount)
            })
            .OrderBy(x => x.Month)
            .ToListAsync(ct);

        return Ok(data);
    }

    /// <summary>
    /// revenue by payment method
    /// </summary>
    [HttpGet("by-payment-method")]
    public async Task<ActionResult> GetRevenueByPaymentMethod([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken ct = default)
    {
        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;

        var data = await _db.Payments
            .AsNoTracking()
            .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate && p.Status == 1)
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new
            {
                PaymentMethod = g.Key,
                TotalAmount = g.Sum(p => p.Amount),
                TransactionCount = g.Count(),
                AvgTransaction = g.Average(p => p.Amount)
            })
            .ToListAsync(ct);

        return Ok(data);
    }

    /// <summary>
    /// Top products by revenue
    /// </summary>
    [HttpGet("top-products")]
    public async Task<ActionResult> GetTopProducts([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;

        var data = await _db.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Order)
            .Where(oi => oi.Order.CreatedAt >= startDate && oi.Order.CreatedAt <= endDate)
            .GroupBy(oi => oi.VariantId)
            .Select(g => new
            {
                VariantId = g.Key,
                QuantitySold = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.UnitPrice * oi.Quantity),
                AvgPrice = g.Average(oi => oi.UnitPrice)
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(limit)
            .ToListAsync(ct);

        return Ok(data);
    }

    /// <summary>
    /// Return rate
    /// </summary>
    [HttpGet("return-rate")]
    public async Task<ActionResult> GetReturnRate([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken ct = default)
    {
        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;

        var totalOrders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .CountAsync(ct);

        var returnedOrders = await _db.ReturnRequests
            .AsNoTracking()
            .Include(rr => rr.Order)
            .Where(rr => rr.Order.CreatedAt >= startDate && rr.Order.CreatedAt <= endDate)
            .Select(rr => rr.OrderId)
            .Distinct()
            .CountAsync(ct);

        var returnAmount = await _db.ReturnRequests
            .AsNoTracking()
            .Include(rr => rr.Order)
            .Where(rr => rr.Order.CreatedAt >= startDate && rr.Order.CreatedAt <= endDate)
            .SumAsync(rr => rr.Order.TotalAmount, ct);

        return Ok(new
        {
            TotalOrders = totalOrders,
            ReturnedOrders = returnedOrders,
            ReturnRate = totalOrders > 0 ? (double)returnedOrders / totalOrders * 100 : 0,
            ReturnAmount = returnAmount
        });
    }

    /// <summary>
    /// Revenue summary
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult> GetRevenueSummary([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken ct = default)
    {
        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;

        var totalRevenue = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .SumAsync(o => o.TotalAmount, ct);

        var totalOrders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .CountAsync(ct);

        var successfulPayments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate && p.Status == 1)
            .CountAsync(ct);

        var uniqueCustomers = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .Select(o => o.CustomerId)
            .Distinct()
            .CountAsync(ct);

        return Ok(new
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            AvgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0,
            SuccessfulPayments = successfulPayments,
            UniqueCustomers = uniqueCustomers
        });
    }
}
