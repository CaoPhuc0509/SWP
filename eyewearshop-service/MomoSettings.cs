namespace eyewearshop_service;

public class MomoSettings
{
    /// <summary>
    /// MoMo partner code (provided in merchant dashboard).
    /// </summary>
    public string PartnerCode { get; set; } = string.Empty;

    /// <summary>
    /// MoMo access key (used for signing requests).
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// MoMo secret key (keep this secure, never expose to frontend).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint base URL (sandbox or production).
    /// Example: https://test-payment.momo.vn or https://payment.momo.vn
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// URL in your frontend that MoMo redirects the user to after payment.
    /// Example: https://your-domain.com/payment/momo/return
    /// </summary>
    public string RedirectUrl { get; set; } = string.Empty;

    /// <summary>
    /// Backend IPN/webhook URL that MoMo calls to confirm final payment status.
    /// Example: https://api.your-domain.com/payment/momo/ipn
    /// </summary>
    public string IpnUrl { get; set; } = string.Empty;

    /// <summary>
    /// MoMo request type, e.g. captureWallet for basic AIO capture.
    /// </summary>
    public string RequestType { get; set; } = "captureWallet";
}

