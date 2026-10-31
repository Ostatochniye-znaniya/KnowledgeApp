using Microsoft.AspNetCore.Mvc;
using KnowledgeApp.Application.Services;
using KnowledgeApp.API.Contracts;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Application.Exceptions;

namespace KnowledgeApp.API.Controllers;

public class TestingController : BaseController
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
    public async Task<IResult> GetTestingById(int id)
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
                testingRequest.SemesterId,
                testingRequest.Room,
                testingRequest.LmsUrl,
                testingRequest.IsInPerson
            );

            TestingModel createdTesting = await _testingService.Create(testingModel);
            return Results.Json(createdTesting);
        }
        catch (DocumentValidationException e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
        catch (DocumentConflictException e)
        {
            return Results.Conflict(new { error = e.Message });
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
            var current = await _testingService.GetById(id);
            var testingModel = new TestingModel(
                id,
                testingRequest.GroupId,
                testingRequest.DisciplineId,
                testingRequest.ScheduledDate,
                testingRequest.ScheduledTime,
                testingRequest.Status ?? "scheduled",
                testingRequest.ResultOfTesting,
                testingRequest.ReportId,
                testingRequest.SemesterId,
                testingRequest.Room,
                testingRequest.LmsUrl,
                testingRequest.IsInPerson ?? current.IsInPerson
            );

            TestingModel updatedTesting = await _testingService.Update(testingModel);
            return Results.Json(updatedTesting);
        }
        catch (DocumentValidationException e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
        catch (DocumentConflictException e)
        {
            return Results.Conflict(new { error = e.Message });
        }
        catch (Exception e)
        {
            return Results.Problem(e.Message);
        }
    }

    [HttpPatch("{id}")]
    public async Task<IResult> UpdateDelivery(
        int id,
        [FromBody]
        TestingDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _testingService.UpdateDelivery(
                id, request.IsInPerson, request.Room, request.LmsUrl, cancellationToken);
            return updated == null
                ? Results.NotFound(new { error = "Тестирование не найдено" })
                : Results.Json(updated);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
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

    [HttpGet]
    public async Task<IResult> GetTestingSchedule()
    {
        try
        {
            var schedule = await _testingService.GetTestingScheduleAsync();
            return Results.Json(schedule);
        }
        catch (Exception e)
        {
            return Results.Problem(e.Message);
        }
    }
}