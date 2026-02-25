using eyewearshop_service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    public record AddCartItemRequest(long VariantId, int Quantity);
    public record UpdateCartItemRequest(int Quantity);

    /// <summary>
    /// Get the current user's shopping cart (session-based) with calculated subtotal.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetMyCart(CancellationToken ct)
    {
        var summary = await _cartService.GetCartSummaryAsync(ct);
        return Ok(summary);
    }

    /// <summary>
    /// Add an item to the cart (session-based) by variant id.
    /// </summary>
    [HttpPost("items")]
    public async Task<ActionResult> AddItem([FromBody] AddCartItemRequest request, CancellationToken ct)
    {
        var (result, error, statusCode) = await _cartService.AddItemAsync(
            request.VariantId,
            request.Quantity,
            ct);

        if (error != null)
        {
            return StatusCode(statusCode ?? 400, error);
        }

        return Ok(result);
    }

    /// <summary>
    /// Update quantity of a cart item (session-based) by variant id.
    /// </summary>
    [HttpPut("items/{variantId:long}")]
    public async Task<ActionResult> UpdateItem([FromRoute] long variantId, [FromBody] UpdateCartItemRequest request, CancellationToken ct)
    {
        var (result, error, statusCode) = await _cartService.UpdateItemAsync(
            variantId,
            request.Quantity,
            ct);

        if (error != null)
        {
            return StatusCode(statusCode ?? 400, error);
        }

        return Ok(result);
    }

    /// <summary>
    /// Remove an item from the cart (session-based) by variant id.
    /// </summary>
    [HttpDelete("items/{variantId:long}")]
    public ActionResult RemoveItem([FromRoute] long variantId)
    {
        var (success, error, statusCode) = _cartService.RemoveItemAsync(variantId).GetAwaiter().GetResult();
        if (!success)
        {
            return StatusCode(statusCode ?? 404, error);
        }

        return NoContent();
    }

    /// <summary>
    /// Clear all items from the current user's cart (session-based).
    /// </summary>
    [HttpDelete]
    public ActionResult ClearCart()
    {
        _cartService.ClearCart();
        return NoContent();
    }
}