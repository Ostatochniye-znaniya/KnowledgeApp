using System;
using System.Collections.Generic;

namespace KnowledgeApp.DataAccess.Entities;

public partial class Status
{
    public int Id { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
