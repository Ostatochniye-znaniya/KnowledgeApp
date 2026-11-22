using Amazon.Runtime;
using Amazon.S3;
using KnowledgeApp.Infrastructure.Storage;
using Testcontainers.Minio;

namespace KnowledgeApp.IntegrationTests.Infrastructure;

/// <summary>MinIO в Testcontainers для тестов хранилища документов.</summary>
public class MinioFixture : IAsyncLifetime
{
    // Тот же образ, что в docker-compose: minio/minio убрали с Docker Hub
    private readonly MinioContainer _minio = new MinioBuilder("pgsty/minio:RELEASE.2026-08-04T00-00-00Z").Build();

    public S3StorageOptions CreateOptions(string bucket) => new()
    {
        Endpoint = _minio.GetConnectionString(),
        AccessKey = _minio.GetAccessKey(),
        SecretKey = _minio.GetSecretKey(),
        Bucket = bucket
    };

    /// <summary>Отдельный клиент S3, чтобы проверять содержимое бакета в обход приложения.</summary>
    public IAmazonS3 CreateClient() => new AmazonS3Client(
        new BasicAWSCredentials(_minio.GetAccessKey(), _minio.GetSecretKey()),
        new AmazonS3Config { ServiceURL = _minio.GetConnectionString(), ForcePathStyle = true, AuthenticationRegion = "us-east-1" });

    public Task InitializeAsync() => _minio.StartAsync();

    public Task DisposeAsync() => _minio.DisposeAsync().AsTask();
}
