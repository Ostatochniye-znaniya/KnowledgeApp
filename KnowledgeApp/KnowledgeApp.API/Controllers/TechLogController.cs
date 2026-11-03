using KnowledgeApp.Application.DTOs.Admin;
using KnowledgeApp.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.API.Controllers;

[ApiController]
[Route("admin/tech-logs")]
public sealed class TechLogController : AdminControllerBase
{
    private readonly TechLogService _techLogService;

    public TechLogController(TechLogService techLogService)
    {
        _techLogService = techLogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(TechLogPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IResult> GetLogs(
        [FromQuery] string[]? level,
        [FromQuery] string? service,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        [FromQuery] string? traceId,
        [FromQuery] string? search,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50) =>
        Execute(async () => Results.Json(await _techLogService.GetLogs(
            new TechLogQuery(level ?? [], service, dateFrom, dateTo, traceId, search, page, pageSize),
            cancellationToken)));

    [HttpGet("filters")]
    [ProducesResponseType(typeof(TechLogFiltersDto), StatusCodes.Status200OK)]
    public Task<IResult> GetFilters(CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await _techLogService.GetFilters(cancellationToken)));

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(TechLogDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> GetLog(long id, CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await _techLogService.GetLog(id, cancellationToken)));
}
