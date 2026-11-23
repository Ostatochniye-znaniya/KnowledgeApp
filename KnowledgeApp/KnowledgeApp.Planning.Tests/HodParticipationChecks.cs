using KnowledgeApp.Application.DTOs.GroupsTesting;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Application.Services;
using KnowledgeApp.Domain.Planning;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using KnowledgeApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MySqlConnector;

internal static class HodParticipationChecks
{
    public static async Task Integration(string serverConnection)
    {
        var database = "hod_groups_check_" + Guid.NewGuid().ToString("N");
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
                // Проверяем именно цепочку миграций, а не только EnsureCreated.
                await db.Database.MigrateAsync();
                var faculty = new Faculty { FacultyName = "Факультет проверки HOD" };
                var department = new Department { Name = "Кафедра проверки HOD", Faculty = faculty };
                var otherDepartment = new Department { Name = "Чужая кафедра", Faculty = faculty };
                var user = new User { Name = "Проверяющий HOD", Email = "hod-test@example.invalid", Password = "", Department = department };
                StudyProgram Profile(string name, Department owner, int count) => new() {
                    Name = name, Department = owner, CypherOfTheDirection = "09.03.03",
                    StudyGroups = Enumerable.Range(1, count).Select(i => new StudyGroup { GroupNumber = "241-" + i.ToString("000") }).ToList() };
                var profile = Profile("Три активные и архивная", department, 4);
                var pair = Profile("Две группы", department, 2);
                var single = Profile("Одна активная", department, 2);
                var foreign = Profile("Чужой профиль", otherDepartment, 1);
                var archive = Profile("Архивирование после рекомендации", department, 2);
                profile.StudyGroups.Last().IsActive = false;
                single.StudyGroups.Last().IsActive = false;
                db.AddRange(profile, pair, single, foreign, archive, user);
                await db.SaveChangesAsync();
                var groups = profile.StudyGroups.OrderBy(g => g.Id).ToArray();
                var pairs = pair.StudyGroups.OrderBy(g => g.Id).ToArray();
                var singles = single.StudyGroups.OrderBy(g => g.Id).ToArray();
                var archivedGroups = archive.StudyGroups.OrderBy(g => g.Id).ToArray();
                var current = await db.Semesters.SingleAsync(s => s.SemesterYear == 2026 && s.SemesterPart == 2);
                var past = await db.Semesters.SingleAsync(s => s.SemesterYear == 2026 && s.SemesterPart == 1);
                db.Testings.Add(new Testing { GroupId = groups[0].Id, SemesterId = past.Id, Status = "Completed" });
                await db.SaveChangesAsync();
                var actor = new Actor(user.Id, department.Id);
                var clock = new FixedClock();
                var settings = Options.Create(new GroupsTestingOptions());
                var planning = new GroupsTestingService(db, settings, clock, actor);
                Check((await planning.GetForHod(default)).Plan is null, "HOD не запускает автоподбор вместо ЛПР");
                var plan = await planning.AutoFill(default);
                var studentPeriod = new StudentListFormationPeriodDto(new(2026, 11, 16), new(2026, 11, 30));
                plan = await planning.Update(new UpdateGroupsTestingRequest { CurrentSemesterId = current.Id, Version = plan.Version,
                    Changes = [], StudentListFormationPeriod = studentPeriod }, default);
                Check((await planning.Get(null, null, default))!.StudentListFormationPeriod == studentPeriod,
                    "Период формирования списка студентов сохраняется и читается после открытия страницы");
                var periodVersion = plan.Version;
                plan = await planning.Update(new UpdateGroupsTestingRequest { CurrentSemesterId = current.Id, Version = plan.Version,
                    Changes = [], StudentListFormationPeriod = studentPeriod }, default);
                Check(plan.Version == periodVersion, "Повторное сохранение того же периода не изменяет версию");
                await Reject<ArgumentException>(() => planning.Update(new UpdateGroupsTestingRequest {
                    CurrentSemesterId = current.Id, Version = plan.Version, Changes = [],
                    StudentListFormationPeriod = new(new(2026, 12, 1), new(2026, 11, 1)) }, default));
                await Reject<PlanningRuleException>(() => planning.Update(new UpdateGroupsTestingRequest {
                    CurrentSemesterId = current.Id, Version = plan.Version - 1, Changes = [],
                    StudentListFormationPeriod = new(new(2026, 12, 1), new(2026, 12, 10)) }, default));
                Check((await planning.Get(null, null, default))!.StudentListFormationPeriod == studentPeriod,
                    "Некорректный период и устаревшая версия не перезаписывают сроки");
                plan = await planning.Update(new UpdateGroupsTestingRequest { CurrentSemesterId = current.Id, Version = plan.Version,
                    Changes = [new(groups[1].Id, current.Id, true), new(pairs[0].Id, current.Id, true), new(archivedGroups[0].Id, current.Id, true)] }, default);
                await db.StudyGroups.Where(g => g.Id == archivedGroups[0].Id).ExecuteUpdateAsync(s => s.SetProperty(g => g.IsActive, false));
                var hod = await planning.GetForHod(default);
                Check(hod.Plan!.StudentListFormationPeriod == studentPeriod, "Изменение назначений сохраняет период семестра");
                Check(hod.Plan!.StudyPrograms.Count == 4 && hod.Choices.All(c => c.StudyProgramId != foreign.Id), "Только своя кафедра");
                Check(hod.Choices.Single(c => c.StudyProgramId == archive.Id).RecommendedGroupId == archivedGroups[0].Id
                    && hod.Choices.Single(c => c.StudyProgramId == archive.Id).Editable, "Архивирование не стирает рекомендацию и не блокирует отказ/замену");
                Check(hod.Choices.Single(c => c.StudyProgramId == profile.Id).EligibleGroupIds.Order().SequenceEqual(new[] { groups[1].Id, groups[2].Id }.Order()),
                    "Замена ограничена циклом, соседним семестром и активностью");
                Check(hod.Plan.StudyPrograms.Single(p => p.StudyProgramId == single.Id).Groups.Single(g => g.GroupId == singles[0].Id)
                    .PlannedTestings.All(c => c.Planned), "Архивная группа не мешает правилу одиночной активной группы");
                var disciplines = new GroupDisciplineService(new GroupDisciplineRepository(db), actor, settings, clock);
                Check((await disciplines.GetGroupsAsync(null)).Count == 0, "До подтверждения нет новых групп на выборе дисциплин");

                Task<HodGroupsResponse> Save(params HodProfileDecisionRequest[] decisions) => planning.UpdateForHod(
                    new UpdateHodGroupsRequest { CurrentSemesterId = current.Id, Version = hod.Plan!.Version, Decisions = decisions.ToList() }, default);
                // Новая активная группа не даёт заменить уже выданный автоподбор.
                await db.StudyGroups.Where(g => g.Id == singles[1].Id).ExecuteUpdateAsync(s => s.SetProperty(g => g.IsActive, true));
                hod = await planning.GetForHod(default);
                Check(hod.Choices.Single(c => c.StudyProgramId == single.Id).EligibleGroupIds.SequenceEqual(new[] { singles[0].Id }),
                    "У автоподбора доступно только восстановление исходной группы");
                await Reject<PlanningRuleException>(() => Save(new HodProfileDecisionRequest(single.Id, singles[1].Id, "Обход автоподбора")));
                Check(!await db.GroupParticipationDecisions.AnyAsync(), "Запрещённая замена не сохраняет решение");
                await db.StudyGroups.Where(g => g.Id == singles[1].Id).ExecuteUpdateAsync(s => s.SetProperty(g => g.IsActive, false));
                hod = await planning.GetForHod(default);
                await Reject<DocumentAccessException>(() => Save(new HodProfileDecisionRequest(foreign.Id, foreign.StudyGroups.Single().Id, "Причина")));
                await Reject<ArgumentException>(() => Save(new HodProfileDecisionRequest(profile.Id, groups[2].Id, " ")));
                await Reject<PlanningRuleException>(() => Save(new HodProfileDecisionRequest(profile.Id, pairs[0].Id, "Чужой профиль")));
                await Reject<PlanningRuleException>(() => Save(new HodProfileDecisionRequest(profile.Id, groups[0].Id, "Нельзя повторять")));
                await Reject<PlanningRuleException>(() => Save(new HodProfileDecisionRequest(profile.Id, groups[3].Id, "Архив")));
                hod = await Save(new HodProfileDecisionRequest(profile.Id, groups[2].Id, "Замена рекомендованной группы"),
                    new(pair.Id, pairs[0].Id, null), new(single.Id, null, "Нет возможности провести проверку"),
                    new(archive.Id, archivedGroups[1].Id, "Рекомендованная группа расформирована"));
                Check((await db.GroupParticipationDecisions.CountAsync()) == 4, "Пакет сохранён целиком");
                Check((await db.RecommendationHistory.SingleAsync(r => r.StudyGroupId == groups[1].Id && r.SemesterId == current.Id)).Author == "manual",
                    "Исходная рекомендация ЛПР сохранена");
                Check(!await db.RecommendationHistory.AnyAsync(r => r.StudyGroupId == groups[2].Id && r.SemesterId == current.Id), "Замена HOD не становится рекомендацией ЛПР");
                Check(!await db.Testings.AnyAsync(t => t.GroupId == groups[2].Id && t.SemesterId == current.Id),
                    "Подтверждение участия сохраняет решение, а тестирования создаются при выборе дисциплин");
                Check(!await db.Testings.AnyAsync(t => t.GroupId == singles[0].Id && t.SemesterId == current.Id), "Отказ не создаёт тестирования");
                hod = await planning.GetForHod(default);
                Check(!hod.Plan!.RequiresRecalculation, "Замена не создаёт ложной потребности пересчёта");
                Check(hod.Choices.Single(c => c.StudyProgramId == single.Id).SelectedGroupId is null,
                    "Отмена автоподбора сохраняется при повторном открытии");
                await db.StudyGroups.Where(g => g.Id == singles[1].Id).ExecuteUpdateAsync(s => s.SetProperty(g => g.IsActive, true));
                hod = await planning.GetForHod(default);
                await Reject<PlanningRuleException>(() => Save(new HodProfileDecisionRequest(single.Id, singles[1].Id, "Замена после отмены")));
                await db.StudyGroups.Where(g => g.Id == singles[1].Id).ExecuteUpdateAsync(s => s.SetProperty(g => g.IsActive, false));
                hod = await planning.GetForHod(default);
                var lpr = await planning.Get(null, null, default);
                var lprProfile = lpr!.StudyPrograms.Single(p => p.StudyProgramId == profile.Id);
                Check(lprProfile.ParticipationDecision!.SelectedGroupId == groups[2].Id && lprProfile.ParticipationDecision.RecommendedGroupId == groups[1].Id,
                    "ЛПР видит обе стороны решения");
                Check((await disciplines.GetGroupsAsync(null)).Select(g => g.GroupId).Order().SequenceEqual(new[] { groups[2].Id, pairs[0].Id, archivedGroups[1].Id }.Order()),
                    "Выбор дисциплин использует итог HOD и текущий, а не самый поздний семестр");
                hod = await Save(new HodProfileDecisionRequest(single.Id, singles[0].Id, null));
                Check(hod.Choices.Single(c => c.StudyProgramId == single.Id).SelectedGroupId == singles[0].Id
                    && (await db.RecommendationHistory.SingleAsync(r => r.StudyGroupId == singles[0].Id && r.SemesterId == current.Id)).Author == "automatic",
                    "Снятую автоматическую рекомендацию можно восстановить без смены источника");
                hod = await Save(new HodProfileDecisionRequest(single.Id, null, "Повторная отмена автоподбора"));
                await Reject<PlanningRuleException>(() => planning.Update(new UpdateGroupsTestingRequest {
                    CurrentSemesterId = current.Id, Version = hod.Plan!.Version, Changes = [new(groups[1].Id, current.Id, false)] }, default));
                await planning.AutoFill(default);
                Check(await db.RecommendationHistory.AnyAsync(r => r.StudyGroupId == groups[1].Id && r.SemesterId == current.Id), "Повторный автоподбор не стирает рекомендацию");
                hod = await planning.GetForHod(default);
                var firstDecision = await db.GroupParticipationDecisions.SingleAsync(d => d.StudyProgramId == profile.Id && d.SemesterId == current.Id);
                Check(firstDecision.DecidedByUserId == user.Id && firstDecision.Reason == "Замена рекомендованной группы", "Сохраняются причина и автор");

                // До выбора дисциплин решение можно отменить и восстановить.
                hod = await Save(new HodProfileDecisionRequest(profile.Id, null, "Отмена до назначения дисциплин"));
                Check(!await db.Testings.AnyAsync(t => t.GroupId == groups[2].Id && t.SemesterId == current.Id), "Отмена участия не создаёт тестирования");
                hod = await Save(new HodProfileDecisionRequest(profile.Id, groups[2].Id, "Участие восстановлено"));
                var discipline = new Discipline { Name = "Дисциплина проверки", DepartmentId = department.Id };
                db.Disciplines.Add(discipline); await db.SaveChangesAsync();
                db.StudyProgramDisciplines.Add(new StudyProgramDiscipline { StudyProgramId = profile.Id, DisciplineId = discipline.Id });
                await db.SaveChangesAsync();
                await disciplines.SetDisciplinesAsync(groups[2].Id, [discipline.Id], null);
                var oldVersion = hod.Plan!.Version;
                await Reject<PlanningRuleException>(() => Save(new HodProfileDecisionRequest(profile.Id, groups[1].Id, "Устаревшая версия")));
                hod = await planning.GetForHod(default);
                Check(!hod.Choices.Single(c => c.StudyProgramId == profile.Id).Editable, "После выбора дисциплин замена заблокирована");
                await Reject<PlanningRuleException>(() => Save(new HodProfileDecisionRequest(profile.Id, groups[1].Id, "Уже есть дисциплины")));
                Check(hod.Plan!.Version > oldVersion, "Изменение дисциплин обновляет общую версию плана");
                Check(await db.Testings.AnyAsync(t => t.GroupId == groups[2].Id && t.DisciplineId == discipline.Id), "Заблокированная замена не теряет дисциплины");
                Console.WriteLine("PASS HOD: миграции, область кафедры, замена/отказ, архив, циклы, дисциплины и версии (MySQL)");
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
    private sealed class Actor(int id, int departmentId) : ICurrentUserProvider
    {
        public int? UserId => id;
        public int? DepartmentId => departmentId;
        public int? FacultyId => null;
    }
    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);
    }
}
