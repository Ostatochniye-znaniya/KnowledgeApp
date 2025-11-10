using Microsoft.AspNetCore.Mvc;
using KnowledgeApp.Application.Services;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.API.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class EmployeeRightsRequestController : ControllerBase
    {
        private readonly EmployeeRightsRequestService _employeeRightsRequestService;

        public EmployeeRightsRequestController(EmployeeRightsRequestService employeeRightsRequestService)
        {
            _employeeRightsRequestService = employeeRightsRequestService;
        }

        [HttpGet]
        public async Task<IResult> GetAllEmployeeRightsRequests(int page = 1, int pageSize = 50)
        {
            try
            {
                List<EmployeeRightsRequestModel> requests = await _employeeRightsRequestService.GetAllEmployeeRightsRequests(page, pageSize);
                return Results.Json(requests);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpGet]
        public async Task<IResult> GetEmployeeRightsRequestById(int id)
        {
            try
            {
                EmployeeRightsRequestModel request = await _employeeRightsRequestService.GetEmployeeRightsRequestById(id);
                return Results.Json(request);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpGet]
        public async Task<IResult> GetAllEmployeeRightsRequestsByUserId(int userId, int page = 1, int pageSize = 50)
        {
            try
            {
                List<EmployeeRightsRequestModel> requests = await _employeeRightsRequestService.GetAllEmployeeRightsRequestsByUserId(userId, page, pageSize);
                return Results.Json(requests);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPost]
        public async Task<IResult> CreateEmployeeRightsRequest(EmployeeRightsRequestModel request)
        {
            try
            {
                await _employeeRightsRequestService.CreateEmployeeRightsRequest(request);
                return Results.Ok("Запрос прав сотрудника успешно создан");
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPut]
        public async Task<IResult> UpdateEmployeeRightsRequest(EmployeeRightsRequestModel request)
        {
            try
            {
                await _employeeRightsRequestService.UpdateEmployeeRightsRequest(request);
                return Results.Ok("Запрос прав сотрудника успешно обновлен");
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpDelete]
        public async Task<IResult> DeleteEmployeeRightsRequest(int id)
        {
            try
            {
                await _employeeRightsRequestService.DeleteEmployeeRightsRequest(id);
                return Results.Ok("Запрос прав сотрудника успешно удален");
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpGet]
        public async Task<IResult> GetActiveEmployeeRightsRequests(int page = 1, int pageSize = 50)
        {
            try
            {
                List<EmployeeRightsRequestModel> activeRequests = await _employeeRightsRequestService.GetActiveEmployeeRightsRequests(page, pageSize);
                return Results.Json(activeRequests);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpGet]
        public async Task<IResult> GetActiveEmployeeRightsRequestsByUser(int userId, int page = 1, int pageSize = 50)
        {
            try
            {
                List<EmployeeRightsRequestModel> activeRequests = await _employeeRightsRequestService.GetActiveEmployeeRightsRequestsByUser(userId, page, pageSize);
                return Results.Json(activeRequests);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpGet]
        public async Task<IResult> GetEmployeeRightsRequestsByDivision(string structuralDivision, int page = 1, int pageSize = 50)
        {
            try
            {
                List<EmployeeRightsRequestModel> divisionRequests = await _employeeRightsRequestService.GetEmployeeRightsRequestsByDivision(structuralDivision, page, pageSize);
                return Results.Json(divisionRequests);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpGet]
        public async Task<IResult> GetEmployeeRightsRequestsByDivisionAndUserId(string structuralDivision, int userId, int page = 1, int pageSize = 50)
        {
            try
            {
                List<EmployeeRightsRequestModel> divisionRequests = await _employeeRightsRequestService.GetEmployeeRightsRequestsByDivisionAndUserId(structuralDivision, userId, page, pageSize);
                return Results.Json(divisionRequests);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }
    }
}