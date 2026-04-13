namespace KnowledgeApp.Domain.Entities;

public class StudentModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Year { get; set; }
    public int GroupId { get; set; }
    public string Status { get; set; }

    public StudentModel(int id, string name, int year, int groupId, string status)
    {
        Id = id;
        Name = name;
        Year = year;
        GroupId = groupId;
        Status = status;
    }
    public StudentModel(string name, int year, int groupId, string status)
    {
        Name = name;
        Year = year;
        GroupId = groupId;
        Status = status;
    }
}
