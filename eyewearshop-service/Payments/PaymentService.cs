using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using eyewearshop_data;
using eyewearshop_data.Entities;
using eyewearshop_data.Interfaces;
using eyewearshop_service.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace eyewearshop_service.Payments;

public class PaymentService : IPaymentService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<Payment> _paymentRepository;
    private readonly IRepository<PaymentTransaction> _paymentTransactionRepository;
    private readonly IReadOnlyDictionary<string, IPaymentGateway> _gatewaysByName;
    private readonly MomoSettings _momoSettings;
    private readonly VnPaySettings _vnPaySettings;

    public PaymentService(
        IRepository<Order> orderRepository,
        IRepository<Payment> paymentRepository,
        IRepository<PaymentTransaction> paymentTransactionRepository,
        IEnumerable<IPaymentGateway> gateways,
        IOptions<MomoSettings> momoOptions,
        IOptions<VnPaySettings> vnPayOptions)
    {
        _orderRepository = orderRepository;
        _paymentRepository = paymentRepository;
        _paymentTransactionRepository = paymentTransactionRepository;
        _momoSettings = momoOptions.Value;
        _vnPaySettings = vnPayOptions.Value;
        _gatewaysByName = gateways.ToDictionary(g => g.Name, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<object>> GetOrderPaymentsAsync(long customerId, long orderId, CancellationToken ct = default)
    {
        // Ensure the order belongs to the customer
        var orderExists = await _orderRepository
            .Query()
            .AnyAsync(o => o.OrderId == orderId && o.CustomerId == customerId, ct);

        if (!orderExists)
        {
            return Array.Empty<object>();
        }

        var payments = await _paymentRepository
            .Query()
            .AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.PaymentId,
                p.PaymentType,
                p.PaymentMethod,
                p.Amount,
                p.Status,
                p.Note,
                p.CreatedAt,
                p.UpdatedAt
            })
            .Cast<object>()
            .ToListAsync(ct);

        return payments;
    }

    public async Task<(object? result, string? error, int? statusCode)> CreatePaymentAsync(
        long customerId,
        long orderId,
        string paymentType,
        string? paymentMethod,
        decimal amount,
        string? note,
        CancellationToken ct = default)
    {
        if (amount <= 0)
        {
            return (null, "Payment amount must be greater than 0.", 400);
        }

        var order = await _orderRepository
            .Query()
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == customerId, ct);

        if (order == null)
        {
            return (null, "Order not found.", 404);
        }

        if (order.Status == OrderStatuses.Cancelled)
        {
            return (null, "Cannot make payment for a cancelled order.", 400);
        }

        var totalPaid = await _paymentRepository
            .Query()
            .Where(p => p.OrderId == orderId && p.Status == 1)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        var remainingBalance = order.TotalAmount - totalPaid;
        if (amount > remainingBalance)
        {
            return (null, $"Payment amount ({amount}) exceeds remaining balance ({remainingBalance}).", 400);
        }

        // For MoMo or VNPay, create a gateway transaction and return redirect URL instead of immediately confirming payment.
        if (!string.IsNullOrWhiteSpace(paymentMethod) &&
            (paymentMethod.Equals("MOMO", StringComparison.OrdinalIgnoreCase) ||
             paymentMethod.Equals("VNPAY", StringComparison.OrdinalIgnoreCase)))
        {
            var gatewayName = paymentMethod.ToUpperInvariant();
            if (!_gatewaysByName.TryGetValue(gatewayName, out var gateway))
            {
                return (null, $"Payment gateway '{paymentMethod}' is not configured.", 400);
            }

            var redirect = await gateway.CreatePaymentAsync(order, amount, ct);

            var now = DateTime.UtcNow;
            var tx = new PaymentTransaction
            {
                OrderId = orderId,
                Gateway = redirect.Gateway,
                RequestId = redirect.RequestId,
                Amount = redirect.Amount,
                Currency = redirect.Currency,
                Status = 0,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _paymentTransactionRepository.AddAsync(tx, ct);
            await _paymentTransactionRepository.SaveChangesAsync(ct);

            var response = new
            {
                PaymentTransactionId = tx.PaymentTransactionId,
                tx.OrderId,
                tx.Gateway,
                tx.RequestId,
                tx.Amount,
                tx.Currency,
                tx.Status,
                RedirectUrl = redirect.PayUrl
            };

            return (response, null, 200);
        }

        // Default behaviour: create a local Payment record (e.g. COD, manual BANK_TRANSFER, etc.)
        var nowManual = DateTime.UtcNow;
        var payment = new Payment
        {
            OrderId = orderId,
            CustomerId = customerId,
            PaymentType = paymentType.Trim(),
            PaymentMethod = paymentMethod?.Trim(),
            Amount = amount,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            Status = 0,
            CreatedAt = nowManual,
            UpdatedAt = nowManual
        };

        await _paymentRepository.AddAsync(payment, ct);
        await _paymentRepository.SaveChangesAsync(ct);

        var manualResponse = new
        {
            payment.PaymentId,
            payment.OrderId,
            payment.PaymentType,
            payment.PaymentMethod,
            payment.Amount,
            payment.Status,
            payment.CreatedAt
        };

        return (manualResponse, null, 200);
    }

    public async Task<(object? result, string? error, int? statusCode)> ConfirmPaymentAsync(
        long customerId,
        long paymentId,
        CancellationToken ct = default)
    {
        var payment = await _paymentRepository
            .Query()
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.CustomerId == customerId, ct);

        if (payment == null)
        {
            return (null, "Payment not found.", 404);
        }

        if (payment.Status == 1)
        {
            return (null, "Payment is already confirmed.", 400);
        }

        payment.Status = 1;
        payment.UpdatedAt = DateTime.UtcNow;

        var totalPaid = await _paymentRepository
            .Query()
            .Where(p => p.OrderId == payment.OrderId && p.Status == 1)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        var order = payment.Order!;
        if (totalPaid >= order.TotalAmount && order.Status == OrderStatuses.Pending)
        {
            // Keep as Pending; staff flow can update later.
        }

        await _paymentRepository.SaveChangesAsync(ct);

        var response = new
        {
            payment.PaymentId,
            payment.Status,
            payment.UpdatedAt
        };

        return (response, null, 200);
    }

    public async Task<object?> GetPaymentAsync(long customerId, long paymentId, CancellationToken ct = default)
    {
        var payment = await _paymentRepository
            .Query()
            .AsNoTracking()
            .Where(p => p.PaymentId == paymentId && p.CustomerId == customerId)
            .Select(p => new
            {
                p.PaymentId,
                p.OrderId,
                p.PaymentType,
                p.PaymentMethod,
                p.Amount,
                p.Status,
                p.Note,
                p.CreatedAt,
                p.UpdatedAt,
                Order = new
                {
                    p.Order!.OrderNumber,
                    p.Order.TotalAmount
                }
            })
            .FirstOrDefaultAsync(ct);

        return payment;
    }

    public async Task<(bool success, string message)> HandleMomoIpnAsync(MomoIpnRequest request, CancellationToken ct = default)
    {
        // Basic config validation
        if (string.IsNullOrWhiteSpace(_momoSettings.PartnerCode) ||
            string.IsNullOrWhiteSpace(_momoSettings.AccessKey) ||
            string.IsNullOrWhiteSpace(_momoSettings.SecretKey))
        {
            return (false, "MoMo settings not configured.");
        }

        if (!string.Equals(request.PartnerCode, _momoSettings.PartnerCode, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.AccessKey, _momoSettings.AccessKey, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "PartnerCode or AccessKey mismatch.");
        }

        // Build raw signature string according to MoMo IPN documentation.
        var rawSignature =
            $"accessKey={request.AccessKey}" +
            $"&amount={request.Amount}" +
            $"&extraData={request.ExtraData}" +
            $"&message={request.Message}" +
            $"&orderId={request.OrderId}" +
            $"&orderInfo={request.OrderInfo}" +
            $"&orderType={request.OrderType}" +
            $"&partnerCode={request.PartnerCode}" +
            $"&payType={request.PayType}" +
            $"&requestId={request.RequestId}" +
            $"&responseTime={request.ResponseTime}" +
            $"&resultCode={request.ResultCode}" +
            $"&transId={request.TransId}";

        var calculatedSignature = SignHmacSha256(rawSignature, _momoSettings.SecretKey);
        if (!string.Equals(calculatedSignature, request.Signature, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Invalid signature.");
        }

        // Idempotency: find existing transaction by RequestId
        var tx = await _paymentTransactionRepository
            .Query()
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.RequestId == request.RequestId, ct);

        if (tx == null)
        {
            return (false, "Payment transaction not found.");
        }

        if (tx.Status == 1)
        {
            // Already processed as success; treat as idempotent success.
            return (true, "Already processed.");
        }

        var now = DateTime.UtcNow;
        tx.GatewayTransactionId = request.TransId.ToString();
        tx.UpdatedAt = now;
        tx.RawResponse = JsonSerializer.Serialize(request);

        if (request.ResultCode == 0)
        {
            tx.Status = 1;
            tx.PaidAt = now;

            // Create a confirmed Payment record if not already present for this request.
            var existingPayment = await _paymentRepository
                .Query()
                .FirstOrDefaultAsync(p =>
                    p.OrderId == tx.OrderId &&
                    p.PaymentType == "E_WALLET" &&
                    p.PaymentMethod == "MOMO" &&
                    p.Amount == tx.Amount, ct);

            if (existingPayment == null)
            {
                var order = tx.Order ?? await _orderRepository
                    .Query()
                    .FirstAsync(o => o.OrderId == tx.OrderId, ct);

                var payment = new Payment
                {
                    OrderId = tx.OrderId,
                    CustomerId = order.CustomerId,
                    PaymentType = "E_WALLET",
                    PaymentMethod = "MOMO",
                    Amount = tx.Amount,
                    Note = $"MoMo transId={request.TransId}",
                    Status = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _paymentRepository.AddAsync(payment, ct);
            }
        }
        else
        {
            tx.Status = 2; // Failed
        }

        await _paymentTransactionRepository.SaveChangesAsync(ct);
        await _paymentRepository.SaveChangesAsync(ct);

        return (true, "OK");
    }

    public async Task<(bool success, string message)> HandleVnPayReturnAsync(IDictionary<string, string> queryParams, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_vnPaySettings.TmnCode) ||
            string.IsNullOrWhiteSpace(_vnPaySettings.HashSecret))
        {
            return (false, "VNPay settings not configured.");
        }

        if (!queryParams.TryGetValue("vnp_SecureHash", out var receivedHash) ||
            !queryParams.TryGetValue("vnp_TxnRef", out var txnRef) ||
            !queryParams.TryGetValue("vnp_ResponseCode", out var responseCode) ||
            !queryParams.TryGetValue("vnp_TransactionStatus", out var transactionStatus) ||
            !queryParams.TryGetValue("vnp_Amount", out var amountStr))
        {
            return (false, "Missing required VNPay parameters.");
        }

        // Rebuild data for hash: all vnp_* params except vnp_SecureHash and vnp_SecureHashType, sorted by key
        var toHash = new SortedDictionary<string, string>();
        foreach (var kvp in queryParams)
        {
            if (!kvp.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
                continue;
            if (kvp.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) ||
                kvp.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                continue;

            toHash[kvp.Key] = kvp.Value;
        }

        var rawData = string.Join("&", toHash.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var calculatedHash = HmacSha512(_vnPaySettings.HashSecret, rawData);
        if (!string.Equals(calculatedHash, receivedHash, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Invalid VNPay signature.");
        }

        var tx = await _paymentTransactionRepository
            .Query()
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.RequestId == txnRef && t.Gateway == "VNPAY", ct);

        if (tx == null)
        {
            return (false, "Payment transaction not found.");
        }

        if (tx.Status == 1)
        {
            return (true, "Already processed.");
        }

        var now = DateTime.UtcNow;
        queryParams.TryGetValue("vnp_TransactionNo", out var gatewayTransNo);
        tx.GatewayTransactionId = gatewayTransNo;
        tx.UpdatedAt = now;
        tx.RawResponse = JsonSerializer.Serialize(queryParams);

        var isSuccess = responseCode == "00" && transactionStatus == "00";
        if (isSuccess)
        {
            tx.Status = 1;
            tx.PaidAt = now;

            // VNPay amount is multiplied by 100
            if (long.TryParse(amountStr, out var vnpAmount))
            {
                tx.Amount = vnpAmount / 100m;
            }

            var existingPayment = await _paymentRepository
                .Query()
                .FirstOrDefaultAsync(p =>
                    p.OrderId == tx.OrderId &&
                    p.PaymentType == "E_WALLET" &&
                    p.PaymentMethod == "VNPAY" &&
                    p.Amount == tx.Amount, ct);

            if (existingPayment == null)
            {
                var order = tx.Order ?? await _orderRepository
                    .Query()
                    .FirstAsync(o => o.OrderId == tx.OrderId, ct);

                var payment = new Payment
                {
                    OrderId = tx.OrderId,
                    CustomerId = order.CustomerId,
                    PaymentType = "E_WALLET",
                    PaymentMethod = "VNPAY",
                    Amount = tx.Amount,
                    Note = $"VNPay transNo={gatewayTransNo}",
                    Status = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _paymentRepository.AddAsync(payment, ct);
            }
        }
        else
        {
            tx.Status = 2;
        }

        await _paymentTransactionRepository.SaveChangesAsync(ct);
        await _paymentRepository.SaveChangesAsync(ct);

        return (true, "OK");
    }

    public async Task<object?> GetOrderPaymentStatusAsync(long customerId, long orderId, CancellationToken ct = default)
    {
        var order = await _orderRepository
            .Query()
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == customerId, ct);

        if (order == null)
        {
            return null;
        }

        var totalPaid = await _paymentRepository
            .Query()
            .Where(p => p.OrderId == orderId && p.Status == 1)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        var latestTx = order.PaymentTransactions
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefault();

        return new
        {
            order.OrderId,
            order.OrderNumber,
            order.Status,
            order.TotalAmount,
            TotalPaid = totalPaid,
            RemainingBalance = order.TotalAmount - totalPaid,
            LatestTransaction = latestTx == null ? null : new
            {
                latestTx.PaymentTransactionId,
                latestTx.Gateway,
                latestTx.RequestId,
                latestTx.GatewayTransactionId,
                latestTx.Amount,
                latestTx.Currency,
                latestTx.Status,
                latestTx.CreatedAt,
                latestTx.UpdatedAt,
                latestTx.PaidAt
            }
        };
    }

    private static string SignHmacSha256(string data, string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var dataBytes = Encoding.UTF8.GetBytes(data);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    private static string HmacSha512(string key, string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);

        using var hmac = new HMACSHA512(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}

