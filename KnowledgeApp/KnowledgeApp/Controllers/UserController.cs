using KnowledgeApp.Core.Models;
using KnowledgeApp.Application.Services;
using Microsoft.AspNetCore.Mvc;
using KnowledgeApp.API.Contracts;

namespace KnowledgeApp.API.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAll();
        return Ok(users);
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
