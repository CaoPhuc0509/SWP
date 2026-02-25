using eyewearshop_data.Entities;
using eyewearshop_data.Interfaces;
using eyewearshop_service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_service.Cart;

public class CartService : ICartService
{
    private readonly ISessionCartService _sessionCartService;
    private readonly IRepository<ProductVariant> _variantRepository;

    public CartService(
        ISessionCartService sessionCartService,
        IRepository<ProductVariant> variantRepository)
    {
        _sessionCartService = sessionCartService;
        _variantRepository = variantRepository;
    }

    public async Task<object> GetCartSummaryAsync(CancellationToken ct = default)
    {
        var cartItems = _sessionCartService.GetCart();

        if (cartItems.Count == 0)
        {
            return new
            {
                Items = Array.Empty<object>(),
                Summary = new
                {
                    SubTotal = 0m,
                    ItemCount = 0
                }
            };
        }

        var variantIds = cartItems.Select(ci => ci.VariantId).ToList();

        var variants = await _variantRepository
            .Query()
            .AsNoTracking()
            .Where(v => variantIds.Contains(v.VariantId) && v.Status == 1)
            .Include(v => v.Product)
            .ThenInclude(p => p.Images.Where(i => i.Status == 1 && i.IsPrimary))
            .Select(v => new
            {
                v.VariantId,
                v.Color,
                v.Price,
                v.StockQuantity,
                v.PreOrderQuantity,
                Product = new
                {
                    v.Product.ProductId,
                    v.Product.ProductName,
                    v.Product.Sku,
                    v.Product.ProductType,
                    PrimaryImageUrl = v.Product.Images
                        .Where(i => i.Status == 1)
                        .OrderByDescending(i => i.IsPrimary)
                        .ThenBy(i => i.SortOrder)
                        .Select(i => i.Url)
                        .FirstOrDefault()
                }
            })
            .ToListAsync(ct);

        var items = cartItems
            .Join(
                variants,
                ci => ci.VariantId,
                v => v.VariantId,
                (ci, v) => new
                {
                    VariantId = ci.VariantId,
                    Quantity = ci.Quantity,
                    Variant = v,
                    LineTotal = v.Price * ci.Quantity
                })
            .ToList();

        var subTotal = items.Sum(x => x.LineTotal);

        return new
        {
            Items = items,
            Summary = new
            {
                SubTotal = subTotal,
                ItemCount = items.Sum(x => x.Quantity)
            }
        };
    }

    public async Task<(object? result, string? error, int? statusCode)> AddItemAsync(
        long variantId,
        int quantity,
        CancellationToken ct = default)
    {
        if (quantity <= 0)
        {
            return (null, "Quantity must be greater than 0.", 400);
        }

        var variant = await _variantRepository
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.VariantId == variantId && v.Status == 1, ct);

        if (variant == null)
        {
            return (null, "Variant not found.", 404);
        }

        _sessionCartService.AddItem(variantId, quantity);

        var currentQuantity = _sessionCartService
            .GetCart()
            .FirstOrDefault(x => x.VariantId == variantId)?.Quantity ?? quantity;

        var result = new
        {
            VariantId = variantId,
            Quantity = currentQuantity
        };

        return (result, null, 200);
    }

    public async Task<(object? result, string? error, int? statusCode)> UpdateItemAsync(
        long variantId,
        int quantity,
        CancellationToken ct = default)
    {
        if (quantity <= 0)
        {
            return (null, "Quantity must be greater than 0.", 400);
        }

        var variant = await _variantRepository
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.VariantId == variantId && v.Status == 1, ct);

        if (variant == null)
        {
            return (null, "Variant not found.", 404);
        }

        var updated = _sessionCartService.UpdateItem(variantId, quantity);
        if (!updated)
        {
            return (null, "Item not found in cart.", 404);
        }

        var result = new
        {
            VariantId = variantId,
            Quantity = quantity
        };

        return (result, null, 200);
    }

    public Task<(bool success, string? error, int? statusCode)> RemoveItemAsync(long variantId)
    {
        var removed = _sessionCartService.RemoveItem(variantId);
        if (!removed)
        {
            return Task.FromResult<(bool, string?, int?)>((false, "Item not found in cart.", 404));
        }

        return Task.FromResult<(bool, string?, int?)>((true, null, 204));
    }

    public void ClearCart()
    {
        _sessionCartService.ClearCart();
    }
}

