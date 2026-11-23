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
                Check((await service.GetGroupsAsync(current.Id)).Any(r => r.GroupId == group.Id && r.Disciplines.Count == 0),
                    "Подтверждённая группа видна до создания тестирований");
                await service.SetDisciplinesAsync(group.Id, [own.Id], current.Id);
                var first = await db.Testings.AsNoTracking().SingleAsync(t => t.GroupId == group.Id && t.SemesterId == current.Id);
                Check(first.Id > 0 && first.DisciplineId == own.Id && first.GroupParticipationDecisionId != null,
                    "Первое сохранение создаёт тестирование с ID, дисциплиной и ссылкой на решение кафедры");
                await service.SetDisciplinesAsync(group.Id, [own.Id, crossFaculty.Id], current.Id);
                await service.SetDisciplinesAsync(group.Id, [own.Id, crossFaculty.Id], current.Id);
                var saved = await db.Testings.AsNoTracking().Where(t => t.GroupId == group.Id && t.SemesterId == current.Id).ToListAsync();
                Check(saved.Count == 2 && saved.Any(t => t.Id == first.Id && t.DisciplineId == own.Id)
                    && saved.Any(t => t.Id > 0 && t.DisciplineId == crossFaculty.Id),
                    "Добавление второй дисциплины сохраняет первый ID, повтор не создаёт дубликаты");
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
                Check(!await db.Testings.AnyAsync(t => t.GroupId == group.Id && t.DisciplineId != null),
                    "Промежуточный выбор можно очистить до начала следующего этапа");
                await service.SetDisciplinesAsync(group.Id, [crossFaculty.Id], current.Id);
                await db.Testings.Where(t => t.GroupId == group.Id && t.DisciplineId == crossFaculty.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.ScheduledDate, new DateTime(2026, 12, 1)));
                await Reject<InvalidOperationException>(() => service.SetDisciplinesAsync(group.Id, [], current.Id));
                row = (await service.GetGroupsAsync(current.Id)).Single(r => r.GroupId == group.Id);
                Check(row.Disciplines.Single().Locked, "Назначенная дата защищает дисциплину от снятия и отмечается в UI");
                await service.SetDisciplinesAsync(group.Id, [crossFaculty.Id], current.Id);
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
