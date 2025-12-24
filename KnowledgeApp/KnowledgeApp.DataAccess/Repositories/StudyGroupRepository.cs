using KnowledgeApp.Core.Models;
using KnowledgeApp.DataAccess.Context;
using KnowledgeApp.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.DataAccess.Repositories
{
    public class StudyGroupRepository
    {
        private readonly KnowledgeTestDbContext _context;

        public StudyGroupRepository(KnowledgeTestDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StudyGroup>> GetAllAsync()
        {
            return await _context.Set<StudyGroup>().Include(s=>s.StudyProgram).ToListAsync();
        }
        public async Task<IEnumerable<StudyGroup>> GetAllFilteredAsync(int? facultyId, int? studyProgrammId)
        {
            var query = _context.StudyGroups.AsQueryable();
            if(facultyId != null)
            {
                query= query.Include(g => g.StudyProgram).ThenInclude(g => g.Department).Where(u => u.StudyProgram.Department.FacultyId == facultyId);
            }
            if(studyProgrammId != null)
            {
                query=query.Where(g=>g.StudyProgramId == studyProgrammId);
            }
            return await query.Include(g => g.RecommendationHistory).ThenInclude(g=>g.Semester).ToListAsync();
        }

        public async Task<IEnumerable<StudyGroup>> GetAllChosenAsync(int semId)
        {
            return await _context.Set<StudyGroup>().Include(s=>s.RecommendationHistory).Where(s=>s.RecommendationHistory.Any(s=>s.SemesterId == semId && s.IsChosenForTesting == true)).ToListAsync();
        }
        public async Task<IEnumerable<StudyGroup>> GetAllRecommendedAsync(int semId, int? facultyId, int? studyProgrammId)
        {
            var query = _context.StudyGroups.AsQueryable();
            if (facultyId != null)
            {
                query = query.Include(g => g.StudyProgram).ThenInclude(g => g.Department).Where(u => u.StudyProgram.Department.FacultyId == facultyId);
            }
            if (studyProgrammId != null)
            {
                query = query.Where(g => g.StudyProgramId == studyProgrammId);
            }
            return await query.Include(g => g.RecommendationHistory).ThenInclude(g => g.Semester).Where(s => s.RecommendationHistory.Any(s => s.SemesterId == semId)).ToListAsync();
        }

        public async Task<StudyGroup?> GetByIdAsync(int id)
        {
            return await _context.Set<StudyGroup>().FindAsync(id);
        }

        public async Task AddAsync(StudyGroup studyGroup)
        {
            await _context.Set<StudyGroup>().AddAsync(studyGroup);
            await _context.SaveChangesAsync();
        }

        public async Task<StudyGroupModel> CreateStudyGroup(StudyGroupModel studyGroupModel)
        {
            var groupNumber = await _context.StudyGroups.SingleOrDefaultAsync(f => f.GroupNumber == studyGroupModel.GroupNumber);
            if (groupNumber != null) throw new Exception("Учебная группа с такими groupNumber уже существует");
            var studyGroupEntity = new StudyGroup
            {
                Id = studyGroupModel.Id,
                GroupNumber = studyGroupModel.GroupNumber,
                StudyProgramId = studyGroupModel.StudyProgramId,
            };

            await _context.StudyGroups.AddAsync(studyGroupEntity);
            await _context.SaveChangesAsync();

            StudyGroupModel createdStudent = new StudyGroupModel(studyGroupEntity.Id, studyGroupEntity.GroupNumber, studyGroupEntity.StudyProgramId);
            return createdStudent;
        }
        public async Task UpdateAsync(StudyGroup studyGroup)
        {
            _context.Set<StudyGroup>().Update(studyGroup);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _context.Set<StudyGroup>().Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}
