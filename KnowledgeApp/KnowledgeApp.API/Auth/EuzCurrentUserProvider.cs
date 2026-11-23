using KnowledgeApp.API.Authentication;
using KnowledgeApp.Application.Interfaces;

namespace KnowledgeApp.API.Auth;

public sealed class EuzCurrentUserProvider(IHttpContextAccessor context) : ICurrentUserProvider
{
    public int? UserId => GetId("user_id");
    public int? FacultyId => GetId("faculty_id");
    public int? DepartmentId => GetId("department_id");

    private int? GetId(string claim)
    {
        var identity = context.HttpContext?.User.Identities.FirstOrDefault(i =>
            i.IsAuthenticated && i.AuthenticationType == EuzAuthenticationHandler.SchemeName);
        return int.TryParse(identity?.FindFirst(claim)?.Value, out var id) && id > 0 ? id : null;
    }
}
