namespace KnowledgeApp.API.Contracts
{
    public class ReportResponse
    {
        public int Id { get; set; }
        public int? DisciplineId { get; set; }

        public int? TeacherId { get; set; }

        public string? FilePath { get; set; }

        public bool? IsCorrect { get; set; }

        public string? ResultOfAttestation { get; set; }

        public bool? DoneInPaperForm { get; set; }

        public bool? DoneInElectronicForm { get; set; }

        public bool? AllDone { get; set; }
    }
}