using Microsoft.AspNetCore.Mvc;
using KnowledgeApp.Application.Services;
using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Application.DTOs;

namespace KnowledgeApp.API.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly TestingService _testingService;
        private readonly IPdfGeneratorService _pdfGeneratorService;

        public ScheduleController(TestingService testingService, IPdfGeneratorService pdfGeneratorService)
        {
            _testingService = testingService;
            _pdfGeneratorService = pdfGeneratorService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var schedule = await _testingService.GetTestingScheduleWithFacultyAsync();
                return View(schedule);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return View(new List<TestingScheduleWithFacultyDto>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTestingSchedule()
        {
            try
            {
                var schedule = await _testingService.GetTestingScheduleAsync();
                return Ok(schedule);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTestingScheduleWithFaculty(int? facultyId, int? semesterId)
        {
            try
            {
                var allData = await _testingService.GetTestingScheduleWithFacultyAsync(semesterId);

                var filteredData = facultyId.HasValue && facultyId.Value > 0
                    ? allData.Where(x => x.FacultyId == facultyId.Value).ToList()
                    : allData;

                return Ok(filteredData);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFaculties()
        {
            try
            {
                var faculties = await _testingService.GetFacultiesAsync();
                return Ok(faculties);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSemesters()
        {
            try
            {
                var semesters = await _testingService.GetSemestersAsync();
                return Ok(semesters);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentSemester()
        {
            try
            {
                var currentSemester = await _testingService.GetCurrentSemesterAsync();
                return Ok(currentSemester);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadPdf()
        {
            try
            {
                var schedule = await _testingService.GetTestingScheduleAsync();

                if (schedule == null || !schedule.Any())
                {
                    return BadRequest("Нет данных для формирования отчета");
                }

                var pdfBytes = _pdfGeneratorService.GenerateTestingSchedulePdf(schedule);

                return File(pdfBytes, "application/pdf", $"Расписание_тестирований_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка при генерации PDF: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadPdfByFaculty(int facultyId, int semesterId)
        {
            try
            {
                var allData = await _testingService.GetTestingScheduleWithFacultyAsync(semesterId > 0 ? semesterId : null);

                var filteredData = facultyId > 0
                    ? allData.Where(x => x.FacultyId == facultyId).ToList()
                    : allData;

                if (filteredData == null || !filteredData.Any())
                {
                    return BadRequest("Нет данных для формирования отчета");
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
                return BadRequest($"Ошибка при генерации PDF: {ex.Message}");
            }
        }
    }
}