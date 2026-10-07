using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Application.Services;
using Microsoft.AspNetCore.Mvc;
using KnowledgeApp.API.Contracts;

namespace KnowledgeApp.API.Controllers;

public class UserController : BaseController
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    //TestAuth
    [HttpGet("current")]
    public IActionResult GetCurrentUserDataTest()
    {
        return Ok(new
        {
            UserName = "TestUser",
            Role = "Admin",
            DepartmentId = 12,
            Email = "test@example.com"
        });
    }
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAll();
        return Ok(users);
    }

    [HttpGet]
    public async Task<IActionResult> GetUserAccessList()
    {
        var users = await _userService.GetAll();
        var result = users.Select(u => new
        {
            id = u.Id,
            name = u.Name,
            email = u.Email,
            facultyId = u.FacultyId,
            facultyName = u.Faculty != null ? u.Faculty.FacultyName : null,
            roles = u.UserRoles
                .Where(ur => ur.Role != null)
                .Select(ur => new
                {
                    roleId = ur.RoleId,
                    roleName = ur.Role!.RoleName
                })
                .ToList()
        }).ToList();

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> SetUserRoles([FromBody] SetUserRolesRequest request)
    {
        if (request == null || request.UserId <= 0)
        {
            return BadRequest("Некорректный запрос");
        }

        var success = await _userService.SetUserRolesAsync(request.UserId, request.RoleIds ?? new List<int>());
        return success
            ? Ok(new { success = true, userId = request.UserId, roleIds = request.RoleIds })
            : NotFound("Пользователь не найден");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult> CreateUser([FromBody] UserRequest userRequest)
    {
        await _userService.CreateAsync(new UserModel
            {
                Name = userRequest.Name,
                Email = userRequest.Email,
                Password = userRequest.Password,
                StatusId = userRequest.StatusId,
                FacultyId = userRequest.FacultyId
            });

        return CreatedAtAction(nameof(GetAllUsers), null);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserModel model)
    {
        var success = await _userService.UpdateAsync(id, model);
        return success ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var success = await _userService.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }
}
