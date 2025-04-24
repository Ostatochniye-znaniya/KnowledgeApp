using KnowledgeApp.DataAccess.Context;
using KnowledgeApp.DataAccess.Entities;
using KnowledgeApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using System.Runtime.CompilerServices;

namespace KnowledgeApp.DataAccess.Repositories
{
    public class SemesterRepository
    {
        private readonly KnowledgeTestDbContext _context;

        public SemesterRepository(KnowledgeTestDbContext context)
        {
            _context = context;
        }

        public async Task<SemesterModel> CreateSemester(SemesterModel semesterModel)
        {
            var semesterEntity = new Semester
            {
                SemesterName = semesterModel.SemesterName
            };

            await _context.Semesters.AddAsync(semesterEntity);
            await _context.SaveChangesAsync();

            SemesterModel createdSemester = new SemesterModel(semesterEntity.Id, semesterEntity.SemesterName);
            return createdSemester;
        }

        public async Task<List<SemesterModel>> GetAllSemesters()
        {
            //достаем данные из бд
            var semesterEntities = await _context.Semesters
                .AsNoTracking()
                .ToListAsync();
            
            // преобразуем entities в models
            var semesters = semesterEntities
                .Select(semesterEntity =>
                {
                    var semesterModel = new SemesterModel(
                        semesterEntity.Id,
                        semesterEntity.SemesterName);
                    
                    return semesterModel;
                })
                .ToList();
            
            return semesters;
        }

        public async Task<SemesterModel> GetSemesterById(int semesterId)
        {
            var semesterEntity = await _context.Semesters.SingleOrDefaultAsync(s => s.Id == semesterId);
            if (semesterEntity == null) throw new Exception("Semester с таким id не существует");
            SemesterModel semester = new SemesterModel(semesterEntity.Id, semesterEntity.SemesterName);
            return semester;
        }

        public async Task<SemesterModel> UpdateSemester(SemesterModel semesterModel)
        {
            var semesterEntity = await _context.Semesters.SingleOrDefaultAsync(s => s.Id == semesterModel.Id);
            if (semesterEntity == null) throw new Exception("Semester с таким id не существует");

            semesterEntity.SemesterName = semesterModel.SemesterName;
            _context.SaveChanges();
            var semester = new SemesterModel(semesterEntity.Id, semesterEntity.SemesterName);
            return semester;
        }

        public async Task<bool> DeleteSemester(int semesterId)
        {
            var semesterEntity = await _context.Semesters.SingleOrDefaultAsync(s => s.Id == semesterId);
            if (semesterEntity == null) throw new Exception("Semester с таким id не существует");
            _context.Remove(semesterEntity);
            _context.SaveChanges();

            var semester = await _context.Semesters.SingleOrDefaultAsync(s => s.Id == semesterId);
            if (semester == null) return true;
            else return false;
        }
    }
}