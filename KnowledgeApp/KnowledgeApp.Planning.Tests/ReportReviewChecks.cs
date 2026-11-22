using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Application.Services;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using KnowledgeApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;

internal static class ReportReviewChecks
{
    public static void Validation()
    {
        using var db = new KnowledgeTestDbContext();
        var service = Service(db, new OrderTestUser());
        foreach (var comment in new string?[] { null, "", "   ", new string('x', 1001) })
            Reject<DocumentValidationException>(() => service.RejectForLpr(1, comment, "version", default));
        Reject<DocumentValidationException>(() => service.ApproveForLpr(1, null, default));
        Reject<DocumentValidationException>(() => service.GetForLpr(0, null, null, null, default));
        Reject<DocumentValidationException>(() => service.GetForLpr(null, -1, null, null, default));
        Reject<DocumentValidationException>(() => service.GetForLpr(null, null, null, "unknown", default));
        Check(db.Database.GetDbConnection().State == System.Data.ConnectionState.Closed,
            "Некорректные запросы не должны обращаться к БД");
    }

    // Только по явному флагу. Создаёт отдельную БД со случайным именем и удаляет её в finally.
    public static async Task Integration(string serverConnection)
    {
        var database = "lpr_reports_check_" + Guid.NewGuid().ToString("N");
        var connection = new MySqlConnectionStringBuilder(serverConnection) { Database = database };
        const string variable = "ConnectionStrings__DefaultConnection";
        var previous = Environment.GetEnvironmentVariable(variable);
        Environment.SetEnvironmentVariable(variable, connection.ConnectionString);
        try
        {
            await using var db = new KnowledgeTestDbContext();
            Check(db.Database.GetDbConnection().Database == database, "Проверка должна использовать отдельную БД");
            try
            {
                await db.Database.EnsureCreatedAsync();
                var faculty = new Faculty { FacultyName = "Проверка факультет 1" };
                var otherFaculty = new Faculty { FacultyName = "Проверка факультет 2" };
                var department = new Department { Name = "Кафедра 1", Faculty = faculty };
                var otherDepartment = new Department { Name = "Кафедра 2", Faculty = otherFaculty };
                var discipline = new Discipline { Name = "Проверяемая дисциплина", Department = department };
                var otherDiscipline = new Discipline { Name = "Другая дисциплина", Department = otherDepartment };
                var teacher = new User { Name = "Проверяющий преподаватель", Email = "test@example.invalid", Password = "", Faculty = faculty };
                var report = new Report { Discipline = discipline, Teacher = teacher };
                var otherReport = new Report { Discipline = otherDiscipline, FilePath = "reports/other.docx", FileName = "other.docx", Status = DocumentStatus.Pending };
                var group = new StudyGroup { GroupNumber = "241-999", StudyProgram = new StudyProgram { Name = "Проверяемый профиль", Department = department } };
                var semester = new Semester { SemesterYear = 2026, SemesterPart = 2, YearPart = 1 };
                db.AddRange(report, otherReport, group, semester);
                await db.SaveChangesAsync();
                db.Add(new Testing { GroupId = group.Id, DisciplineId = discipline.Id, SemesterId = semester.Id, ReportId = report.Id });
                db.Add(new DisciplineTeacher { DisciplineId = discipline.Id, ResponsibleTeacherId = teacher.Id });
                await db.SaveChangesAsync();
                var repository = new ReportDocumentRepository(db);
                var user = new ReportTestUser(teacher.Id, faculty.Id);
                var service = Service(db, user);
                var workflow = new TeacherWorkflowService(db, user, null!, service);
                Check((await workflow.GetReports(default)).Single().Status == "not_submitted", "Без файла отчёт не сдан");

                await repository.AttachFile(report.Id, "reports/version1.docx", "version1.docx", DateTime.UtcNow);
                var pending = (await service.GetForLpr(null, null, null, "pending", default)).Single(r => r.Id == report.Id);
                Check(pending.DoneInElectronicForm == false && pending.ReviewComment == null, "Загрузка ещё не является принятием");
                Check((await service.GetForLpr(null, null, null, null, default)).Count == 2, "ЛПР видит оба факультета");
                Check((await service.GetForLpr(otherFaculty.Id, null, null, null, default)).Single().Id == otherReport.Id, "Фильтр факультета");
                Check((await service.GetForLpr(faculty.Id, otherDepartment.Id, null, null, default)).Count == 0, "Фильтры должны пересекаться");
                Check((await service.GetForDean(null, null, null)).Single().Id == report.Id, "Область декана не расширилась");
                Check((await service.GetForLpr(null, null, "2026/2027", null, default)).Single().Id == report.Id, "Фильтр учебного года");

                var rejected = await service.RejectForLpr(report.Id, "  Исправить число участников.  ", pending.FileVersion, default);
                Check(rejected.Status == "rejected" && rejected.DoneInElectronicForm == false && rejected.AllDone == false
                    && rejected.ReviewComment == "Исправить число участников.", "Возврат сохраняет замечания и признак Нет");
                var teacherRejected = (await workflow.GetReports(default)).Single();
                Check(teacherRejected.Status == "rejected" && teacherRejected.ReviewComment == rejected.ReviewComment,
                    "Преподаватель должен видеть то же решение и замечания");
                await RejectAsync<DocumentConflictException>(() => service.ApproveForLpr(report.Id, pending.FileVersion, default));

                await repository.AttachFile(report.Id, "reports/version2.docx", "version2.docx", DateTime.UtcNow);
                var reuploaded = (await service.GetForLpr(null, null, null, null, default)).Single(r => r.Id == report.Id);
                Check(reuploaded.Status == "pending" && reuploaded.ReviewComment == null && reuploaded.DoneInElectronicForm == false,
                    "Исправленный файл должен снова ожидать проверки");
                await RejectAsync<DocumentConflictException>(() => service.ApproveForLpr(report.Id, pending.FileVersion, default));
                Check(!await repository.Review(report.Id, DocumentStatus.Signed, "Нет", DateTime.UtcNow, teacher.Id, "reports/version1.docx"),
                    "Атомарная проверка не должна принять заменённый файл");

                var approved = await service.ApproveForLpr(report.Id, reuploaded.FileVersion, default);
                Check(approved.Status == "signed" && approved.ReviewComment == "Нет" && approved.DoneInElectronicForm == true
                    && approved.AllDone == false, "Принятие не должно означать сдачу бумаги");
                var saved = await db.Reports.AsNoTracking().SingleAsync(r => r.Id == report.Id);
                Check(saved.ReviewedAt.HasValue && saved.ReviewedByUserId == teacher.Id, "Должен сохраняться автор и время проверки");
                Check((await workflow.GetReports(default)).Single().Status == "signed", "Преподаватель видит принятие");
                Check(!(await repository.AttachFile(report.Id, "reports/forbidden.docx", "forbidden.docx", DateTime.UtcNow)).Replaced,
                    "Принятый файл нельзя заменить");
                await repository.SetPaperDelivery(report.Id, true, default);
                Check((await repository.GetById(report.Id))!.AllDone == true, "Всё сдано только после обеих форм");
                Console.WriteLine("PASS Цикл отчёта ЛПР/преподавателя, фильтры, повторная загрузка и защита от гонок (MySQL)");
            }
            finally
            {
                await db.Database.EnsureDeletedAsync();
            }
        }
        finally { Environment.SetEnvironmentVariable(variable, previous); }
    }

    private static ReportDocumentService Service(KnowledgeTestDbContext db, ICurrentUserProvider user) =>
        new(new ReportDocumentRepository(db), new DepartmentRepository(db), new OrderTestStorage(), user,
            NullLogger<ReportDocumentService>.Instance, new FacultyRepository(db));

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Reject<T>(Func<Task> action) where T : Exception => RejectAsync<T>(action).GetAwaiter().GetResult();
    private static async Task RejectAsync<T>(Func<Task> action) where T : Exception
    {
        try { await action(); } catch (T) { return; }
        throw new Exception($"Ожидалось исключение {typeof(T).Name}");
    }

    private sealed class ReportTestUser(int userId, int facultyId) : ICurrentUserProvider
    {
        public int? UserId => userId;
        public int? FacultyId => facultyId;
        public int? DepartmentId => null;
    }
}
