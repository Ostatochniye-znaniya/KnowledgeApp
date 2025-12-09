namespace KnowledgeApp.API.Contracts
{
    public class TestingForRecommendationResponse
    {
        public int Id { get; set; }
        public int? SemesterYear { get; set; }
        public int? SemesterPart { get; set; }
        public bool? IsChosen { get; set; }
    }
}
