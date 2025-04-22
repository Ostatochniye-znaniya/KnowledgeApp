namespace KnowledgeApp.API.Contracts
{
    public class DepartmentResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int? FacultyId { get; set; }
    }
}
