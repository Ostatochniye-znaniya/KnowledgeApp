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
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> DownloadGroupTemplate(
        [FromQuery, Range(1, int.MaxValue)] int groupId,
        [FromQuery, Range(1, int.MaxValue)] int semesterId,
        CancellationToken cancellationToken) =>
        Execute(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pdf = await _groupReportService.GenerateTemplate(groupId, semesterId, cancellationToken);
            return Results.File(pdf, "application/pdf", $"Приказ_с_отчётом_группа_{groupId}_семестр_{semesterId}.pdf");
        });

    [HttpGet]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> DownloadStoredGroupTemplate(
        [FromQuery, Range(1, int.MaxValue)] int groupId,
        [FromQuery, Range(1, int.MaxValue)] int semesterId,
        CancellationToken cancellationToken) =>
        Execute(async () =>
        {
            var file = await _groupReportService.OpenStoredTemplate(groupId, semesterId, cancellationToken);
            return PdfFile(file);
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
        Execute(async () => PdfFile(await _reportDocumentService.OpenFile(reportId, cancellationToken)));
}
