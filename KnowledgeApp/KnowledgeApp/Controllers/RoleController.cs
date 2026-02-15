using Microsoft.AspNetCore.Mvc;
using KnowledgeApp.Application.Services;
using KnowledgeApp.API.Contracts;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.API.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class RoleController : ControllerBase
    {
        private readonly RoleService _roleService;
        public RoleController(RoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<IResult> GetRoles()
        {
            try
            {
                List<RoleModel> roles = await _roleService.GetAll();
                return Results.Json(roles);

            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IResult> GetRoleById(int id)
        {
            try
            {
                RoleModel role = await _roleService.GetRoleById(id);
                return Results.Json(role);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPost]
        public async Task<IResult> CreateRole(RoleRequest roleRequest)
        {
            try
            {
                var newRoleModel = new RoleModel(roleRequest.RoleName);
                RoleModel newRoleId = await _roleService.CreateRole(newRoleModel);
                return Results.Json(newRoleId);

            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPut]
        public async Task<IResult> UpdateRole(int roleId, RoleRequest roleRequest)
        {
            try
            {
                var updatedRoleModel = new RoleModel(roleId, roleRequest.RoleName);
                var updatedRole = await _roleService.UpdateRole(updatedRoleModel);
                return Results.Json(updatedRole);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpDelete]
        public async Task<IResult> DeleteRole(int roleId)
        {
            try
            {
                var result = await _roleService.DeleteRole(roleId);
                return Results.Json(result);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }
    }
}