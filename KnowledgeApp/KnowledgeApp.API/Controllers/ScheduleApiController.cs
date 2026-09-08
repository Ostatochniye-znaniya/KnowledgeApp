using Microsoft.AspNetCore.Mvc;
using KnowledgeApp.Application.Services;
using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Application.DTOs;

namespace KnowledgeApp.API.Controllers
{
    public class ScheduleApiController : BaseController
    {
        private readonly TestingService _testingService;
        private readonly IPdfGeneratorService _pdfGeneratorService;

        public ScheduleApiController(TestingService testingService, IPdfGeneratorService pdfGeneratorService)
        {
            _testingService = testingService;
            _pdfGeneratorService = pdfGeneratorService;
        }

        /// <summary>
        /// Получить расписание тестирований с фильтрацией по факультету и семестру
        /// </summary>
        /// <param name="facultyId">ID факультета (0 - все факультеты)</param>
        /// <param name="semesterId">ID семестра (0 - все семестры)</param>
        [HttpGet]
        public async Task<IActionResult> GetSchedule([FromQuery] int facultyId = 0, [FromQuery] int semesterId = 0)
        {
            try
            {
                var allData = await _testingService.GetTestingScheduleWithFacultyAsync(semesterId > 0 ? semesterId : null);

                var filteredData = facultyId > 0
                    ? allData.Where(x => x.FacultyId == facultyId).ToList()
                    : allData;

                return Ok(filteredData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Получить список всех факультетов
        /// </summary>
        [HttpGet("faculties")]
        public async Task<IActionResult> GetFaculties()
        {
            try
            {
                var faculties = await _testingService.GetFacultiesAsync();
                return Ok(faculties);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Получить список всех семестров
        /// </summary>
        [HttpGet("semesters")]
        public async Task<IActionResult> GetSemesters()
        {
            try
            {
                var semesters = await _testingService.GetSemestersAsync();
                return Ok(semesters);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Получить текущий семестр
        /// </summary>
        
        [HttpGet("pdf")]
        public async Task<IActionResult> DownloadPdf([FromQuery] int facultyId = 0, [FromQuery] int semesterId = 0)
        {
            try
            {
                var allData = await _testingService.GetTestingScheduleWithFacultyAsync(semesterId > 0 ? semesterId : null);

                var filteredData = facultyId > 0
                    ? allData.Where(x => x.FacultyId == facultyId).ToList()
                    : allData;

                if (filteredData == null || !filteredData.Any())
                {
                    return BadRequest(new { error = "Нет данных для формирования отчета" });
                }

                var facultyName = facultyId > 0
                    ? (await _testingService.GetFacultiesAsync()).First(f => f.Id == facultyId).Name
                    : "Все факультеты";

                string semesterPeriod = "Все семестры";
                if (semesterId > 0)
                {
                    var semester = (await _testingService.GetSemestersAsync()).FirstOrDefault(s => s.Id == semesterId);
                    if (semester != null)
                    {
                        semesterPeriod = semester.GetPeriod();
                    }
                }

                var scheduleDto = filteredData.Select(x => new TestingScheduleDto
                {
                    Number = x.Number,
                    GroupName = x.GroupName,
                    ProgramName = x.ProgramName,
                    DisciplineName = x.DisciplineName,
                    DepartmentName = x.DepartmentName,
                    TeacherName = x.TeacherName,
                    Date = x.Date,
                    Time = x.Time
                }).ToList();

                var pdfBytes = _pdfGeneratorService.GenerateTestingSchedulePdf(scheduleDto, facultyName, facultyId, semesterPeriod, semesterId);

                var filename = $"Расписание_тестирований_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                return File(pdfBytes, "application/pdf", filename);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}