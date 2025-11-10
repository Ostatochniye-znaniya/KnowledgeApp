namespace KnowledgeApp.Core.Models;

public class UserModel
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public int StatusId { get; set; }

    public int FacultyId { get; set; }

    public UserModel() { }

    public UserModel(int id, string? name, string? email, string? password, int statusId, int facultyId)
    {
        Id = id;
        Name = name;
        Email = email;
        Password = password;
        StatusId = statusId;
        FacultyId = facultyId;
    }
    public UserModel(string? name, string? email, string? password, int statusId, int facultyId)
    {
        Name = name;
        Email = email;
        Password = password;
        StatusId = statusId;
        FacultyId = facultyId;
    }
}