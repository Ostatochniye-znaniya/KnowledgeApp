namespace KnowledgeApp.API.Contracts
{
    public class GetRecommendationsRequest
    {
        public int? SemesterId { get; set; }
        public int? StudyGroupId { get; set; }
    }
}
