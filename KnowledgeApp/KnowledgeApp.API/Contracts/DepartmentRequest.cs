namespace KnowledgeApp.API.Contracts
{
    public class DepartmentRequest
    {
        public string Name { get; set; } = null!;
        public int? FacultyId { get; set; }
    }
}
