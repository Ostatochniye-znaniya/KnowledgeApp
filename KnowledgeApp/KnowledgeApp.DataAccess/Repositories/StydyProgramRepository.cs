using KnowledgeApp.Core.Models;
using KnowledgeApp.DataAccess.Context;
using KnowledgeApp.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.DataAccess.Repositories
{
    public class StudyProgramRepository
    {
        private readonly KnowledgeTestDbContext _context;

        public StudyProgramRepository(KnowledgeTestDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StudyProgram>> GetAllAsync()
        {
            return await _context.Set<StudyProgram>().ToListAsync();
        }

        public async Task<StudyProgram?> GetByIdAsync(int id)
        {
            return await _context.Set<StudyProgram>().FindAsync(id);
        }

        public async Task AddAsync(StudyProgram studyProgram)
        {
            await _context.Set<StudyProgram>().AddAsync(studyProgram);
            await _context.SaveChangesAsync();
        }

        public async Task<StudyProgramModel> CreateStudyProgram(StudyProgramModel studyProgramModel)
        {
            var name = await _context.StudyPrograms.SingleOrDefaultAsync(f => f.Name == studyProgramModel.Name);
            var departmentId = await _context.StudyPrograms.SingleOrDefaultAsync(f => f.DepartmentId == studyProgramModel.DepartmentId);
            var cypherOfTheDirection = await _context.StudyPrograms.SingleOrDefaultAsync(f => f.CypherOfTheDirection == studyProgramModel.CypherOfTheDirection);
            if (name != null || departmentId != null || cypherOfTheDirection != null) throw new Exception("Образовательная программа с такими name или departmentId или cypherOfTheDirection уже существует");
            var studyProgramEntity = new StudyProgram
            {
                Id = studyProgramModel.Id,
                Name = studyProgramModel.Name,
                DepartmentId = studyProgramModel.DepartmentId,
                CypherOfTheDirection = studyProgramModel.CypherOfTheDirection
            };

            await _context.StudyPrograms.AddAsync(studyProgramEntity);
            await _context.SaveChangesAsync();

            StudyProgramModel createdStudent = new StudyProgramModel(studyProgramEntity.Id, studyProgramEntity.Name, studyProgramEntity.DepartmentId, studyProgramEntity.CypherOfTheDirection);
            return createdStudent;
        }

        public async Task UpdateAsync(StudyProgram studyProgram)
        {
            _context.Set<StudyProgram>().Update(studyProgram);
            await _context.SaveChangesAsync();
        }
public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int? DepartmentId { get; set; }
        public string? CypherOfTheDirection { get; set; }
        public List<int> StudyGroupIds { get; set; } = new List<int>();
        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _context.Set<StudyProgram>().Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}
