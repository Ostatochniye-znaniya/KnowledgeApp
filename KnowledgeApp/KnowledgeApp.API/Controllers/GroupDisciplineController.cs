using KnowledgeApp.API.Contracts;
using KnowledgeApp.Application.Services;
using KnowledgeApp.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.API.Controllers;

/// <summary>
/// Выбор дисциплин для групп заведующим кафедрой (страница «Выбор дисциплин», ТЗ п. 2.4).
///   GET GroupDiscipline/GetDepartment                  — кафедра текущего пользователя
///   GET GroupDiscipline/GetGroups?semesterId=
///   GET GroupDiscipline/GetAvailableDisciplines/{groupId}
///   PUT GroupDiscipline/SetDisciplines/{groupId}   body: { disciplineIds: [..], semesterId? }
/// Кафедра берётся из текущего пользователя: группы других кафедр не видны (403 на чужую группу).
/// semesterId необязателен: по умолчанию берётся самый поздний семестр.
/// </summary>
public class GroupDisciplineController : BaseController
{
    private readonly GroupDisciplineService _service;

    public GroupDisciplineController(GroupDisciplineService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<DepartmentResponse>> GetDepartment()
    {
        try
        {
            var (id, name) = await _service.GetCurrentDepartmentAsync();
            return Ok(new DepartmentResponse { Id = id, Name = name });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new { message = e.Message });
        }
        catch (UnauthorizedAccessException e)
        {
            return Forbidden(e);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GroupDisciplinesResponse>>> GetGroups([FromQuery] int? semesterId)
    {
        try
        {
            var groups = await _service.GetGroupsAsync(semesterId);
            return Ok(groups.Select(ToGroupResponse));
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new { message = e.Message });
        }
        catch (UnauthorizedAccessException e)
        {
            return Forbidden(e);
        }
    }

    [HttpGet("{groupId}")]
    public async Task<ActionResult<IEnumerable<DisciplineResponse>>> GetAvailableDisciplines(int groupId)
    {
        try
        {
            var disciplines = await _service.GetAvailableDisciplinesAsync(groupId);
            return Ok(disciplines.Select(ToDisciplineResponse));
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new { message = e.Message });
        }
        catch (UnauthorizedAccessException e)
        {
            return Forbidden(e);
        }
    }

    [HttpPut("{groupId}")]
    public async Task<ActionResult> SetDisciplines(int groupId, [FromBody] SetGroupDisciplinesRequest request)
    {
        try
        {
            await _service.SetDisciplinesAsync(groupId, request.DisciplineIds, request.SemesterId);
            return NoContent();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new { message = e.Message });
        }
        catch (UnauthorizedAccessException e)
        {
            return Forbidden(e);
        }
        catch (ArgumentException e)
        {
            return BadRequest(new { message = e.Message });
        }
        catch (InvalidOperationException e)
        {
            return Conflict(new { message = e.Message });
        }
    }

    private ObjectResult Forbidden(UnauthorizedAccessException e)
    {
        return StatusCode(StatusCodes.Status403Forbidden, new { message = e.Message });
    }

    private static GroupDisciplinesResponse ToGroupResponse(GroupDisciplinesModel model)
    {
        return new GroupDisciplinesResponse
        {
            GroupId = model.GroupId,
            GroupNumber = model.GroupNumber,
            StudyProgramId = model.StudyProgramId,
            StudyProgramName = model.StudyProgramName,
            DepartmentId = model.DepartmentId,
            DepartmentName = model.DepartmentName,
            FacultyName = model.FacultyName,
            SemesterId = model.SemesterId,
            MaxDisciplines = Math.Min(GroupDisciplineService.MaxDisciplinesPerGroup, model.SlotCount),
            Disciplines = model.Disciplines.Select(ToDisciplineResponse).ToList()
        };
    }

    private static DisciplineResponse ToDisciplineResponse(DisciplineModel model)
    {
        return new DisciplineResponse
        {
            Id = model.Id,
            Name = model.Name,
            DepartmentId = model.DepartmentId
        };
    }
}
