namespace KnowledgeApp.API.Contracts
{
    public class StudyGroupResponse
    {
        public int Id { get; set; }
        public string GroupNumber { get; set; } = null!;
        public int? StudyProgramId { get; set; }
        public List<int> StudentIds { get; set; } = new List<int>();
        public List<int> TestingIds { get; set; } = new List<int>();
    }
}
