using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace eyewearshop_service.VietQr;

public interface IVietQrClient
{
    Task<VietQrTokenResponse> GetTokenAsync(CancellationToken ct = default);
    Task<VietQrGenerateResponse> GenerateQrAsync(VietQrGenerateRequest request, string bearerToken, CancellationToken ct = default);
}

public class VietQrClient : IVietQrClient
{
    private readonly HttpClient _http;
    private readonly VietQrSettings _settings;

    public VietQrClient(HttpClient http, IOptions<VietQrSettings> options)
    {
        _http = http;
        _settings = options.Value;
    }

    public async Task<VietQrTokenResponse> GetTokenAsync(CancellationToken ct = default)
    {
        var baseUrl = _settings.GetBaseUrl().TrimEnd('/');
        var url = $"{baseUrl}/vqr/api/token_generate";

        var req = new HttpRequestMessage(HttpMethod.Post, url);

        var raw = $"{_settings.Username}:{_settings.Password}";
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        req.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"VietQR token_generate failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");

        return JsonSerializer.Deserialize<VietQrTokenResponse>(body, JsonOptions())
               ?? throw new InvalidOperationException("Invalid VietQR token response.");
    }

    public async Task<VietQrGenerateResponse> GenerateQrAsync(VietQrGenerateRequest request, string bearerToken, CancellationToken ct = default)
    {
        var baseUrl = _settings.GetBaseUrl().TrimEnd('/');
        var url = $"{baseUrl}/vqr/api/qr/generate-customer";

        var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var json = JsonSerializer.Serialize(request, JsonOptions());
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"VietQR generate-customer failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");

        return JsonSerializer.Deserialize<VietQrGenerateResponse>(body, JsonOptions())
               ?? throw new InvalidOperationException("Invalid VietQR generate response.");
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

public record VietQrTokenResponse(string AccessToken, string TokenType, int ExpiresIn);

public record VietQrGenerateRequest(
    string BankCode,
    string BankAccount,
    string UserBankName,
    string Content,
    int QrType,
    long? Amount,
    string? OrderId,
    string? TransType,
    string? TerminalCode,
    string? SubTerminalCode,
    string? ServiceCode,
    string? UrlLink,
    string? Note,
    object[]? AdditionalData,
    string? Sign
);

public record VietQrGenerateResponse(
    string? BankCode,
    string? BankName,
    string? BankAccount,
    string? UserBankName,
    string? Amount,
    string? Content,
    string? QrCode,
    string? ImgId,
    int? Existing,
    string? TransactionId,
    string? TransactionRefId,
    string? QrLink,
    string? TerminalCode,
    string? SubTerminalCode,
    string? ServiceCode,
    string? OrderId,
    object[]? AdditionalData,
    string? VaAccount
);
