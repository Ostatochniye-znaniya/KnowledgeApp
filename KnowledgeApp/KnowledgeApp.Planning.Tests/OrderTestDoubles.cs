using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Infrastructure.Storage;

internal sealed class OrderTestStorage : IFileStorage
{
    public Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Storage must not be called by validation tests");
    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Storage must not be called by validation tests");
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Storage must not be called by validation tests");
}

internal sealed class OrderTestUser : ICurrentUserProvider
{
    public int? UserId => null;
    public int? FacultyId => null;
    public int? DepartmentId => null;
}
