using Microsoft.AspNetCore.Mvc;
using KnowledgeApp.Application.Services;
using KnowledgeApp.API.Contracts;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.API.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class TestingController : ControllerBase
    {
        private readonly TestingService _testingService;

        public TestingController(TestingService testingService)
        {
            _testingService = testingService;
        }

        [HttpGet]
        public async Task<IResult> GetAllTestings()
        {
            try
            {
                var testings = await _testingService.GetAll();
                return Results.Json(testings);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IResult> GetTesting(int id)
        {
            try
            {
                TestingModel testing = await _testingService.GetById(id);
                return Results.Json(testing);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPost]
        public async Task<IResult> CreateTesting(TestingRequest testingRequest)
        {
            try
            {
                var testingModel = new TestingModel(
                    0,
                    testingRequest.GroupId,
                    testingRequest.DisciplineId,
                    testingRequest.ScheduledDate,
                    testingRequest.ScheduledTime,
                    testingRequest.Status ?? "scheduled",
                    testingRequest.ResultOfTesting,
                    testingRequest.ReportId,
                    testingRequest.SemesterId
                );

                TestingModel createdTesting = await _testingService.Create(testingModel);
                return Results.Json(createdTesting);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IResult> UpdateTesting(int id, TestingRequest testingRequest)
        {
            try
            {
                var testingModel = new TestingModel(
                    id,
                    testingRequest.GroupId,
                    testingRequest.DisciplineId,
                    testingRequest.ScheduledDate,
                    testingRequest.ScheduledTime,
                    testingRequest.Status ?? "scheduled",
                    testingRequest.ResultOfTesting,
                    testingRequest.ReportId,
                    testingRequest.SemesterId
                );

                TestingModel updatedTesting = await _testingService.Update(testingModel);
                return Results.Json(updatedTesting);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IResult> DeleteTesting(int id)
        {
            try
            {
                bool result = await _testingService.Delete(id);
                return Results.Json(result);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpGet("group/{groupId}")]
        public async Task<IResult> GetTestingsByGroup(int groupId)
        {
            try
            {
                List<TestingModel> testings = await _testingService.GetByGroupId(groupId);
                return Results.Json(testings);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpGet("department/{departmentId}")]
        public async Task<IResult> GetTestingsByDepartment(int departmentId)
        {
            try
            {
                List<TestingModel> testings = await _testingService.GetByDepartmentId(departmentId);
                return Results.Json(testings);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }
    }
}