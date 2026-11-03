using KnowledgeApp.API.Contracts.Admin;
using KnowledgeApp.Application.DTOs.Admin;
using KnowledgeApp.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.API.Controllers;

[ApiController]
[Route("admin/users")]
public sealed class AdminUserController : AdminControllerBase
{
    private readonly AdminUserService _userService;

    public AdminUserController(AdminUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminUserPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] int? roleId,
        [FromQuery] int? departmentId,
        [FromQuery] int? facultyId,
        [FromQuery] string[]? status,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50) =>
        Execute(async () => Results.Json(await _userService.GetUsers(
            new AdminUserQuery(search, roleId, departmentId, facultyId, status ?? [], page, pageSize),
            cancellationToken)));

    [HttpGet("filters")]
    [ProducesResponseType(typeof(AdminUserFiltersDto), StatusCodes.Status200OK)]
    public Task<IResult> GetFilters(CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await _userService.GetFilters(cancellationToken)));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> GetUser(int id, CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await _userService.GetUser(id, cancellationToken)));

    [HttpPost]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> CreateUser(
        [FromBody] CreateAdminUserRequest request,
        CancellationToken cancellationToken) =>
        Execute(async () =>
        {
            var user = await _userService.CreateUser(
                new CreateAdminUserCommand(
                    request.Name,
                    request.Email,
                    request.RoleId,
                    request.DepartmentId,
                    request.FacultyId),
                cancellationToken);
            return Results.Json(user, statusCode: StatusCodes.Status201Created);
        });
}
