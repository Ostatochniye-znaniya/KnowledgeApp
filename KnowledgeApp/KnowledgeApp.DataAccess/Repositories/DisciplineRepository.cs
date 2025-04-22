using KnowledgeApp.DataAccess.Context;
using KnowledgeApp.DataAccess.Entities;
using KnowledgeApp.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.DataAccess.Repositories
{
    public class DisciplineRepository
    {
        private readonly KnowledgeTestDbContext _context;

        public DisciplineRepository(KnowledgeTestDbContext context)
        {
            _context = context;
        }

        public async Task<DisciplineModel> CreateDiscipline(DisciplineModel disciplineModel)
        {
            // Если указан DepartmentId, проверяем, что соответствующая кафедра существует
            if (disciplineModel.DepartmentId != null)
            {
                var department = await _context.Departments
                    .SingleOrDefaultAsync(d => d.Id == disciplineModel.DepartmentId);
                if (department == null)
                    throw new Exception("Department с таким id не существует");
            }

            var disciplineEntity = new Discipline
            {
                Name = disciplineModel.Name,
                DepartmentId = disciplineModel.DepartmentId
            };

            await _context.Disciplines.AddAsync(disciplineEntity);
            await _context.SaveChangesAsync();

            DisciplineModel createdDiscipline = new DisciplineModel(
                disciplineEntity.Id,
                disciplineEntity.Name,
                disciplineEntity.DepartmentId);

            return createdDiscipline;
        }

        public async Task<List<DisciplineModel>> GetAllDisciplines()
        {
            var disciplineEntities = await _context.Disciplines
                .AsNoTracking()
                .ToListAsync();

            var disciplines = disciplineEntities
                .Select(d => new DisciplineModel(d.Id, d.Name, d.DepartmentId))
                .ToList();

            return disciplines;
        }

        public async Task<DisciplineModel> GetDisciplineById(int disciplineId)
        {
            var disciplineEntity = await _context.Disciplines
                .SingleOrDefaultAsync(d => d.Id == disciplineId);

            if (disciplineEntity == null)
                throw new Exception("Discipline с таким id не существует");

            return new DisciplineModel(
                disciplineEntity.Id,
                disciplineEntity.Name,
                disciplineEntity.DepartmentId);
        }

        public async Task<DisciplineModel> UpdateDiscipline(DisciplineModel disciplineModel)
        {
            var disciplineEntity = await _context.Disciplines
                .SingleOrDefaultAsync(d => d.Id == disciplineModel.Id);

            if (disciplineEntity == null)
                throw new Exception("Discipline с таким id не существует");

            // Если указан новый DepartmentId, проверяем, что он существует
            if (disciplineModel.DepartmentId != null)
            {
                var department = await _context.Departments
                    .SingleOrDefaultAsync(d => d.Id == disciplineModel.DepartmentId);
                if (department == null)
                    throw new Exception("Department с таким id не существует");
            }

            disciplineEntity.Name = disciplineModel.Name;
            disciplineEntity.DepartmentId = disciplineModel.DepartmentId;

            await _context.SaveChangesAsync();

            return new DisciplineModel(
                disciplineEntity.Id,
                disciplineEntity.Name,
                disciplineEntity.DepartmentId);
        }

        public async Task<bool> DeleteDiscipline(int disciplineId)
        {
            var disciplineEntity = await _context.Disciplines
                .SingleOrDefaultAsync(d => d.Id == disciplineId);

            if (disciplineEntity == null)
                throw new Exception("Discipline с таким id не существует");

            _context.Disciplines.Remove(disciplineEntity);
            await _context.SaveChangesAsync();

            var discipline = await _context.Disciplines
                .SingleOrDefaultAsync(d => d.Id == disciplineId);

            return discipline == null;
        }
    }
}
