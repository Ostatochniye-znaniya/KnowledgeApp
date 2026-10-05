using KnowledgeApp.API.Contracts;
using KnowledgeApp.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.API.Controllers;

public class ScheduleDocumentController : DocumentControllerBase
{
    private readonly ScheduleDocumentService _scheduleDocumentService;

    public ScheduleDocumentController(ScheduleDocumentService scheduleDocumentService)
    {
        _scheduleDocumentService = scheduleDocumentService;
    }

    [HttpPost]
    [RequestSizeLimit(UploadRequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadRequestLimit)]
    public Task<IResult> Upload([FromForm] ScheduleDocumentUploadRequest request, CancellationToken cancellationToken) =>
        Execute(async () =>
        {
            if (request.File == null)
                return Results.BadRequest(new { error = "Файл не передан" });

            await using var stream = request.File.OpenReadStream();
            var document = await _scheduleDocumentService.Upload(request.SemesterId, stream, request.File.FileName,
                request.File.Length, cancellationToken);
            return Results.Json(document);
        });

    [HttpGet]
    public Task<IResult> GetCurrent([FromQuery] int semesterId) =>
        Execute(async () =>
        {
            var document = await _scheduleDocumentService.GetCurrent(semesterId);
            // Results.Json(null) отдаёт пустое тело
            return document == null
                ? Results.Content("null", "application/json")
                : Results.Json(document);
        });

    [HttpGet("{id}")]
    public Task<IResult> Download(int id, CancellationToken cancellationToken) =>
        Execute(async () => PdfFile(await _scheduleDocumentService.OpenFile(id, cancellationToken)));
}
