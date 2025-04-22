using KnowledgeApp.DataAccess.Repositories;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.Application.Services
{
    public class DepartmentService
    {
        private readonly DepartmentRepository _departmentRepository;

        public DepartmentService(DepartmentRepository departmentRepository)
        {
            _departmentRepository = departmentRepository;
        }

        public async Task<List<DepartmentModel>> GetAll()
        {
            List<DepartmentModel> departments = await _departmentRepository.GetAllDepartments();
            return departments;
        }

        public async Task<DepartmentModel> GetDepartmentById(int departmentId)
        {
            DepartmentModel department = await _departmentRepository.GetDepartmentById(departmentId);
            return department;
        }

        public async Task<DepartmentModel> CreateDepartment(DepartmentModel departmentModel)
        {
            DepartmentModel createdDepartmentId = await _departmentRepository.CreateDepartment(departmentModel);

            return createdDepartmentId;
        }

        public async Task<DepartmentModel> UpdateDepartment(DepartmentModel departmentModel)
        {
            DepartmentModel updatedDepartmentModel = await _departmentRepository.UpdateDepatment(departmentModel);
            return updatedDepartmentModel;
        }

        public async Task<bool> DeleteDepartment(int departmentId)
        {
            bool result = await _departmentRepository.DeleteDepartment(departmentId);
            return result;
        }
    }
}
