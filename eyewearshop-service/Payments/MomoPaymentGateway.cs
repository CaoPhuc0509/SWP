using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using eyewearshop_data.Entities;
using Microsoft.Extensions.Options;

namespace eyewearshop_service.Payments;

public class MomoPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly MomoSettings _settings;

    public string Name => "MOMO";

    public MomoPaymentGateway(HttpClient httpClient, IOptions<MomoSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public async Task<PaymentRedirectResult> CreatePaymentAsync(Order order, decimal amount, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.PartnerCode) ||
            string.IsNullOrWhiteSpace(_settings.AccessKey) ||
            string.IsNullOrWhiteSpace(_settings.SecretKey) ||
            string.IsNullOrWhiteSpace(_settings.Endpoint))
        {
            throw new InvalidOperationException("MoMo settings are not configured. Please set the Momo section in appsettings.");
        }

        var endpoint = _settings.Endpoint.TrimEnd('/');
        var url = $"{endpoint}/v2/gateway/api/create";

        var requestId = Guid.NewGuid().ToString("N");
        var orderId = order.OrderNumber;

        var requestBody = new Dictionary<string, object?>
        {
            ["partnerCode"] = _settings.PartnerCode,
            ["accessKey"] = _settings.AccessKey,
            ["requestId"] = requestId,
            ["amount"] = Convert.ToInt64(amount),
            ["orderId"] = orderId,
            ["orderInfo"] = $"Payment for order {order.OrderNumber}",
            ["redirectUrl"] = _settings.RedirectUrl,
            ["ipnUrl"] = _settings.IpnUrl,
            ["requestType"] = _settings.RequestType,
            ["lang"] = "vi"
        };

        // Build raw signature string according to MoMo docs (v2 AIO)
        var rawSignature =
            $"accessKey={_settings.AccessKey}&amount={requestBody["amount"]}&extraData=&ipnUrl={_settings.IpnUrl}" +
            $"&orderId={orderId}&orderInfo={requestBody["orderInfo"]}&partnerCode={_settings.PartnerCode}" +
            $"&redirectUrl={_settings.RedirectUrl}&requestId={requestId}&requestType={_settings.RequestType}";

        var signature = SignHmacSha256(rawSignature, _settings.SecretKey);

        requestBody["signature"] = signature;
        requestBody["extraData"] = string.Empty;

        var json = JsonSerializer.Serialize(requestBody);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(httpRequest, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"MoMo create payment failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {responseContent}");
        }

        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        var resultCode = root.GetProperty("resultCode").GetInt32();
        if (resultCode != 0)
        {
            var message = root.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "Unknown error";
            throw new InvalidOperationException($"MoMo create payment returned error resultCode={resultCode}, message={message}");
        }

        var payUrl = root.GetProperty("payUrl").GetString() ?? throw new InvalidOperationException("MoMo response missing payUrl.");

        return new PaymentRedirectResult(
            Gateway: Name,
            RequestId: requestId,
            PayUrl: payUrl,
            Amount: amount,
            Currency: "VND");
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
}

