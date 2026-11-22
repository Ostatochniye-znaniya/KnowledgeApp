using KnowledgeApp.Application.DTOs.GroupsTesting;
using KnowledgeApp.Application.Services;
using KnowledgeApp.Domain.Planning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.API.Controllers;

public sealed class HodGroupsController(GroupsTestingService service) : DocumentControllerBase
{
    [HttpGet("~/HodGroups/GetGroups")]
    public Task<IResult> GetGroups(CancellationToken cancellationToken) =>
        Run(() => service.GetForHod(cancellationToken));

    [HttpPatch("~/HodGroups/UpdateGroups")]
    public Task<IResult> UpdateGroups([FromBody] UpdateHodGroupsRequest? request, CancellationToken cancellationToken) =>
        Run(() => service.UpdateForHod(request, cancellationToken));

    private Task<IResult> Run(Func<Task<HodGroupsResponse>> action) => Execute(async () =>
    {
        Response.Headers.CacheControl = "no-store";
        try { return Results.Json(await action()); }
        catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
        catch (PlanningRuleException e) { return Results.Conflict(new { error = e.Message }); }
        catch (DbUpdateConcurrencyException) { return Results.Conflict(new { error = "План изменился. Обновите таблицу." }); }
    });
}
