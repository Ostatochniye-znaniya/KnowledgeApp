using KnowledgeApp.DataAccess.Repositories;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.Application.Services
{
    public class FacultyStatisticService
    {
        private readonly FacultyStatisticRepository _facultystatisticRepository;

        public FacultyStatisticService(FacultyStatisticRepository facultystatisticRepository)
        {
            _facultystatisticRepository = facultystatisticRepository;
        }

        public async Task<List<FacultyStatisticModel>> GetAll()
        {
            List<FacultyStatisticModel> facultystatistics = await _facultystatisticRepository.GetAllFacultyStatistics();
            return facultystatistics;
        }

        public async Task<FacultyStatisticModel> GetFacultyStatisticById(int facultystatisticId)
        {
            FacultyStatisticModel facultystatistic = await _facultystatisticRepository.GetFacultyStatisticById(facultystatisticId);
            return facultystatistic;
        }

        public async Task<FacultyStatisticModel> CreateFacultyStatistic(FacultyStatisticModel facultystatisticModel)
        {
            FacultyStatisticModel createdFacultyStatisticId = await _facultystatisticRepository.CreateFacultyStatistic(facultystatisticModel);

            return createdFacultyStatisticId;
        }

        public async Task<FacultyStatisticModel> UpdateFacultyStatistic(FacultyStatisticModel facultystatisticModel)
        {
            FacultyStatisticModel updatedFacultyStatisticModel = await _facultystatisticRepository.UpdateFacultyStatistic(facultystatisticModel);
            return updatedFacultyStatisticModel;
        }

        public async Task<bool> DeleteFacultyStatistic(int facultystatisticId)
        {
            bool result = await _facultystatisticRepository.DeleteFacultyStatistic(facultystatisticId);
            return result;
        }
    }
}