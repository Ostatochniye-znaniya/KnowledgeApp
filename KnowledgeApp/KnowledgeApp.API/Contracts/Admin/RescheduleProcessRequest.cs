namespace KnowledgeApp.API.Contracts.Admin;

public sealed record RescheduleProcessRequest(DateOnly? Date, string? Time, string? Reason);
