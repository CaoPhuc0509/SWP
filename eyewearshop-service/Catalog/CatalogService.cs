using eyewearshop_data;
using eyewearshop_data.Entities;
using eyewearshop_data.Interfaces;
using eyewearshop_service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_service.Catalog;

public class CatalogService : ICatalogService
{
    private readonly IRepository<Category> _categoryRepository;
    private readonly IRepository<Brand> _brandRepository;
    private readonly IRepository<Product> _productRepository;

    public CatalogService(
        IRepository<Category> categoryRepository,
        IRepository<Brand> brandRepository,
        IRepository<Product> productRepository)
    {
        _categoryRepository = categoryRepository;
        _brandRepository = brandRepository;
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyList<object>> GetActiveCategoriesAsync(CancellationToken ct = default)
    {
        return await _categoryRepository
            .Query()
            .AsNoTracking()
            .Where(x => x.Status == 1)
            .OrderBy(x => x.CategoryName)
            .Select(x => new { x.CategoryId, x.CategoryName })
            .Cast<object>()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<object>> GetActiveBrandsAsync(CancellationToken ct = default)
    {
        return await _brandRepository
            .Query()
            .AsNoTracking()
            .Where(x => x.Status == 1)
            .OrderBy(x => x.BrandName)
            .Select(x => new { x.BrandId, x.BrandName })
            .Cast<object>()
            .ToListAsync(ct);
    }

    public async Task<object> GetProductsAsync(
        string? q,
        string? productType,
        long? categoryId,
        long? brandId,
        string? color,
        decimal? minPrice,
        decimal? maxPrice,
        decimal? minA,
        decimal? maxA,
        decimal? minB,
        decimal? maxB,
        decimal? minDbl,
        decimal? maxDbl,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _productRepository
            .Query()
            .AsNoTracking()
            .Where(p => p.Status == 1);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim().ToLower();
            query = query.Where(p => p.ProductName.ToLower().Contains(qq) || p.Sku.ToLower().Contains(qq));
        }

        if (!string.IsNullOrWhiteSpace(productType))
        {
            var pt = productType.Trim().ToUpperInvariant();
            query = query.Where(p => p.ProductType.ToUpper() == pt);
        }

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId);

        if (brandId.HasValue)
            query = query.Where(p => p.BrandId == brandId);

        if (!string.IsNullOrWhiteSpace(color) || minPrice.HasValue || maxPrice.HasValue)
        {
            query = query.Where(p => p.Variants.Any(v => v.Status == 1));

            if (!string.IsNullOrWhiteSpace(color))
            {
                var c = color.Trim().ToLowerInvariant();
                query = query.Where(p => p.Variants.Any(v => v.Color != null && v.Color.ToLower() == c));
            }

            if (minPrice.HasValue)
                query = query.Where(p => p.Variants.Any(v => v.Price >= minPrice.Value));

            if (maxPrice.HasValue)
                query = query.Where(p => p.Variants.Any(v => v.Price <= maxPrice.Value));
        }

        if (minA.HasValue || maxA.HasValue || minB.HasValue || maxB.HasValue || minDbl.HasValue || maxDbl.HasValue)
        {
            query = query.Where(p => (p.ProductType == ProductTypes.Frame && p.FrameSpec != null) ||
                                     (p.ProductType == ProductTypes.Sunglasses && p.SunglassesSpec != null));

            if (minA.HasValue)
                query = query.Where(p => (p.FrameSpec != null && p.FrameSpec.A >= minA.Value) ||
                                         (p.SunglassesSpec != null && p.SunglassesSpec.A >= minA.Value));

            if (maxA.HasValue)
                query = query.Where(p => (p.FrameSpec != null && p.FrameSpec.A <= maxA.Value) ||
                                         (p.SunglassesSpec != null && p.SunglassesSpec.A <= maxA.Value));

            if (minB.HasValue)
                query = query.Where(p => (p.FrameSpec != null && p.FrameSpec.B >= minB.Value) ||
                                         (p.SunglassesSpec != null && p.SunglassesSpec.B >= minB.Value));

            if (maxB.HasValue)
                query = query.Where(p => (p.FrameSpec != null && p.FrameSpec.B <= maxB.Value) ||
                                         (p.SunglassesSpec != null && p.SunglassesSpec.B <= maxB.Value));

            if (minDbl.HasValue)
                query = query.Where(p => (p.FrameSpec != null && p.FrameSpec.Dbl >= minDbl.Value) ||
                                         (p.SunglassesSpec != null && p.SunglassesSpec.Dbl >= minDbl.Value));

            if (maxDbl.HasValue)
                query = query.Where(p => (p.FrameSpec != null && p.FrameSpec.Dbl <= maxDbl.Value) ||
                                         (p.SunglassesSpec != null && p.SunglassesSpec.Dbl <= maxDbl.Value));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.Sku,
                p.ProductType,
                p.BasePrice,
                p.BrandId,
                p.CategoryId,
                PrimaryImageUrl = p.Images
                    .Where(i => i.Status == 1)
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                MinVariantPrice = p.Variants.Where(v => v.Status == 1).Select(v => (decimal?)v.Price).Min(),
                MaxVariantPrice = p.Variants.Where(v => v.Status == 1).Select(v => (decimal?)v.Price).Max(),
                InStock = p.Variants.Any(v => v.Status == 1 && v.StockQuantity > 0),
                PreOrderAvailable = p.Variants.Any(v => v.Status == 1 && v.PreOrderQuantity > 0)
            })
            .ToListAsync(ct);

        return new
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        };
    }

    public async Task<object?> GetProductDetailAsync(long productId, CancellationToken ct = default)
    {
        var query = _productRepository
            .Query()
            .AsNoTracking()
            .Where(p => p.ProductId == productId && p.Status == 1);

        var product = await query
            .Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.Sku,
                p.Description,
                p.ProductType,
                p.BasePrice,
                p.Specifications,
                p.BrandId,
                BrandName = p.Brand != null ? p.Brand.BrandName : null,
                p.CategoryId,
                CategoryName = p.Category != null ? p.Category.CategoryName : null,
                Images = p.Images
                    .Where(i => i.Status == 1)
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => new { i.ImageId, i.Url, i.SortOrder, i.IsPrimary, i.VariantId }),
                Variants = p.Variants
                    .Where(v => v.Status == 1)
                    .OrderBy(v => v.VariantId)
                    .Select(v => new
                    {
                        v.VariantId,
                        v.Color,
                        v.RefractiveIndex,
                        v.BaseCurve,
                        v.Diameter,
                        v.VariantSku,
                        v.Price,
                        v.StockQuantity,
                        v.PreOrderQuantity,
                        v.ExpectedDateRestock
                    }),
                FrameSpec = p.FrameSpec == null ? null : new
                {
                    p.FrameSpec.RimType,
                    p.FrameSpec.Material,
                    p.FrameSpec.A,
                    p.FrameSpec.B,
                    p.FrameSpec.Dbl,
                    p.FrameSpec.TempleLength,
                    p.FrameSpec.LensWidth,
                    p.FrameSpec.Shape,
                    p.FrameSpec.Weight,
                    p.FrameSpec.HingeType,
                    p.FrameSpec.HasNosePads
                },
                SunglassesSpec = p.SunglassesSpec == null ? null : new
                {
                    p.SunglassesSpec.RimType,
                    p.SunglassesSpec.Material,
                    p.SunglassesSpec.A,
                    p.SunglassesSpec.B,
                    p.SunglassesSpec.Dbl,
                    p.SunglassesSpec.TempleLength,
                    p.SunglassesSpec.Shape,
                    p.SunglassesSpec.Weight,
                    p.SunglassesSpec.LensMaterial,
                    p.SunglassesSpec.LensType,
                    p.SunglassesSpec.UvProtection,
                    p.SunglassesSpec.TintColor
                },
                RxLensSpec = p.RxLensSpec == null ? null : new
                {
                    p.RxLensSpec.DesignType,
                    p.RxLensSpec.Material,
                    p.RxLensSpec.LensWidth,
                    p.RxLensSpec.MinSphere,
                    p.RxLensSpec.MaxSphere,
                    p.RxLensSpec.MinCylinder,
                    p.RxLensSpec.MaxCylinder,
                    p.RxLensSpec.MinAxis,
                    p.RxLensSpec.MaxAxis,
                    p.RxLensSpec.MinAdd,
                    p.RxLensSpec.MaxAdd,
                    Features = p.RxLensSpec.RxLensSpecFeatures
                        .Select(x => new { x.FeatureId, x.Feature.Name }),
                    p.RxLensSpec.HasAntiReflective,
                    p.RxLensSpec.HasBlueLightFilter,
                    p.RxLensSpec.HasUVProtection,
                    p.RxLensSpec.HasScratchResistant
                },
                ContactLensSpec = p.ContactLensSpec == null ? null : new
                {
                    p.ContactLensSpec.BaseCurve,
                    p.ContactLensSpec.Diameter,
                    p.ContactLensSpec.MinSphere,
                    p.ContactLensSpec.MaxSphere,
                    p.ContactLensSpec.MinCylinder,
                    p.ContactLensSpec.MaxCylinder,
                    p.ContactLensSpec.MinAxis,
                    p.ContactLensSpec.MaxAxis,
                    p.ContactLensSpec.LensType,
                    p.ContactLensSpec.Material,
                    p.ContactLensSpec.WaterContent,
                    p.ContactLensSpec.OxygenPermeability,
                    p.ContactLensSpec.ReplacementSchedule,
                    p.ContactLensSpec.IsToric,
                    p.ContactLensSpec.IsMultifocal
                }
            })
            .FirstOrDefaultAsync(ct);

        return product;
    }
}

