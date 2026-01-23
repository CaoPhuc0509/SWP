namespace eyewearshop_service;

public class VietQrSettings
{
    public string Environment { get; set; } = "DEV";
    public string DevBaseUrl { get; set; } = "https://dev.vietqr.org";
    public string ProdBaseUrl { get; set; } = "https://api.vietqr.org";

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public string BankCode { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string UserBankName { get; set; } = string.Empty;

    public int QrType { get; set; } = 0;
    public string TerminalCode { get; set; } = string.Empty;
    public string SubTerminalCode { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public string UrlLink { get; set; } = string.Empty;

    public string PartnerTokenUsername { get; set; } = string.Empty;
    public string PartnerTokenPassword { get; set; } = string.Empty;
    public string PartnerTokenSecret { get; set; } = "CHANGE_ME_PARTNER_TOKEN_SECRET";

    public string GetBaseUrl() => Environment.Equals("PROD", StringComparison.OrdinalIgnoreCase)
        ? ProdBaseUrl
        : DevBaseUrl;
}
