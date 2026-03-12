namespace eyewearshop_service;

public class GhnSettings
{
    public string Token { get; set; } = string.Empty;
    public int ShopId { get; set; }
    public string BaseUrl { get; set; } = "https://online-gateway.ghn.vn";

    /// <summary>2 = E-Commerce Delivery (standard), 5 = Express</summary>
    public int ServiceTypeId { get; set; } = 2;

    // Default parcel dimensions for eyeglasses
    public int DefaultWeightGram { get; set; } = 500;
    public int DefaultLengthCm { get; set; } = 20;
    public int DefaultWidthCm { get; set; } = 15;
    public int DefaultHeightCm { get; set; } = 10;
}
