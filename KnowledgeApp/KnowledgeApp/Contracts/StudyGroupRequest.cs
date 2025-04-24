namespace KnowledgeApp.API.Contracts
{
    public class StudyGroupRequest
    {
        public string GroupNumber { get; set; } = null!;
        public int? StudyProgramId { get; set; }
    }
}
