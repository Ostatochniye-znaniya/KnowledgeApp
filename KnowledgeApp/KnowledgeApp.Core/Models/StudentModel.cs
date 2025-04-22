namespace KnowledgeApp.Core.Models;

public class StudentModel
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int GroupId { get; set; }
    public StudentModel(int id, int userid, int groupid)
    {
        Id = id;
        UserId = userid;
        GroupId = groupid;
    }
    public StudentModel(int userid, int groupid)
    {
        UserId = userid;
        GroupId = groupid;
    }
}
