namespace KnowledgeApp.API.Contracts;

public sealed record UpdateTestingAssignmentsRequest(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyList<int> TeacherIds);
