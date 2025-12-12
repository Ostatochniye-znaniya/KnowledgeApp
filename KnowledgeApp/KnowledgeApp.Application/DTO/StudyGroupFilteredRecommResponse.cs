using KnowledgeApp.DataAccess.Entities;

namespace KnowledgeApp.API.Contracts
{
    public class StudyGroupFilteredRecommResponse
    {
        public int Id { get; set; }
        public string GroupNumber { get; set; } = null!;
        public int? StudyProgramId { get; set; }
        public List<TestingForRecommendationResponse> Testings { get; set; } = new List<TestingForRecommendationResponse>();
        public TestingForRecommendationResponse? TestingNow { get; set; }
        public List<TestingForRecommendationResponse> TestingsPlanned { get; set; } = new List<TestingForRecommendationResponse>();
    }
}
