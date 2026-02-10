using System.Security.Cryptography;
using System.Text;
using eyewearshop_data.Entities;
using Microsoft.Extensions.Options;

namespace eyewearshop_service.Payments;

public class VnPayPaymentGateway : IPaymentGateway
{
    private readonly VnPaySettings _settings;

    public string Name => "VNPAY";

    public VnPayPaymentGateway(IOptions<VnPaySettings> options)
    {
        _settings = options.Value;
    }

    public Task<PaymentRedirectResult> CreatePaymentAsync(Order order, decimal amount, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.TmnCode) ||
            string.IsNullOrWhiteSpace(_settings.HashSecret) ||
            string.IsNullOrWhiteSpace(_settings.PaymentUrl) ||
            string.IsNullOrWhiteSpace(_settings.ReturnUrl))
        {
            throw new InvalidOperationException("VNPay settings are not configured. Please set the VnPay section in appsettings.");
        }

        var vnpUrl = _settings.PaymentUrl.TrimEnd('?');

        var createDate = DateTime.UtcNow;
        var txnRef = $"{order.OrderNumber}-{Guid.NewGuid():N}".Substring(0, 32); // VNPay max 34 chars

        var query = new SortedDictionary<string, string>
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _settings.TmnCode,
            ["vnp_Amount"] = ((long)(amount * 100)).ToString(), // in smallest unit
            ["vnp_CreateDate"] = createDate.ToString("yyyyMMddHHmmss"),
            ["vnp_CurrCode"] = _settings.CurrCode,
            ["vnp_IpAddr"] = "127.0.0.1",
            ["vnp_Locale"] = _settings.Locale,
            ["vnp_OrderInfo"] = $"Payment for order {order.OrderNumber}",
            ["vnp_OrderType"] = _settings.OrderType,
            ["vnp_ReturnUrl"] = _settings.ReturnUrl,
            ["vnp_TxnRef"] = txnRef
        };

        // 1) Build raw data string (NOT URL-encoded) for secure hash calculation
        var rawData = string.Join("&", query.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var secureHash = HmacSha512(_settings.HashSecret, rawData);

        // 2) Build actual query string with URL-encoded values
        var encodedQuery = string.Join("&", query.Select(kvp =>
            $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

        var fullQuery = $"{encodedQuery}&vnp_SecureHash={secureHash}";
        var payUrl = $"{vnpUrl}?{fullQuery}";

        var result = new PaymentRedirectResult(
            Gateway: Name,
            RequestId: txnRef,
            PayUrl: payUrl,
            Amount: amount,
            Currency: _settings.CurrCode);

        return Task.FromResult(result);
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

