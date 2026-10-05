namespace KnowledgeApp.Application.Interfaces;

public interface ICurrentUserProvider
{
    int? UserId { get; }

    int? FacultyId { get; }
}
