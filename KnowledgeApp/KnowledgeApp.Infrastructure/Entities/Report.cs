using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Infrastructure.Entities;

public partial class Report
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

    public string? FileName { get; set; }

    public DateTime? UploadedAt { get; set; }

    public DocumentStatus? Status { get; set; }

    public string? ReviewComment { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public int? ReviewedByUserId { get; set; }

    public virtual Discipline? Discipline { get; set; }

    public virtual User? Teacher { get; set; }

    public virtual ICollection<Testing> Testings { get; set; } = new List<Testing>();
}
