using KnowledgeApp.DataAccess.Entities;
using KnowledgeApp.DataAccess.Repositories;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.Application.Services
{
    public class StudyProgramService
    {
        private readonly StudyProgramRepository _repository;

        public StudyProgramService(StudyProgramRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<StudyProgram>> GetAllProgramsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<StudyProgram?> GetProgramByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task CreateProgramAsync(StudyProgram studyProgram)
        {
            await _repository.AddAsync(studyProgram);
        }

        public async Task UpdateProgramAsync(StudyProgram studyProgram)
        {
            await _repository.UpdateAsync(studyProgram);
        }

        public async Task DeleteProgramAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task CreateProgramAsync(StudyProgramModel studyProgramModel)
        {
            throw new NotImplementedException();
        }
    }
}