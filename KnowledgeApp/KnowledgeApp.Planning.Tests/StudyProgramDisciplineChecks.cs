using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Application.Services;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using KnowledgeApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

internal static class StudyProgramDisciplineChecks
{
    public static async Task Integration(string serverConnection)
    {
        var database = "program_disciplines_check_" + Guid.NewGuid().ToString("N");
        var connection = new MySqlConnectionStringBuilder(serverConnection) { Database = database };
        const string variable = "ConnectionStrings__DefaultConnection";
        var previous = Environment.GetEnvironmentVariable(variable);
        Environment.SetEnvironmentVariable(variable, connection.ConnectionString);
        try
        {
            await using var db = new KnowledgeTestDbContext();
            Check(db.Database.GetDbConnection().Database == database, "Требуется отдельная тестовая БД");
            try
            {
                await db.Database.MigrateAsync();
                var faculty = new Faculty { FacultyName = "Факультет проверки программ" };
                var otherFaculty = new Faculty { FacultyName = "Другой факультет" };
                var department = new Department { Name = "Своя кафедра", Faculty = faculty };
                var sibling = new Department { Name = "Соседняя кафедра", Faculty = faculty };
                var external = new Department { Name = "Внешняя кафедра", Faculty = otherFaculty };
                StudyProgram Profile(string name, Department owner) => new() { Name = name,
                    CypherOfTheDirection = "09.03.03", Department = owner,
                    StudyGroups = [new StudyGroup { GroupNumber = "241-361" }] };
                var profile = Profile("Программа с дисциплинами", department);
                var empty = Profile("Перечень не заполнен", department);
                var foreign = Profile("Чужая группа", external);
                var own = new Discipline { Name = "Своя дисциплина программы", Department = department };
                var sameFaculty = new Discipline { Name = "Дисциплина соседней кафедры", Department = sibling };
                var crossFaculty = new Discipline { Name = "Межфакультетская дисциплина", Department = external };
                var unrelated = new Discipline { Name = "Своя кафедра, другая программа", Department = department };
                db.AddRange(profile, empty, foreign, own, sameFaculty, crossFaculty, unrelated);
                await db.SaveChangesAsync();
                var group = profile.StudyGroups.Single();
                var emptyGroup = empty.StudyGroups.Single();
                var foreignGroup = foreign.StudyGroups.Single();
                var current = await db.Semesters.SingleAsync(s => s.SemesterYear == 2026 && s.SemesterPart == 2);
                foreach (var selected in new[] { group, emptyGroup, foreignGroup })
                {
                    db.GroupParticipationDecisions.Add(new GroupParticipationDecision {
                        StudyProgramId = selected.StudyProgramId, SemesterId = current.Id,
                        SelectedGroupId = selected.Id, DecidedAt = DateTime.UtcNow });
                    db.Testings.AddRange(new Testing { GroupId = selected.Id, SemesterId = current.Id },
                        new Testing { GroupId = selected.Id, SemesterId = current.Id });
                }
                db.StudyProgramDisciplines.AddRange(new[] { own, sameFaculty, crossFaculty }
                    .Select(d => new StudyProgramDiscipline { StudyProgramId = profile.Id, DisciplineId = d.Id }));
                await db.SaveChangesAsync();
                var service = new GroupDisciplineService(new GroupDisciplineRepository(db), new Actor(department.Id));
                var available = await service.GetAvailableDisciplinesAsync(group.Id);
                Check(available.Select(d => d.Id).ToHashSet().SetEquals([own.Id, sameFaculty.Id, crossFaculty.Id]),
                    "Перечень определяется программой, а не кафедрой дисциплины");
                Check(available.Single(d => d.Id == crossFaculty.Id).FacultyName == otherFaculty.FacultyName,
                    "Передаются кафедра и факультет внешней дисциплины");
                Check((await service.GetAvailableDisciplinesAsync(emptyGroup.Id)).Count == 0,
                    "Пустая программа не подменяется дисциплинами своей кафедры");
                await Reject<UnauthorizedAccessException>(() => service.GetAvailableDisciplinesAsync(foreignGroup.Id));
                await Reject<UnauthorizedAccessException>(() => service.SetDisciplinesAsync(foreignGroup.Id, [own.Id], current.Id));
                var groupTestings = db.Testings.Where(t => t.GroupId == group.Id && t.SemesterId == current.Id);
                var testingIds = await groupTestings.OrderBy(t => t.Id).Select(t => t.Id).ToArrayAsync();
                // Старый выбор дисциплины, сделанный до автоматического создания отчётов.
                await groupTestings.Where(t => t.Id == testingIds[0])
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.DisciplineId, own.Id));
                await service.SetDisciplinesAsync(group.Id, [own.Id, crossFaculty.Id], current.Id);
                var saved = await groupTestings.AsNoTracking().Include(t => t.Report).OrderBy(t => t.Id).ToListAsync();
                Check(saved.Select(t => t.Id).SequenceEqual(testingIds)
                    && saved.Select(t => t.DisciplineId).SequenceEqual(new int?[] { own.Id, crossFaculty.Id })
                    && saved.All(t => t.Report is { Id: > 0, FilePath: null } && t.Report.DisciplineId == t.DisciplineId),
                    "Для старой и новой дисциплин созданы отчёты в существующих тестированиях");
                var originalReports = saved.Select(t => t.ReportId!.Value).ToArray();
                Check(originalReports.Distinct().Count() == 2, "Каждое тестирование получает свой отчёт");
                await service.SetDisciplinesAsync(group.Id, [crossFaculty.Id, own.Id], current.Id);
                Check((await groupTestings.OrderBy(t => t.Id).Select(t => t.ReportId!.Value).ToArrayAsync()).SequenceEqual(originalReports),
                    "Повторное сохранение сохраняет те же отчёты");
                await service.SetDisciplinesAsync(group.Id, [crossFaculty.Id, sameFaculty.Id], current.Id);
                Check(!await db.Reports.AnyAsync(r => r.Id == originalReports[0])
                    && await groupTestings.AnyAsync(t => t.DisciplineId == sameFaculty.Id && t.Report!.DisciplineId == sameFaculty.Id),
                    "При замене создан отчёт для новой дисциплины и удалён прежний пустой");
                await service.SetDisciplinesAsync(group.Id, [own.Id, crossFaculty.Id], current.Id);
                await Reject<ArgumentException>(() => service.SetDisciplinesAsync(group.Id, [own.Id, sameFaculty.Id, crossFaculty.Id], current.Id));
                await Reject<ArgumentException>(() => service.SetDisciplinesAsync(group.Id, [own.Id, own.Id], current.Id));
                await Reject<ArgumentException>(() => service.SetDisciplinesAsync(group.Id, [unrelated.Id], current.Id));
                Check(await db.Testings.AnyAsync(t => t.GroupId == group.Id && t.DisciplineId == crossFaculty.Id),
                    "Ошибочный запрос не перезаписывает прежний выбор");
                await db.StudyProgramDisciplines.Where(p => p.StudyProgramId == profile.Id && p.DisciplineId == own.Id).ExecuteDeleteAsync();
                await service.SetDisciplinesAsync(group.Id, [own.Id, crossFaculty.Id], current.Id);
                var rows = await service.GetGroupsAsync(current.Id);
                var row = rows.Single(r => r.GroupId == group.Id);
                Check(rows.All(r => r.GroupId != foreignGroup.Id) && row.AvailableDisciplinesCount == 2,
                    "Список групп ограничен своей кафедрой и содержит размер перечня программы");
                Check(!row.Disciplines.Single(d => d.Id == own.Id).IsInStudyProgram,
                    "Прежний выбор вне нового перечня виден и сохраняется");
                await service.SetDisciplinesAsync(group.Id, [crossFaculty.Id], current.Id);
                await Reject<ArgumentException>(() => service.SetDisciplinesAsync(group.Id, [own.Id, crossFaculty.Id], current.Id));
                await service.SetDisciplinesAsync(group.Id, [], current.Id);
                Check(await groupTestings.CountAsync(t => t.DisciplineId == null && t.ReportId == null) == 2
                    && !await db.Reports.AnyAsync(),
                    "Очистка выбора удаляет пустые отчёты и оставляет два существующих тестирования");
                await service.SetDisciplinesAsync(group.Id, [crossFaculty.Id], current.Id);
                var selectedTesting = groupTestings.Where(t => t.DisciplineId == crossFaculty.Id);
                await selectedTesting
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.ScheduledDate, new DateTime(2026, 12, 1)));
                await Reject<InvalidOperationException>(() => service.SetDisciplinesAsync(group.Id, [], current.Id));
                row = (await service.GetGroupsAsync(current.Id)).Single(r => r.GroupId == group.Id);
                Check(row.Disciplines.Single().Locked, "Назначенная дата защищает дисциплину от снятия и отмечается в UI");
                await service.SetDisciplinesAsync(group.Id, [crossFaculty.Id], current.Id);
                await selectedTesting.ExecuteUpdateAsync(s => s.SetProperty(t => t.ScheduledDate, (DateTime?)null));
                var reportId = await selectedTesting.Select(t => t.ReportId!.Value).SingleAsync();
                Check((await new ReportDocumentRepository(db).AttachFile(reportId, "reports/upload.docx", "upload.docx", DateTime.UtcNow)).Replaced,
                    "Созданный отчёт принимает файл через обычный механизм загрузки");
                await Reject<InvalidOperationException>(() => service.SetDisciplinesAsync(group.Id, [], current.Id));
                await service.SetDisciplinesAsync(group.Id, [crossFaculty.Id], current.Id);
                Check(await selectedTesting.AnyAsync(t => t.ReportId == reportId && t.Report!.FilePath == "reports/upload.docx"),
                    "Нельзя снять дисциплину с загруженным отчётом, повторное сохранение сохраняет файл");
                Console.WriteLine("PASS Дисциплины: миграции, программа профиля, чужие кафедры/факультеты, ограничения, прежний выбор и защита следующего этапа (MySQL)");
            }
            finally { await db.Database.EnsureDeletedAsync(); }
        }
        finally { Environment.SetEnvironmentVariable(variable, previous); }
    }

    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static async Task Reject<T>(Func<Task> action) where T : Exception
    {
        try { await action(); } catch (T) { return; }
        throw new Exception($"Ожидалось исключение {typeof(T).Name}");
    }
    private sealed class Actor(int departmentId) : ICurrentUserProvider
    {
        public int? UserId => null;
        public int? DepartmentId => departmentId;
        public int? FacultyId => null;
    }
}
