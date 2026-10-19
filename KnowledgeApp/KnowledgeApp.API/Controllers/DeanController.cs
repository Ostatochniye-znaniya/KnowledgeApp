using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Infrastructure.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.API.Controllers;

public sealed class DeanController : Controller
{
    private readonly KnowledgeTestDbContext _context;
    private readonly ICurrentUserProvider _currentUser;

    public DeanController(KnowledgeTestDbContext context, ICurrentUserProvider currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Reports(CancellationToken cancellationToken)
    {
        ViewData["ProfileName"] = _currentUser.UserId is int userId
            ? await _context.Users.AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => user.Name)
                .SingleOrDefaultAsync(cancellationToken) ?? "Ответственный"
            : "Ответственный";
        return View();
    }
}
