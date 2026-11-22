using KnowledgeApp.Application.DTOs.GroupsTesting;
using KnowledgeApp.Application.Services;
using KnowledgeApp.Domain.Planning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.API.Controllers;

public sealed class GroupsTestingController(GroupsTestingService service, ILogger<GroupsTestingController> logger)
    : BaseController
{
    [HttpPost("/autoFillPlannedTestings")]
    public Task<IResult> AutoFillPlannedTestings(CancellationToken ct) =>
        Execute(async () => Results.Ok(await service.AutoFill(ct)));

    [HttpGet("/getGroupsTesting")]
    public Task<IResult> GetGroupsTesting([FromQuery] int? facultyId,
        [FromQuery] string? cypherOfTheDirection, CancellationToken ct) => Execute(async () =>
    {
        var result = await service.Get(facultyId, cypherOfTheDirection, ct);
        return result is null ? Results.NoContent() : Results.Ok(result);
    });

    [HttpPatch("/updateGroupsTesting")]
    public Task<IResult> UpdateGroupsTesting([FromBody] UpdateGroupsTestingRequest? request, CancellationToken ct) =>
        Execute(async () => Results.Ok(await service.Update(request, ct)));

    private async Task<IResult> Execute(Func<Task<IResult>> action)
    {
        Response.Headers.CacheControl = "no-store";
        try { return await action(); }
        catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
        catch (PlanningRuleException e) { return Results.Conflict(new { error = e.Message }); }
        catch (DbUpdateConcurrencyException) { return Results.Conflict(new { error = "План изменён другим пользователем. Обновите список." }); }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested) { throw; }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка планирования тестирований {Path}", Request.Path);
            return Results.Problem("Не удалось обработать план тестирований. Проверьте миграции и журнал сервера.");
        }
    }
}
