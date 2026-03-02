namespace KnowledgeApp.API.Contracts
{
    public class EmployeeRightsCreateRequest
    {
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string? StructuralDivision { get; set; }
        public string? JobName { get; set; }
        public DateOnly JobStart { get; set; }
        public DateOnly? JobEnd { get; set; }
        public bool IsActive { get; set; }
        public string? CategoryName { get; set; }
    }
}
