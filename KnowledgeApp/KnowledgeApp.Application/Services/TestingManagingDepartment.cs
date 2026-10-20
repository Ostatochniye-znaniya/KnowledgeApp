using KnowledgeApp.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Services;

/// <summary>
/// Какая кафедра назначает преподавателей и период проверки (ТЗ, п. 2.5).
/// Дисциплина кафедры того же факультета, что и группа, — назначает кафедра группы.
/// Дисциплина кафедры другого факультета — кафедра группы отправляет заявку,
/// и назначает кафедра дисциплины (только после заявки).
/// </summary>
public static class TestingManagingDepartment
{
    public sealed record Info(
        int? GroupDepartmentId,
        int? GroupFacultyId,
        int? DisciplineDepartmentId,
        int? DisciplineFacultyId,
        DateTime? ExternalRequestSentAt)
    {
        /// <summary>Дисциплина закреплена за кафедрой другого факультета.</summary>
        public bool IsExternal => GroupFacultyId != DisciplineFacultyId;

        /// <summary>Кафедра, которая сейчас назначает; null — пока заявка не отправлена.</summary>
        public int? ManagingDepartmentId =>
            !IsExternal ? GroupDepartmentId
            : ExternalRequestSentAt.HasValue ? DisciplineDepartmentId
            : null;
    }

    public static Task<Info?> LoadAsync(
        KnowledgeTestDbContext context,
        int testingId,
        CancellationToken cancellationToken)
    {
        return context.Testings
            .AsNoTracking()
            .Where(t => t.Id == testingId)
            .Select(t => new Info(
                t.Group!.StudyProgram!.DepartmentId,
                t.Group.StudyProgram.Department!.FacultyId,
                t.Discipline!.DepartmentId,
                t.Discipline.Department!.FacultyId,
                t.ExternalRequestSentAt))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
