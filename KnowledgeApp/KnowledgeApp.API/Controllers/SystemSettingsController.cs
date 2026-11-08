using KnowledgeApp.API.Contracts.Admin;
using KnowledgeApp.Application.DTOs.Admin;
using KnowledgeApp.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.API.Controllers;

[ApiController]
public sealed class SystemSettingsController : AdminControllerBase
{
    private readonly SystemSettingsService _settingsService;

    public SystemSettingsController(SystemSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet("~/admin/settings")]
    [ProducesResponseType(typeof(SystemSettingsDto), StatusCodes.Status200OK)]
    public Task<IResult> Get(CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await _settingsService.Get(cancellationToken)));

    [HttpPut("~/admin/settings")]
    [ProducesResponseType(typeof(SystemSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IResult> Update(
        [FromBody] UpdateSystemSettingsRequest request,
        CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await _settingsService.Update(
            new UpdateSystemSettingsCommand(
                request.SupportEmail,
                request.SupportPhone,
                request.WelcomeText,
                request.AccessRequestInstructions,
                request.NotificationTemplate),
            CurrentActor(),
            cancellationToken)));

    [HttpGet("~/settings/public")]
    [ProducesResponseType(typeof(PublicSettingsDto), StatusCodes.Status200OK)]
    public Task<IResult> GetPublic(CancellationToken cancellationToken) =>
        Execute(async () => Results.Json(await _settingsService.GetPublic(cancellationToken)));
}
