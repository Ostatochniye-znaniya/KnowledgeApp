using System;
using System.Collections.Generic;

namespace KnowledgeApp.DataAccess.Entities;

public partial class Testing
{
    public int Id { get; set; }

    public int? GroupId { get; set; }

    public int? DisciplineId { get; set; }

    public DateOnly? ScheduledDate { get; set; }

    public TimeOnly? ScheduledTime { get; set; }

    public string? Status { get; set; }

    public string? ResultOfTesting { get; set; }

    public int? ReportId { get; set; }

    public virtual Discipline? Discipline { get; set; }

    public virtual StudyGroup? Group { get; set; }

    public virtual Report? Report { get; set; }
}
