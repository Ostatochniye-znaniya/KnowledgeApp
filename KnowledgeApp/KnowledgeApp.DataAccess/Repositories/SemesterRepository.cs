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
                SemesterYear = semesterModel.SemesterYear,
                SemesterPart = semesterModel.SemesterPart
            };

            await _context.Semesters.AddAsync(semesterEntity);
            await _context.SaveChangesAsync();

            SemesterModel createdSemester = new SemesterModel(semesterEntity.Id, semesterEntity.SemesterYear, semesterEntity.SemesterPart);
            return createdSemester;
        }
        public async Task<SemesterModel> GetCurrentSemesterAsync()
        {
            var now = DateTime.Now;
            int year = now.Month == 1 ? now.Year - 1 : now.Year; 
            int part = (now.Month >= 9 || now.Month <= 1) ? 2 : 1; 

            var entity = await _context.Semesters
                .FirstOrDefaultAsync(s => s.SemesterYear == year && s.SemesterPart == part);

            if (entity == null)
            {
                entity = new Semester { SemesterYear = year, SemesterPart = part };
                _context.Semesters.Add(entity);
                await _context.SaveChangesAsync();
            }

            return new SemesterModel(entity.Id, entity.SemesterYear, entity.SemesterPart);
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
                        semesterEntity.SemesterYear,
                        semesterEntity.SemesterPart);
                    
                    return semesterModel;
                })
                .ToList();
            
            return semesters;
        }
        public async Task<List<SemesterModel>> GetSemestersTimeline(int fromYear,int fromPart,int toYear,int toPart)
        {
            var semesterEntities = await _context.Semesters
                .AsNoTracking()
                .Where(s=> (s.SemesterYear>fromYear && s.SemesterYear<toYear) || (s.SemesterYear==fromYear && s.SemesterPart>=fromPart) || (s.SemesterYear == toYear && s.SemesterPart<=toPart)).ToListAsync();

            var semesters = semesterEntities
                .Select(semesterEntity =>
                {
                    var semesterModel = new SemesterModel(
                        semesterEntity.Id,
                        semesterEntity.SemesterYear,
                        semesterEntity.SemesterPart);

                    return semesterModel;
                })
                .ToList();

            return semesters;
        }

        public async Task<SemesterModel> GetSemesterById(int semesterId)
        {
            var semesterEntity = await _context.Semesters.SingleOrDefaultAsync(s => s.Id == semesterId);
            if (semesterEntity == null) throw new Exception("Semester с таким id не существует");
            SemesterModel semester = new SemesterModel(semesterEntity.Id, semesterEntity.SemesterYear, semesterEntity.SemesterPart);
            return semester;
        }

        public async Task<SemesterModel> UpdateSemester(SemesterModel semesterModel)
        {
            var semesterEntity = await _context.Semesters.SingleOrDefaultAsync(s => s.Id == semesterModel.Id);
            if (semesterEntity == null) throw new Exception("Semester с таким id не существует");

            semesterEntity.SemesterYear = semesterModel.SemesterYear;
            semesterEntity.SemesterPart = semesterModel.SemesterPart;
            _context.SaveChanges();
            var semester = new SemesterModel(semesterEntity.Id, semesterEntity.SemesterYear, semesterEntity.SemesterPart);
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