using System;
using System.Collections.Generic;

namespace KnowledgeApp.DataAccess.Entities;

public partial class Discipline
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? DepartmentId { get; set; }

    public virtual Department? Department { get; set; }

    public virtual ICollection<DisciplineTeacher> DisciplineTeachers { get; set; } = new List<DisciplineTeacher>();

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    public virtual ICollection<Testing> Testings { get; set; } = new List<Testing>();
}
