namespace KnowledgeApp.Infrastructure.Storage;

public class S3StorageOptions
{
    public string Endpoint { get; set; } = "http://localhost:9000";

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string Bucket { get; set; } = "knowledgeapp-documents";

    public string Region { get; set; } = "us-east-1";

    public static S3StorageOptions FromEnvironment()
    {
        var options = new S3StorageOptions();
        options.Endpoint = Environment.GetEnvironmentVariable("S3_ENDPOINT") ?? options.Endpoint;
        options.AccessKey = Environment.GetEnvironmentVariable("S3_ACCESS_KEY") ?? options.AccessKey;
        options.SecretKey = Environment.GetEnvironmentVariable("S3_SECRET_KEY") ?? options.SecretKey;
        options.Bucket = Environment.GetEnvironmentVariable("S3_BUCKET") ?? options.Bucket;
        options.Region = Environment.GetEnvironmentVariable("S3_REGION") ?? options.Region;
        return options;
    }
}
