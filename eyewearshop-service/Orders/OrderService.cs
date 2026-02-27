using eyewearshop_data;
using eyewearshop_data.Entities;
using eyewearshop_data.Interfaces;
using eyewearshop_service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_service.Orders;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;

    public OrderService(IRepository<Order> orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<object> GetMyOrdersAsync(
        long customerId,
        string? orderType,
        short? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _orderRepository
            .Query()
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(orderType))
        {
            query = query.Where(o => o.OrderType == orderType.Trim().ToUpperInvariant());
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var total = await query.CountAsync(ct);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.OrderId,
                o.OrderNumber,
                o.OrderType,
                o.Status,
                o.PaymentStatus,
                o.SubTotal,
                o.ShippingFee,
                o.DiscountAmount,
                o.TotalAmount,
                o.CreatedAt,
                o.UpdatedAt,
                ItemCount = o.Items.Count,
                ShippingInfo = o.ShippingInfo == null ? null : new
                {
                    o.ShippingInfo.TrackingNumber,
                    o.ShippingInfo.Carrier,
                    o.ShippingInfo.ShippedAt,
                    o.ShippingInfo.DeliveredAt
                }
            })
            .ToListAsync(ct);

        return new
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = orders
        };
    }

    public async Task<object?> GetOrderDetailAsync(
        long customerId,
        long orderId,
        CancellationToken ct = default)
    {
        var order = await _orderRepository
            .Query()
            .AsNoTracking()
            .Where(o => o.OrderId == orderId && o.CustomerId == customerId)
            .Select(o => new
            {
                o.OrderId,
                o.OrderNumber,
                o.OrderType,
                o.Status,
                o.PaymentStatus,
                o.SubTotal,
                o.ShippingFee,
                o.DiscountAmount,
                o.TotalAmount,
                o.CreatedAt,
                o.UpdatedAt,
                Prescription = o.OrderPrescription == null ? null : new
                {
                    o.OrderPrescription.SavedPrescriptionId,
                    o.OrderPrescription.RightSphere,
                    o.OrderPrescription.RightCylinder,
                    o.OrderPrescription.RightAxis,
                    o.OrderPrescription.RightAdd,
                    o.OrderPrescription.RightPD,
                    o.OrderPrescription.LeftSphere,
                    o.OrderPrescription.LeftCylinder,
                    o.OrderPrescription.LeftAxis,
                    o.OrderPrescription.LeftAdd,
                    o.OrderPrescription.LeftPD,
                    o.OrderPrescription.Notes,
                    o.OrderPrescription.PrescribedBy,
                    o.OrderPrescription.PrescriptionDate,
                    o.OrderPrescription.CreatedAt
                },
                ShippingInfo = o.ShippingInfo == null ? null : new
                {
                    o.ShippingInfo.RecipientName,
                    o.ShippingInfo.PhoneNumber,
                    o.ShippingInfo.AddressLine,
                    o.ShippingInfo.City,
                    o.ShippingInfo.District,
                    o.ShippingInfo.Ward,
                    o.ShippingInfo.ShippingMethod,
                    o.ShippingInfo.TrackingNumber,
                    o.ShippingInfo.Carrier,
                    o.ShippingInfo.ShippedAt,
                    o.ShippingInfo.EstimatedDeliveryDate,
                    o.ShippingInfo.DeliveredAt
                },
                Items = o.Items.Select(oi => new
                {
                    oi.OrderItemId,
                    oi.UnitPrice,
                    oi.Quantity,
                    oi.SubTotal,
                    oi.Description,
                    Variant = oi.Variant == null ? null : new
                    {
                        oi.Variant.VariantId,
                        oi.Variant.Color,
                        Product = oi.Variant.Product == null ? null : new
                        {
                            oi.Variant.Product.ProductId,
                            oi.Variant.Product.ProductName,
                            oi.Variant.Product.Sku,
                            oi.Variant.Product.ProductType
                        }
                    }
                }),
                Payments = o.Payments.Select(p => new
                {
                    p.PaymentId,
                    p.PaymentType,
                    p.PaymentMethod,
                    p.Amount,
                    p.Status,
                    p.CreatedAt
                })
            })
            .FirstOrDefaultAsync(ct);

        return order;
    }
    public async Task<(bool success, string? error, int? statusCode)> DeleteAwaitingPaymentOrderAsync(
        long customerId,
        long orderId,
        CancellationToken ct = default)
    {
        var order = await _orderRepository
            .Query()
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == customerId, ct);

        if (order == null) return (false, "Order not found.", 404);

        if (order.Status != OrderStatuses.AwaitingPayment || order.PaymentStatus != PaymentStatuses.Unpaid)
        {
            return (false, "Only unpaid orders awaiting payment can be deleted.", 400);
        }

        order.Status = OrderStatuses.Deleted;
        order.UpdatedAt = DateTime.UtcNow;

        await _orderRepository.SaveChangesAsync(ct);
        return (true, null, 200);
    }

    public async Task ChangeStatusAsync(long orderId, short newStatus, string role)
    {
        var order = await _orderRepository
            .Query()
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            throw new Exception("Order not found");

        if (!IsValidTransition(order.Status, newStatus, role))
            throw new Exception("You are not allowed to change this order status");

        order.Status = newStatus;

        await _orderRepository.SaveChangesAsync();
    }

    private bool IsValidTransition(short current, short next, string role)
    {
        // SALES STAFF
        if (role == RoleNames.SalesSupport)
        {
            return
                (current == OrderStatuses.Pending && next == OrderStatuses.Validated) ||
                (current == OrderStatuses.Validated && next == OrderStatuses.Confirmed) ||
                (current == OrderStatuses.Confirmed && next == OrderStatuses.Cancelled) ||
                (current == OrderStatuses.Shipped && next == OrderStatuses.Completed) ||
                (current == OrderStatuses.ReturnRequested && next == OrderStatuses.ReturnApproved) ||
                (current == OrderStatuses.ReturnRequested && next == OrderStatuses.ReturnRejected);
        }

        // OPERATION STAFF
        if (role == RoleNames.Operations)
        {
            return
                (current == OrderStatuses.Confirmed && next == OrderStatuses.Processing) ||
                (current == OrderStatuses.Processing && next == OrderStatuses.Shipped) ||
                (current == OrderStatuses.Shipped && next == OrderStatuses.Delivered) ||
                (current == OrderStatuses.Delivered && next == OrderStatuses.Completed) ||
                (current == OrderStatuses.ReturnApproved && next == OrderStatuses.ReturnProcessing) ||
                (current == OrderStatuses.ReturnProcessing && next == OrderStatuses.ReturnCompleted);
        }

        return false;
    }
}

