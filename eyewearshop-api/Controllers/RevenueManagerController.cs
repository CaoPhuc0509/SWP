using System.Drawing;
using eyewearshop_data;
using eyewearshop_data.Entities;
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
    /// Revenue by day (include return orders, return items) (format: mm/dd/yy)
    /// </summary>
    [HttpGet("daily")]
    public async Task<ActionResult> GetDailyRevenue(
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate,
    CancellationToken ct = default)
    {
        startDate = (startDate ?? DateTime.UtcNow.AddDays(-30)).ToUniversalTime();
        endDate = (endDate ?? DateTime.UtcNow).ToUniversalTime();

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
    /// Revenue by month (group by month) (format: mm/yyyy)
    /// </summary>
    [HttpGet("monthly")]
    public async Task<ActionResult> GetMonthlyRevenue(
    [FromQuery] int? month,
    [FromQuery] int? year,
    CancellationToken ct = default)
    {
        month ??= DateTime.UtcNow.Month;
        year ??= DateTime.UtcNow.Year;

        var data = await _db.Orders
            .AsNoTracking()
            .Where(o =>
                o.CreatedAt.Month == month &&
                o.CreatedAt.Year == year)
            .Select(o => new
            {
                Month = o.CreatedAt.Month,
                Year = o.CreatedAt.Year,
                o.TotalAmount,
                OrderId = o.OrderId
            })
            .GroupBy(x => new { x.Year, x.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
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
            .ToListAsync(ct);

        return Ok(data);
    }

    /// <summary>
    /// Revenue by year (group by year) (format: yyyy)
    /// </summary>
    [HttpGet("yearly")]
    public async Task<ActionResult> GetYearRevenue(
     [FromQuery] int? startYear,
     [FromQuery] int? endYear,
     CancellationToken ct = default)
    {
        startYear ??= DateTime.UtcNow.Year - 5;
        endYear ??= DateTime.UtcNow.Year;

        var ordersQuery = _db.Orders
            .AsNoTracking()
            .Where(o =>
                o.CreatedAt.Year >= startYear &&
                o.CreatedAt.Year <= endYear);

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
            .Where(rr =>
                rr.Order.CreatedAt.Year >= startYear &&
                rr.Order.CreatedAt.Year <= endYear)
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
            .Where(rri =>
                rri.ReturnRequest.Order.CreatedAt.Year >= startYear &&
                rri.ReturnRequest.Order.CreatedAt.Year <= endYear)
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
                    .FirstOrDefault(r => r.Year == y.Year)?.ReturnedItems ?? 0
            })
            .OrderBy(x => x.Year)
            .ToList();

        return Ok(result);
    }


    /// <summary>
    /// Top products by revenue (by month) (format: mm/yyyy) limit by top n products (default 10)
    /// </summary>
    [HttpGet("top-products")]
    public async Task<ActionResult> GetTopProducts(
    [FromQuery] int? month,
    [FromQuery] int? year,
    [FromQuery] int? limit,
    CancellationToken ct = default)
    {
        month ??= DateTime.UtcNow.Month;
        year ??= DateTime.UtcNow.Year;
        var top = limit ?? 10;

        var topVariants = await _db.OrderItems
            .AsNoTracking()
            .Where(oi =>
                oi.Order.Status == 2 &&
                oi.Status == 1 &&
                oi.Order.CreatedAt.Month == month &&
                oi.Order.CreatedAt.Year == year)
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

        var variantIds = topVariants.Select(x => x.VariantId).ToList();

        // B2: load full data
        var variants = await _db.ProductVariants
            .AsNoTracking()
            .Where(v => variantIds.Contains(v.VariantId))
            .Include(v => v.Product)
                .ThenInclude(p => p.Brand)
            .Include(v => v.Product)
                .ThenInclude(p => p.Category)
            .Include(v => v.Images)
            .ToListAsync(ct);

        // B3: merge lại
        var result = topVariants.Select(tv =>
        {
            var v = variants.FirstOrDefault(x => x.VariantId == tv.VariantId);

            return new
            {
                // FULL PRODUCT
                Product = v?.Product == null ? null : new
                {
                    v.Product.ProductId,
                    v.Product.ProductName,
                    v.Product.Sku,
                    v.Product.ProductType,
                    v.Product.BasePrice,
                    v.Product.Specifications,
                    v.Product.Status,

                    Brand = v.Product.Brand == null ? null : new
                    {
                        v.Product.Brand.BrandId,
                        v.Product.Brand.BrandName
                    },

                    Category = v.Product.Category == null ? null : new
                    {
                        v.Product.Category.CategoryId,
                        v.Product.Category.CategoryName
                    }
                },

                // FULL VARIANT
                Variant = v == null ? null : new
                {
                    v.VariantId,
                    v.VariantSku,
                    v.Color,
                    v.Price,
                    v.BaseCurve,
                    v.Diameter,
                    v.RefractiveIndex,
                    v.StockQuantity,
                    v.PreOrderQuantity,
                    v.ExpectedDateRestock,
                    v.Status,

                    // lấy ảnh primary
                    Image = v.Images
                        .Where(i => i.IsPrimary)
                        .Select(i => new
                        {
                            i.ImageId,
                            i.Url
                        })
                        .FirstOrDefault()
                },

                // stats
                tv.QuantitySold,
                tv.TotalRevenue,
                tv.AvgPrice
            };
        });

        return Ok(result);
    }



    /// <summary>
    /// Revenue summary format: mm/dd/yy
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult> GetRevenueSummary(
    [FromQuery] DateTime? startDate,
    [FromQuery] DateTime? endDate,
    CancellationToken ct = default)
    {
        startDate = (startDate ?? DateTime.UtcNow.AddDays(-30)).ToUniversalTime();
        endDate = (endDate ?? DateTime.UtcNow).ToUniversalTime();

        var ordersQuery = _db.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= startDate &&
                        o.CreatedAt <= endDate &&
                        o.Status == 2);

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