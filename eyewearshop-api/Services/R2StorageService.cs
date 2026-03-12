using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using eyewearshop_service;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace eyewearshop_api.Services;

public interface IR2StorageService
{
    /// <summary>
    /// Uploads an image file to R2 and returns its public URL.
    /// </summary>
    /// <param name="file">The file to upload (must be image/*).</param>
    /// <param name="folder">Folder prefix inside the bucket, e.g. "avatars" or "products".</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Public URL of the uploaded object.</returns>
    Task<string> UploadAsync(IFormFile file, string folder, CancellationToken ct = default);

    /// <summary>
    /// Deletes an object from R2 by its key.
    /// </summary>
    /// <param name="objectKey">The object key inside the bucket, e.g. "avatars/abc123.jpg".</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(string objectKey, CancellationToken ct = default);

    /// <summary>
    /// Extracts the object key from a full public URL stored in the database.
    /// Returns null if the URL does not belong to this bucket.
    /// </summary>
    string? ExtractKeyFromUrl(string url);
}

public class R2StorageService : IR2StorageService
{
    private readonly CloudflareR2Settings _settings;
    private readonly AmazonS3Client _s3;
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public R2StorageService(IOptions<CloudflareR2Settings> options)
    {
        _settings = options.Value;

        var credentials = new BasicAWSCredentials(_settings.AccessKeyId, _settings.SecretAccessKey);
        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{_settings.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
            // R2 is in auto region; must silence region validation
            AuthenticationRegion = "auto",
        };

        _s3 = new AmazonS3Client(credentials, config);
    }

    public async Task<string> UploadAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty.", nameof(file));

        if (file.Length > MaxFileSizeBytes)
            throw new ArgumentException($"File exceeds the 5 MB size limit.", nameof(file));

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only image files are allowed.", nameof(file));

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var objectKey = $"{folder.Trim('/')}/{Guid.NewGuid()}{extension}";

        using var stream = file.OpenReadStream();

        var request = new PutObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = objectKey,
            InputStream = stream,
            ContentType = file.ContentType,
            // Make uploaded objects publicly readable
            CannedACL = S3CannedACL.PublicRead,
            // R2 does not support STREAMING-AWS4-HMAC-SHA256-PAYLOAD (chunked signing).
            // Disable it so the SDK sends a standard signed request instead.
            DisablePayloadSigning = true,
        };

        await _s3.PutObjectAsync(request, ct);

        return $"{_settings.PublicBaseUrl.TrimEnd('/')}/{objectKey}";
    }

    public async Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = objectKey,
        };

        await _s3.DeleteObjectAsync(request, ct);
    }

    public string? ExtractKeyFromUrl(string url)
    {
        var prefix = _settings.PublicBaseUrl.TrimEnd('/') + "/";
        if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        return url[prefix.Length..];
    }
}
