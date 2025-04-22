namespace KnowledgeApp.Core.Models;

public class RoleModel
{
    public int Id { get; set; }

    public string RoleName { get; set; } = null!;

    public RoleModel(int id, string rolename)
    {
        Id = id;
        RoleName = rolename;
    }
    public RoleModel(string rolename)
    {
        RoleName = rolename;
    }
}