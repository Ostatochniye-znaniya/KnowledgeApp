namespace KnowledgeApp.API.Contracts
{
    public class UpdateTestingOrderRequest
    {
        public int Id { get; set; }
        public DateOnly OrderDate { get; set; }
        public int Number { get; set; }
        public string EducationControlEmployeeName { get; set; }
        public string EducationMethodEmployeeName { get; set; }
        public DateOnly TestingSummaryReportUpTo { get; set; }
        public DateOnly QuestionnaireSummaryReportUpTo { get; set; }
        public DateOnly PaperReportUpTo { get; set; }
        public DateOnly DigitalReportUpTo { get; set; }
        public int SemesterId { get; set; }
    }
}
