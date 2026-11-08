using KnowledgeApp.Application.DTOs.Documents;

namespace KnowledgeApp.Application.DTOs.Admin;

public sealed record AdminUserQuery(
    string? Search,
    int? RoleId,
    int? DepartmentId,
    int? FacultyId,
    IReadOnlyCollection<string> Statuses,
    int Page,
    int PageSize);

public sealed record AdminUserRoleDto(int Id, string Name);

public sealed record AdminUserDto(
    int Id,
    string? Name,
    string? Email,
    int? DepartmentId,
    string? Department,
    int? FacultyId,
    string? Faculty,
    IReadOnlyList<AdminUserRoleDto> Roles,
    string Role,
    string Status,
    string StatusLabel,
    DateTimeOffset? LastLoginAt);

public sealed record AdminUserPageDto(
    IReadOnlyList<AdminUserDto> Items,
    int Total,
    int Page,
    int PageSize,
    IReadOnlyList<AdminStatusCountDto> StatusCounts);

public sealed record AdminUserFiltersDto(
    IReadOnlyList<FilterOptionDto> Roles,
    IReadOnlyList<FilterOptionDto> Departments,
    IReadOnlyList<FilterOptionDto> Faculties,
    IReadOnlyList<FilterOptionDto> Statuses);

public sealed record AdminUserRoleChangeDto(
    int Id,
    string? PreviousRoles,
    string NewRoles,
    string? Reason,
    int? ChangedByUserId,
    string ChangedByName,
    DateTimeOffset ChangedAt);

public sealed record CreateAdminUserCommand(
    string? Name,
    string? Email,
    int? RoleId,
    int? DepartmentId,
    int? FacultyId);
