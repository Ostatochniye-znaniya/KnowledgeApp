using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Logging;

namespace KnowledgeApp.Infrastructure.Storage;

public class S3FileStorage : IFileStorage, IDisposable
{
    private readonly IAmazonS3 _client;
    private readonly S3StorageOptions _options;
    private readonly ILogger<S3FileStorage> _logger;
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private volatile bool _bucketReady;

    public S3FileStorage(S3StorageOptions options, ILogger<S3FileStorage> logger)
    {
        _options = options;
        _logger = logger;

        var config = new AmazonS3Config
        {
            ServiceURL = options.Endpoint,
            // у MinIO бакет в пути, а не в поддомене
            ForcePathStyle = true,
            AuthenticationRegion = options.Region
        };
        _client = new AmazonS3Client(new BasicAWSCredentials(options.AccessKey, options.SecretKey), config);
    }

    public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);

        var request = new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        };
        await _client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);

        try
        {
            var response = await _client.GetObjectAsync(_options.Bucket, key, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);
        await _client.DeleteObjectAsync(_options.Bucket, key, cancellationToken);
    }

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (_bucketReady) return;

        await _bucketLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketReady) return;

            if (!await AmazonS3Util.DoesS3BucketExistV2Async(_client, _options.Bucket))
            {
                await _client.PutBucketAsync(new PutBucketRequest { BucketName = _options.Bucket }, cancellationToken);
                _logger.LogInformation("Создан бакет {Bucket} для документов", _options.Bucket);
            }

            _bucketReady = true;
        }
        finally
        {
            _bucketLock.Release();
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        _bucketLock.Dispose();
    }
}
