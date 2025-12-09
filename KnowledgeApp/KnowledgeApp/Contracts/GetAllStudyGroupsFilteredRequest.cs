namespace KnowledgeApp.API.Contracts
{
    public class GetAllStudyGroupsFilteredRequest
    {
        public int? FacultyId { get; set; }
        public int? StudyProgrammId { get; set; }
    }
}
