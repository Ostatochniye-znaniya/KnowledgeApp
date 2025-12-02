using KnowledgeApp.DataAccess.Database.Entities;
using System;
using System.Collections.Generic;

namespace KnowledgeApp.DataAccess.Entities;

public partial class Semester
{
    public int Id { get; set; }
    public int SemesterYear { get; set; }
    public int SemesterPart { get; set; }
    public virtual ICollection<TestingOrder> TestingOrders { get; set; } = new List<TestingOrder>();
}
