using KnowledgeApp.Application.DTOs.Statistics;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.API.Controllers;

public sealed class StatisticsController : BaseController
{
    private readonly StatisticsService _statisticsService;

    public StatisticsController(StatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(StatisticsFilterOptionsDto), StatusCodes.Status200OK)]
    public async Task<IResult> GetFilters(CancellationToken cancellationToken)
    {
        try
        {
            return Results.Json(await _statisticsService.GetFilters(cancellationToken));
        }
        catch (Exception e)
        {
            return Results.Problem(e.Message);
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(StatisticsSummaryDto), StatusCodes.Status200OK)]
    public async Task<IResult> GetSummary(
        [FromQuery] int? semesterId,
        [FromQuery] int? facultyId,
        [FromQuery] int? departmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Json(await _statisticsService.GetSummary(semesterId, facultyId, departmentId, cancellationToken));
        }
        catch (DocumentValidationException e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
        catch (DocumentNotFoundException e)
        {
            return Results.NotFound(new { error = e.Message });
        }
        catch (Exception e)
        {
            return Results.Problem(e.Message);
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(DepartmentStatisticsDetailsDto), StatusCodes.Status200OK)]
    public async Task<IResult> GetDepartmentDetails(
        [FromQuery] int? semesterId,
        [FromQuery] int? departmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Json(await _statisticsService.GetDepartmentDetails(semesterId, departmentId, cancellationToken));
        }
        catch (DocumentValidationException e)
        {
            return Results.BadRequest(new { error = e.Message });
        }
        catch (DocumentNotFoundException e)
        {
            return Results.NotFound(new { error = e.Message });
        }
        catch (Exception e)
        {
            return Results.Problem(e.Message);
        }
    }
}
