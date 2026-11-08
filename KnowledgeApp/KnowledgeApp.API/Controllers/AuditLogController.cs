using KnowledgeApp.Application.DTOs.Admin;
using KnowledgeApp.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.API.Controllers;

[ApiController]
[Route("admin/audit-logs")]
public sealed class AuditLogController : AdminControllerBase
{
    private readonly AuditLogService _auditLogService;

    public AuditLogController(AuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AuditLogPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IResult> GetLogs(
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        [FromQuery] int? userId,
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] string? entityId,
        [FromQuery] string? search,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50) =>
        Execute(async () => Results.Json(await _auditLogService.GetLogs(
            new AuditLogQuery(dateFrom, dateTo, userId, action, entityType, entityId, search, page, pageSize),
            cancellationToken)));

    [HttpGet("filters")]
    [ProducesResponseType(typeof(AuditLogFiltersDto), StatusCodes.Status200OK)]
    public Task<IResult> GetFilters(CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await _auditLogService.GetFilters(cancellationToken)));
}
