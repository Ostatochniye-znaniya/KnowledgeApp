using KnowledgeApp.DataAccess.Context;
using KnowledgeApp.DataAccess.Entities;
using KnowledgeApp.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.DataAccess.Repositories
{
    public class TestingRepository
    {
        private readonly KnowledgeTestDbContext _context;

        public TestingRepository(KnowledgeTestDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Testing>> GetAll()
        {
            return await _context.Set<Testing>().ToListAsync();
            // var testingEntities = await _context.Testings
            //     .AsNoTracking()
            //     .ToListAsync();

            // return testingEntities.Select(e => ToModel(e)).ToList();
        }

        public async Task<TestingModel> GetById(int id)
        {
            var entity = await _context.Testings.FindAsync(id);
            if (entity == null) throw new Exception($"Тестирование с ID {id} не найдено");
            return ToModel(entity);
        }

        public async Task<TestingModel> Create(TestingModel model)
        {
            ValidateModel(model);

            var entity = new Testing
            {
                GroupId = model.GroupId,
                DisciplineId = model.DisciplineId,
                ScheduledDate = model.ScheduledDate,
                ScheduledTime = model.ScheduledTime,
                Status = model.Status,
                ResultOfTesting = model.ResultOfTesting,
                ReportId = model.ReportId,
                SemesterId = model.SemesterId
            };

            await _context.Testings.AddAsync(entity);
            await _context.SaveChangesAsync();

            return ToModel(entity);
        }

        public async Task<TestingModel> Update(TestingModel model)
        {
            var entity = await _context.Testings.FindAsync(model.Id);
            if (entity == null) throw new Exception($"Тестирование с ID {model.Id} не найдено");

            ValidateModel(model);

            entity.GroupId = model.GroupId;
            entity.DisciplineId = model.DisciplineId;
            entity.ScheduledDate = model.ScheduledDate;
            entity.ScheduledTime = model.ScheduledTime;
            entity.Status = model.Status;
            entity.ResultOfTesting = model.ResultOfTesting;
            entity.ReportId = model.ReportId;
            entity.SemesterId = model.SemesterId;

            await _context.SaveChangesAsync();

            return ToModel(entity);
        }

        public async Task<bool> Delete(int id)
        {
            var entity = await _context.Testings.FindAsync(id);
            if (entity == null) throw new Exception($"Тестирование с ID {id} не найдено");

            _context.Testings.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<TestingModel>> GetByGroupId(int groupId)
        {
            var entities = await _context.Testings
                .Where(t => t.GroupId == groupId)
                .AsNoTracking()
                .ToListAsync();

            return entities.Select(e => ToModel(e)).ToList();
        }

        private void ValidateModel(TestingModel model)
        {
            if (model.GroupId == 0 || model.DisciplineId == 0 || model.SemesterId == 0)
                throw new Exception("Необходимо указать groupId, disciplineId и semesterId");

            if (!_context.StudyGroups.Any(g => g.Id == model.GroupId))
                throw new Exception($"Учебная группа с ID {model.GroupId} не существует");

            if (!_context.Disciplines.Any(d => d.Id == model.DisciplineId))
                throw new Exception($"Дисциплина с ID {model.DisciplineId} не существует");

            if (!_context.Semesters.Any(s => s.Id == model.SemesterId))
                throw new Exception($"Семестр с ID {model.SemesterId} не существует");

            if (model.ReportId.HasValue && !_context.Reports.Any(r => r.Id == model.ReportId))
                throw new Exception($"Отчет с ID {model.ReportId} не существует");
        }

        private TestingModel ToModel(Testing entity)
        {
            return new TestingModel(
                entity.Id,
                entity.GroupId,
                entity.DisciplineId,
                entity.ScheduledDate,
                entity.ScheduledTime,
                entity.Status,
                entity.ResultOfTesting,
                entity.ReportId,
                entity.SemesterId
            );
        }
    }
}