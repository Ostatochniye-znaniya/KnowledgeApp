using System.Data;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Repositories;

public class OrderRepository
{
    private readonly KnowledgeTestDbContext _context;

    public OrderRepository(KnowledgeTestDbContext context)
    {
        _context = context;
    }

    public async Task<SemesterModel?> GetSemester(int semesterId)
    {
        var semester = await _context.Semesters.AsNoTracking().SingleOrDefaultAsync(s => s.Id == semesterId);
        return semester == null
            ? null
            : new SemesterModel(semester.Id, semester.SemesterYear, semester.SemesterPart, semester.YearPart);
    }

    public async Task<int> Create(OrderModel order, DateTime createdAt, int? createdByUserId, string creationToken)
    {
        var entity = new Order
        {
            Number = order.Number,
            CreationToken = creationToken,
            Name = order.Name,
            Status = order.Status,
            OrderDate = order.OrderDate,
            SemesterId = order.SemesterId,
            EducationControlDepartmentHead = order.EducationControlDepartmentHead,
            EducationalMethodicalCenterHead = order.EducationalMethodicalCenterHead,
            InspectionSummaryDeadline = order.InspectionSummaryDeadline,
            SurveySummaryDeadline = order.SurveySummaryDeadline,
            PaperReportDeadline = order.PaperReportDeadline,
            RequiresPaperReports = order.RequiresPaperReports,
            ElectronicReportDeadline = order.ElectronicReportDeadline,
            TestingStartDate = order.TestingStartDate,
            TestingEndDate = order.TestingEndDate,
            SurveyStartDate = order.SurveyStartDate,
            SurveyEndDate = order.SurveyEndDate,
            PreparationStartDate = order.PreparationStartDate,
            PreparationEndDate = order.PreparationEndDate,
            ResultsProcessingStartDate = order.ResultsProcessingStartDate,
            TestingStageStartDate = order.TestingStageStartDate, TestingStageEndDate = order.TestingStageEndDate,
            OnlineSeminarDate = order.OnlineSeminarDate,
            FileKey = order.FileKey,
            FileSize = order.FileSize,
            CreatedAt = createdAt,
            CreatedByUserId = createdByUserId
        };

        _context.Orders.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<List<OrderModel>> GetAll(string? status = null)
    {
        return await _context.Orders.AsNoTracking()
            .Where(o => status == null || o.Status == status)
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.Id)
            .Select(o => new OrderModel
            {
                Id = o.Id,
                Number = o.Number,
                Name = o.Name,
                Status = o.Status,
                OrderDate = o.OrderDate,
                TestingStartDate = o.TestingStartDate,
                TestingEndDate = o.TestingEndDate,
                SemesterId = o.SemesterId,
                SemesterYear = o.Semester != null ? o.Semester.SemesterYear : 0,
                SemesterPart = o.Semester != null ? o.Semester.SemesterPart : 0
            })
            .ToListAsync();
    }

    public Task<OrderModel?> GetDetails(int id, CancellationToken cancellationToken) =>
        _context.Orders.AsNoTracking().Where(o => o.Id == id)
            .Select(o => new OrderModel
            {
                Id = o.Id, Number = o.Number, Name = o.Name, Status = o.Status, OrderDate = o.OrderDate,
                SemesterId = o.SemesterId,
                SemesterYear = o.Semester != null ? o.Semester.SemesterYear : 0,
                SemesterPart = o.Semester != null ? o.Semester.SemesterPart : 0,
                TestingStartDate = o.TestingStartDate, TestingEndDate = o.TestingEndDate,
                EducationControlDepartmentHead = o.EducationControlDepartmentHead,
                EducationalMethodicalCenterHead = o.EducationalMethodicalCenterHead,
                InspectionSummaryDeadline = o.InspectionSummaryDeadline,
                SurveySummaryDeadline = o.SurveySummaryDeadline,
                PaperReportDeadline = o.PaperReportDeadline, RequiresPaperReports = o.RequiresPaperReports,
                PreparationStartDate = o.PreparationStartDate, PreparationEndDate = o.PreparationEndDate,
                TestingStageStartDate = o.TestingStageStartDate, TestingStageEndDate = o.TestingStageEndDate,
                ResultsProcessingStartDate = o.ResultsProcessingStartDate,
                SurveyStartDate = o.SurveyStartDate, SurveyEndDate = o.SurveyEndDate,
                OnlineSeminarDate = o.OnlineSeminarDate, FileKey = o.FileKey,
                ElectronicReportDeadline = o.ElectronicReportDeadline,
                FileSize = o.FileSize, CreatedAt = o.CreatedAt, CreatedByUserId = o.CreatedByUserId,
                CreatedByName = _context.Users.Where(u => u.Id == o.CreatedByUserId).Select(u => u.Name).FirstOrDefault()
            }).SingleOrDefaultAsync(cancellationToken);

    public Task<string?> GetFileKey(int id) =>
        _context.Orders.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => o.FileKey)
            .SingleOrDefaultAsync();

    public Task<int?> FindCreation(string token) => _context.Orders.AsNoTracking()
        .Where(o => o.CreationToken == token).Select(o => (int?)o.Id).SingleOrDefaultAsync();

    public Task<bool> IsFileReferenced(string key) =>
        _context.Orders.AsNoTracking().AnyAsync(o => o.FileKey == key);

    public Task<T> Write<T>(Func<Task<T>> action, CancellationToken ct) =>
        _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            _context.ChangeTracker.Clear();
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            var result = await action();
            await transaction.CommitAsync(ct);
            return result;
        });

    // Единый порядок блокировок: счётчик, затем приказ. Используется созданием и подписанием.
    public async Task LockNumbers(CancellationToken ct) =>
        _ = await _context.OrderNumberSequences
            .FromSqlRaw("SELECT * FROM order_number_sequence WHERE id = 1 FOR UPDATE").ToListAsync(ct);

    public async Task<string> AllocateNumber(CancellationToken ct)
    {
        var sequence = _context.OrderNumberSequences.Local.Single(s => s.Id == 1);
        var legacyNumbers = await _context.Orders.Where(o => o.Status == "issued" && o.Number != null)
            .Select(o => o.Number).ToListAsync(ct);
        var maximum = Math.Max(sequence.LastNumber, legacyNumbers.Select(OrderNumber.NumericPart).DefaultIfEmpty().Max());
        sequence.LastNumber = checked(maximum + 1);
        return OrderNumber.Format(sequence.LastNumber);
    }

    public async Task<Order?> LockOrder(int id, CancellationToken ct) =>
        (await _context.Orders.FromSqlInterpolated($"SELECT * FROM orders WHERE id = {id} FOR UPDATE")
            .ToListAsync(ct)).SingleOrDefault();

    public Task Save(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
