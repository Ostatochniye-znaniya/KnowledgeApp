namespace KnowledgeApp.API.Contracts;

/// <summary>
/// Период проверки, преподаватели и необязательная рекомендация (дата и пара задаются только вместе).
/// Запрос без рекомендации снимает ранее заданную.
/// </summary>
public sealed record UpdateTestingAssignmentsRequest(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyList<int> TeacherIds,
    DateOnly? RecommendedDate = null,
    int? RecommendedPair = null);
