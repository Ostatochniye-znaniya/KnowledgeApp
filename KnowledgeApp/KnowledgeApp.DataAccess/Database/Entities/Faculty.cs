using System;
using System.Collections.Generic;

namespace KnowledgeApp.DataAccess.Entities;

public partial class Faculty
{
    public int Id { get; set; }

    public string FacultyName { get; set; } = null!;

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
