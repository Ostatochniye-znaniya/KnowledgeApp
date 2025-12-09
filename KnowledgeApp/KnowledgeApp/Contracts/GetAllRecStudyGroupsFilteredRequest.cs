namespace KnowledgeApp.API.Contracts
{
    public class GetAllRecStudyGroupsFilteredRequest
    {
        public int? FacultyId { get; set; }
        public int? StudyProgrammId { get; set; }
        public int SemseterId { get; set; }
    }
}
