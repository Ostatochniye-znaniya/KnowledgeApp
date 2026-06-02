namespace KnowledgeApp.Application.DTOs
{
    public class TestingScheduleDto
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string ProgramName { get; set; } = string.Empty;
        public string DisciplineName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
    }
}