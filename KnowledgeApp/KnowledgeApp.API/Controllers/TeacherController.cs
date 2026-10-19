using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.API.Controllers;

public sealed class TeacherController : Controller
{
    private readonly KnowledgeTestDbContext _context;
    private readonly ICurrentUserProvider _currentUser;

    public TeacherController(KnowledgeTestDbContext context, ICurrentUserProvider currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Approvals(CancellationToken cancellationToken)
    {
        await SetProfileName(cancellationToken);
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Reports(CancellationToken cancellationToken)
    {
        await SetProfileName(cancellationToken);
        return View();
    }

    private async Task SetProfileName(CancellationToken cancellationToken)
    {
        ViewData["ProfileName"] = _currentUser.UserId is int userId
            ? await _context.Users.AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => user.Name)
                .SingleOrDefaultAsync(cancellationToken) ?? "Преподаватель"
            : "Преподаватель";
    }
}
