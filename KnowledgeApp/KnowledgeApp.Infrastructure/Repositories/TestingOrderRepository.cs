using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Repositories;

public class TestingOrderRepository
{
    private readonly KnowledgeTestDbContext _context;

    public TestingOrderRepository(KnowledgeTestDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TestingOrderModel>> GetAllAsync()
    {
        var entities = await _context.TestingOrders
            .AsNoTracking()
            .Include(to => to.Semester)
            .ToListAsync();

        return entities.Select(ToModel).ToList();
    }

    public async Task<TestingOrderModel> GetByIdAsync(int id)
    {
        var entity = await _context.TestingOrders
            .Include(to => to.Semester)
            .FirstOrDefaultAsync(to => to.Id == id);
        
        if (entity == null)
            throw new Exception($"Приказ о тестировании с ID {id} не найден");

        return ToModel(entity);
    }

    public async Task<TestingOrderModel> CreateAsync(TestingOrderModel model)
    {
        await ValidateModelAsync(model);

        var entity = new TestingOrder
        {
            OrderDate = model.OrderDate,
            Number = model.Number,
            EducationControlEmployeeName = model.EducationControlEmployeeName,
            EducationMethodEmployeeName = model.EducationMethodEmployeeName,
            TestingSummaryReportUpTo = model.TestingSummaryReportUpTo,
            QuestionnaireSummaryReportUpTo = model.QuestionnaireSummaryReportUpTo,
            PaperReportUpTo = model.PaperReportUpTo,
            DigitalReportUpTo = model.DigitalReportUpTo,
            SemesterId = model.SemesterId
        };

        await _context.TestingOrders.AddAsync(entity);
        await _context.SaveChangesAsync();

        return ToModel(entity);
    }

    public async Task<TestingOrderModel> UpdateAsync(TestingOrderModel model)
    {
        var entity = await _context.TestingOrders.FindAsync(model.Id);
        if (entity == null)
            throw new Exception($"Приказ о тестировании с ID {model.Id} не найден");

        await ValidateModelAsync(model);

        entity.OrderDate = model.OrderDate;
        entity.Number = model.Number;
        entity.EducationControlEmployeeName = model.EducationControlEmployeeName;
        entity.EducationMethodEmployeeName = model.EducationMethodEmployeeName;
        entity.TestingSummaryReportUpTo = model.TestingSummaryReportUpTo;
        entity.QuestionnaireSummaryReportUpTo = model.QuestionnaireSummaryReportUpTo;
        entity.PaperReportUpTo = model.PaperReportUpTo;
        entity.DigitalReportUpTo = model.DigitalReportUpTo;
        entity.SemesterId = model.SemesterId;

        await _context.SaveChangesAsync();

        return ToModel(entity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _context.TestingOrders.FindAsync(id);
        if (entity == null)
            throw new Exception($"Приказ о тестировании с ID {id} не найден");

        _context.TestingOrders.Remove(entity);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<TestingOrderModel>> GetBySemesterIdAsync(int semesterId)
    {
        var entities = await _context.TestingOrders
            .Where(to => to.SemesterId == semesterId)
            .AsNoTracking()
            .Include(to => to.Semester)
            .ToListAsync();

        return entities.Select(ToModel).ToList();
    }

    public async Task<TestingOrderModel> GetByNumberAsync(int number)
    {
        var entity = await _context.TestingOrders
            .Include(to => to.Semester)
            .FirstOrDefaultAsync(to => to.Number == number);
        
        if (entity == null)
            throw new Exception($"Приказ о тестировании с номером {number} не найден");

        return ToModel(entity);
    }

    private async Task ValidateModelAsync(TestingOrderModel model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model), "Модель приказа о тестировании не может быть null");

        if (model.SemesterId.HasValue)
        {
            var semesterExists = await _context.Semesters.AnyAsync(s => s.Id == model.SemesterId.Value);
            if (!semesterExists)
                throw new Exception($"Семестр с ID {model.SemesterId.Value} не существует");
        }

        if (model.Number.HasValue && model.Number.Value <= 0)
            throw new Exception("Номер приказа должен быть положительным числом");

        if (model.OrderDate.HasValue && model.OrderDate.Value > DateOnly.FromDateTime(DateTime.Now))
            throw new Exception("Дата приказа не может быть в будущем");

        // Validate that report dates are after order date if both are provided
        if (model.OrderDate.HasValue)
        {
            if (model.TestingSummaryReportUpTo.HasValue && model.TestingSummaryReportUpTo.Value < model.OrderDate.Value)
                throw new Exception("Дата отчета по тестированию не может быть раньше даты приказа");

            if (model.QuestionnaireSummaryReportUpTo.HasValue && model.QuestionnaireSummaryReportUpTo.Value < model.OrderDate.Value)
                throw new Exception("Дата отчета по анкетированию не может быть раньше даты приказа");

            if (model.PaperReportUpTo.HasValue && model.PaperReportUpTo.Value < model.OrderDate.Value)
                throw new Exception("Дата бумажного отчета не может быть раньше даты приказа");

            if (model.DigitalReportUpTo.HasValue && model.DigitalReportUpTo.Value < model.OrderDate.Value)
                throw new Exception("Дата цифрового отчета не может быть раньше даты приказа");
        }
    }

    private TestingOrderModel ToModel(TestingOrder entity)
    {
        return new TestingOrderModel(
            entity.Id,
            entity.OrderDate,
            entity.Number,
            entity.EducationControlEmployeeName,
            entity.EducationMethodEmployeeName,
            entity.TestingSummaryReportUpTo,
            entity.QuestionnaireSummaryReportUpTo,
            entity.PaperReportUpTo,
            entity.DigitalReportUpTo,
            entity.SemesterId
        );
    }
}