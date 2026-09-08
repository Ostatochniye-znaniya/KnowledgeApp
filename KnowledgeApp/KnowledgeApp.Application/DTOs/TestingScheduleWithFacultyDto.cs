namespace KnowledgeApp.Application.DTOs
{
    public class TestingScheduleWithFacultyDto : TestingScheduleDto
    {
        public string FacultyName { get; set; } = string.Empty;
        public int FacultyId { get; set; }
        public int SemesterId { get; set; }
    }
}