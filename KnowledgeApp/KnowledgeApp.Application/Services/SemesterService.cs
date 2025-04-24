using KnowledgeApp.DataAccess.Repositories;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.Application.Services
{
    public class SemesterService
    {
        private readonly SemesterRepository _semesterRepository;

        public SemesterService(SemesterRepository semesterRepository)
        {
            _semesterRepository = semesterRepository;
        }

        public async Task<List<SemesterModel>> GetAll()
        {
            List<SemesterModel> semesters = await _semesterRepository.GetAllSemesters();
            return semesters;
        }

        public async Task<SemesterModel> GetSemesterById(int semesterId)
        {
            SemesterModel semester = await _semesterRepository.GetSemesterById(semesterId);
            return semester;
        }

        public async Task<SemesterModel> CreateSemester(SemesterModel semesterModel)
        {
            SemesterModel createdSemesterId = await _semesterRepository.CreateSemester(semesterModel);
            return createdSemesterId;
        }

        public async Task<SemesterModel> UpdateSemester(SemesterModel semesterModel)
        {
            SemesterModel updatedSemesterModel = await _semesterRepository.UpdateSemester(semesterModel);
            return updatedSemesterModel;
        }

        public async Task<bool> DeleteSemester(int semesterId)
        {
            bool result = await _semesterRepository.DeleteSemester(semesterId);
            return result;
        }
    }
}