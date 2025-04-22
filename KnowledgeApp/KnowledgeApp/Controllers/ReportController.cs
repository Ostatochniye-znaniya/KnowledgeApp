using Microsoft.AspNetCore.Mvc;
using KnowledgeApp.Application.Services;
using KnowledgeApp.API.Contracts;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.API.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ReportController : ControllerBase
    {
        private readonly ReportService _reportService;
        public ReportController(ReportService reportService)
        {
            _reportService = reportService;
        }
        
        [HttpGet]
        public async Task<IResult> GetReports()
        {
            try
            {
                List<ReportModel> reports = await _reportService.GetAll();
                return Results.Json(reports);

            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpGet]
        public async Task<IResult> GetReportById(int reportId)
        {
            try
            {
                ReportModel report = await _reportService.GetReportById(reportId);
                return Results.Json(report);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPost]
        public async Task<IResult> CreateReport(ReportRequest reportRequest)
        {
            try
            {
                var newReportModel = new ReportModel(
                        reportRequest.DisciplineId,
                        reportRequest.TeacherId,
                        reportRequest.FilePath,
                        reportRequest.IsCorrect,
                        reportRequest.ResultOfAttestation,
                        reportRequest.DoneInPaperForm,
                        reportRequest.DoneInElectronicForm,
                        reportRequest.AllDone);

                ReportModel newReportId = await _reportService.CreateReport(newReportModel);
                return Results.Json(newReportId);

            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPut]
        public async Task<IResult> UpdateReport(int reportId, ReportRequest reportRequest)
        {
            try
            {
                var updatedReportModel = new ReportModel(
                        reportRequest.DisciplineId,
                        reportRequest.TeacherId,
                        reportRequest.FilePath,
                        reportRequest.IsCorrect,
                        reportRequest.ResultOfAttestation,
                        reportRequest.DoneInPaperForm,
                        reportRequest.DoneInElectronicForm,
                        reportRequest.AllDone);

                var updatedReport = await _reportService.UpdateReport(updatedReportModel);
                return Results.Json(updatedReport);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpDelete]
        public async Task<IResult> DeleteReport(int reportId)
        {
            try
            {
                var result = await _reportService.DeleteReport(reportId);
                return Results.Json(result);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }
    }
}

