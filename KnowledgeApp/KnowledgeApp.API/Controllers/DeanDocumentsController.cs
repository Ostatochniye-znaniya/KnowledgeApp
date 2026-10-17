using KnowledgeApp.API.Contracts;
using KnowledgeApp.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KnowledgeApp.API.Controllers;

public class DeanDocumentsController : DocumentControllerBase
{
    private readonly ReportDocumentService _reportDocumentService;
    private readonly ScheduleDocumentService _scheduleDocumentService;

    public DeanDocumentsController(ReportDocumentService reportDocumentService, ScheduleDocumentService scheduleDocumentService)
    {
        _reportDocumentService = reportDocumentService;
        _scheduleDocumentService = scheduleDocumentService;
    }

    [HttpGet]
    public Task<IResult> GetReports([FromQuery] int? departmentId, [FromQuery] string? academicYear, [FromQuery] string? status) =>
        Execute(async () => Results.Json(await _reportDocumentService.GetForDean(departmentId, academicYear, status)));

    [HttpGet]
    public Task<IResult> GetReportFilters() =>
        Execute(async () => Results.Json(await _reportDocumentService.GetFilterOptions()));

    [HttpGet("{id}")]
    public Task<IResult> DownloadReport(int id, CancellationToken cancellationToken) =>
        Execute(async () => PdfFile(await _reportDocumentService.OpenFileForDean(id, cancellationToken)));

    [HttpPost("{id}")]
    public Task<IResult> ApproveReport(int id) =>
        Execute(async () => Results.Json(await _reportDocumentService.Approve(id)));

    [HttpPost("{id}")]
    public Task<IResult> RejectReport(int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RejectDocumentRequest? request) =>
        Execute(async () => Results.Json(await _reportDocumentService.Reject(id, request?.Comment)));

    [HttpGet]
    public Task<IResult> GetSchedules([FromQuery] int? semesterId, [FromQuery] string? status) =>
        Execute(async () => Results.Json(await _scheduleDocumentService.GetForDean(semesterId, status)));

    [HttpGet]
    public Task<IResult> GetScheduleFilters() =>
        Execute(async () => Results.Json(await _scheduleDocumentService.GetFilterOptions()));

    [HttpGet("{id}")]
    public Task<IResult> DownloadSchedule(int id, CancellationToken cancellationToken) =>
        Execute(async () => PdfFile(await _scheduleDocumentService.OpenFile(id, cancellationToken)));

    [HttpPost("{id}")]
    public Task<IResult> ApproveSchedule(int id) =>
        Execute(async () => Results.Json(await _scheduleDocumentService.Approve(id)));

    [HttpPost("{id}")]
    public Task<IResult> RejectSchedule(int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RejectDocumentRequest? request) =>
        Execute(async () => Results.Json(await _scheduleDocumentService.Reject(id, request?.Comment)));
}
