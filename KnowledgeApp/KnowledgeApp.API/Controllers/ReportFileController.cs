using KnowledgeApp.API.Contracts;
using KnowledgeApp.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.API.Controllers;

public class ReportFileController : DocumentControllerBase
{
    private readonly ReportDocumentService _reportDocumentService;

    public ReportFileController(ReportDocumentService reportDocumentService)
    {
        _reportDocumentService = reportDocumentService;
    }

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
