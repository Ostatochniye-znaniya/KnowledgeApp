using System;
using System.Collections.Generic;

namespace KnowledgeApp.DataAccess.Entities;

public partial class Student
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int GroupId { get; set; }

    public virtual StudyGroup Group { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
