namespace eyewearshop_service.Payments;

/// <summary>
/// Model for MoMo IPN/webhook payload (v2).
/// Only includes fields we actually use.
/// </summary>
public record MomoIpnRequest(
    string PartnerCode,
    string AccessKey,
    string OrderId,
    string RequestId,
    long Amount,
    long TransId,
    string OrderInfo,
    string OrderType,
    long ResponseTime,
    int ResultCode,
    string Message,
    string PayType,
    string ExtraData,
    string Signature);

