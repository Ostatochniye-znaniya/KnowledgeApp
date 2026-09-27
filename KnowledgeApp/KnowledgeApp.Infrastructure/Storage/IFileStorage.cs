namespace KnowledgeApp.Infrastructure.Storage;

public interface IFileStorage
{
    Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
