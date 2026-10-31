using KnowledgeApp.Domain.Planning;
using KnowledgeApp.Application.DTOs.GroupsTesting;
using KnowledgeApp.Application.DTOs.Orders;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using KnowledgeApp.Application.Services;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using KnowledgeApp.Infrastructure.Data.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Options;
using System.Text.Json;

var semesters = new[] { new PlanningSemester(8, 2026, 2), new(15, 2027, 1), new(31, 2027, 2) };
var tests = new List<(string Name, Action Run)>();
void Test(string name, Action run) => tests.Add((name, run));
void Assert(bool condition, string? message = null)
{
    if (!condition) throw new Exception(message ?? "Проверка не выполнена");
}
ProfilePlan Plan(int count, Participation[]? history = null, PlanAssignment[]? choices = null) =>
    GroupTestingPlanner.Calculate(Enumerable.Range(1, count).ToArray(), history ?? [], semesters, choices ?? []);
Participation H(int group, int semesterOffset) => new(group, semesters[0].Position + semesterOffset);
PlanAssignment M(int group, int semesterIndex) => new(group, semesters[semesterIndex].Id, "manual");
void Reject(Action action)
{
    try { action(); } catch (PlanningRuleException) { return; }
    throw new Exception("Недопустимый выбор не отклонён");
}

Test("Одна группа автоматически назначается на текущий и два будущих семестра", () =>
{
    var p = Plan(1);
    Assert(p.Assignments.Count == 3 && p.Assignments.All(a => a.GroupId == 1 && a.Author == "automatic"));
    Assert(p.Cells.Values.All(c => !c.Editable));
});
Test("Одна группа может тестироваться в соседних семестрах", () => Assert(Plan(1, [H(1, -1)]).Assignments.Count == 3));
Test("Две группы без истории требуют первого ручного выбора", () =>
{
    var p = Plan(2);
    Assert(p.Assignments.Count == 0 && p.Cells[(1, 8)].Editable && p.Cells[(2, 8)].Editable);
    Assert(!p.Cells[(1, 15)].Editable);
});
Test("После первого ручного выбора две группы чередуются", () =>
{
    Assert(Plan(2, choices: [M(1, 0)]).Assignments.Select(a => a.GroupId).SequenceEqual([1, 2, 1]));
});
Test("Две группы продолжают чередование по истории", () =>
    Assert(Plan(2, [H(1, -2), H(2, -1)]).Assignments.Select(a => a.GroupId).SequenceEqual([1, 2, 1])));
Test("Чередование двух групп не теряется при перерыве", () =>
    Assert(Plan(2, [H(2, -3)]).Assignments[0].GroupId == 1));
Test("Три группы с пустой историей не получают автоподбор", () => Assert(Plan(3).Assignments.Count == 0));
Test("Для трёх групп одного прохождения недостаточно для автоподбора", () =>
{
    var p = Plan(3, [H(1, -1)]);
    Assert(p.Assignments.Count == 0 && p.Cells[(2, 8)].Editable && p.Cells[(3, 8)].Editable);
    Assert(!p.Cells[(1, 8)].Editable);
});
Test("При n-1 пройденных групп назначается оставшаяся", () =>
{
    var p = Plan(4, [H(1, -3), H(2, -2), H(3, -1)]);
    Assert(p.Assignments.Count == 1 && p.Assignments[0].GroupId == 4);
});
Test("После двух ручных выборов последняя группа назначается автоматически", () =>
{
    var p = Plan(3, choices: [M(1, 0), M(2, 1)]);
    Assert(p.Assignments.Select(a => a.GroupId).SequenceEqual([1, 2, 3]));
    Assert(p.Assignments[2].Author == "automatic");
});
Test("Новый цикл из трёх групп начинается вручную", () =>
{
    var p = Plan(3, [H(1, -3), H(2, -2), H(3, -1)]);
    Assert(p.Assignments.Count == 0 && p.Cells[(1, 8)].Editable && p.Cells[(2, 8)].Editable);
    Assert(!p.Cells[(3, 8)].Editable);
});
Test("Соседний повтор на границе циклов отклоняется", () =>
    Reject(() => Plan(3, [H(1, -3), H(2, -2), H(3, -1)], [M(3, 0)])));
Test("После разрыва календаря новый цикл не запрещает последнюю группу", () =>
    Assert(Plan(3, [H(1, -5), H(2, -4), H(3, -3)]).Cells[(3, 8)].Editable));
Test("Группа не повторяется внутри незавершённого цикла", () =>
    Reject(() => Plan(3, [H(1, -1)], [M(1, 0)])));
Test("Два ручных назначения одного профиля на семестр отклоняются", () =>
    Reject(() => Plan(3, choices: [M(1, 0), M(2, 0)])));
Test("Ручной выбор будущего семестра не опережает неизвестный предыдущий", () =>
    Reject(() => Plan(3, choices: [M(1, 1)])));
Test("Повторяющиеся записи дисциплин не ускоряют цикл", () =>
    Assert(Plan(3, [H(1, -1), H(1, -1), H(1, -1)]).Assignments.Count == 0));
Test("История старше восьми столбцов всё равно учитывается", () =>
    Assert(Plan(3, [H(1, -12), H(2, -8)]).Assignments[0].GroupId == 3));
Test("Автоматические назначения пересчитываются после замены ручных", () =>
{
    var p = Plan(3, choices: [M(2, 0), M(3, 1), new(3, 31, "automatic")]);
    Assert(p.Assignments.Select(a => a.GroupId).SequenceEqual([2, 3, 1]));
});
Test("Удаление раннего ручного выбора убирает зависимое автоматическое", () =>
    Assert(Plan(3, choices: [new(3, 31, "automatic")]).Assignments.Count == 0));
Test("Фактические тестирования не переназначаются", () =>
{
    var p = Plan(3, choices: [new(2, 8, "manual", true)]);
    Assert(p.Assignments[0].GroupId == 2 && !p.Cells[(2, 8)].Editable);
});
Test("Циклы двух профилей независимы", () =>
{
    var a = Plan(3, [H(1, -2), H(2, -1)]);
    var b = Plan(3);
    Assert(a.Assignments.Count == 1 && b.Assignments.Count == 0);
});

Test("Одного Completed среди нескольких дисциплин достаточно", () =>
    Assert(GroupTestingPlanner.HasCompletedTesting(["Scheduled", "Completed", null])));
Test("Confirmed и произвольный результат не заменяют Completed", () =>
    Assert(!GroupTestingPlanner.HasCompletedTesting(["confirmed", "Scheduled", null])));
Test("Пустой набор тестирований не считается прохождением", () =>
    Assert(!GroupTestingPlanner.HasCompletedTesting([])));
var calendar = new[] {
    new PlanningSemester(101, 2022, 2), new(103, 2023, 1), new(108, 2023, 2), new(113, 2024, 1), new(120, 2024, 2),
    new PlanningSemester(2, 2025, 1), new(4, 2025, 2), new(5, 2026, 1),
    new(8, 2026, 2), new(15, 2027, 1), new(31, 2027, 2), new(100, 2028, 1)
};
Test("Календарь сохраняет реальные несмежные ID семестров", () =>
    Assert(PlanningCalendar.GetWindow(calendar, new DateOnly(2026, 10, 19)).Select(s => s.Id).SequenceEqual([101, 103, 108, 113, 120, 2, 4, 5, 8, 15, 31])));
Test("Январь относится к осеннему семестру предыдущего года", () =>
    Assert(PlanningCalendar.GetWindow(calendar, new DateOnly(2027, 1, 15))[8].Id == 8));
Test("Первое февраля начинает весенний семестр", () =>
    Assert(PlanningCalendar.GetWindow(calendar, new DateOnly(2027, 2, 1))[8].Id == 15));
Test("Неполный справочник не подменяется самым ранним семестром", () =>
    Reject(() => PlanningCalendar.GetWindow(calendar.Where(s => s.Id != 8), new DateOnly(2026, 10, 19))));
Test("Дубликат календарного семестра требует исправления справочника", () =>
    Reject(() => PlanningCalendar.GetWindow(calendar.Append(new(999, 2026, 2)), new DateOnly(2026, 10, 19))));

Test("Миграция создаёт только две новые таблицы и не меняет старые", () =>
{
    var migration = new AddGroupsTestingPlanning();
    Assert(migration.UpOperations.OfType<CreateTableOperation>().Select(o => o.Name)
        .Order().SequenceEqual(new[] { "planned_testings", "testing_planning_state" }.Order()));
    Assert(!migration.UpOperations.Any(o => o is AlterColumnOperation or DropColumnOperation or DropTableOperation));
    Assert(migration.DownOperations.Count == 2 && migration.DownOperations.All(o => o is DropTableOperation));
});
Test("Исправление FK не меняет столбцы или данные старых таблиц", () =>
{
    var migration = new FixTestingSemesterForeignKey();
    Assert(migration.UpOperations.Count == 3);
    Assert(migration.UpOperations.OfType<DropForeignKeyOperation>().Single().Name == "testing_ibfk_4");
    var keys = migration.UpOperations.OfType<AddForeignKeyOperation>().ToArray();
    Assert(keys.Any(k => k.Columns.SequenceEqual(["semester_id"]) && k.PrincipalTable == "semesters"));
    Assert(keys.Any(k => k.Columns.SequenceEqual(["report_id"]) && k.PrincipalTable == "reports"));
});
Test("Модель имеет уникальную пару группа-семестр и Restrict на внешних ключах", () =>
{
    using var db = new KnowledgeTestDbContextFactory().CreateDbContext([]);
    var entity = db.Model.FindEntityType(typeof(RecommendationHistory))!;
    Assert(entity.GetProperties().Count() == 6 && entity.FindProperty("Author") is not null);
    Assert(entity.GetIndexes().Any(i => i.IsUnique && i.Properties.Select(p => p.Name)
        .SequenceEqual(new[] { "StudyGroupId", "SemesterId" })));
    Assert(entity.GetForeignKeys().Where(k => k.Properties.Single().Name != "UserId").All(k => k.DeleteBehavior == DeleteBehavior.Restrict));
    Assert(entity.GetForeignKeys().Single(k => k.Properties.Single().Name == "UserId").DeleteBehavior == DeleteBehavior.SetNull);
    Assert(db.Database.GetDbConnection().State == System.Data.ConnectionState.Closed);
});
Test("Старая модель Testing и RecommendationHistory сохранена", () =>
{
    using var db = new KnowledgeTestDbContextFactory().CreateDbContext([]);
    var testing = db.Model.FindEntityType(typeof(Testing))!;
    Assert(testing.FindProperty("GroupId") is not null && testing.FindProperty("SemesterId") is not null);
    Assert(db.Model.FindEntityType(typeof(RecommendationHistory)) is not null);
});
Test("Testing.SemesterId ссылается на Semester, а ReportId отдельно на Report", () =>
{
    using var db = new KnowledgeTestDbContextFactory().CreateDbContext([]);
    var testing = db.Model.FindEntityType(typeof(Testing))!;
    var semester = testing.GetForeignKeys().Single(k => k.Properties.Single().Name == "SemesterId");
    var report = testing.GetForeignKeys().Single(k => k.Properties.Single().Name == "ReportId");
    Assert(semester.PrincipalEntityType.ClrType == typeof(Semester));
    Assert(report.PrincipalEntityType.ClrType == typeof(Report));
    Assert(db.Database.GetDbConnection().State == System.Data.ConnectionState.Closed);
});
Test("Служебная строка имеет версию конкуренции и начальное состояние без плана", () =>
{
    using var db = new KnowledgeTestDbContextFactory().CreateDbContext([]);
    var entity = db.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model
        .FindEntityType(typeof(TestingPlanningState))!;
    Assert(entity.FindProperty("Version")!.IsConcurrencyToken);
    var seed = entity.GetSeedData().Single();
    Assert((int)seed["Id"]! == 1 && (long)seed["Version"]! == 0);
});
Test("Запрос блокировки состояния транслируется без подключения к БД", () =>
{
    using var db = new KnowledgeTestDbContextFactory().CreateDbContext([]);
    var sql = db.TestingPlanningStates.FromSqlRaw("SELECT * FROM testing_planning_state WHERE id = 1 FOR UPDATE").ToQueryString();
    Assert(sql.Contains("FOR UPDATE") && sql.Contains("testing_planning_state"));
    Assert(db.Database.GetDbConnection().State == System.Data.ConnectionState.Closed);
});
Test("PATCH отклоняет пустой пакет и отсутствие версии до доступа к БД", () =>
{
    using var db = new KnowledgeTestDbContextFactory().CreateDbContext([]);
    var service = new GroupsTestingService(db, Options.Create(new GroupsTestingOptions()), TimeProvider.System);
    foreach (var request in new UpdateGroupsTestingRequest?[] { null,
        new() { CurrentSemesterId = 8, Changes = [new(1, 8, true)] },
        new() { CurrentSemesterId = 8, Version = 1, Changes = [] },
        new() { CurrentSemesterId = 8, Version = 1, Changes = [new(1, 8, true), new(1, 8, false)] } })
    {
        var rejected = false;
        try { _ = service.Update(request, default); } catch (ArgumentException) { rejected = true; }
        Assert(rejected);
    }
});
Test("PATCH не трактует отсутствующее planned как снятие назначения", () =>
{
    var rejected = false;
    try { JsonSerializer.Deserialize<GroupsTestingChange>("{\"groupId\":1,\"semesterId\":8}", new JsonSerializerOptions(JsonSerializerDefaults.Web)); }
    catch (JsonException) { rejected = true; }
    Assert(rejected);
});

Test("Статусы приказа отделяют issued от draft", () =>
{
    Assert(OrderStatuses.IsValid("draft") && OrderStatuses.IsValid("issued"));
    Assert(!OrderStatuses.IsValid("published") && !OrderStatuses.IsValid(""));
    Assert(new Order().Status == "issued");
});
Test("Модель и миграция статуса сохраняют старые приказы как issued", () =>
{
    using var db = new KnowledgeTestDbContextFactory().CreateDbContext([]);
    var status = db.Model.FindEntityType(typeof(Order))!.FindProperty("Status")!;
    Assert(!status.IsNullable && (string?)status.GetDefaultValue() == "issued");
    var migration = new AddOrderStatus();
    Assert(migration.UpOperations.Count == 2);
    var column = migration.UpOperations.OfType<AddColumnOperation>().Single();
    Assert(column.Table == "orders" && column.Name == "status" && (string?)column.DefaultValue == "issued");
    Assert(migration.UpOperations.OfType<AddCheckConstraintOperation>().Single().Table == "orders");
});
Test("Snapshot после слияния сохраняет все сущности, поля и внешние ключи модели", () =>
{
    using var db = new KnowledgeTestDbContextFactory().CreateDbContext([]);
    var current = db.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model;
    var snapshot = db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsAssembly>().ModelSnapshot!.Model;
    Assert(current.GetEntityTypes().Select(e => e.Name).ToHashSet()
        .SetEquals(snapshot.GetEntityTypes().Select(e => e.Name)), "Набор сущностей snapshot отличается от модели");
    foreach (var entity in current.GetEntityTypes())
    {
        var saved = snapshot.FindEntityType(entity.Name)!;
        Assert(entity.GetProperties().Select(p => p.Name).ToHashSet()
            .SetEquals(saved.GetProperties().Select(p => p.Name)), "Поля snapshot: " + entity.Name);
        foreach (var property in entity.GetProperties())
        {
            var savedProperty = saved.FindProperty(property.Name)!;
            Assert(property.GetColumnName() == savedProperty.GetColumnName()
                && property.IsNullable == savedProperty.IsNullable
                && property.IsConcurrencyToken == savedProperty.IsConcurrencyToken,
                "Сопоставление snapshot: " + entity.Name + "." + property.Name);
        }
        string Key(Microsoft.EntityFrameworkCore.Metadata.IForeignKey key) =>
            string.Join(",", key.Properties.Select(p => p.Name)) + "->" + key.PrincipalEntityType.Name
            + ":" + key.DeleteBehavior;
        Assert(entity.GetForeignKeys().Select(Key).ToHashSet()
            .SetEquals(saved.GetForeignKeys().Select(Key)), "Внешние ключи snapshot: " + entity.Name);
    }
    Assert(db.Database.GetDbConnection().State == System.Data.ConnectionState.Closed);
});
Test("Неизвестный статус отклоняется до обращения к БД и PDF-хранилищу", () =>
{
    using var db = new KnowledgeTestDbContextFactory().CreateDbContext([]);
    var service = new OrderService(new OrderRepository(db), new OrderPdfGenerator(),
        new OrderTestStorage(), new OrderTestUser(), NullLogger<OrderService>.Instance);
    foreach (var call in new Func<Task>[] {
        () => service.Create(new CreateOrderDto { Status = "unknown" }, default),
        () => service.GetEssentials("unknown"),
        () => service.GetEssentials(" ") })
    {
        var rejected = false;
        try { call().GetAwaiter().GetResult(); } catch (DocumentValidationException) { rejected = true; }
        Assert(rejected);
    }
    Assert(db.Database.GetDbConnection().State == System.Data.ConnectionState.Closed);
});


OrderModel ValidOrder() => new()
{
    TestingStartDate = new(2026, 10, 20), TestingEndDate = new(2027, 1, 30),
    PreparationStartDate = new(2026, 10, 20), PreparationEndDate = new(2026, 10, 30),
    TestingStageStartDate = new(2026, 11, 1), TestingStageEndDate = new(2026, 12, 20),
    ResultsProcessingStartDate = new(2026, 12, 21), InspectionSummaryDeadline = new(2027, 1, 30),
    SurveyStartDate = new(2026, 11, 1), SurveyEndDate = new(2026, 11, 5),
    SurveySummaryDeadline = new(2026, 11, 6), ElectronicReportDeadline = new(2027, 1, 29),
    OnlineSeminarDate = new(2026, 10, 22), RequiresPaperReports = false, PaperReportDeadline = null
};
void RejectDates(OrderModel order)
{
    try { OrderDates.Validate(order); } catch (DocumentValidationException) { return; }
    throw new Exception("Некорректные даты приняты");
}
Test("Общий период охватывает три последовательных этапа, в том числе через Новый год", () =>
    OrderDates.Validate(ValidOrder()));
Test("Границы общего периода включительны", () =>
{
    var order = ValidOrder(); order.OnlineSeminarDate = order.TestingEndDate; OrderDates.Validate(order);
});
Test("Онлайн-семинар обязателен и входит в общий период", () =>
{
    var order = ValidOrder(); order.OnlineSeminarDate = null; RejectDates(order);
    order.OnlineSeminarDate = new(2026, 10, 19); RejectDates(order);
    order.OnlineSeminarDate = new(2027, 1, 31); RejectDates(order);
});
Test("Все сроки отчётов ограничены общим периодом", () =>
{
    var order = ValidOrder(); order.ElectronicReportDeadline = new(2027, 1, 31); RejectDates(order);
    order = ValidOrder(); order.SurveySummaryDeadline = new(2027, 1, 31); RejectDates(order);
    order = ValidOrder(); order.InspectionSummaryDeadline = new(2027, 1, 31); RejectDates(order);
});
Test("Бумажный срок обязателен только при включённом флаге", () =>
{
    var order = ValidOrder(); OrderDates.Validate(order);
    order.RequiresPaperReports = true; RejectDates(order);
    order.PaperReportDeadline = new(2027, 1, 31); RejectDates(order);
    order.PaperReportDeadline = order.TestingEndDate; OrderDates.Validate(order);
});
Test("Второй этап не совпадает по смыслу с общими границами", () =>
{
    var order = ValidOrder(); order.TestingStageStartDate = null; RejectDates(order);
    order = ValidOrder(); order.TestingStageStartDate = new(2026, 10, 29); RejectDates(order);
    order = ValidOrder(); order.ResultsProcessingStartDate = new(2026, 12, 19); RejectDates(order);
});
Test("Пять прежних дат по-прежнему обязательны для нового документа", () =>
{
    foreach (var clear in new Action<OrderModel>[] {
        o => o.SurveyStartDate = null, o => o.SurveyEndDate = null,
        o => o.PreparationStartDate = null, o => o.PreparationEndDate = null,
        o => o.ResultsProcessingStartDate = null })
    {
        var order = ValidOrder(); clear(order); RejectDates(order);
    }
});
Test("Счётчик распознаёт старые номера и новый суффикс ОД", () =>
{
    Assert(OrderNumber.NumericPart("233") == 233 && OrderNumber.NumericPart("234-ОД") == 234);
    Assert(OrderNumber.NumericPart(null) == 0 && OrderNumber.NumericPart("TEST-900") == 0);
    Assert(OrderNumber.Format(234) == "234-ОД");
});
Test("Черновик допускает NULL номер, выданные номера защищены уникальным индексом", () =>
{
    using var db = new KnowledgeTestDbContextFactory().CreateDbContext([]);
    var order = db.Model.FindEntityType(typeof(Order))!;
    Assert(order.FindProperty("Number")!.IsNullable);
    Assert(order.GetIndexes().Any(i => i.IsUnique && i.Properties.Single().Name == "Number"));
    Assert(order.GetIndexes().Any(i => i.IsUnique && i.Properties.Single().Name == "CreationToken"));
    Assert(order.FindProperty("PaperReportDeadline")!.IsNullable);
    Assert(new Order { Status = "draft" }.Number is null);
});
Test("Testing ссылается на рекомендацию с запретом удаления назначения", () =>
{
    using var db = new KnowledgeTestDbContextFactory().CreateDbContext([]);
    var testing = db.Model.FindEntityType(typeof(Testing))!;
    var reference = testing.GetForeignKeys().Single(k => k.Properties.Single().Name == "RecommendationHistoryId");
    Assert(reference.PrincipalEntityType.ClrType == typeof(RecommendationHistory));
    Assert(reference.DeleteBehavior == DeleteBehavior.Restrict);
    Assert(db.Model.FindEntityType("KnowledgeApp.Infrastructure.Entities.PlannedTesting") is null);
});
Test("Миграция проверяет дубликаты заранее, переносит планы и только затем удаляет старую таблицу", () =>
{
    var migration = new ConsolidateLprPlanningAndOrders();
    var operations = migration.UpOperations;
    Assert(operations[0] is SqlOperation guard && guard.Sql.Contains("lpr_preflight"));
    Assert(operations.Last() is DropTableOperation drop && drop.Name == "planned_testings");
    Assert(operations.OfType<SqlOperation>().Any(s => s.Sql.Contains("INSERT INTO recommendation_history")
        && s.Sql.Contains("FROM planned_testings") && s.Sql.Contains("UPDATE testing")));
    Assert(operations.OfType<SqlOperation>().Any(s => s.Sql.Contains("lpr_transfer_guard")));
});
Test("Совместимость старой схемы восстанавливается раньше миграции nullable group_id", () =>
{
    var migration = new RepairLegacyOrderColumns();
    Assert(migration.UpOperations.OfType<SqlOperation>().Count() == 8);
    Assert(migration.UpOperations.OfType<SqlOperation>().All(s => s.Sql.Contains("information_schema")));
});
Test("Полный SQL миграций строится без БД и содержит совместимый порядок переходов", () =>
{
    using var db = new KnowledgeTestDbContextFactory().CreateDbContext([]);
    var sql = db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().GenerateScript();
    Assert(sql.IndexOf("20261018140000_RepairLegacyOrderColumns", StringComparison.Ordinal)
        < sql.IndexOf("20261018171428_MakeOrderGroupOptional", StringComparison.Ordinal));
    Assert(sql.Contains("lpr_preflight") && sql.Contains("lpr_transfer_guard"));
    Assert(sql.LastIndexOf("INSERT INTO recommendation_history", StringComparison.Ordinal)
        < sql.LastIndexOf("DROP TABLE `planned_testings`", StringComparison.Ordinal));
    Assert(sql.Contains("online_seminar_date") && sql.Contains("order_number_sequence"));
    Assert(db.Database.GetDbConnection().State == System.Data.ConnectionState.Closed);
});

string PdfText(OrderModel order)
{
    var bytes = new OrderPdfGenerator().Generate(order);
    using var document = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfReader(new MemoryStream(bytes)));
    return string.Join("\n", Enumerable.Range(1, document.GetNumberOfPages())
        .Select(p => iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(document.GetPage(p))))
        .Replace('\u00a0', ' ');
}
OrderModel PdfOrder(bool paper)
{
    var order = ValidOrder();
    order.Status = paper ? "issued" : "draft"; order.Number = paper ? "234-ОД" : null;
    order.Name = OrderPdfGenerator.OrderName; order.OrderDate = new(2026, 10, 19);
    order.SemesterYear = 2026; order.SemesterPart = 2;
    order.EducationControlDepartmentHead = "Иванов Иван Иванович";
    order.EducationalMethodicalCenterHead = "Петров Пётр Петрович";
    order.RequiresPaperReports = paper; order.PaperReportDeadline = paper ? new DateOnly(2027, 1, 28) : null;
    return order;
}
Test("PDF черновика имеет отметку, отдельные даты второго этапа и онлайн-семинар, без требования бумаги", () =>
{
    var text = PdfText(PdfOrder(false));
    Assert(text.Contains("ЧЕРНОВИК") && text.Contains("без номера"));
    Assert(text.Contains("20.10.2026") && text.Contains("30.01.2027"));
    Assert(text.Contains("01.11.2026") && text.Contains("20.12.2026") && text.Contains("22.10.2026"));
    Assert(!text.Contains("в бумажном"));
});
Test("PDF выпущенного приказа содержит официальный номер и бумажный срок", () =>
{
    var text = PdfText(PdfOrder(true));
    Assert(text.Contains("234-ОД") && text.Contains("28.01.2027") && text.Contains("в бумажном"));
    Assert(!text.Contains("ЧЕРНОВИК"));
});

var failed = 0;
foreach (var (name, run) in tests)
{
    try { run(); Console.WriteLine("PASS " + name); }
    catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e.Message); }
}
Console.WriteLine($"{tests.Count - failed}/{tests.Count} passed");
if (failed == 0 && args.Contains("--orders-contract"))
{
    var order = new OrderDetailsDto
    {
        Id = 41, Number = null, Name = "Приказ", Status = "draft",
        OrderDate = new DateOnly(2026, 10, 19),
        Semester = new OrderSemesterDto { Id = 305, SemesterYear = 2027, SemesterPart = 1 },
        TestingStartDate = new DateOnly(2026, 10, 20), TestingEndDate = new DateOnly(2027, 1, 30),
        EducationControlDepartmentHead = "Тестов Иван Иванович",
        EducationalMethodicalCenterHead = "Тестова Анна Ивановна",
        InspectionSummaryDeadline = new DateOnly(2026, 12, 20), SurveySummaryDeadline = new DateOnly(2026, 12, 21),
        PaperReportDeadline = null, RequiresPaperReports = false, ElectronicReportDeadline = new DateOnly(2026, 12, 23),
        PreparationStartDate = new(2026, 10, 20), PreparationEndDate = new(2026, 10, 30),
        TestingStageStartDate = new(2026, 11, 1), TestingStageEndDate = new(2026, 12, 18),
        ResultsProcessingStartDate = new(2026, 12, 19), OnlineSeminarDate = new(2026, 10, 22),
        SurveyStartDate = new(2026, 11, 1), SurveyEndDate = new(2026, 11, 5),
        FileSize = 2048, CreatedAt = new DateTime(2026, 10, 19, 10, 0, 0, DateTimeKind.Utc)
    };
    Console.WriteLine("ORDERS_CONTRACT_JSON:" + JsonSerializer.Serialize(order, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
}
if (failed == 0 && args.Contains("--contract"))
{
    var p = Plan(3, choices: [M(1, 0), M(2, 1)]);
    var response = new GroupsTestingResponse(8, 3, false,
        calendar.Take(11).Select((s, index) => new TestingSemesterDto(s.Id, s.Year, s.Part,
            index < 8 ? "past" : index == 8 ? "current" : "future")).ToArray(),
        [new TestingStudyProgramDto(7, "Тестовый профиль", "09.03.03", 1, "Факультет",
            Enumerable.Range(1, 3).Select(id => new TestingGroupDto(id, "26-" + id, null,
                calendar.Take(8).Select(s => new TestingHistoryCellDto(s.Id, null)).ToArray(),
                semesters.Select(s => {
                    var c = p.Cells[(id, s.Id)];
                    return new PlannedTestingCellDto(s.Id, c.Planned, c.Author, c.Editable, c.RestrictionReason);
                }).ToArray())).ToArray())]);
    Console.WriteLine("CONTRACT_JSON:" + JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
}
return failed == 0 ? 0 : 1;
