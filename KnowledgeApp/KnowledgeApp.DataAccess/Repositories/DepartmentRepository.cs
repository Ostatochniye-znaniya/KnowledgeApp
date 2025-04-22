using KnowledgeApp.DataAccess.Context;
using KnowledgeApp.DataAccess.Entities;
using KnowledgeApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;

namespace KnowledgeApp.DataAccess.Repositories
{
    public class DepartmentRepository
    {
        private readonly KnowledgeTestDbContext _context;

        public DepartmentRepository(KnowledgeTestDbContext context)
        {
            _context = context;
        }

        public async Task<DepartmentModel> CreateDepartment(DepartmentModel departmentModel)
        {
            var faculty = await _context.Faculties.SingleOrDefaultAsync(f => f.Id == departmentModel.FacultyId);
            if (faculty == null) throw new Exception("Факультета с таким id не существует");

            var departmentEntity = new Department
            {
                Name = departmentModel.Name,
                FacultyId = departmentModel.FacultyId
            };

            await _context.Departments.AddAsync(departmentEntity);
            await _context.SaveChangesAsync();

            DepartmentModel createdDepartment = new DepartmentModel(departmentEntity.Id, departmentEntity.Name, departmentEntity.FacultyId);
            return createdDepartment;
        }

        public async Task<List<DepartmentModel>> GetAllDepartments()
        {
            //достаем данные из бд
            var departmentEntities = await _context.Departments
                .AsNoTracking()
                .ToListAsync();

            // преобразуем entities в models
            var departments = departmentEntities
                .Select(departmentEntity =>
                {
                    var departmentModel = new DepartmentModel(
                        departmentEntity.Id,
                        departmentEntity.Name,
                        departmentEntity.FacultyId);

                    return departmentModel;
                })
                .ToList();

            return departments;
        }

        public async Task<DepartmentModel> GetDepartmentById(int departmentId)
        {
            var departmentEntity = await _context.Departments.SingleOrDefaultAsync(d => d.Id == departmentId);
            if (departmentEntity == null) throw new Exception("Department с таким id не существует");
            DepartmentModel department = new DepartmentModel(departmentEntity.Id, departmentEntity.Name, departmentEntity.FacultyId);
            return department;
        }

        public async Task<DepartmentModel> UpdateDepatment(DepartmentModel departmentModel)
        {
            var departmentEntity = await _context.Departments.SingleOrDefaultAsync(d => d.Id == departmentModel.Id);
            if (departmentEntity == null) throw new Exception("Department с таким id не существует");

            var facultyEntity = await _context.Faculties.SingleOrDefaultAsync(f => f.Id == departmentModel.FacultyId);
            if (facultyEntity == null) throw new Exception("Факультета с таким id не существует");

            departmentEntity.Name = departmentModel.Name;
            departmentEntity.FacultyId = departmentEntity.FacultyId;
            _context.SaveChanges();
            var department = new DepartmentModel(departmentEntity.Id, departmentEntity.Name, departmentEntity.FacultyId);
            return department;
        }

        public async Task<bool> DeleteDepartment(int departmentId)
        {
            var departmentEntity = await _context.Departments.SingleOrDefaultAsync(d => d.Id == departmentId);
            if (departmentEntity == null) throw new Exception("Department с таким id не существует");
            _context.Remove(departmentEntity);
            _context.SaveChanges();

            var department = await _context.Departments.SingleOrDefaultAsync(d => d.Id == departmentId);
            if (department == null) return true;
            else return false;
        }
    }
}
