using System.Security.Claims;
using KnowledgeApp.Application.DTOs.Admin;
using KnowledgeApp.Application.Interfaces;

namespace KnowledgeApp.API.Controllers;

public abstract class AdminControllerBase : DocumentControllerBase
{
    protected AdminActor CurrentActor()
    {
        var claimUserId = int.TryParse(User.FindFirstValue("user_id"), out var id) ? id : (int?)null;
        var currentUser = HttpContext.RequestServices.GetRequiredService<ICurrentUserProvider>();
        return new AdminActor(
            claimUserId ?? currentUser.UserId,
            User.FindFirstValue(ClaimTypes.Name),
            User.FindFirstValue(ClaimTypes.Role),
            ClientIp());
    }

    private string? ClientIp()
    {
        var forwarded = Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
