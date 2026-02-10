using System.Security.Claims;
using eyewearshop_data;
using eyewearshop_data.Entities;
using eyewearshop_service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// List the current user's orders with optional filtering by order type/status and pagination.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetMyOrders(
        [FromQuery] string? orderType,
        [FromQuery] short? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = GetUserIdOrThrow();

        var result = await _orderService.GetMyOrdersAsync(
            userId,
            orderType,
            status,
            page,
            pageSize,
            ct);

        return Ok(result);
    }

    /// <summary>
    /// Get a single order detail for the current user, including items, payments, shipping info,
    /// and the order prescription snapshot (if the order used one).
    /// </summary>
    [HttpGet("{orderId:long}")]
    public async Task<ActionResult> GetOrderDetail([FromRoute] long orderId, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var order = await _orderService.GetOrderDetailAsync(userId, orderId, ct);

        if (order == null) return NotFound();

        return Ok(order);
    }

    private long GetUserIdOrThrow()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdClaim, out var userId))
            throw new InvalidOperationException("Missing user id claim.");
        return userId;
    }
    [Authorize(Roles = $"{RoleNames.SalesSupport},{RoleNames.Operations}")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> ChangeStatus(
       Guid id,
       [FromBody] ChangeOrderStatusRequest request)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        await _orderService.ChangeStatusAsync(id, request.NewStatus, role);
        return Ok("Order status updated successfully");
    }
}