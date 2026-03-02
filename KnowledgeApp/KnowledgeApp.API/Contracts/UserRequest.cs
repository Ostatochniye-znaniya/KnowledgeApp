namespace KnowledgeApp.API.Contracts;

public class UserRequest
{
    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public int StatusId { get; set; }

    public int FacultyId { get; set; }
}