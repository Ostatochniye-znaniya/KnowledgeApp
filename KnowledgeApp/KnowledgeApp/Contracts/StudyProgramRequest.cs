namespace KnowledgeApp.API.Contracts
{
    public class StudyProgramRequest
    {
        public string Name { get; set; } = null!;
        public int? DepartmentId { get; set; }
        public string? CypherOfTheDirection { get; set; }
    }
}