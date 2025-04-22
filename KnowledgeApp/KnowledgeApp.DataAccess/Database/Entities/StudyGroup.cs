using System;
using System.Collections.Generic;

namespace KnowledgeApp.DataAccess.Entities;

public partial class StudyGroup
{
    public int Id { get; set; }

    public string GroupNumber { get; set; } = null!;

    public int? StudyProgramId { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual StudyProgram? StudyProgram { get; set; }

    public virtual ICollection<Testing> Testings { get; set; } = new List<Testing>();
}
