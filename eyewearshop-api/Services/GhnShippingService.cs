using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using eyewearshop_service;
using Microsoft.Extensions.Options;

namespace eyewearshop_api.Services;

public interface IGhnShippingService
{
    /// <summary>Calls GHN Calculate Fee API and returns the total shipping fee in VND.</summary>
    Task<decimal> CalculateFeeAsync(int toDistrictId, string toWardCode, decimal insuranceValue, CancellationToken ct = default);

    /// <summary>Returns all GHN provinces/cities.</summary>
    Task<JsonElement> GetProvincesAsync(CancellationToken ct = default);

    /// <summary>Returns GHN districts for a given province.</summary>
    Task<JsonElement> GetDistrictsAsync(int provinceId, CancellationToken ct = default);

    /// <summary>Returns GHN wards for a given district.</summary>
    Task<JsonElement> GetWardsAsync(int districtId, CancellationToken ct = default);
}

public class GhnShippingService : IGhnShippingService
{
    private readonly HttpClient _http;
    private readonly GhnSettings _settings;
    private const decimal MaxInsurance = 5_000_000m;
    private const string FeeEndpoint      = "shiip/public-api/v2/shipping-order/fee";
    private const string ProvinceEndpoint = "shiip/public-api/master-data/province";
    private const string DistrictEndpoint = "shiip/public-api/master-data/district";
    private const string WardEndpoint     = "shiip/public-api/master-data/ward?district_id";

    public GhnShippingService(HttpClient http, IOptions<GhnSettings> options)
    {
        _settings = options.Value;
        _http = http;
        _http.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
    }

    // ──── Shared helpers ───────────────────────────────────────────────────────

    /// Builds a request with the Token header pre-applied.
    private HttpRequestMessage BuildRequest(HttpMethod method, string endpoint, object? body = null)
    {
        var req = new HttpRequestMessage(method, endpoint);
        req.Headers.Add("Token", _settings.Token);
        if (body != null)
            req.Content = JsonContent.Create(body);
        return req;
    }

    /// Sends the request, checks for success, and returns the cloned "data" element.
    private async Task<JsonElement> CallAndExtractDataAsync(HttpRequestMessage req, CancellationToken ct)
    {
        var response = await _http.SendAsync(req, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GHN API error {(int)response.StatusCode}: {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.TryGetProperty("code", out var codeEl) && codeEl.GetInt32() != 200)
            throw new InvalidOperationException($"GHN returned error: {root.GetProperty("message").GetString()}");

        // Clone so the element outlives the JsonDocument's lifetime
        return root.GetProperty("data").Clone();
    }

    // ──── Calculate Shipping Fee ───────────────────────────────────────────────

    public async Task<decimal> CalculateFeeAsync(
        int toDistrictId,
        string toWardCode,
        decimal insuranceValue,
        CancellationToken ct = default)
    {
        var payload = new
        {
            service_type_id = _settings.ServiceTypeId,
            to_district_id  = toDistrictId,
            to_ward_code    = toWardCode,
            weight          = _settings.DefaultWeightGram,
            length          = _settings.DefaultLengthCm,
            width           = _settings.DefaultWidthCm,
            height          = _settings.DefaultHeightCm,
            insurance_value = (int)Math.Min(insuranceValue, MaxInsurance),
            coupon          = (string?)null
        };

        var req = BuildRequest(HttpMethod.Post, FeeEndpoint, payload);
        // ShopId is required only for fee calculation
        req.Headers.Add("ShopId", _settings.ShopId.ToString());

        var response = await _http.SendAsync(req, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GHN API error {(int)response.StatusCode}: {body}");

        var result = JsonSerializer.Deserialize<GhnFeeResponse>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result?.Code != 200 || result.Data == null)
            throw new InvalidOperationException($"GHN returned error: {result?.Message ?? body}");

        return result.Data.Total;
    }

    // ──── Master Data (Province / District / Ward) ─────────────────────────────

    public Task<JsonElement> GetProvincesAsync(CancellationToken ct = default)
        => CallAndExtractDataAsync(BuildRequest(HttpMethod.Get, ProvinceEndpoint), ct);

    public Task<JsonElement> GetDistrictsAsync(int provinceId, CancellationToken ct = default)
        => CallAndExtractDataAsync(
            BuildRequest(HttpMethod.Get, DistrictEndpoint, new { province_id = provinceId }), ct);

    public Task<JsonElement> GetWardsAsync(int districtId, CancellationToken ct = default)
        => CallAndExtractDataAsync(
            BuildRequest(HttpMethod.Post, WardEndpoint, new { district_id = districtId }), ct);

    // ──── Private response DTOs ────────────────────────────────────────────────

    private class GhnFeeResponse
    {
        [JsonPropertyName("code")]    public int Code { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("data")]    public GhnFeeData? Data { get; set; }
    }

    private class GhnFeeData
    {
        [JsonPropertyName("total")]         public decimal Total { get; set; }
        [JsonPropertyName("service_fee")]   public decimal ServiceFee { get; set; }
        [JsonPropertyName("insurance_fee")] public decimal InsuranceFee { get; set; }
    }
}
