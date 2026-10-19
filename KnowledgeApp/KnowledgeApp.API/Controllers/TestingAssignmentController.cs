using KnowledgeApp.API.Contracts;
using KnowledgeApp.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.API.Controllers;

[ApiController]
public sealed class TestingAssignmentController : DocumentControllerBase
{
    private readonly TestingAssignmentService _assignmentService;

    public TestingAssignmentController(TestingAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    /// <summary>Возвращает период проверки и статусы назначенных преподавателей.</summary>
    [HttpGet("~/Testing/{testingId:int}/teacher-assignments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> GetAssignments(int testingId, CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(
            await _assignmentService.GetAssignments(testingId, cancellationToken)));

    /// <summary>Возвращает преподавателей, привязанных к дисциплине тестирования.</summary>
    [HttpGet("~/Testing/{testingId:int}/available-teachers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> GetAvailableTeachers(int testingId, CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(
            await _assignmentService.GetAvailableTeachers(testingId, cancellationToken)));

    /// <summary>Возвращает сетку пар и время начала и окончания каждой пары.</summary>
    [HttpGet("~/Testing/pair-slots")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IResult GetPairSlots() => Results.Json(_assignmentService.GetPairSlots());

    /// <summary>Задаёт период проверки и заменяет список назначенных преподавателей.</summary>
    [HttpPut("~/Testing/{testingId:int}/teacher-assignments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> UpdateAssignments(
        int testingId,
        [FromBody] UpdateTestingAssignmentsRequest request,
        CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await _assignmentService.UpdateAssignments(
            testingId,
            request.PeriodStart,
            request.PeriodEnd,
            request.TeacherIds,
            cancellationToken)));

    /// <summary>Возвращает уведомления по проверкам кафедры.</summary>
    [HttpGet("~/Testing/department-notifications")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IResult> GetDepartmentNotifications(
        [FromQuery] bool unreadOnly,
        CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(
            await _assignmentService.GetDepartmentNotifications(unreadOnly, cancellationToken)));

    /// <summary>Отмечает уведомление кафедры как прочитанное.</summary>
    [HttpPut("~/Testing/department-notifications/{notificationId:int}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> MarkDepartmentNotificationRead(
        int notificationId,
        CancellationToken cancellationToken) =>
        Execute(async () =>
        {
            await _assignmentService.MarkDepartmentNotificationRead(notificationId, cancellationToken);
            return Results.NoContent();
        });
}
