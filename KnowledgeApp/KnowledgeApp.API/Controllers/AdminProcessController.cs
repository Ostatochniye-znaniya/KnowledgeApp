using KnowledgeApp.Application.DTOs.Admin;
using KnowledgeApp.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.API.Controllers;

[ApiController]
[Route("admin/processes")]
public sealed class AdminProcessController : DocumentControllerBase
{
    private readonly AdminProcessService _processService;

    public AdminProcessController(AdminProcessService processService)
    {
        _processService = processService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminProcessPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IResult> GetProcesses(
        [FromQuery] string? search,
        [FromQuery] string[]? approvalStatus,
        [FromQuery] string[]? stage,
        [FromQuery] string[]? reportStatus,
        [FromQuery] int? semesterId,
        [FromQuery] int? facultyId,
        [FromQuery] int? departmentId,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50) =>
        Execute(async () => Results.Json(await _processService.GetProcesses(
            new AdminProcessQuery(
                search,
                approvalStatus ?? [],
                stage ?? [],
                reportStatus ?? [],
                semesterId,
                facultyId,
                departmentId,
                dateFrom,
                dateTo,
                page,
                pageSize),
            cancellationToken)));

    [HttpGet("filters")]
    [ProducesResponseType(typeof(AdminProcessFiltersDto), StatusCodes.Status200OK)]
    public Task<IResult> GetFilters(CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await _processService.GetFilters(cancellationToken)));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AdminProcessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> GetProcess(int id, CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await _processService.GetProcess(id, cancellationToken)));
}
