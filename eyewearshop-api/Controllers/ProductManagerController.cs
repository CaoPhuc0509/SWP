using eyewearshop_data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/manager/products")]
[Authorize(Roles = "Manager")]
public class ProductManagerController : ControllerBase
{
    private readonly EyewearShopDbContext _db;

    public ProductManagerController(EyewearShopDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// get products list
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var total = await _db.Products.CountAsync(ct);
        var products = await _db.Products
            .AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.Sku,
                p.ProductType,
                p.BasePrice,
                VariantCount = p.Variants.Count,
                p.Status,
                p.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new { Page = page, PageSize = pageSize, Total = total, Items = products });
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var product = new eyewearshop_data.Entities.Product
        {
            ProductName = request.ProductName,
            Sku = request.Sku,
            Description = request.Description,
            ProductType = request.ProductType,
            BasePrice = request.BasePrice,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = 1
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetProductDetail), new { productId = product.ProductId }, product);
    }

    /// <summary>
    /// Get product detail
    /// </summary>
    [HttpGet("{productId}")]
    public async Task<ActionResult> GetProductDetail([FromRoute] long productId, CancellationToken ct)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.ProductId == productId, ct);

        if (product == null)
            return NotFound();

        return Ok(product);
    }

    /// <summary>
    /// update product info
    /// </summary>
    [HttpPut("{productId}")]
    public async Task<ActionResult> UpdateProduct([FromRoute] long productId, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.ProductId == productId, ct);
        if (product == null)
            return NotFound();

        product.ProductName = request.ProductName;
        product.Description = request.Description;
        product.BasePrice = request.BasePrice;
        product.Status = request.Status;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(product);
    }

    /// <summary>
    /// add variant to product (color, price, stockQuantity, preOrderQuantity)
    /// </summary>
    [HttpPost("{productId}/variants")]
    public async Task<ActionResult> AddVariant([FromRoute] long productId, [FromBody] AddVariantRequest request, CancellationToken ct)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.ProductId == productId, ct);
        if (product == null)
            return NotFound("Product not found");

        var variant = new eyewearshop_data.Entities.ProductVariant
        {
            ProductId = productId,
            Color = request.Color,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            PreOrderQuantity = request.PreOrderQuantity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = 1
        };

        _db.ProductVariants.Add(variant);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetProductDetail), new { productId }, variant);
    }

    /// <summary>
    /// update variant of product
    /// </summary>
    [HttpPut("{productId}/variants/{variantId}")]
    public async Task<ActionResult> UpdateVariant([FromRoute] long productId, [FromRoute] long variantId, [FromBody] UpdateVariantRequest request, CancellationToken ct)
    {
        var variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.VariantId == variantId && v.ProductId == productId, ct);
        if (variant == null)
            return NotFound();

        variant.Color = request.Color;
        variant.Price = request.Price;
        variant.StockQuantity = request.StockQuantity;
        variant.PreOrderQuantity = request.PreOrderQuantity;
        variant.Status = request.Status;
        variant.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(variant);
    }
}

public record CreateProductRequest(string ProductName, string Sku, string? Description, string ProductType, decimal? BasePrice, long? CategoryId, long? BrandId);
public record UpdateProductRequest(string ProductName, string? Description, decimal? BasePrice, short Status);
public record AddVariantRequest(string? Color, decimal Price, int StockQuantity, int PreOrderQuantity);
public record UpdateVariantRequest(string? Color, decimal Price, int StockQuantity, int PreOrderQuantity, short Status);
