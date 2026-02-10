using eyewearshop_data.Entities;

namespace eyewearshop_service.Interfaces;

public interface ICatalogService
{
    Task<IReadOnlyList<object>> GetActiveCategoriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<object>> GetActiveBrandsAsync(CancellationToken ct = default);

    Task<object> GetProductsAsync(
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
        CancellationToken ct = default);

    Task<object?> GetProductDetailAsync(long productId, CancellationToken ct = default);
}

