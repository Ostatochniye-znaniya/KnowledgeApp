namespace KnowledgeApp.API.Contracts;

public class UserResponse
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public int StatusId { get; set; }

    public int FacultyId { get; set; }
}