namespace KnowledgeApp.API.Contracts.Admin;

public sealed record CreateAdminUserRequest(
    string? Name,
    string? Email,
    int? RoleId,
    int? DepartmentId,
    int? FacultyId);
