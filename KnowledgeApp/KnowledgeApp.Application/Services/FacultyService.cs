using KnowledgeApp.DataAccess.Repositories;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.Application.Services
{
    public class FacultyService
    {
        private readonly FacultyRepository _facultyRepository;

        public FacultyService(FacultyRepository facultyRepository)
        {
            _facultyRepository = facultyRepository;
        }

        public async Task<List<FacultyModel>> GetAll()
        {
            List<FacultyModel> faculties = await _facultyRepository.GetAllFaculties();
            return faculties;
        }

        public async Task<FacultyModel> GetFacultyById(int facultyId)
        {
            FacultyModel faculty = await _facultyRepository.GetFacultyById(facultyId);
            return faculty;
        }

        public async Task<FacultyModel> CreateFaculty(FacultyModel facultyModel)
        {
            FacultyModel createdFacultyId = await _facultyRepository.CreateFaculty(facultyModel);

            return createdFacultyId;
        }

        public async Task<FacultyModel> UpdateFaculty(FacultyModel facultyModel)
        {
            FacultyModel updatedFacultyModel = await _facultyRepository.UpdateFaculty(facultyModel);
            return updatedFacultyModel;
        }

        public async Task<bool> DeleteFaculty(int facultyId)
        {
            bool result = await _facultyRepository.DeleteFaculty(facultyId);
            return result;
        }
    }
}