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

        public async Task UpdateAsync(StudyProgram studyProgram)
        {
            _context.Set<StudyProgram>().Update(studyProgram);
            await _context.SaveChangesAsync();
        }

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
