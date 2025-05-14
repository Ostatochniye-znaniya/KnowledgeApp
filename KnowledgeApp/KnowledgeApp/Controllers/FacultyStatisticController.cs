using Microsoft.AspNetCore.Mvc;
using KnowledgeApp.Application.Services;
using KnowledgeApp.API.Contracts;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.API.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class FacultyStatisticController : ControllerBase
    {
        private readonly FacultyStatisticService _facultystatisticService;
        public FacultyStatisticController(FacultyStatisticService facultystatisticService)
        {
            _facultystatisticService = facultystatisticService;
        }

        [HttpGet]
        public async Task<IResult> GetFacultyStatistics()
        {
            try
            {
                List<FacultyStatisticModel> facultystatistics = await _facultystatisticService.GetAll();
                return Results.Json(facultystatistics);

            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpGet]
        public async Task<IResult> GetFacultyStatisticById(int facultystatisticId)
        {
            try
            {
                FacultyStatisticModel facultystatistic = await _facultystatisticService.GetFacultyStatisticById(facultystatisticId);
                return Results.Json(facultystatistic);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }
    }
}
