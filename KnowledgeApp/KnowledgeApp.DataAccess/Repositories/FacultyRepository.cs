using KnowledgeApp.DataAccess.Context;
using KnowledgeApp.DataAccess.Entities;
using KnowledgeApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;

namespace KnowledgeApp.DataAccess.Repositories
{
    public class FacultyRepository
    {
        private readonly KnowledgeTestDbContext _context;

        public FacultyRepository(KnowledgeTestDbContext context)
        {
            _context = context;
        }

        public async Task<FacultyModel> CreateFaculty(FacultyModel facultyModel)
        {
            var facultyEntity = new Faculty
            {
                FacultyName = facultyModel.FacultyName,
            };

            await _context.Faculties.AddAsync(facultyEntity);
            await _context.SaveChangesAsync();

            FacultyModel createdFaculty = new FacultyModel(facultyEntity.Id, facultyEntity.FacultyName);
            return createdFaculty;
        }

        public async Task<List<FacultyModel>> GetAllFaculties()
        {
            //достаем данные из бд
            var facultyEntities = await _context.Faculties
                .AsNoTracking()
                .ToListAsync();

            // преобразуем entities в models
            var faculties = facultyEntities
                .Select(facultyEntity =>
                {
                    var facultyModel = new FacultyModel(
                        facultyEntity.Id,
                        facultyEntity.FacultyName);

                    return facultyModel;
                })
                .ToList();

            return faculties;
        }

        public async Task<FacultyModel> GetFacultyById(int facultyId)
        {
            var facultyEntity = await _context.Faculties.SingleOrDefaultAsync(d => d.Id == facultyId);
            if (facultyEntity == null) throw new Exception("Faculty с таким id не существует");
            FacultyModel faculty = new FacultyModel(facultyEntity.Id, facultyEntity.FacultyName);
            return faculty;
        }

        public async Task<FacultyModel> UpdateFaculty(FacultyModel facultyModel)
        {
            var facultyEntity = await _context.Faculties.SingleOrDefaultAsync(d => d.Id == facultyModel.Id);
            if (facultyEntity == null) throw new Exception("Faculty с таким id не существует");

            facultyEntity.FacultyName = facultyModel.FacultyName;
            _context.SaveChanges();
            var faculty = new FacultyModel(facultyEntity.Id, facultyEntity.FacultyName);
            return faculty;
        }

        public async Task<bool> DeleteFaculty(int facultyId)
        {
            var facultyEntity = await _context.Faculties.SingleOrDefaultAsync(d => d.Id == facultyId);
            if (facultyEntity == null) throw new Exception("Faculty с таким id не существует");
            _context.Remove(facultyEntity);
            _context.SaveChanges();

            var faculty = await _context.Faculties.SingleOrDefaultAsync(d => d.Id == facultyId);
            if (faculty == null) return true;
            else return false;
        }
    }
}
