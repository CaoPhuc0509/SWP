namespace eyewearshop_service;

public class CloudflareR2Settings
{
    public string AccountId { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Public base URL for the bucket, e.g. https://pub-xxx.r2.dev or your custom domain.
    /// Must NOT end with a trailing slash.
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}
