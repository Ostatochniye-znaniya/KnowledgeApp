using KnowledgeApp.Core.Models;
using KnowledgeApp.DataAccess.Repositories;

namespace KnowledgeApp.Application.Services
{
    public class RecommendationHistoryService
    {
        private readonly RecommendationHistoryRepository _recommendationHistoryRepository;

        public RecommendationHistoryService(RecommendationHistoryRepository recommendationHistoryRepository)
        {
            _recommendationHistoryRepository = recommendationHistoryRepository;
        }

        public async Task<List<RecommendationHistoryModel>> GetAllAsync(
            int? semesterId = null,
            int? studyGroupId = null,
            int? userId = null)
        {
            return await _recommendationHistoryRepository.GetAllAsync(semesterId, studyGroupId, userId);
        }

        public async Task<RecommendationHistoryModel> GetByIdAsync(int recommendationId)
        {
            return await _recommendationHistoryRepository.GetById(recommendationId);
        }

        public async Task AddRecommendationAsync(RecommendationHistoryModel recommendationModel)
        {
            await _recommendationHistoryRepository.AddRecommendationAsync(recommendationModel);
        }

        public async Task UpdateRecommendationAsync(RecommendationHistoryModel recommendationModel)
        {
            await _recommendationHistoryRepository.UpdateRecommendationAsync(recommendationModel);
        }

        public async Task DeleteRecommendationAsync(int recommendationId)
        {
            await _recommendationHistoryRepository.DeleteRecommendationAsync(recommendationId);
        }
    }
}
