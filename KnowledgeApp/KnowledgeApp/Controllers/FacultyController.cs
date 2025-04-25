using Microsoft.AspNetCore.Mvc;
using KnowledgeApp.Application.Services;
using KnowledgeApp.API.Contracts;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.API.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class FacultyController : ControllerBase
    {
        private readonly FacultyService _facultyService;
        public FacultyController(FacultyService facultyService)
        {
            _facultyService = facultyService;
        }

        [HttpGet]
        public async Task<IResult> GetFaculties()
        {
            try
            {
                List<FacultyModel> faculties = await _facultyService.GetAll();
                return Results.Json(faculties);

            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpGet]
        public async Task<IResult> GetFacultyById(int facultyId)
        {
            try
            {
                FacultyModel faculty = await _facultyService.GetFacultyById(facultyId);
                return Results.Json(faculty);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPost]
        public async Task<IResult> CreateFaculty(FacultyRequest facultyRequest)
        {
            try
            {
                var newFacultyModel = new FacultyModel(facultyRequest.FacultyName);
                FacultyModel newFacultyId = await _facultyService.CreateFaculty(newFacultyModel);
                return Results.Json(newFacultyId);

            } catch(Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPut]
        public async Task<IResult> UpdateFaculty(int facultyId, FacultyRequest facultyRequest)
        {
            try
            {
                var updatedFacultyModel = new FacultyModel(facultyId, facultyRequest.FacultyName);
                var updatedFaculty = await _facultyService.UpdateFaculty(updatedFacultyModel);
                return Results.Json(updatedFaculty);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpDelete]
        public async Task<IResult> DeleteFaculty(int facultyId)
        {
            try
            {
                var result = await _facultyService.DeleteFaculty(facultyId);
                return Results.Json(result);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }
    }
}
