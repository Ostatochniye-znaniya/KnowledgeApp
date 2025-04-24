using KnowledgeApp.Application.Services;
using KnowledgeApp.API.Contracts;
using Microsoft.AspNetCore.Mvc;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.API.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class StudyProgramController : ControllerBase
    {
        private readonly StudyProgramService _studyProgramService;

        public StudyProgramController(StudyProgramService studyProgramService)
        {
            _studyProgramService = studyProgramService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudyProgramResponse>>> GetAll()
        {
            var programs = await _studyProgramService.GetAllProgramsAsync();
            var response = programs.Select(p => new StudyProgramResponse
            {
                Id = p.Id,
                Name = p.Name,
                DepartmentId = p.DepartmentId,
                CypherOfTheDirection = p.CypherOfTheDirection,
                StudyGroupIds = p.StudyGroups.Select(g => g.Id).ToList()
            });

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StudyProgramResponse>> GetById(int id)
        {
            var program = await _studyProgramService.GetProgramByIdAsync(id);
            if (program == null)
                return NotFound();

            var response = new StudyProgramResponse
            {
                Id = program.Id,
                Name = program.Name,
                DepartmentId = program.DepartmentId,
                CypherOfTheDirection = program.CypherOfTheDirection,
                StudyGroupIds = program.StudyGroups.Select(g => g.Id).ToList()
            };

            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] StudyProgramRequest request)
        {
            await _studyProgramService.CreateProgramAsync(new StudyProgramModel
            {
                Name = request.Name,
                DepartmentId = request.DepartmentId,
                CypherOfTheDirection = request.CypherOfTheDirection
            });

            return CreatedAtAction(nameof(GetAll), null);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] StudyProgramRequest request)
        {
            var program = await _studyProgramService.GetProgramByIdAsync(id);
            if (program == null)
                return NotFound();

            program.Name = request.Name;
            program.DepartmentId = request.DepartmentId;
            program.CypherOfTheDirection = request.CypherOfTheDirection;

            await _studyProgramService.UpdateProgramAsync(program);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var program = await _studyProgramService.GetProgramByIdAsync(id);
            if (program == null)
                return NotFound();

            await _studyProgramService.DeleteProgramAsync(id);
            return NoContent();
        }
    }
}
