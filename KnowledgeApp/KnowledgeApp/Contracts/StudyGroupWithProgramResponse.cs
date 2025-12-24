namespace KnowledgeApp.API.Contracts
{
    public class StudyGroupWithProgramResponse
    {
        public int Id { get; set; }
        public string GroupNumber { get; set; } = null!;
        public StudyProgramResponse? StudyProgram { get; set; }
        public List<int> StudentIds { get; set; } = new List<int>();
        public List<int> TestingIds { get; set; } = new List<int>();
    }
}
