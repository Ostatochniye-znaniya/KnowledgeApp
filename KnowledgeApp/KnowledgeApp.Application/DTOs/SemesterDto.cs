namespace KnowledgeApp.Application.DTOs
{
    public class SemesterDto
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int Part { get; set; }
        public string Name { get; set; } = string.Empty;

        public string GetPeriod()
        {
            if (Part == 1)
            {
                return $"в апреле-мае {Year} года";
            }
            else
            {
                return $"в ноябре-декабре {Year} года";
            }
        }
    }
}