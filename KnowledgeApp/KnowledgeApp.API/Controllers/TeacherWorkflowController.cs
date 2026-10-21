using KnowledgeApp.API.Contracts;
using KnowledgeApp.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace KnowledgeApp.API.Controllers;

public sealed class TeacherWorkflowController : DocumentControllerBase
{
    private readonly TeacherWorkflowService _teacherWorkflowService;

    public TeacherWorkflowController(TeacherWorkflowService teacherWorkflowService)
    {
        _teacherWorkflowService = teacherWorkflowService;
    }

    /// <summary>Возвращает тестирования, назначенные текущему преподавателю.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IResult> GetTestings(CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await _teacherWorkflowService.GetTestings(cancellationToken)));

    /// <summary>Сохраняет формат проведения, аудиторию и ссылку LMS тестирования.</summary>
    [HttpPut("{id:int}/delivery")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> UpdateTestingDelivery(
        int id,
        [FromBody] UpdateTestingDeliveryRequest request,
        CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await _teacherWorkflowService.UpdateTestingDelivery(
            id, request.IsInPerson, request.Room, request.LmsUrl, cancellationToken)));

    /// <summary>Подтверждает назначенное тестирование.</summary>
    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> ApproveTesting(int id, CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(
            await _teacherWorkflowService.ApproveTesting(id, cancellationToken)));

    /// <summary>Отклоняет назначенное тестирование с обязательным комментарием.</summary>
    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> RejectTesting(
        int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RejectDocumentRequest? request,
        CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(
            await _teacherWorkflowService.RejectTesting(id, request?.Comment, cancellationToken)));

    /// <summary>Выбирает дату и время тестирования в пределах периода, заданного заведующим кафедрой.</summary>
    [HttpPut("{id:int}/choose-time")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> ChooseTestingTime(
        int id,
        [FromBody] ChooseTestingTimeRequest request,
        CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await _teacherWorkflowService.ChooseTestingTime(
            id, request.Date, request.PairNumber, cancellationToken)));

    /// <summary>Возвращает уведомления текущего преподавателя.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IResult> GetNotifications(
        [FromQuery] bool unreadOnly,
        CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(
            await _teacherWorkflowService.GetNotifications(unreadOnly, cancellationToken)));

    /// <summary>Отмечает уведомление текущего преподавателя как прочитанное.</summary>
    [HttpPut("{id:int}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> MarkNotificationRead(int id, CancellationToken cancellationToken) =>
        Execute(async () =>
        {
            await _teacherWorkflowService.MarkNotificationRead(id, cancellationToken);
            return Results.NoContent();
        });

    /// <summary>Возвращает отчёты, назначенные текущему преподавателю.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IResult> GetReports(CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await _teacherWorkflowService.GetReports(cancellationToken)));

    /// <summary>Формирует DOCX-отчёт для назначенной преподавателю группы и дисциплины.</summary>
    [HttpGet]
    [Produces("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> GenerateReport(
        [FromQuery, Range(1, int.MaxValue)] int groupId,
        [FromQuery, Range(1, int.MaxValue)] int semesterId,
        [FromQuery, Range(1, int.MaxValue)] int disciplineId,
        CancellationToken cancellationToken) =>
        Execute(async () =>
        {
            var docx = await _teacherWorkflowService.GenerateReport(
                groupId, semesterId, disciplineId, cancellationToken);
            return Results.File(docx,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                GroupReportService.GetDocxFileName(groupId, semesterId, disciplineId));
        });

    /// <summary>Возвращает пустой DOCX-шаблон отчёта без данных группы.</summary>
    [HttpGet]
    [Produces("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IResult> GenerateBlankReportTemplate() =>
        Execute(() =>
        {
            var docx = _teacherWorkflowService.GenerateBlankReportTemplate();
            return Task.FromResult<IResult>(Results.File(
                docx,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "Шаблон_отчёта.docx"));
        });

    /// <summary>Загружает DOCX-отчёт текущего преподавателя на проверку.</summary>
    [HttpPost("{id:int}/upload")]
    [RequestSizeLimit(UploadRequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadRequestLimit)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> UploadReport(
        int id,
        [FromForm] ReportFileUploadRequest request,
        CancellationToken cancellationToken) =>
        Execute(async () =>
        {
            if (request.File == null)
                return Results.BadRequest(new { error = "Файл не передан" });

            await using var stream = request.File.OpenReadStream();
            var report = await _teacherWorkflowService.UploadReport(
                id, stream, request.File.FileName, request.File.Length, cancellationToken);
            return Results.Json(report);
        });
}
