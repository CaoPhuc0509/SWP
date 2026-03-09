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

    /// <summary>
    /// Get all orders with optional filtering by order type/status and pagination.
    /// Only accessible by SalesSupport and Operations roles.
    /// </summary>
    [Authorize(Roles = $"{RoleNames.SalesSupport},{RoleNames.Operations}")]
    [HttpGet("all")]
    public async Task<ActionResult> GetAllOrders(
        [FromQuery] string? orderType,
        [FromQuery] short? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _orderService.GetAllOrdersAsync(
            orderType,
            status,
            page,
            pageSize,
            ct);

        return Ok(result);
    }

    /// <summary>
    /// Get order detail by ID for SalesSupport and Operations staff (no customer filtering).
    /// </summary>
    [Authorize(Roles = $"{RoleNames.SalesSupport},{RoleNames.Operations}")]
    [HttpGet("staff-view/{orderId:long}")]
    public async Task<ActionResult> GetOrderByIdForStaff([FromRoute] long orderId, CancellationToken ct)
    {
        var order = await _orderService.GetOrderByIdForStaffAsync(orderId, ct);

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
    [HttpPut("{Orderid}/status")]
    public async Task<IActionResult> ChangeStatus(
   long Orderid,
   [FromBody] ChangeOrderStatusRequest request)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        await _orderService.ChangeStatusAsync(Orderid, request.NewStatus, role);
        return Ok("Order status updated successfully");
    }

    /// <summary>
    /// Customer soft-delete an order that is awaiting payment and still unpaid.
    /// This sets OrderStatus=Deleted (9) but does not remove the row.
    /// </summary>
    [HttpDelete("{orderId:long}")]
    public async Task<ActionResult> DeleteAwaitingPaymentOrder([FromRoute] long orderId, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var (success, error, statusCode) = await _orderService.DeleteAwaitingPaymentOrderAsync(userId, orderId, ct);
        if (!success)
        {
            return StatusCode(statusCode ?? 400, error);
        }

        return Ok(new { message = "Order deleted." });
    }
}