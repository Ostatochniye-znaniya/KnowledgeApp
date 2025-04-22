using System;
using System.Collections.Generic;

namespace KnowledgeApp.DataAccess.Entities;

public partial class StudyProgram
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? DepartmentId { get; set; }

    public string? CypherOfTheDirection { get; set; }

    public virtual Department? Department { get; set; }

    public virtual ICollection<StudyGroup> StudyGroups { get; set; } = new List<StudyGroup>();
}
