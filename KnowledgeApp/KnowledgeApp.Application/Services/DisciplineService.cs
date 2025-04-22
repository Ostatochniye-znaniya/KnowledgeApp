using KnowledgeApp.DataAccess.Repositories;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.Application.Services
{
    public class DisciplineService
    {
        private readonly DisciplineRepository _disciplineRepository;

        public DisciplineService(DisciplineRepository disciplineRepository)
        {
            _disciplineRepository = disciplineRepository;
        }

        public async Task<List<DisciplineModel>> GetAll()
        {
            return await _disciplineRepository.GetAllDisciplines();
        }

        public async Task<DisciplineModel> GetDisciplineById(int disciplineId)
        {
            return await _disciplineRepository.GetDisciplineById(disciplineId);
        }

        public async Task<DisciplineModel> CreateDiscipline(DisciplineModel disciplineModel)
        {
            return await _disciplineRepository.CreateDiscipline(disciplineModel);
        }

        public async Task<DisciplineModel> UpdateDiscipline(DisciplineModel disciplineModel)
        {
            return await _disciplineRepository.UpdateDiscipline(disciplineModel);
        }

        public async Task<bool> DeleteDiscipline(int disciplineId)
        {
            return await _disciplineRepository.DeleteDiscipline(disciplineId);
        }
    }
}
