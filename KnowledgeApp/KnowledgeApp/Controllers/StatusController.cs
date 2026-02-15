using Microsoft.AspNetCore.Mvc;
using KnowledgeApp.Application.Services;
using KnowledgeApp.API.Contracts;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.API.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class StatusController : ControllerBase
    {
        private readonly StatusService _statusService;  
        public StatusController(StatusService statusService)
        {
            _statusService = statusService;
        }

        [HttpGet]
        public async Task<IResult> GetStatuses()
        {
            try
            {
                List<StatusModel> statuses = await _statusService.GetAll();
                return Results.Json(statuses);

            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IResult> GetStatusById(int id)
        {
            try
            {
                StatusModel status = await _statusService.GetStatusById(id);
                return Results.Json(status);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPost]
        public async Task<IResult> CreateStatus(StatusRequest statusRequest)
        {
            try
            {
                var newStatusModel = new StatusModel(statusRequest.StatusName);
                StatusModel newStatusId = await _statusService.CreateStatus(newStatusModel);
                return Results.Json(newStatusId);

            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPut]
        public async Task<IResult> UpdateStatus(int statusId, StatusRequest statusRequest)
        {
            try
            {
                var updatedStatusModel = new StatusModel(statusId, statusRequest.StatusName);
                var updatedStatus = await _statusService.UpdateStatus(updatedStatusModel);
                return Results.Json(updatedStatus);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpDelete]
        public async Task<IResult> DeleteStatus(int statusId)
        {
            try
            {
                var result = await _statusService.DeleteStatus(statusId);
                return Results.Json(result);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }
    }
}