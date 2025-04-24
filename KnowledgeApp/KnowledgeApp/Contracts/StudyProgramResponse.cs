namespace KnowledgeApp.API.Contracts
{
    public class StudyProgramResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int? DepartmentId { get; set; }
        public string? CypherOfTheDirection { get; set; }
        public List<int> StudyGroupIds { get; set; } = new List<int>();
    }
}
