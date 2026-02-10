using eyewearshop_service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public CatalogController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    /// <summary>
    /// List active product categories for filters/navigation.
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult> GetCategories(CancellationToken ct)
    {
        var categories = await _catalogService.GetActiveCategoriesAsync(ct);
        return Ok(categories);
    }

    /// <summary>
    /// List active brands for filters/navigation.
    /// </summary>
    [HttpGet("brands")]
    public async Task<ActionResult> GetBrands(CancellationToken ct)
    {
        var brands = await _catalogService.GetActiveBrandsAsync(ct);
        return Ok(brands);
    }

    /// <summary>
    /// Search/browse products with filtering and pagination.
    /// Supports filtering by product type, category, brand, variant color, price range, and frame dimensions (A/B/DBL).
    /// </summary>
    [HttpGet("products")]
    public async Task<ActionResult> GetProducts(
        [FromQuery] string? q,
        [FromQuery] string? productType,
        [FromQuery] long? categoryId,
        [FromQuery] long? brandId,
        [FromQuery] string? color,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] decimal? minA, // Frame size A
        [FromQuery] decimal? maxA,
        [FromQuery] decimal? minB, // Frame size B
        [FromQuery] decimal? maxB,
        [FromQuery] decimal? minDbl, // Frame size DBL
        [FromQuery] decimal? maxDbl,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _catalogService.GetProductsAsync(
            q,
            productType,
            categoryId,
            brandId,
            color,
            minPrice,
            maxPrice,
            minA,
            maxA,
            minB,
            maxB,
            minDbl,
            maxDbl,
            page,
            pageSize,
            ct);

        return Ok(result);
    }

    /// <summary>
    /// Get a single product with variants, images, and type-specific specifications.
    /// </summary>
    [HttpGet("products/{productId:long}")]
    public async Task<ActionResult> GetProductDetail([FromRoute] long productId, CancellationToken ct)
    {
        var product = await _catalogService.GetProductDetailAsync(productId, ct);
        if (product == null) return NotFound();
        return Ok(product);
    }
}
