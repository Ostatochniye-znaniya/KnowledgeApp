namespace KnowledgeApp.Core.Models;

public class StatusModel
{
    public int Id { get; set; }

    public string StatusName { get; set; } = null!;

    public StatusModel(int id, string statusname)
    {
        Id = id;
        StatusName = statusname;
    }
    public StatusModel(string statusname)
    {
        StatusName = statusname;
    }
}