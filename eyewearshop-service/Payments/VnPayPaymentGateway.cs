using eyewearshop_data.Entities;
using Microsoft.Extensions.Options;
using VNPAY;
using VNPAY.Models.Enums;

namespace eyewearshop_service.Payments;

public class VnPayPaymentGateway : IPaymentGateway
{
    private readonly VnPaySettings _settings;
    private readonly IVnpayClient _vnpayClient;

    public string Name => "VNPAY";

    public VnPayPaymentGateway(IOptions<VnPaySettings> options, IVnpayClient vnpayClient)
    {
        _settings = options.Value;
        _vnpayClient = vnpayClient;
    }

    public Task<PaymentRedirectResult> CreatePaymentAsync(Order order, decimal amount, CancellationToken ct = default)
    {
        var description = $"Payment for order {order.OrderNumber}";
        var paymentUrlInfo = _vnpayClient.CreatePaymentUrl((double)amount, description, BankCode.ANY);

        var result = new PaymentRedirectResult(
            Gateway: Name,
            RequestId: paymentUrlInfo.PaymentId.ToString(),
            PayUrl: paymentUrlInfo.Url,
            Amount: amount,
            Currency: _settings.CurrCode);

        return Task.FromResult(result);
    }
}
 
