using Microsoft.AspNetCore.Mvc;
using KnowledgeApp.Application.Services;
using KnowledgeApp.API.Contracts;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.API.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class DepartmentController : ControllerBase
    {
        private readonly DepartmentService _departmentService;
        public DepartmentController(DepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        [HttpGet]
        public async Task<IResult> GetDepartments()
        {
            try
            {
                List<DepartmentModel> departments = await _departmentService.GetAll();
                return Results.Json(departments);

            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpGet]
        public async Task<IResult> GetDepatmentById(int departmentId)
        {
            try
            {
                DepartmentModel department = await _departmentService.GetDepartmentById(departmentId);
                return Results.Json(department);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPost]
        public async Task<IResult> CreateDepartment(DepartmentRequest departmentRequest)
        {
            try
            {
                var newDepartmentModel = new DepartmentModel(departmentRequest.Name, departmentRequest.FacultyId);
                DepartmentModel newDepartmentId = await _departmentService.CreateDepartment(newDepartmentModel);
                return Results.Json(newDepartmentId);

            } catch(Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPut]
        public async Task<IResult> UpdateDepartment(int departmentId, DepartmentRequest departmentRequest)
        {
            try
            {
                var updatedDepartmentModel = new DepartmentModel(departmentId, departmentRequest.Name, departmentRequest.FacultyId);
                var updatedDepartment = await _departmentService.UpdateDepartment(updatedDepartmentModel);
                return Results.Json(updatedDepartment);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpDelete]
        public async Task<IResult> DeleteDepartment(int departmentId)
        {
            try
            {
                var result = await _departmentService.DeleteDepartment(departmentId);
                return Results.Json(result);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }
    }
}
