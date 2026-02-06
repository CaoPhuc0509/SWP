using eyewearshop_data;
using eyewearshop_service.Cart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly EyewearShopDbContext _db;
    private readonly ISessionCartService _cartService;

    public CartController(EyewearShopDbContext db, ISessionCartService cartService)
    {
        _db = db;
        _cartService = cartService;
    }

    public record AddCartItemRequest(long VariantId, int Quantity);
    public record UpdateCartItemRequest(int Quantity);

    [HttpGet]
    public async Task<ActionResult> GetMyCart(CancellationToken ct)
    {
        var cartItems = _cartService.GetCart();
        
        if (cartItems.Count == 0)
        {
            return Ok(new
            {
                Items = Array.Empty<object>(),
                Summary = new
                {
                    SubTotal = 0m,
                    ItemCount = 0
                }
            });
        }

        var variantIds = cartItems.Select(ci => ci.VariantId).ToList();
        
        var variants = await _db.ProductVariants
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
            .Join(variants, 
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

        return Ok(new
        {
            Items = items,
            Summary = new
            {
                SubTotal = subTotal,
                ItemCount = items.Sum(x => x.Quantity)
            }
        });
    }

    [HttpPost("items")]
    public async Task<ActionResult> AddItem([FromBody] AddCartItemRequest request, CancellationToken ct)
    {
        if (request.Quantity <= 0) return BadRequest("Quantity must be greater than 0.");

        var variant = await _db.ProductVariants
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.VariantId == request.VariantId && v.Status == 1, ct);

        if (variant == null) return NotFound("Variant not found.");

        _cartService.AddItem(request.VariantId, request.Quantity);

        return Ok(new 
        { 
            VariantId = request.VariantId, 
            Quantity = _cartService.GetCart().FirstOrDefault(x => x.VariantId == request.VariantId)?.Quantity ?? request.Quantity
        });
    }

    [HttpPut("items/{variantId:long}")]
    public async Task<ActionResult> UpdateItem([FromRoute] long variantId, [FromBody] UpdateCartItemRequest request, CancellationToken ct)
    {
        if (request.Quantity <= 0) return BadRequest("Quantity must be greater than 0.");

        var variant = await _db.ProductVariants
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.VariantId == variantId && v.Status == 1, ct);

        if (variant == null) return NotFound("Variant not found.");

        var updated = _cartService.UpdateItem(variantId, request.Quantity);
        if (!updated) return NotFound("Item not found in cart.");

        return Ok(new { VariantId = variantId, Quantity = request.Quantity });
    }

    [HttpDelete("items/{variantId:long}")]
    public ActionResult RemoveItem([FromRoute] long variantId)
    {
        var removed = _cartService.RemoveItem(variantId);
        if (!removed) return NotFound("Item not found in cart.");

        return NoContent();
    }

    [HttpDelete]
    public ActionResult ClearCart()
    {
        _cartService.ClearCart();
        return NoContent();
    }
}