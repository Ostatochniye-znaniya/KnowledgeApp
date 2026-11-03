namespace KnowledgeApp.API.Contracts.Admin;

public sealed record ChangeUserRoleRequest(int? RoleId, string? Reason);
