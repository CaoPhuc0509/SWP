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
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.Status == OrderStatuses.Completed)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalRevenue = g.Sum(x => x.TotalAmount),
                TotalOrders = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        var returnsData = await (
            from rri in _db.ReturnRequestItems
            join rr in _db.ReturnRequests on rri.ReturnRequestId equals rr.ReturnRequestId
            join o in _db.Orders on rr.OrderId equals o.OrderId
            where o.CreatedAt >= startDate &&
                  o.CreatedAt <= endDate &&
                  rr.Status == ReturnRequestStatuses.Completed
            group new { rri, rr } by o.CreatedAt.Date into g
            select new
            {
                Date = g.Key,
                ReturnedOrders = g.Select(x => x.rr.OrderId).Distinct().Count(),
                ReturnedItems = g.Sum(x => (int?)x.rri.Quantity) ?? 0,
                TotalReturnedRevenue = g.Sum(x => (decimal?)(x.rri.OrderItem.UnitPrice * x.rri.Quantity)) ?? 0
            }
        ).ToListAsync(ct);

        return Ok(
            data.Select(d => 
            {
                var ret = returnsData.FirstOrDefault(r => r.Date == d.Date);
                var totalReturnedRevenue = ret?.TotalReturnedRevenue ?? 0;
                return new
                {
                    Date = d.Date.ToString("MM/dd/yyyy"),
                    d.TotalRevenue,
                    d.TotalOrders,
                    ReturnedOrders = ret?.ReturnedOrders ?? 0,
                    ReturnedItems = ret?.ReturnedItems ?? 0,
                    TotalReturnedRevenue = totalReturnedRevenue,
                    NetRevenue = d.TotalRevenue - totalReturnedRevenue,
                };
            })
        );
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
                o.CreatedAt.Year == year
                && o.Status == OrderStatuses.Completed)
            .GroupBy(x => new { x.CreatedAt.Year, x.CreatedAt.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalRevenue = g.Sum(x => x.TotalAmount),
                TotalOrders = g.Count()
            })
            .ToListAsync(ct);

        var returnsData = await (
            from rri in _db.ReturnRequestItems
            join rr in _db.ReturnRequests on rri.ReturnRequestId equals rr.ReturnRequestId
            join o in _db.Orders on rr.OrderId equals o.OrderId
            where o.CreatedAt.Month == month &&
                  o.CreatedAt.Year == year &&
                  rr.Status == ReturnRequestStatuses.Completed
            group new { rri, rr } by new { o.CreatedAt.Year, o.CreatedAt.Month } into g
            select new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                ReturnedOrders = g.Select(x => x.rr.OrderId).Distinct().Count(),
                ReturnedItems = g.Sum(x => (int?)x.rri.Quantity) ?? 0,
                TotalReturnedRevenue = g.Sum(x => (decimal?)(x.rri.OrderItem.UnitPrice * x.rri.Quantity)) ?? 0
            }
        ).ToListAsync(ct);

        return Ok(
            data.Select(d => 
            {
                var ret = returnsData.FirstOrDefault(r => r.Year == d.Year && r.Month == d.Month);
                var totalReturnedRevenue = ret?.TotalReturnedRevenue ?? 0;
                return new
                {
                    Month = $"{d.Month:00}/{d.Year}",
                    d.TotalRevenue,
                    d.TotalOrders,
                    ReturnedOrders = ret?.ReturnedOrders ?? 0,
                    ReturnedItems = ret?.ReturnedItems ?? 0,
                    TotalReturnedRevenue = totalReturnedRevenue,
                    NetRevenue = d.TotalRevenue - totalReturnedRevenue,
                };
            })
        );
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

        var data = await _db.Orders
            .AsNoTracking()
            .Where(o =>
                o.CreatedAt.Year >= startYear &&
                o.CreatedAt.Year <= endYear
                && o.Status == OrderStatuses.Completed)
            .GroupBy(o => o.CreatedAt.Year)
            .Select(g => new
            {
                Year = g.Key,
                TotalRevenue = g.Sum(x => x.TotalAmount),
                TotalOrders = g.Count()
            })
            .OrderBy(x => x.Year)
            .ToListAsync(ct);

        var returnsData = await (
            from rri in _db.ReturnRequestItems
            join rr in _db.ReturnRequests on rri.ReturnRequestId equals rr.ReturnRequestId
            join o in _db.Orders on rr.OrderId equals o.OrderId
            where o.CreatedAt.Year >= startYear &&
                  o.CreatedAt.Year <= endYear &&
                  rr.Status == ReturnRequestStatuses.Completed
            group new { rri, rr } by o.CreatedAt.Year into g
            select new
            {
                Year = g.Key,
                ReturnedOrders = g.Select(x => x.rr.OrderId).Distinct().Count(),
                ReturnedItems = g.Sum(x => (int?)x.rri.Quantity) ?? 0,
                TotalReturnedRevenue = g.Sum(x => (decimal?)(x.rri.OrderItem.UnitPrice * x.rri.Quantity)) ?? 0
            }
        ).ToListAsync(ct);

        return Ok(
            data.Select(d => 
            {
                var ret = returnsData.FirstOrDefault(r => r.Year == d.Year);
                var totalReturnedRevenue = ret?.TotalReturnedRevenue ?? 0;
                return new
                {
                    d.Year,
                    d.TotalRevenue,
                    d.TotalOrders,
                    ReturnedOrders = ret?.ReturnedOrders ?? 0,
                    ReturnedItems = ret?.ReturnedItems ?? 0,
                    TotalReturnedRevenue = totalReturnedRevenue,
                    NetRevenue = d.TotalRevenue - totalReturnedRevenue
                };
            })
        );
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
        try
        {
            month ??= DateTime.UtcNow.Month;
            year ??= DateTime.UtcNow.Year;

            if (month < 1 || month > 12)
                return BadRequest(new { Message = "Invalid month. Month must be between 1 and 12." });

            var top = limit ?? 10;
            if (top <= 0)
                return BadRequest(new { Message = "Limit must be greater than 0." });

            var topVariants = await _db.OrderItems
                .AsNoTracking()
                .Where(oi =>
                    oi.Order.Status == OrderStatuses.Completed &&
                    oi.Order.CreatedAt.Month == month &&
                    oi.Order.CreatedAt.Year == year &&
                    oi.VariantId != null) // Fix potential null VariantId crash
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

            return Ok(
                result.Select(r => new
                {
                    r.Product,
                    r.Variant,
                    r.QuantitySold,
                    r.TotalRevenue,
                    r.AvgPrice
                })
            );
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching top products.", Error = ex.Message });
        }
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
        try
        {
            startDate = (startDate ?? DateTime.UtcNow.AddDays(-30)).ToUniversalTime();
            endDate = (endDate ?? DateTime.UtcNow).ToUniversalTime();

            if (startDate > endDate)
                return BadRequest(new { Message = "Start date cannot be greater than end date." });

            var ordersQuery = _db.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAt >= startDate &&
                            o.CreatedAt <= endDate && o.Status == OrderStatuses.Completed);

            var totalRevenue = await ordersQuery
                .SumAsync(o => (decimal?)o.TotalAmount, ct);

            var totalOrders = await ordersQuery
                .CountAsync(ct);

            // Fetch only returned orders that are completed and is a RETURN type
            var returnedOrdersQuery = _db.ReturnRequests
                .AsNoTracking()
                .Where(rr => rr.Order.CreatedAt >= startDate &&
                             rr.Order.CreatedAt <= endDate &&
                             rr.Status == ReturnRequestStatuses.Completed
                             );

            var returnedOrders = await returnedOrdersQuery
                .Select(rr => rr.OrderId)
                .Distinct()
                .CountAsync(ct);

            // Accurately sum the revenue of returned items rather than entire order amount
            var returnedRevenue = await (
                from rri in _db.ReturnRequestItems
                join rr in _db.ReturnRequests on rri.ReturnRequestId equals rr.ReturnRequestId
                join o in _db.Orders on rr.OrderId equals o.OrderId
                where o.CreatedAt >= startDate &&
                    o.CreatedAt <= endDate &&
                    rr.Status == ReturnRequestStatuses.Completed
                select (decimal?)(rri.OrderItem.UnitPrice * rri.Quantity)
                ).SumAsync(ct) ?? 0;

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
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while fetching revenue summary.", Error = ex.Message });
        }
    }
}