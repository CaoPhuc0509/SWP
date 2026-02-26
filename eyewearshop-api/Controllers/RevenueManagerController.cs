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
    /// Revenue by day (include return orders, return items)
    /// </summary>
    [HttpGet("daily")]
    public async Task<ActionResult> GetDailyRevenue(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken ct = default)
    {
        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;

        var data = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .Select(o => new
            {
                Date = o.CreatedAt.Date,
                o.TotalAmount,
                OrderId = o.OrderId
            })
            .GroupBy(x => x.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalRevenue = g.Sum(x => x.TotalAmount),
                TotalOrders = g.Count(),

                ReturnedOrders = _db.ReturnRequests
                    .Where(rr => g.Select(x => x.OrderId).Contains(rr.OrderId))
                    .Select(rr => rr.OrderId)
                    .Distinct()
                    .Count(),

                ReturnedItems = _db.ReturnRequestItems
                    .Where(rri => g.Select(x => x.OrderId)
                    .Contains(rri.ReturnRequest.OrderId))
                    .Sum(rri => (int?)rri.Quantity) ?? 0
            })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        return Ok(data);
    }

    /// <summary>
    /// Revenue by month (group by month)
    /// </summary>
    [HttpGet("monthly")]
    public async Task<ActionResult> GetMonthlyRevenue(
        [FromQuery] int? month,
        CancellationToken ct = default)
    {
        month ??= DateTime.UtcNow.Month;

        var data = await _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt.Month == month)
            .Select(o => new
            {
                Month = o.CreatedAt.Month,
                o.TotalAmount,
                OrderId = o.OrderId
            })
            .GroupBy(x => x.Month)
            .Select(g => new
            {
                Month = g.Key,
                TotalRevenue = g.Sum(x => x.TotalAmount),
                TotalOrders = g.Count(),

                ReturnedOrders = _db.ReturnRequests
                    .Where(rr => g.Select(x => x.OrderId).Contains(rr.OrderId))
                    .Select(rr => rr.OrderId)
                    .Distinct()
                    .Count(),

                ReturnedItems = _db.ReturnRequestItems
                    .Where(rri => g.Select(x => x.OrderId)
                    .Contains(rri.ReturnRequest.OrderId))
                    .Sum(rri => (int?)rri.Quantity) ?? 0
            })
            .OrderBy(x => x.Month)
            .ToListAsync(ct);

        return Ok(data);
    }

    [HttpGet("yearly")]
    public async Task<ActionResult> GetYearRevenue(
    [FromQuery] DateTime? startYear,
    [FromQuery] DateTime? endYear,
    CancellationToken ct = default)
    {
        startYear ??= DateTime.UtcNow.AddYears(-5);
        endYear ??= DateTime.UtcNow;

        var ordersQuery = _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= startYear && o.CreatedAt <= endYear);

        var yearlyOrders = await ordersQuery
            .GroupBy(o => o.CreatedAt.Year)
            .Select(g => new
            {
                Year = g.Key,
                TotalRevenue = g.Sum(x => x.TotalAmount),
                TotalOrders = g.Count()
            })
            .ToListAsync(ct);

        var yearlyReturnedOrders = await _db.ReturnRequests
            .AsNoTracking()
            .Where(rr => rr.Order.CreatedAt >= startYear &&
                         rr.Order.CreatedAt <= endYear)
            .GroupBy(rr => rr.Order.CreatedAt.Year)
            .Select(g => new
            {
                Year = g.Key,
                ReturnedOrders = g.Select(x => x.OrderId)
                                  .Distinct()
                                  .Count()
            })
            .ToListAsync(ct);

        var yearlyReturnedItems = await _db.ReturnRequestItems
            .AsNoTracking()
            .Where(rri => rri.ReturnRequest.Order.CreatedAt >= startYear &&
                          rri.ReturnRequest.Order.CreatedAt <= endYear)
            .GroupBy(rri => rri.ReturnRequest.Order.CreatedAt.Year)
            .Select(g => new
            {
                Year = g.Key,
                ReturnedItems = g.Sum(x => x.Quantity)
            })
            .ToListAsync(ct);

        var result = yearlyOrders
            .Select(y => new
            {
                Year = y.Year,
                y.TotalRevenue,
                y.TotalOrders,
                ReturnedOrders = yearlyReturnedOrders
                    .FirstOrDefault(r => r.Year == y.Year)?.ReturnedOrders ?? 0,
                ReturnedItems = yearlyReturnedItems
                    .FirstOrDefault(r => r.Year == y.Year)?.ReturnedItems ?? 0,
                ReturnRate = y.TotalOrders > 0
                    ? (double)(
                        yearlyReturnedOrders
                            .FirstOrDefault(r => r.Year == y.Year)?.ReturnedOrders ?? 0
                      ) / y.TotalOrders * 100
                    : 0
            })
            .OrderBy(x => x.Year)
            .ToList();

        return Ok(result);
    }


    /// <summary>
    /// Top products by revenue (by month)
    /// </summary>
    [HttpGet("top-products")]
    public async Task<ActionResult> GetTopProducts(
        [FromQuery] DateTime? month,
        [FromQuery] int? limit,
        CancellationToken ct = default)
    {
        var targetMonth = month ?? DateTime.UtcNow;
        var top = limit ?? 10;

        var data = await _db.OrderItems
            .AsNoTracking()
            .Where(oi =>
                oi.Order.Status == 2 &&          // completed orders
                oi.Status == 1 &&                // active item
                oi.Order.CreatedAt.Year == targetMonth.Year &&
                oi.Order.CreatedAt.Month == targetMonth.Month)
            .GroupBy(oi => oi.VariantId)
            .Select(g => new
            {
                VariantId = g.Key,
                QuantitySold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.UnitPrice * x.Quantity),
                AvgPrice = g.Average(x => x.UnitPrice)
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(top)
            .ToListAsync(ct);

        return Ok(data);
    }



    /// <summary>
    /// Revenue summary
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult> GetRevenueSummary(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken ct = default)
    {
        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;

        var ordersQuery = _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= startDate &&
                        o.CreatedAt <= endDate &&
                        o.Status == 2); // chỉ tính order completed

        var totalRevenue = await ordersQuery
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0;

        var totalOrders = await ordersQuery
            .CountAsync(ct);

        var returnedOrdersQuery = _db.ReturnRequests
            .AsNoTracking()
            .Where(rr => rr.Order.CreatedAt >= startDate &&
                         rr.Order.CreatedAt <= endDate);

        var returnedOrders = await returnedOrdersQuery
            .Select(rr => rr.OrderId)
            .Distinct()
            .CountAsync(ct);

        var returnedRevenue = await returnedOrdersQuery
            .SumAsync(rr => (decimal?)rr.Order.TotalAmount, ct) ?? 0;

        var netRevenue = totalRevenue - returnedRevenue;

        return Ok(new
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            ReturnedOrders = returnedOrders,
            ReturnedRevenue = returnedRevenue,
            NetRevenue = netRevenue,
            ReturnRate = totalOrders > 0
                ? (double)returnedOrders / totalOrders * 100
                : 0
        });
    }
}