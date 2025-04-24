using Microsoft.AspNetCore.Mvc;
using KnowledgeApp.Application.Services;
using KnowledgeApp.API.Contracts;
using KnowledgeApp.Core.Models;
using System.Net.Http.Headers;

namespace KnowledgeApp.API.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class SemesterController : ControllerBase
    {
        private readonly SemesterService _semesterService;
        public SemesterController(SemesterService semesterService)
        {
            _semesterService = semesterService;
        }

        [HttpGet]
        public async Task<IResult> GetSemesters()
        {
            try
            {
                List<SemesterModel> semesters = await _semesterService.GetAll();
                return Results.Json(semesters);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpGet]
        public async Task<IResult> GetSemesterById(int semesterId)
        {
            try
            {
                SemesterModel semester = await _semesterService.GetSemesterById(semesterId);
                return Results.Json(semester);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPost]
        public async Task<IResult> CreateSemester(SemesterRequest semesterRequest)
        {
            try
            {
                var newSemesterModel = new SemesterModel(semesterRequest.SemesterName);
                SemesterModel newSemesterId = await _semesterService.CreateSemester(newSemesterModel);
                return Results.Json(newSemesterId);
            }
            catch(Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPut]
        public async Task<IResult> UpdateSemester(int semesterId, SemesterRequest semesterRequest)
        {
            try
            {
                var updatedSemesterModel = new SemesterModel(semesterId, semesterRequest.SemesterName);
                var updatedSemester = await _semesterService.UpdateSemester(updatedSemesterModel);
                return Results.Json(updatedSemester);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }


        [HttpDelete]
        public async Task<IResult> DeleteSemester(int semesterId)
        {
            try
            {
                var result = await _semesterService.DeleteSemester(semesterId);
                return Results.Json(result);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }
    }
}