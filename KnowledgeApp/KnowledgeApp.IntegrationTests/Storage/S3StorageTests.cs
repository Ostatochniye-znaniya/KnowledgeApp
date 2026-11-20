using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using KnowledgeApp.Infrastructure.Storage;
using KnowledgeApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeApp.IntegrationTests.Storage;

/// <summary>
/// Хранение документов в S3 на настоящем MinIO: напрямую через S3FileStorage
/// и через API загрузки графика проверки (файл в MinIO, метаданные в MySQL).
/// </summary>
public class S3StorageTests : IntegrationTestBase, IClassFixture<MinioFixture>
{
    private readonly MinioFixture _minio;
    private readonly string _bucket = $"test-{Guid.NewGuid():N}";
    private readonly IAmazonS3 _s3;

    public S3StorageTests(KnowledgeAppFactory factory, MinioFixture minio) : base(factory)
    {
        _minio = minio;
        _s3 = minio.CreateClient();
    }

    public override Task DisposeAsync()
    {
        _s3.Dispose();
        return base.DisposeAsync();
    }

    [Fact]
    public async Task FileStorage_SaveAndRead_ReturnsSameBytes()
    {
        using var storage = CreateStorage();
        var content = RandomBytes(256 * 1024);

        await storage.SaveAsync("docs/file.bin", new MemoryStream(content), "application/octet-stream");
        await using var stream = await storage.OpenReadAsync("docs/file.bin");

        Assert.NotNull(stream);
        Assert.Equal(content, await ReadAll(stream));
    }

    [Fact]
    public async Task FileStorage_CreatesBucketOnFirstUse()
    {
        using var storage = CreateStorage();

        await storage.SaveAsync("a.txt", new MemoryStream("a"u8.ToArray()), "text/plain");

        var buckets = await _s3.ListBucketsAsync();
        Assert.Contains(buckets.Buckets, b => b.BucketName == _bucket);
    }

    [Fact]
    public async Task FileStorage_MissingKey_ReturnsNull()
    {
        using var storage = CreateStorage();

        Assert.Null(await storage.OpenReadAsync("missing.pdf"));
    }

    [Fact]
    public async Task FileStorage_Delete_RemovesObject()
    {
        using var storage = CreateStorage();
        await storage.SaveAsync("to-delete.txt", new MemoryStream("x"u8.ToArray()), "text/plain");

        await storage.DeleteAsync("to-delete.txt");

        Assert.Null(await storage.OpenReadAsync("to-delete.txt"));
    }

    [Fact]
    public async Task ScheduleUpload_StoresFileInMinio_AndDownloadsSameBytes()
    {
        using var client = CreateClientWithMinio();
        var pdf = Pdf("первая версия");

        using var upload = await UploadSchedule(client, pdf, "График ФИТ.pdf");

        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var document = await upload.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("График ФИТ.pdf", document.GetProperty("fileName").GetString());
        Assert.Equal(pdf.Length, document.GetProperty("fileSize").GetInt64());

        var keys = await ListKeys();
        var key = Assert.Single(keys);
        Assert.StartsWith($"schedules/{TestSeed.ItFacultyId}/{TestSeed.CurrentSemesterId}/", key);

        using var download = await client.GetAsync($"ScheduleDocument/Download/{document.GetProperty("id").GetInt32()}");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal(pdf, await download.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task ScheduleReupload_ReplacesFileAndDeletesPrevious()
    {
        using var client = CreateClientWithMinio();
        using (var first = await UploadSchedule(client, Pdf("первая версия"), "v1.pdf"))
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstKey = Assert.Single(await ListKeys());

        var second = Pdf("вторая версия");
        using var reupload = await UploadSchedule(client, second, "v2.pdf");

        Assert.Equal(HttpStatusCode.OK, reupload.StatusCode);
        var secondKey = Assert.Single(await ListKeys());
        Assert.NotEqual(firstKey, secondKey);

        var current = await client.GetFromJsonAsync<JsonElement>($"ScheduleDocument/GetCurrent?semesterId={TestSeed.CurrentSemesterId}");
        Assert.Equal("v2.pdf", current.GetProperty("fileName").GetString());
        var bytes = await client.GetByteArrayAsync($"ScheduleDocument/Download/{current.GetProperty("id").GetInt32()}");
        Assert.Equal(second, bytes);
    }

    [Fact]
    public async Task ScheduleUpload_NotPdf_IsRejectedAndNothingStored()
    {
        using var client = CreateClientWithMinio();

        using var response = await UploadSchedule(client, Encoding.UTF8.GetBytes("<html/>"), "fake.pdf");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ListKeys());
    }

    [Fact]
    public async Task ScheduleDownload_FileMissingInStorage_ReturnsNotFound()
    {
        using var client = CreateClientWithMinio();
        using var upload = await UploadSchedule(client, Pdf("удалим из хранилища"), "lost.pdf");
        var id = (await upload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        await _s3.DeleteObjectAsync(_bucket, Assert.Single(await ListKeys()));

        using var download = await client.GetAsync($"ScheduleDocument/Download/{id}");

        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
    }

    private S3FileStorage CreateStorage() =>
        new(_minio.CreateOptions(_bucket), NullLogger<S3FileStorage>.Instance);

    private HttpClient CreateClientWithMinio()
    {
        var options = _minio.CreateOptions(_bucket);
        var factory = Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<S3StorageOptions>();
            services.AddSingleton(options);
        }));
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri($"http://localhost{KnowledgeAppFactory.PathBase}/")
        });
    }

    private static async Task<HttpResponseMessage> UploadSchedule(HttpClient client, byte[] content, string fileName)
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        using var form = new MultipartFormDataContent
        {
            { new StringContent(TestSeed.CurrentSemesterId.ToString()), "SemesterId" },
            { file, "File", fileName }
        };
        return await client.PostAsync("ScheduleDocument/Upload", form);
    }

    private async Task<List<string>> ListKeys()
    {
        try
        {
            var response = await _s3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = _bucket });
            return (response.S3Objects ?? []).Select(o => o.Key).ToList();
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }
    }

    private static byte[] Pdf(string marker) => Encoding.UTF8.GetBytes($"%PDF-1.7\n% {marker}\n%%EOF");

    private static byte[] RandomBytes(int length)
    {
        var bytes = new byte[length];
        Random.Shared.NextBytes(bytes);
        return bytes;
    }

    private static async Task<byte[]> ReadAll(Stream stream)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return buffer.ToArray();
    }
}
