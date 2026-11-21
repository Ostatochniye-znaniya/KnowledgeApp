using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Infrastructure.Entities;
using KnowledgeApp.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowledgeApp.IntegrationTests.Workflow;

/// <summary>
/// Сквозной сценарий выбора преподавателей: кафедра назначает преподавателя и период,
/// преподаватель выбирает пару, место проведения и согласует или отклоняет время.
/// Текущего пользователя сервисы берут из ICurrentUserProvider (не из MOCK-авторизации),
/// поэтому в тестах он подменяется и переключается между заведующим и преподавателем.
/// </summary>
public class TeacherSelectionWorkflowTests : IntegrationTestBase
{
    private const int OwnDisciplineId = 1;
    private const int ExternalDisciplineId = 2;
    private const int OwnTestingId = 1;
    private const int ExternalTestingId = 2;
    private const int SecondOwnTestingId = 3;
    private const int ExternalTeacherId = 10;

    private static readonly DateOnly PeriodStart = new(2026, 11, 23);
    private static readonly DateOnly PeriodEnd = new(2026, 12, 12);
    private static readonly DateOnly TestingDate = new(2026, 11, 24);

    private readonly SwitchableCurrentUser _currentUser = new();
    private readonly HttpClient _client;

    public TeacherSelectionWorkflowTests(KnowledgeAppFactory factory) : base(factory)
    {
        _client = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICurrentUserProvider>();
                services.AddSingleton<ICurrentUserProvider>(_currentUser);
            }))
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri($"http://localhost{KnowledgeAppFactory.PathBase}/")
            });
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await using var db = Factory.CreateDbContext();

        // Обход дефекта схемы: в модели Testing.SemesterId объявлен внешним ключом на reports
        // (testing_ibfk_4 в KnowledgeTestDbContext и InitialCreate), а не на semesters,
        // поэтому без отчёта с Id семестра тестирование не сохранить.
        db.Reports.Add(new Report { Id = TestSeed.CurrentSemesterId });

        db.Users.Add(new User
        {
            Id = ExternalTeacherId, Name = "Внешний Преподаватель Иванович", Email = "ext@mospolytech.ru",
            Password = string.Empty, StatusId = 1, FacultyId = TestSeed.BasicFacultyId,
            DepartmentId = TestSeed.FbkDepartmentId
        });
        db.Disciplines.AddRange(
            // дисциплина кафедры группы — назначает сама кафедра группы
            new Discipline { Id = OwnDisciplineId, Name = "Базы данных", DepartmentId = TestSeed.IiitDepartmentId },
            // дисциплина кафедры другого факультета — нужна заявка
            new Discipline { Id = ExternalDisciplineId, Name = "Философия", DepartmentId = TestSeed.FbkDepartmentId });
        db.DisciplineTeachers.AddRange(
            new DisciplineTeacher { DisciplineId = OwnDisciplineId, ResponsibleTeacherId = TestSeed.TeacherUserId },
            new DisciplineTeacher { DisciplineId = ExternalDisciplineId, ResponsibleTeacherId = ExternalTeacherId });
        db.Testings.AddRange(
            new Testing { Id = OwnTestingId, GroupId = TestSeed.WebGroupId, DisciplineId = OwnDisciplineId, SemesterId = TestSeed.CurrentSemesterId },
            new Testing { Id = ExternalTestingId, GroupId = TestSeed.WebGroupId, DisciplineId = ExternalDisciplineId, SemesterId = TestSeed.CurrentSemesterId },
            new Testing { Id = SecondOwnTestingId, GroupId = TestSeed.WebGroupId, DisciplineId = OwnDisciplineId, SemesterId = TestSeed.CurrentSemesterId });
        await db.SaveChangesAsync();
    }

    public override Task DisposeAsync()
    {
        _client.Dispose();
        return base.DisposeAsync();
    }

    [Fact]
    public async Task OwnDiscipline_AssignChooseTimeAndApprove()
    {
        // 1. Заведующий кафедрой группы назначает преподавателя и период
        ActAsHeadOf(TestSeed.IiitDepartmentId);
        var assignments = await Ok(await AssignTeachers(OwnTestingId, TestSeed.TeacherUserId));
        var teacher = Assert.Single(assignments.GetProperty("teachers").EnumerateArray());
        Assert.Equal(TestSeed.TeacherUserId, teacher.GetProperty("teacherId").GetInt32());
        Assert.Equal("pending", teacher.GetProperty("status").GetString());
        // При первом назначении период меняется с пустого на заданный, поэтому сейчас приходит
        // уведомление testing_period_updated, а не testing_assigned
        Assert.Equal(1, await CountNotifications(n => n.RecipientUserId == TestSeed.TeacherUserId &&
                                                      n.TestingId == OwnTestingId));

        // 2. Преподаватель видит назначение
        ActAsTeacher(TestSeed.TeacherUserId);
        var testings = await Ok(await _client.GetAsync("TeacherWorkflow/GetTestings"));
        Assert.Contains(testings.EnumerateArray(), t => t.GetProperty("testingId").GetInt32() == OwnTestingId);

        // 3. Согласовать без выбранного времени нельзя
        Assert.Equal(HttpStatusCode.BadRequest, (await Approve(OwnTestingId)).StatusCode);

        // 4. Выбор пары и места проведения
        var chosen = await Ok(await ChooseTime(OwnTestingId, TestingDate, pairNumber: 2));
        Assert.Equal("10:40:00", chosen.GetProperty("scheduledTime").GetString());
        Assert.Equal(2, chosen.GetProperty("scheduledPair").GetInt32());
        await Ok(await _client.PutAsJsonAsync($"TeacherWorkflow/UpdateTestingDelivery/{OwnTestingId}/delivery",
            new { isInPerson = true, room = "Пр2301" }));

        // 5. Согласование: статус approved и уведомление кафедре
        var approved = await Ok(await Approve(OwnTestingId));
        Assert.Equal("approved", approved.GetProperty("approvalStatus").GetString());
        Assert.Equal(1, await CountNotifications(n =>
            n.RecipientDepartmentId == TestSeed.IiitDepartmentId && n.Type == "testing_approved"));

        await using var db = Factory.CreateDbContext();
        var testing = await db.Testings.AsNoTracking().SingleAsync(t => t.Id == OwnTestingId);
        Assert.Equal(TestingDate.ToDateTime(TimeOnly.MinValue), testing.ScheduledDate);
        Assert.Equal("Пр2301", testing.Room);

        // 6. Повторный ответ — конфликт
        Assert.Equal(HttpStatusCode.Conflict, (await Approve(OwnTestingId)).StatusCode);
    }

    [Fact]
    public async Task Teacher_RejectsTimeWithReason()
    {
        ActAsHeadOf(TestSeed.IiitDepartmentId);
        await Ok(await AssignTeachers(OwnTestingId, TestSeed.TeacherUserId));
        ActAsTeacher(TestSeed.TeacherUserId);
        await Ok(await ChooseTime(OwnTestingId, TestingDate, pairNumber: 3));

        using var withoutReason = await Reject(OwnTestingId, comment: "  ");
        Assert.Equal(HttpStatusCode.BadRequest, withoutReason.StatusCode);

        var rejected = await Ok(await Reject(OwnTestingId, "В это время у меня пары"));
        Assert.Equal("rejected", rejected.GetProperty("approvalStatus").GetString());
        Assert.Equal("В это время у меня пары", rejected.GetProperty("comment").GetString());
        Assert.Equal(1, await CountNotifications(n =>
            n.RecipientDepartmentId == TestSeed.IiitDepartmentId && n.Type == "testing_rejected"));
    }

    [Fact]
    public async Task ChooseTime_OutsidePeriodOrUnknownPair_IsRejected()
    {
        ActAsHeadOf(TestSeed.IiitDepartmentId);
        await Ok(await AssignTeachers(OwnTestingId, TestSeed.TeacherUserId));
        ActAsTeacher(TestSeed.TeacherUserId);

        Assert.Equal(HttpStatusCode.BadRequest, (await ChooseTime(OwnTestingId, PeriodEnd.AddDays(1), 1)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ChooseTime(OwnTestingId, TestingDate, 8)).StatusCode);
    }

    [Fact]
    public async Task AssignTeacher_NotLinkedToDiscipline_IsRejected()
    {
        ActAsHeadOf(TestSeed.IiitDepartmentId);

        using var response = await AssignTeachers(OwnTestingId, ExternalTeacherId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var db = Factory.CreateDbContext();
        Assert.False(await db.TestingTeacherApprovals.AnyAsync());
    }

    [Fact]
    public async Task ExternalDiscipline_RequiresRequestThenDisciplineDepartmentAssigns()
    {
        // До заявки кафедра группы назначать не может, кафедра дисциплины тоже
        ActAsHeadOf(TestSeed.IiitDepartmentId);
        Assert.Equal(HttpStatusCode.Forbidden, (await AssignTeachers(ExternalTestingId, ExternalTeacherId)).StatusCode);
        ActAsHeadOf(TestSeed.FbkDepartmentId);
        Assert.Equal(HttpStatusCode.Forbidden, (await AssignTeachers(ExternalTestingId, ExternalTeacherId)).StatusCode);

        // Заявку отправляет кафедра группы, кафедра дисциплины получает уведомление
        ActAsHeadOf(TestSeed.IiitDepartmentId);
        await Ok(await _client.PostAsync($"Testing/{ExternalTestingId}/external-request", null));
        Assert.Equal(1, await CountNotifications(n =>
            n.RecipientDepartmentId == TestSeed.FbkDepartmentId && n.Type == "testing_external_request"));

        // Повторная заявка ничего не дублирует
        await Ok(await _client.PostAsync($"Testing/{ExternalTestingId}/external-request", null));
        Assert.Equal(1, await CountNotifications(n => n.Type == "testing_external_request"));

        // Теперь назначает кафедра дисциплины, а кафедра группы — уже нет
        Assert.Equal(HttpStatusCode.Forbidden, (await AssignTeachers(ExternalTestingId, ExternalTeacherId)).StatusCode);
        ActAsHeadOf(TestSeed.FbkDepartmentId);
        var assignments = await Ok(await AssignTeachers(ExternalTestingId, ExternalTeacherId));
        Assert.Equal(ExternalTeacherId,
            Assert.Single(assignments.GetProperty("teachers").EnumerateArray()).GetProperty("teacherId").GetInt32());
    }

    [Fact]
    public async Task ExternalRequest_ForOwnDiscipline_IsRejected()
    {
        ActAsHeadOf(TestSeed.IiitDepartmentId);

        using var response = await _client.PostAsync($"Testing/{OwnTestingId}/external-request", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChooseTime_GroupAlreadyBusy_RollsBackWholeTransaction()
    {
        // Две проверки одной группы, первая уже стоит на 2-й паре
        ActAsHeadOf(TestSeed.IiitDepartmentId);
        await Ok(await AssignTeachers(OwnTestingId, TestSeed.TeacherUserId));
        await Ok(await AssignTeachers(SecondOwnTestingId, TestSeed.TeacherUserId));
        ActAsTeacher(TestSeed.TeacherUserId);
        await Ok(await ChooseTime(OwnTestingId, TestingDate, pairNumber: 2));
        var notificationsBefore = await CountNotifications(_ => true);

        using var conflict = await ChooseTime(SecondOwnTestingId, TestingDate, pairNumber: 2);

        // Проверка слота падает внутри транзакции: ни время, ни уведомления не сохраняются
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Contains("У группы уже есть проверка", await conflict.Content.ReadAsStringAsync());
        await using var db = Factory.CreateDbContext();
        var second = await db.Testings.AsNoTracking().SingleAsync(t => t.Id == SecondOwnTestingId);
        Assert.Null(second.ScheduledDate);
        Assert.Null(second.ScheduledTime);
        Assert.Null(second.ScheduledPair);
        Assert.Equal(notificationsBefore, await CountNotifications(_ => true));

        // Соседняя пара свободна
        await Ok(await ChooseTime(SecondOwnTestingId, TestingDate, pairNumber: 3));
    }

    private void ActAsHeadOf(int departmentId)
    {
        _currentUser.UserId = TestSeed.HodUserId;
        _currentUser.DepartmentId = departmentId;
    }

    private void ActAsTeacher(int teacherId)
    {
        _currentUser.UserId = teacherId;
        _currentUser.DepartmentId = null;
    }

    private Task<HttpResponseMessage> AssignTeachers(int testingId, params int[] teacherIds) =>
        _client.PutAsJsonAsync($"Testing/{testingId}/teacher-assignments",
            new { periodStart = PeriodStart, periodEnd = PeriodEnd, teacherIds });

    private Task<HttpResponseMessage> ChooseTime(int testingId, DateOnly date, int pairNumber) =>
        _client.PutAsJsonAsync($"TeacherWorkflow/ChooseTestingTime/{testingId}/choose-time",
            new { date, pairNumber });

    private Task<HttpResponseMessage> Approve(int testingId) =>
        _client.PostAsync($"TeacherWorkflow/ApproveTesting/{testingId}/approve", null);

    private Task<HttpResponseMessage> Reject(int testingId, string comment) =>
        _client.PostAsJsonAsync($"TeacherWorkflow/RejectTesting/{testingId}/reject", new { comment });

    private async Task<int> CountNotifications(System.Linq.Expressions.Expression<Func<WorkflowNotification, bool>> filter)
    {
        await using var db = Factory.CreateDbContext();
        return await db.WorkflowNotifications.CountAsync(filter);
    }

    private static async Task<JsonElement> Ok(HttpResponseMessage response)
    {
        using (response)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{(int)response.StatusCode}: {body}");
            return body.Length == 0 ? default : JsonDocument.Parse(body).RootElement.Clone();
        }
    }

    private sealed class SwitchableCurrentUser : ICurrentUserProvider
    {
        public int? UserId { get; set; }

        public int? FacultyId { get; set; } = TestSeed.ItFacultyId;

        public int? DepartmentId { get; set; }
    }
}
