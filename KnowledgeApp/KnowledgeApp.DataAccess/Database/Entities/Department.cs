using System;
using System.Collections.Generic;

namespace KnowledgeApp.DataAccess.Entities;

public partial class Department
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? FacultyId { get; set; }

    public virtual ICollection<Discipline> Disciplines { get; set; } = new List<Discipline>();

    public virtual Faculty? Faculty { get; set; }

    public virtual ICollection<StudyProgram> StudyPrograms { get; set; } = new List<StudyProgram>();
}
