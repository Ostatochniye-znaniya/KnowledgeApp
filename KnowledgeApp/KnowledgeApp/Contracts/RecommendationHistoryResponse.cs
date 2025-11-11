namespace KnowledgeApp.API.Contracts
{
    public class RecommendationHistoryResponse
    {
        public int Id { get; set; }
        public DateTime RecommendedAt { get; set; }
        public string? RecommendedBy { get; set; } = string.Empty;
        public int SemesterId { get; set; }
        public int? SemesterYear { get; set; }
        public int? SemesterPart { get; set; }
        public int StudyGroupId { get; set; }
        public string? StudyGroupName { get; set; } = string.Empty;
    }
}
