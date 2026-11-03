using System.Net.Mail;
using KnowledgeApp.Application.DTOs.Admin;
using KnowledgeApp.Application.DTOs.Documents;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Services;

public sealed class AdminUserService
{
    private const int MaxPageSize = 200;
    private const string GuestRoleName = "Гость";
    private readonly KnowledgeTestDbContext _context;

    public AdminUserService(KnowledgeTestDbContext context)
    {
        _context = context;
    }

    public async Task<AdminUserPageDto> GetUsers(AdminUserQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1)
            throw new DocumentValidationException("Номер страницы должен быть больше нуля.");
        if (query.PageSize < 1 || query.PageSize > MaxPageSize)
            throw new DocumentValidationException($"Размер страницы должен быть от 1 до {MaxPageSize}.");

        var statusFilter = AdminUserStatus.Normalize(query.Statuses);
        var statusCodes = await AdminUserStatus.LoadCodes(_context, cancellationToken);

        var users = _context.Users.AsNoTracking();
        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            users = users.Where(u => u.Name!.Contains(search) || u.Email!.Contains(search));
        if (query.FacultyId.HasValue)
            users = users.Where(u => u.FacultyId == query.FacultyId);
        if (query.DepartmentId.HasValue)
        {
            users = users.Where(u =>
                u.DepartmentId == query.DepartmentId ||
                (u.DepartmentId == null &&
                 _context.DisciplineTeachers.Any(dt =>
                     dt.ResponsibleTeacherId == u.Id &&
                     dt.Discipline!.DepartmentId == query.DepartmentId)));
        }
        if (query.RoleId.HasValue)
        {
            var isGuestRole = await _context.Roles
                .AnyAsync(r => r.Id == query.RoleId && r.RoleName == GuestRoleName, cancellationToken);
            users = users.Where(u =>
                u.UserRoles.Any(ur => ur.RoleId == query.RoleId) ||
                (isGuestRole && !u.UserRoles.Any(ur => ur.RoleId != null)));
        }

        var statusGroups = await users
            .GroupBy(u => u.StatusId)
            .Select(g => new { StatusId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var counts = AdminUserStatus.Options()
            .Select(o => new AdminStatusCountDto(
                o.Value,
                o.Label,
                statusGroups
                    .Where(g => CodeOf(g.StatusId, statusCodes) == o.Value)
                    .Sum(g => g.Count)))
            .ToList();

        if (statusFilter.Count > 0)
        {
            var statusIds = statusCodes.Where(s => statusFilter.Contains(s.Value)).Select(s => s.Key).ToList();
            var includeWithoutStatus = statusFilter.Contains(AdminUserStatus.Active);
            users = users.Where(u =>
                (u.StatusId != null && statusIds.Contains(u.StatusId.Value)) ||
                (includeWithoutStatus && u.StatusId == null));
        }

        var total = await users.CountAsync(cancellationToken);
        var pageIds = await users
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var items = await Load(pageIds, statusCodes, cancellationToken);
        return new AdminUserPageDto(items, total, query.Page, query.PageSize, counts);
    }

    public async Task<AdminUserDto> GetUser(int userId, CancellationToken cancellationToken)
    {
        var statusCodes = await AdminUserStatus.LoadCodes(_context, cancellationToken);
        var user = (await Load([userId], statusCodes, cancellationToken)).SingleOrDefault();
        return user ?? throw new DocumentNotFoundException("Пользователь не найден");
    }

    public async Task<AdminUserFiltersDto> GetFilters(CancellationToken cancellationToken)
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .Select(r => new FilterOptionDto { Value = r.Id.ToString(), Label = r.RoleName })
            .ToListAsync(cancellationToken);
        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new FilterOptionDto { Value = d.Id.ToString(), Label = d.Name })
            .ToListAsync(cancellationToken);
        var faculties = await _context.Faculties
            .AsNoTracking()
            .OrderBy(f => f.FacultyName)
            .Select(f => new FilterOptionDto { Value = f.Id.ToString(), Label = f.FacultyName })
            .ToListAsync(cancellationToken);

        return new AdminUserFiltersDto(roles, departments, faculties, AdminUserStatus.Options());
    }

    public async Task<AdminUserDto> CreateUser(CreateAdminUserCommand command, CancellationToken cancellationToken)
    {
        var name = command.Name?.Trim();
        if (string.IsNullOrEmpty(name))
            throw new DocumentValidationException("Укажите ФИО пользователя.");
        if (name.Length > 255)
            throw new DocumentValidationException("ФИО длиннее 255 символов.");

        var email = command.Email?.Trim();
        if (string.IsNullOrEmpty(email) || email.Length > 255 || !IsEmail(email))
            throw new DocumentValidationException("Укажите корректную почту.");
        if (await _context.Users.AnyAsync(u => u.Email == email, cancellationToken))
            throw new DocumentConflictException("Пользователь с такой почтой уже есть.");

        if (command.RoleId.HasValue &&
            !await _context.Roles.AnyAsync(r => r.Id == command.RoleId, cancellationToken))
            throw new DocumentValidationException("Роль не найдена.");

        var facultyId = command.FacultyId;
        if (command.DepartmentId.HasValue)
        {
            var department = await _context.Departments
                .AsNoTracking()
                .Where(d => d.Id == command.DepartmentId)
                .Select(d => new { d.FacultyId })
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new DocumentValidationException("Кафедра не найдена.");
            facultyId ??= department.FacultyId;
        }
        if (facultyId.HasValue &&
            !await _context.Faculties.AnyAsync(f => f.Id == facultyId, cancellationToken))
            throw new DocumentValidationException("Факультет не найден.");

        var user = new User
        {
            Name = name,
            Email = email,
            Password = string.Empty,
            DepartmentId = command.DepartmentId,
            FacultyId = facultyId
        };

        await _context.InTransactionAsync(async () =>
        {
            user.StatusId = await AdminUserStatus.EnsureStatusId(_context, AdminUserStatus.Active, cancellationToken);
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            if (command.RoleId.HasValue)
            {
                _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = command.RoleId });
                await _context.SaveChangesAsync(cancellationToken);
            }
        }, cancellationToken);

        return await GetUser(user.Id, cancellationToken);
    }

    private async Task<List<AdminUserDto>> Load(
        IReadOnlyCollection<int> userIds,
        IReadOnlyDictionary<int, string> statusCodes,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return [];

        var users = await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                u.DepartmentId,
                DepartmentName = u.Department != null ? u.Department.Name : null,
                u.FacultyId,
                FacultyName = u.Faculty != null ? u.Faculty.FacultyName : null,
                u.StatusId,
                u.LastLoginAt
            })
            .ToListAsync(cancellationToken);

        var roles = (await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId != null && userIds.Contains(ur.UserId.Value) && ur.Role != null)
                .Select(ur => new { UserId = ur.UserId!.Value, RoleId = ur.Role!.Id, ur.Role.RoleName })
                .ToListAsync(cancellationToken))
            .ToLookup(r => r.UserId);

        var taught = (await _context.DisciplineTeachers
                .AsNoTracking()
                .Where(dt => dt.ResponsibleTeacherId != null &&
                             userIds.Contains(dt.ResponsibleTeacherId.Value) &&
                             dt.Discipline!.DepartmentId != null)
                .Select(dt => new
                {
                    UserId = dt.ResponsibleTeacherId!.Value,
                    DepartmentId = dt.Discipline!.DepartmentId!.Value,
                    dt.Discipline.Department!.Name
                })
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToLookup(t => t.UserId);

        var divisions = (await _context.EmployeeRightsRequests
                .AsNoTracking()
                .Where(r => userIds.Contains(r.UserId) && r.IsActive && r.StructuralDivision != null)
                .Select(r => new { r.UserId, r.StructuralDivision })
                .ToListAsync(cancellationToken))
            .ToLookup(r => r.UserId);

        return users
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Id)
            .Select(u =>
            {
                var userRoles = roles[u.Id]
                    .OrderBy(r => r.RoleId)
                    .Select(r => new AdminUserRoleDto(r.RoleId, r.RoleName))
                    .ToList();
                var taughtDepartments = taught[u.Id].OrderBy(t => t.Name).ToList();
                var departmentId = u.DepartmentId ??
                                   (taughtDepartments.Count == 1 ? taughtDepartments[0].DepartmentId : null);
                var department = u.DepartmentName ??
                                 (taughtDepartments.Count > 0
                                     ? string.Join(", ", taughtDepartments.Select(t => t.Name))
                                     : divisions[u.Id].Select(d => d.StructuralDivision).FirstOrDefault());
                var status = CodeOf(u.StatusId, statusCodes);

                return new AdminUserDto(
                    u.Id,
                    u.Name,
                    u.Email,
                    departmentId,
                    department,
                    u.FacultyId,
                    u.FacultyName,
                    userRoles,
                    userRoles.FirstOrDefault()?.Name ?? GuestRoleName,
                    status,
                    AdminUserStatus.LabelOf(status),
                    u.LastLoginAt.HasValue ? DocumentStatusInfo.AsUtc(u.LastLoginAt.Value) : null);
            })
            .ToList();
    }

    private static string CodeOf(int? statusId, IReadOnlyDictionary<int, string> statusCodes) =>
        statusId.HasValue
            ? statusCodes.GetValueOrDefault(statusId.Value, AdminUserStatus.Inactive)
            : AdminUserStatus.Active;

    private static bool IsEmail(string value) =>
        MailAddress.TryCreate(value, out var address) && address.Address == value;
}
