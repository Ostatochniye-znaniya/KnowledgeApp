using KnowledgeApp.API.Contracts;
using KnowledgeApp.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace KnowledgeApp.API.Controllers;

public sealed class LprReportsController(ReportDocumentService service) : DocumentControllerBase
{
    [HttpGet("~/LprReports/GetReports")]
    public Task<IResult> GetReports([FromQuery] int? facultyId, [FromQuery] int? departmentId,
        [FromQuery] string? academicYear, [FromQuery] string? status, CancellationToken cancellationToken) =>
        Execute(async () =>
        {
            Response.Headers.CacheControl = "no-store";
            return Results.Json(await service.GetForLpr(facultyId, departmentId, academicYear, status, cancellationToken));
        });

    [HttpGet("~/LprReports/GetFilters")]
    public Task<IResult> GetFilters([FromQuery] int? facultyId, CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await service.GetLprFilterOptions(facultyId, cancellationToken)));

    [HttpGet("~/LprReports/Download/{id:int}")]
    public Task<IResult> Download(int id, [FromQuery] string? fileVersion, CancellationToken cancellationToken) =>
        Execute(async () => DocumentFile(await service.OpenFileForLpr(id, fileVersion, cancellationToken)));

    [HttpPost("~/LprReports/Approve/{id:int}")]
    public Task<IResult> Approve(int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReviewLprReportRequest? request,
        CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await service.ApproveForLpr(id, request?.FileVersion, cancellationToken)));

    [HttpPost("~/LprReports/Reject/{id:int}")]
    public Task<IResult> Reject(int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReviewLprReportRequest? request,
        CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await service.RejectForLpr(id, request?.Comment, request?.FileVersion, cancellationToken)));
}
