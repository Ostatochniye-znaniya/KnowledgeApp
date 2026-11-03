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
    private const string AdminRoleName = "Администратор";
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

    public async Task<AdminUserDto> ChangeRole(
        int userId,
        int? roleId,
        string? reason,
        AdminActor actor,
        CancellationToken cancellationToken)
    {
        if (!roleId.HasValue)
            throw new DocumentValidationException("Укажите роль.");
        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (normalizedReason?.Length > AdminActors.MaxReasonLength)
            throw new DocumentValidationException($"Причина длиннее {AdminActors.MaxReasonLength} символов.");

        var role = await _context.Roles.AsNoTracking().SingleOrDefaultAsync(r => r.Id == roleId, cancellationToken)
                   ?? throw new DocumentValidationException("Роль не найдена.");
        var actorName = await AdminActors.NameOf(_context, actor, cancellationToken);

        await _context.InTransactionAsync(System.Data.IsolationLevel.Serializable, async () =>
        {
            if (!await _context.Users.AnyAsync(u => u.Id == userId, cancellationToken))
                throw new DocumentNotFoundException("Пользователь не найден");

            var currentRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Include(ur => ur.Role)
                .ToListAsync(cancellationToken);
            if (currentRoles.Count == 1 && currentRoles[0].RoleId == role.Id)
                throw new DocumentValidationException("Эта роль уже назначена пользователю.");

            var removesAdmin = currentRoles.Any(ur => ur.Role?.RoleName == AdminRoleName) &&
                               role.RoleName != AdminRoleName;
            if (removesAdmin && actor.UserId == userId)
                throw new DocumentConflictException("Нельзя снять роль администратора с самого себя.");
            if (removesAdmin)
            {
                var otherAdmins = await _context.UserRoles
                    .AnyAsync(ur => ur.UserId != userId && ur.Role!.RoleName == AdminRoleName, cancellationToken);
                if (!otherAdmins)
                    throw new DocumentConflictException("Нельзя снять роль с последнего администратора системы.");
            }

            var previousRoles = currentRoles
                .Where(ur => ur.Role != null)
                .OrderBy(ur => ur.RoleId)
                .Select(ur => ur.Role!.RoleName)
                .ToList();
            _context.UserRoles.RemoveRange(currentRoles);
            _context.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id });
            _context.UserRoleChanges.Add(new UserRoleChange
            {
                UserId = userId,
                PreviousRoles = previousRoles.Count > 0 ? string.Join(", ", previousRoles) : GuestRoleName,
                NewRoles = role.RoleName,
                Reason = normalizedReason,
                ChangedByUserId = actor.UserId,
                ChangedByName = actorName,
                ChangedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return await GetUser(userId, cancellationToken);
    }

    public async Task<AdminUserDto> ChangeStatus(
        int userId,
        string? status,
        AdminActor actor,
        CancellationToken cancellationToken)
    {
        var code = status?.Trim().ToLowerInvariant();
        if (code is not (AdminUserStatus.Active or AdminUserStatus.Blocked))
            throw new DocumentValidationException("Статус должен быть active или blocked.");
        if (code == AdminUserStatus.Blocked && actor.UserId == userId)
            throw new DocumentConflictException("Нельзя заблокировать самого себя.");

        await _context.InTransactionAsync(System.Data.IsolationLevel.Serializable, async () =>
        {
            var user = await _context.Users
                .Include(u => u.Status)
                .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
                ?? throw new DocumentNotFoundException("Пользователь не найден");
            if (AdminUserStatus.CodeOf(user.Status?.StatusName) == code)
                throw new DocumentValidationException(code == AdminUserStatus.Blocked
                    ? "Пользователь уже заблокирован."
                    : "Пользователь уже активен.");

            if (code == AdminUserStatus.Blocked &&
                await _context.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.Role!.RoleName == AdminRoleName,
                    cancellationToken))
            {
                var blockedStatusIds = await _context.Statuses
                    .Where(s => s.StatusName == AdminUserStatus.LabelOf(AdminUserStatus.Blocked))
                    .Select(s => (int?)s.Id)
                    .ToListAsync(cancellationToken);
                var otherActiveAdmins = await _context.UserRoles.AnyAsync(ur =>
                        ur.UserId != userId &&
                        ur.Role!.RoleName == AdminRoleName &&
                        !blockedStatusIds.Contains(ur.User!.StatusId),
                    cancellationToken);
                if (!otherActiveAdmins)
                    throw new DocumentConflictException("Нельзя заблокировать последнего активного администратора.");
            }

            user.StatusId = await AdminUserStatus.EnsureStatusId(_context, code, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return await GetUser(userId, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminUserRoleChangeDto>> GetRoleHistory(
        int userId,
        CancellationToken cancellationToken)
    {
        if (!await _context.Users.AnyAsync(u => u.Id == userId, cancellationToken))
            throw new DocumentNotFoundException("Пользователь не найден");

        var changes = await _context.UserRoleChanges
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.ChangedAt)
            .ThenByDescending(c => c.Id)
            .ToListAsync(cancellationToken);

        return changes
            .Select(c => new AdminUserRoleChangeDto(
                c.Id,
                c.PreviousRoles,
                c.NewRoles,
                c.Reason,
                c.ChangedByUserId,
                c.ChangedByName,
                DocumentStatusInfo.AsUtc(c.ChangedAt)))
            .ToList();
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
