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

    public async Task<int> Create(OrderModel order, DateTime createdAt, int? createdByUserId)
    {
        var entity = new Order
        {
            Number = order.Number,
            Name = order.Name,
            OrderDate = order.OrderDate,
            SemesterId = order.SemesterId,
            EducationControlDepartmentHead = order.EducationControlDepartmentHead,
            EducationalMethodicalCenterHead = order.EducationalMethodicalCenterHead,
            InspectionSummaryDeadline = order.InspectionSummaryDeadline,
            SurveySummaryDeadline = order.SurveySummaryDeadline,
            PaperReportDeadline = order.PaperReportDeadline,
            ElectronicReportDeadline = order.ElectronicReportDeadline,
            TestingStartDate = order.TestingStartDate,
            TestingEndDate = order.TestingEndDate,
            SurveyStartDate = order.SurveyStartDate,
            SurveyEndDate = order.SurveyEndDate,
            PreparationStartDate = order.PreparationStartDate,
            PreparationEndDate = order.PreparationEndDate,
            ResultsProcessingStartDate = order.ResultsProcessingStartDate,
            FileKey = order.FileKey,
            FileSize = order.FileSize,
            CreatedAt = createdAt,
            CreatedByUserId = createdByUserId
        };

        _context.Orders.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<List<OrderModel>> GetAll()
    {
        return await _context.Orders.AsNoTracking()
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.Id)
            .Select(o => new OrderModel
            {
                Id = o.Id,
                Number = o.Number,
                Name = o.Name,
                OrderDate = o.OrderDate,
                SemesterId = o.SemesterId,
                SemesterYear = o.Semester != null ? o.Semester.SemesterYear : 0,
                SemesterPart = o.Semester != null ? o.Semester.SemesterPart : 0
            })
            .ToListAsync();
    }

    public Task<string?> GetFileKey(int id) =>
        _context.Orders.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => o.FileKey)
            .SingleOrDefaultAsync();
}
