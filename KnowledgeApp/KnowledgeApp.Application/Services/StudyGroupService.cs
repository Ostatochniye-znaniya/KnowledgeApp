using KnowledgeApp.DataAccess.Entities;
using KnowledgeApp.DataAccess.Repositories;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.Application.Services
{
    public class StudyGroupService
    {
        private readonly StudyGroupRepository _repository;

        public StudyGroupService(StudyGroupRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<StudyGroup>> GetAllGroupsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<StudyGroup?> GetGroupByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task CreateGroupAsync(StudyGroup studyGroup)
        {
            await _repository.AddAsync(studyGroup);
        }

        public async Task UpdateGroupAsync(StudyGroup studyGroup)
        {
            await _repository.UpdateAsync(studyGroup);
        }

        public async Task DeleteGroupAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task<StudyGroupModel> CreateGroupAsync(StudyGroupModel studyGroupModel)
        {
            StudyGroupModel createdStudyGroupId = await _repository.CreateStudyGroup(studyGroupModel);

            return createdStudyGroupId;
        }
    }
}
