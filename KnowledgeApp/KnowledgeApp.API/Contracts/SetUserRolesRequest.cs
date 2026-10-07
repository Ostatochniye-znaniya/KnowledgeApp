namespace KnowledgeApp.API.Contracts;

public class SetUserRolesRequest
{
    public int UserId { get; set; }
    public List<int> RoleIds { get; set; } = new();
}
