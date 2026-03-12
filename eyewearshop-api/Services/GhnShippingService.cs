using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using eyewearshop_service;
using Microsoft.Extensions.Options;

namespace eyewearshop_api.Services;

public interface IGhnShippingService
{
    /// <summary>
    /// Calls GHN Calculate Fee API and returns the total shipping fee in VND.
    /// </summary>
    Task<decimal> CalculateFeeAsync(int toDistrictId, string toWardCode, decimal insuranceValue, CancellationToken ct = default);
}

public class GhnShippingService : IGhnShippingService
{
    private readonly HttpClient _http;
    private readonly GhnSettings _settings;
    private const decimal MaxInsurance = 5_000_000m;
    private const string FeeEndpoint = "shiip/public-api/v2/shipping-order/fee";

    public GhnShippingService(HttpClient http, IOptions<GhnSettings> options)
    {
        _settings = options.Value;
        _http = http;
        _http.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task<decimal> CalculateFeeAsync(
        int toDistrictId,
        string toWardCode,
        decimal insuranceValue,
        CancellationToken ct = default)
    {
        var payload = new
        {
            service_type_id = _settings.ServiceTypeId,
            to_district_id = toDistrictId,
            to_ward_code = toWardCode,
            weight = _settings.DefaultWeightGram,
            length = _settings.DefaultLengthCm,
            width = _settings.DefaultWidthCm,
            height = _settings.DefaultHeightCm,
            insurance_value = (int)Math.Min(insuranceValue, MaxInsurance),
            coupon = (string?)null
        };

        // Build the request explicitly so we can set per-request headers reliably
        var request = new HttpRequestMessage(HttpMethod.Post, FeeEndpoint)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("Token", _settings.Token);
        request.Headers.Add("ShopId", _settings.ShopId.ToString());

        var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GHN API error {(int)response.StatusCode}: {body}");

        var result = JsonSerializer.Deserialize<GhnFeeResponse>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result?.Code != 200 || result.Data == null)
            throw new InvalidOperationException($"GHN returned error: {result?.Message ?? body}");

        return result.Data.Total;
    }

    // ──── Response DTOs ────────────────────────────────────────────────────────

    private class GhnFeeResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public GhnFeeData? Data { get; set; }
    }

    private class GhnFeeData
    {
        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("service_fee")]
        public decimal ServiceFee { get; set; }

        [JsonPropertyName("insurance_fee")]
        public decimal InsuranceFee { get; set; }
    }
}
