using Microsoft.AspNetCore.Mvc;
using KnowledgeApp.Application.Services;
using KnowledgeApp.API.Contracts;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.API.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class DisciplineController : ControllerBase
    {
        private readonly DisciplineService _disciplineService;

        public DisciplineController(DisciplineService disciplineService)
        {
            _disciplineService = disciplineService;
        }

        [HttpGet]
        public async Task<IResult> GetDisciplines()
        {
            try
            {
                var disciplines = await _disciplineService.GetAll();
                return Results.Json(disciplines);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpGet]
        public async Task<IResult> GetDisciplineById(int disciplineId)
        {
            try
            {
                var discipline = await _disciplineService.GetDisciplineById(disciplineId);
                return Results.Json(discipline);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPost]
        public async Task<IResult> CreateDiscipline(DisciplineRequest disciplineRequest)
        {
            try
            {
                var newDiscipline = new DisciplineModel(
                    disciplineRequest.Name,
                    disciplineRequest.DepartmentId);

                var createdDiscipline = await _disciplineService.CreateDiscipline(newDiscipline);
                return Results.Json(createdDiscipline);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPut]
        public async Task<IResult> UpdateDiscipline(int disciplineId, DisciplineRequest disciplineRequest)
        {
            try
            {
                var updatedDiscipline = new DisciplineModel(
                    disciplineId,
                    disciplineRequest.Name,
                    disciplineRequest.DepartmentId);

                var discipline = await _disciplineService.UpdateDiscipline(updatedDiscipline);
                return Results.Json(discipline);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPut]
        public async Task<IResult> DeleteDiscipline(int disciplineId)
        {
            try
            {
                var result = await _disciplineService.DeleteDiscipline(disciplineId);
                return Results.Json(result);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }
    }
}
