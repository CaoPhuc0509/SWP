namespace eyewearshop_service;

public class VnPaySettings
{
    /// <summary>
    /// VNPay terminal code (vnp_TmnCode).
    /// </summary>
    public string TmnCode { get; set; } = string.Empty;

    /// <summary>
    /// Secret key used to generate vnp_SecureHash.
    /// </summary>
    public string HashSecret { get; set; } = string.Empty;

    /// <summary>
    /// Base URL of VNPay payment gateway, e.g. https://sandbox.vnpayment.vn/paymentv2/vpcpay.html
    /// </summary>
    public string PaymentUrl { get; set; } = string.Empty;

    /// <summary>
    /// Return URL that VNPay redirects the customer to after payment.
    /// </summary>
    public string ReturnUrl { get; set; } = string.Empty;

    /// <summary>
    /// Currency code, usually VND.
    /// </summary>
    public string CurrCode { get; set; } = "VND";

    /// <summary>
    /// Locale, e.g. vn or en.
    /// </summary>
    public string Locale { get; set; } = "vn";

    /// <summary>
    /// Order type, e.g. other or goods.
    /// </summary>
    public string OrderType { get; set; } = "other";
}

