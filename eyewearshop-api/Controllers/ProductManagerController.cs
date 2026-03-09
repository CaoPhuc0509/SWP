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
    public async Task<ActionResult> GetProducts(CancellationToken ct = default)
    {
        var products = await _db.Products
            .AsNoTracking()
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

        return Ok(products);
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
            Specifications = request.Specifications,
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
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Include(p => p.SunglassesSpec)
            .Include(p => p.FrameSpec)
            .Include(p => p.RxLensSpec)
            .Include(p => p.ContactLensSpec)
            .Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.Sku,
                p.Description,
                p.ProductType,
                p.BasePrice,
                p.Specifications,
                Category = p.Category != null ? new { p.Category.CategoryId, p.Category.CategoryName } : null,
                Brand = p.Brand != null ? new { p.Brand.BrandId, p.Brand.BrandName } : null,
                p.Variants,
                p.Images,
                p.SunglassesSpec,
                p.FrameSpec,
                p.RxLensSpec,
                p.ContactLensSpec,
                p.Status,
                p.CreatedAt,
                p.UpdatedAt
            })
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

        if (!string.IsNullOrEmpty(request.ProductName))
            product.ProductName = request.ProductName;

        if (!string.IsNullOrEmpty(request.Sku))
            product.Sku = request.Sku;

        if (!string.IsNullOrEmpty(request.Description))
            product.Description = request.Description;

        if (!string.IsNullOrEmpty(request.ProductType))
            product.ProductType = request.ProductType;

        if (request.BasePrice.HasValue)
            product.BasePrice = request.BasePrice;

        if (request.CategoryId.HasValue)
            product.CategoryId = request.CategoryId;

        if (request.BrandId.HasValue)
            product.BrandId = request.BrandId;

        if (!string.IsNullOrEmpty(request.Specifications))
            product.Specifications = request.Specifications;

        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(product);
    }

    /// <summary>
    /// add variant to product (color, price, stockQuantity, preOrderQuantity)
    /// </summary>
    [HttpPost("{productId}/variants")]
    public async Task<ActionResult> AddVariant(
     [FromRoute] long productId,
     [FromBody] AddVariantRequest request,
     CancellationToken ct)
    {
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == productId, ct);

        if (product == null)
            return NotFound("Product not found");

        var variant = new eyewearshop_data.Entities.ProductVariant
        {
            ProductId = productId,
            Color = request.Color,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            PreOrderQuantity = request.PreOrderQuantity,
            VariantSku = request.VariantSku,
            BaseCurve = request.BaseCurve,
            Diameter = request.Diameter,
            RefractiveIndex = request.RefractiveIndex,
            ExpectedDateRestock = request.ExpectedDateRestock,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = 1
        };

        _db.ProductVariants.Add(variant);
        await _db.SaveChangesAsync(ct);

        
        var response = new VariantResponse(
            variant.VariantId,
            variant.ProductId,
            variant.Color,
            variant.Price,
            variant.StockQuantity,
            variant.PreOrderQuantity,
            variant.VariantSku,
            variant.BaseCurve,
            variant.Diameter,
            variant.RefractiveIndex,
            variant.ExpectedDateRestock,
            variant.Status
        );

        return CreatedAtAction(
            nameof(GetProductDetail),
            new { productId },
            response
        );
    }

    /// <summary>
    /// update variant of product
    /// </summary>
    [HttpPut("{productId}/variants/{variantId}")]
    public async Task<ActionResult> UpdateVariant(
    [FromRoute] long productId,
    [FromRoute] long variantId,
    [FromBody] UpdateVariantRequest request,
    CancellationToken ct)
    {
        var variant = await _db.ProductVariants
            .FirstOrDefaultAsync(v => v.VariantId == variantId && v.ProductId == productId, ct);

        if (variant == null)
            return NotFound();

        variant.Color = request.Color;
        variant.Price = request.Price;
        variant.StockQuantity = request.StockQuantity;
        variant.PreOrderQuantity = request.PreOrderQuantity;
        variant.VariantSku = request.VariantSku;
        variant.BaseCurve = request.BaseCurve;
        variant.Diameter = request.Diameter;
        variant.RefractiveIndex = request.RefractiveIndex;
        variant.ExpectedDateRestock = request.ExpectedDateRestock;
        variant.Status = request.Status;
        variant.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(variant);
    }
    [HttpPatch("{productId}/delete")]
    public async Task<ActionResult> SoftDeleteProduct(
    [FromRoute] long productId,
    CancellationToken ct)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.ProductId == productId, ct);

        if (product == null)
            return NotFound("Product not found");

        product.Status = 0;
        product.UpdatedAt = DateTime.UtcNow;

        var variants = await _db.ProductVariants
            .Where(v => v.ProductId == productId)
            .ToListAsync(ct);

        foreach (var variant in variants)
        {
            variant.Status = 0;
            variant.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            Message = "Product deleted successfully",
            product.ProductId,
            product.Status
        });
    }
}


public record CreateProductRequest(string ProductName, string Sku, string? Description, string ProductType, decimal? BasePrice, long? CategoryId, long? BrandId, string? Specifications);
public record UpdateProductRequest(string? ProductName, string? Sku, string? Description, string? ProductType, decimal? BasePrice, long? CategoryId, long? BrandId, string? Specifications);
public record AddVariantRequest(
    string? Color,
    decimal Price,
    int StockQuantity,
    int PreOrderQuantity,
    string VariantSku,
    decimal? BaseCurve,
    decimal? Diameter,
    decimal? RefractiveIndex,
    DateTime? ExpectedDateRestock
);
public record UpdateVariantRequest(
    string? Color,
    decimal Price,
    int StockQuantity,
    int PreOrderQuantity,
    string VariantSku,
    decimal? BaseCurve,
    decimal? Diameter,
    decimal? RefractiveIndex,
    DateTime? ExpectedDateRestock,
    short Status
);
public record VariantResponse(
    long VariantId,
    long ProductId,
    string? Color,
    decimal Price,
    int StockQuantity,
    int PreOrderQuantity,
    string VariantSku,
    decimal? BaseCurve,
    decimal? Diameter,
    decimal? RefractiveIndex,
    DateTime? ExpectedDateRestock,
    short Status
);