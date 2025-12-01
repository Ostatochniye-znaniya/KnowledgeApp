using KnowledgeApp.Application.Services;
using KnowledgeApp.API.Contracts;
using Microsoft.AspNetCore.Mvc;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.API.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class StudyGroupController : ControllerBase
    {
        private readonly StudyGroupService _studyGroupService;

        public StudyGroupController(StudyGroupService studyGroupService)
        {
            _studyGroupService = studyGroupService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudyGroupResponse>>> GetAll()
        {
            var groups = await _studyGroupService.GetAllGroupsAsync();
            var response = groups.Select(g => new StudyGroupResponse
            {
                Id = g.Id,
                GroupNumber = g.GroupNumber,
                StudyProgramId = g.StudyProgramId,
                StudentIds = g.Students.Select(s => s.Id).ToList(),
                TestingIds = g.Testings.Select(t => t.Id).ToList()
            });

            return Ok(response);
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudyGroupResponse>>> GetAllChosen([FromQuery] int semId)
        {
            var groups = await _studyGroupService.GetAllChosenGroupsAsync(semId);
            var response = groups.Select(g => new StudyGroupResponse
            {
                Id = g.Id,
                GroupNumber = g.GroupNumber,
                StudyProgramId = g.StudyProgramId,
                StudentIds = g.Students.Select(s => s.Id).ToList(),
                TestingIds = g.Testings.Select(t => t.Id).ToList()
            });

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StudyGroupResponse>> GetById(int id)
        {
            var group = await _studyGroupService.GetGroupByIdAsync(id);
            if (group == null)
                return NotFound();

            var response = new StudyGroupResponse
            {
                Id = group.Id,
                GroupNumber = group.GroupNumber,
                StudyProgramId = group.StudyProgramId,
                StudentIds = group.Students.Select(s => s.Id).ToList(),
                TestingIds = group.Testings.Select(t => t.Id).ToList()
            };

            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] StudyGroupRequest request)
        {
            await _studyGroupService.CreateGroupAsync(new StudyGroupModel
            {
                GroupNumber = request.GroupNumber,
                StudyProgramId = request.StudyProgramId
            });

            return CreatedAtAction(nameof(GetAll), null);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] StudyGroupRequest request)
        {
            var group = await _studyGroupService.GetGroupByIdAsync(id);
            if (group == null)
                return NotFound();

            group.GroupNumber = request.GroupNumber;
            group.StudyProgramId = request.StudyProgramId;

            await _studyGroupService.UpdateGroupAsync(group);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var group = await _studyGroupService.GetGroupByIdAsync(id);
            if (group == null)
                return NotFound();

            await _studyGroupService.DeleteGroupAsync(id);
            return NoContent();
        }
    }
}
