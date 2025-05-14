namespace KnowledgeApp.API.Contracts
{
    public class FacultyStatisticResponse
    {
        public int Id { get; set; }
        public string FacultyName { get; set; } = null!;
        public int? GroupCountMust { get; set; }
        public int? GroupCountFact { get; set; }
        public int? DisciplineCountMust { get; set; }
        public int? DisciplineCountFact { get; set; }
        public int? EReportDoneCount { get; set; }
        public int? EReportRevCount { get; set; }
        public int? PapReportDoneCount { get; set; }
        public int? PapReportRevCount { get; set; }
        public bool? AllDone { get; set; }
    }
}
