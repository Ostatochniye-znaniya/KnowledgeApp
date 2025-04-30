using KnowledgeApp.DataAccess.Repositories;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.Application.Services
{
    public class TestingService
    {
        private readonly TestingRepository _testingRepository;

        public TestingService(TestingRepository testingRepository)
        {
            _testingRepository = testingRepository;
        }

        public async Task<List<TestingModel>> GetAll()
        {
            return await _testingRepository.GetAll();
        }

        public async Task<TestingModel> GetById(int id)
        {
            return await _testingRepository.GetById(id);
        }

        public async Task<TestingModel> Create(TestingModel testingModel)
        {
            return await _testingRepository.Create(testingModel);
        }

        public async Task<TestingModel> Update(TestingModel testingModel)
        {
            return await _testingRepository.Update(testingModel);
        }

        public async Task<bool> Delete(int id)
        {
            return await _testingRepository.Delete(id);
        }

        public async Task<List<TestingModel>> GetByGroupId(int groupId)
        {
            return await _testingRepository.GetByGroupId(groupId);
        }
    }
}