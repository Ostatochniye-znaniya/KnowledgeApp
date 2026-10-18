using KnowledgeApp.API.Contracts;
using KnowledgeApp.Application.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.API.Controllers;

public class ReportFileController : DocumentControllerBase
{
    private readonly ReportDocumentService _reportDocumentService;
    private readonly GroupReportService _groupReportService;

    public ReportFileController(
        ReportDocumentService reportDocumentService,
        GroupReportService groupReportService)
    {
        _reportDocumentService = reportDocumentService;
        _groupReportService = groupReportService;
    }

    [HttpGet]
    [Produces("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> GenerateGroupReport(
        [FromQuery, Range(1, int.MaxValue)] int groupId,
        [FromQuery, Range(1, int.MaxValue)] int semesterId,
        [FromQuery, Range(1, int.MaxValue)] int disciplineId,
        CancellationToken cancellationToken) =>
        Execute(async () =>
        {
            var docx = await _groupReportService.GenerateReport(groupId, semesterId, disciplineId, cancellationToken);
            return Results.File(docx,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                GroupReportService.GetDocxFileName(groupId, semesterId, disciplineId));
        });

    [HttpGet]
    [Produces("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> DownloadStoredGroupReport(
        [FromQuery, Range(1, int.MaxValue)] int groupId,
        [FromQuery, Range(1, int.MaxValue)] int semesterId,
        [FromQuery, Range(1, int.MaxValue)] int disciplineId,
        CancellationToken cancellationToken) =>
        Execute(async () =>
        {
            var file = await _groupReportService.OpenStoredReport(
                groupId, semesterId, disciplineId, cancellationToken);
            return DocumentFile(file);
        });

    [HttpGet]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> GenerateGroupReportPdf(
        [FromQuery, Range(1, int.MaxValue)] int groupId,
        [FromQuery, Range(1, int.MaxValue)] int semesterId,
        [FromQuery, Range(1, int.MaxValue)] int disciplineId,
        CancellationToken cancellationToken) =>
        Execute(async () =>
        {
            var pdf = await _groupReportService.GeneratePdfPreview(
                groupId, semesterId, disciplineId, cancellationToken);
            return Results.File(pdf, "application/pdf",
                $"Отчёт_о_проверке_остаточных_знаний_группа_{groupId}_дисциплина_{disciplineId}_семестр_{semesterId}.pdf");
        });

    [HttpPost("{reportId}")]
    [RequestSizeLimit(UploadRequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadRequestLimit)]
    public Task<IResult> Upload(int reportId, [FromForm] ReportFileUploadRequest request, CancellationToken cancellationToken) =>
        Execute(async () =>
        {
            if (request.File == null)
                return Results.BadRequest(new { error = "Файл не передан" });

            await using var stream = request.File.OpenReadStream();
            var report = await _reportDocumentService.UploadFile(reportId, stream, request.File.FileName,
                request.File.Length, cancellationToken);
            return Results.Json(report);
        });

    [HttpGet("{reportId}")]
    public Task<IResult> Download(int reportId, CancellationToken cancellationToken) =>
        Execute(async () => DocumentFile(await _reportDocumentService.OpenFile(reportId, cancellationToken)));
}
